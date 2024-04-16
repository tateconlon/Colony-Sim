using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;

public class Character : IXmlSerializable
{
    public float X => Mathf.Lerp(currTile.X, nextTile.X, movePercentage);
    public float Y => Mathf.Lerp(currTile.Y, nextTile.Y, movePercentage);

    public Vector2 Pos => new Vector3(X, Y);
    
    Tile currTile;
    Tile destTile
    {
        get { return destTile;}
        set
        {
            if (destTile != value)
            {
                destTile = value;
                pathing = null;
            }
        }
    }
    Tile nextTile;
    float movePercentage;
    float moveSpeed;    // tiles/sec

    Job myJob;
    Path_AStar pathing;

    //Item we're carrying, not a backpack or gear
    Inventory inventory;

    public event Action<Character> OnChanged;
    
    public Character(Tile t, float moveSpeed = 5f)
    {
        currTile = destTile = nextTile = t;
        this.moveSpeed = moveSpeed;
    }

    void AbandonJob()
    {
        if (myJob == null) return;
        
        myJob.CancelJob();

        myJob.OnJobCancelled -= OnJobEnded;
        myJob.onJobCompleted -= OnJobEnded;

        myJob = null;
    }


    void Update_DoJob2(float deltaTime)
    {
        if (myJob == null)
        {
            if (TryGetNewJobFromQueue() == false) return;
        }
        
        // We have a job that we can reach!

        //STEP 1: Do we have all the materials?
        if (myJob.HasAllMaterial() == false)
        {
            //We are missing materials
            
            //STEP 2: Are we CARRYING anything that the job location wants?
            //If so, deliver the goods
            // Walk to the job tile, then drop off the stack into the job
            if (inventory != null)
            {
                if (myJob.DesiresInventoryType(inventory))
                {
                    if (currTile == myJob.tile)
                    {
                        //We are at the job site.
                        //Drop off
                        currTile.world.inventoryManager.PlaceInventory(myJob, inventory);
                        //Are we still carrying things?

                        if (inventory.stackSize == 0)
                        {
                            inventory = null;
                        }
                        else
                        {
                            //This is not true, because MY version of the code allows for the inventory to still have a stack size > 0
                            //if you were holding too many supplies.
                            //We should be putting it on the ground (but we are on the job site
                            Debug.LogError("Character is still carrying inventory, which shouldn't be. Just setting to NULL for now. Means we are LEAKING inventories");
                            inventory = null;
                        }
                    }
                    else
                    {   //We still need to walk to the job site
                        destTile = myJob.tile;
                        return;
                    }
                }
                else
                {   //We are carrying inventory the job doesn't want.
                    //Dump the inventory at our feet (or nearest)
                    
                    //Walk to nearest empty tile and dump it
                    if (currTile.world.inventoryManager.PlaceInventory(currTile, inventory))
                    {
                        Debug.LogError("Character tried to place inventory into an invalid tile, maybe there's something here");
                        //FIXME: for the sake of continuing on, we are going to set the inventory to null.
                        //This is not the right behaviour. We are leaking inventory
                        inventory = null;
                    }
                }
            }
            else
            {
                //Job doesn't have all materials, and we aren't carrying anything
                //Walk towards a tile containing the required goods
                //If already on such a tile, pick up the goods.
                
                //FIXME: This is a dumb/simple/unoptimal initial setup
                //Grab the first from the inventory manager
            }
            
            return;  // We can't continue until all materials are satisfied
        }
        
        //Now job has all materials, make sure destination tile == job tile
        destTile = myJob.tile;
        
        //We have all the material?
        //Have we arrived?
        if (currTile == myJob.tile)    
        {
            //We have arrived at the job, so we execute
            //the job's "DoWork", which is mostly going to
            //countdown jobTime and potentially
            //call its "Job Complete" callback
            myJob.DoWork(deltaTime);
            //pathing = null;
        }
    }

    bool TryGetNewJobFromQueue()
    {
        
        //Cycle through all jobs in the queue
        //Dequeue the first job, if you can't set it, then 
        //Re-enqueue it and try the next until you've cycled the full Job Queue.
        int jobQueueCount = currTile.world.jobQueue.Count;
        for (int i = 0; i < jobQueueCount; i++)
        {
            if (currTile.world.jobQueue.TryDequeue(out Job j))
            {
                if (TryAssignJob(j))
                {
                    return true;
                }
                currTile.world.jobQueue.Enqueue(j);
            }
        }

        return false;
    }

    bool TryAssignJob(Job j)
    {
        if (TrySetDestination(j.tile) == false)
        {
            return false; 
        }
        
        myJob = j;
            
        myJob.OnJobCancelled += OnJobEnded;
        myJob.onJobCompleted += OnJobEnded;
        return true;
    }
    
    public void Update(float deltaTime)
    {
        Update_DoJob2(deltaTime);
        
        //Get the next Tile if we need one ie: we're not at the destination tile && there's no next tile or we're at the next tile
        if (currTile != destTile && (nextTile == null || currTile == nextTile) )
        {   //need new tile
            if (pathing == null || pathing.Length() == 0)
            {   //no pathing
                TrySetDestination(destTile);
                if (pathing == null)
                {
                    if(myJob != null) AbandonJob();
                    
                    destTile = currTile;
                    nextTile = currTile;
                }
            }
            else
            {   //pathing existed
                nextTile = pathing.DequeueNextTile();
            }
        }

        if (destTile == currTile)
        {
            return;
        }

        if (nextTile.IsEnterable() == Enterability.Never)
        {
            // Most likely a wall got build, so we just need to reset our pathfinding information/
            // FIXME: Ideally, when a wall gets spawned, we should invalidate our path immediately
            //      so that we don't waste a bunch of time walking towards a dead end.
            //      To save CPU, maybe we can only check every so often?
            //      Maybe we should subscribe to the OnTileChanged event?
            Debug.LogError($"FIXME: we are trying to enter an unwalkable tile");
            nextTile = null;    //Next tile is a no go
            pathing = null; //Our pathfinding is clearly out of date
            return;
        }
        else if(nextTile.IsEnterable() == Enterability.Soon)
        {
            // So the tile we're trying to enter is technically walkable (ie: not a wall),
            // but are we actually allowed to enter right now? (2 characters on same spot? someone sitting in a chair?)
            // We can't enter the tile NOW but we should be able to in the future.
            // This is likely a DOOR
            // So we DON'T bail on our movement/path, but we do return
            // now and don't actually process the movement.
            return;
        }
        else if(nextTile.IsEnterable() == Enterability.Yes)
        { 
        }
        
        float distTotal = Vector2Int.Distance(currTile.Pos, nextTile.Pos);
        float distThisFrame = moveSpeed / nextTile.movementCost * deltaTime;
        float percThisFrame = distThisFrame / distTotal;    //Movement converted to percentage
        movePercentage += percThisFrame;                    //Percentage added to our movePercentage
        
        //We have arrived
        if (movePercentage >= 1)
        {
            currTile = nextTile;
            movePercentage = 0;
            //FIXME: Should we retain overshot movement?
        }

        OnChanged?.Invoke(this);
    }

    public bool TrySetDestination(Tile destTile)
    {
        Path_AStar p = new Path_AStar(currTile.world, currTile, destTile);
        if (p.Length() == 0)
        {
            Debug.LogError($"Character::TrySetDestination: {this} cannot get from {currTile} to {this.destTile}. Setting destination tile to current tile.");
            this.destTile = currTile;
            pathing = null;
            return false;
        }

        this.destTile = destTile;
        pathing = p;
        this.nextTile = pathing.DequeueNextTile();
        
        while (nextTile == currTile)
        {
            Debug.Log("TrySetDestination: nextTile == currTile, advancing to next tile in path.");
            nextTile = pathing.DequeueNextTile();
        }

        return true;
    }
    
    public bool TrySetDestination(Tile destTile, out Path_AStar path)
    {
        path = new Path_AStar(currTile.world, currTile, destTile);
        if (path.Length() == 0)
        {
            Debug.LogError($"Character::TrySetDestination: {this} cannot get from {currTile} to {this.destTile}. Setting destination tile to current tile.");
            // this.destTile = currTile;
            // pathing = null;
            return false;
        }
        
        return true;
    }

    //Job completed or cancelled
    void OnJobEnded(Job _job)
    {
        if (_job != myJob)
        {
            Debug.LogError($"Character being notified about job {{_job}} that isn't his {myJob}. You forgot to unregister something");
            return;
        }
        
        myJob.OnJobCancelled -= OnJobEnded;
        myJob.onJobCompleted -= OnJobEnded;

        myJob = null;
    }

    #region SAVING_AND_LOADING
    
    public XmlSchema GetSchema()
    {
        throw new NotImplementedException();
    }

    public void ReadXml(XmlReader reader)
    {
        //throw new NotImplementedException();
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", currTile.X.ToString());
        writer.WriteAttributeString("Y", currTile.Y.ToString());
        //writer.WriteAttributeString("moveSpeed", moveSpeed.ToString());
        // writer.WriteAttributeString("movementCost", movementCost.ToString());
    }
    
    #endregion
}