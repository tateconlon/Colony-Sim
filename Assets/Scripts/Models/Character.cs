using System;
using System.Collections.Generic;
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

    Tile _destTile;
    Tile destTile
    {
        get { return _destTile;}
        set
        {
            if (_destTile != value)
            {
                _destTile = value;
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
    public Inventory inventory;

    public event Action<Character> OnChanged;
    
    public Character(Tile t, float moveSpeed = 5f)
    {
        currTile = destTile = nextTile = t;
        this.moveSpeed = moveSpeed;
    }

    void Update_DoJob2(float deltaTime)
    {
        //If no job, try and get one.
        //If we can't get one, bail out.
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
                if (myJob.DesiresInventoryType(inventory, out Inventory recipeInventory))
                {   //We are holding the right inventory
                    if (currTile == myJob.tile)
                    {
                        //We are at the job site.
                        //Drop off inventory into the job
                        if (currTile.world.inventoryManager.PlaceInventory(myJob, inventory))
                        {
                            myJob.DoWork(0); //Call OnWorked callback, which things may want to react to that
                        }
                        
                        
                        //Are we still carrying something
                        if (inventory.stackSize == 0)
                        {   //not carrying anything. It's already been removed from InventoryManager
                            inventory = null;
                        }
                        else
                        {
                            //This is not true, because MY version of the code allows for the inventory to still have a stack size > 0
                            //if you were holding too many supplies.
                            //We should be putting it on the ground (but we are on the job site
                            Debug.LogError("Character is still carrying inventory, which shouldn't be. " +
                                           "Just setting to NULL for now. Means we are LEAKING inventories");
                            
                            //This inventory is still present in the inventory manager.
                            //To do this naiive delete, we need to remove it from the inventory.
                            myJob.tile.world.inventoryManager.inventories[inventory.objectType].Remove(inventory);
                            inventory = null;
                        }
                        //end of being at the job tile
                    }
                    else
                    {   
                        //We still need to walk to the job site
                        destTile = myJob.tile;
                        //FIXME: we still need to do proper pathfinding
                        //However, in the movement code will see that we have no pathing and then try to create pathing
                        //to destTile - so this might not be broken. If it can't create pathing to destTile
                        //It will change destTile to currTile
                        //setting destTile invalidates pathfinding.
                        return;
                    }
                }
                else
                {   //We are carrying inventory the job doesn't want.
                    //Dump the inventory at our feet (or nearest)
                    
                    //Walk to nearest empty tile and dump it
                    if (!currTile.world.inventoryManager.PlaceInventory(currTile, inventory))
                    {
                        Debug.LogError("Character tried to place inventory into an invalid tile, maybe there's something here");
                        //FIXME: for the sake of continuing on, we are going to set the inventory to null.
                        //This is not the right behaviour. We are leaking inventory
                        
                        //removing from inventorymanager prevents leaking, but the inventory still evaporates
                        myJob.tile.world.inventoryManager.inventories[inventory.objectType].Remove(inventory);
                        inventory = null;
                    }
                }   //End of dealing with our inventory
            }//end of us having inventory
            else
            {
                //Job doesn't have all materials, and we aren't carrying anything
                //Walk towards a tile containing the required goods
                
                //STEP 1: 
                //If already on a tile with resources the job needs, pick up the required inventory & amount.

                Inventory recipeInv = null;
                bool currTileHasInventoryWeWant = currTile.inventory != null && myJob.DesiresInventoryType(currTile.inventory, out recipeInv);
                bool currTileIsNotStockpile = currTile.furniture == null || !currTile.furniture.IsStockpile();
                bool weCanGrabFromStockpile = myJob.canTakeFromStockpile;
                if (currTileHasInventoryWeWant
                    && (currTileIsNotStockpile || weCanGrabFromStockpile))
                {
                    currTile.world.inventoryManager.PlaceInventory(this, currTile.inventory, recipeInv.UnfilledStackSize);
                }
                else
                {
                    //Step 2: Walk towards a tile with the goods
                    //FIXME: This is a dumb/simple/unoptimal initial setup
                    //Grab the first from the inventory manager

                    Inventory desired = null;
                    List<Inventory> desiredInventories = myJob.GetDesiredInventories();
                    if (desiredInventories.Count > 0)
                    {
                        desired = desiredInventories[0];
                    }
                    else
                    {
                        Debug.LogError("Job doesn't have all materials, but has no desired Inventories.");
                    }

                    Inventory supplier = currTile.world.inventoryManager.GetClosestInventoryOfType
                        (desired.objectType, currTile, desired.UnfilledStackSize, myJob.canTakeFromStockpile);

                    if (supplier == null)
                    {
                        Debug.Log($"No tile contains objects of {desired.objectType} to satisfy {myJob.jobType}");
                    
                        AbandonJob();
                        destTile = currTile; //IF SOMETHING DOESN'T WORK!!! Should this be in Abandon Job? Should this be here?
                        return;
                    }

                    Debug.Log($"Getting {desired.objectType} supplies");
                    destTile = supplier.tile;
                    return;
                }
            }
            
            return;  // We can't continue until all materials are satisfied
        }
        
        //Now job has all materials, make sure destination tile == job tile
        destTile = myJob.tile;
        
        //Job has all the material
        //Have we arrived?
        //myJob.tile check vs. destTile is okay because even if we have a destTile != myJob.tile
        //(meaning we might walk across the jobsite when pathfinding to materials)
        //We would've bailed earlier. But && currTile == destTile supplement might be safer.
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
        //We just test pathing, we will assign pathing later
        //This is because we do not always path towards the job, sometimes we grab materials first
        //This is just to validate that we can do the job
        if (Path_AStar.TestIfPathable(currTile, j.tile, out Path_AStar _) == false)
        {
            return false; 
        }
        
        myJob = j;
            
        myJob.OnJobCancelled += OnJobEnded;
        myJob.OnJobCompleted += OnJobEnded;
        return true;
    }
    
    void AbandonJob()
    {
        Debug.Log("Abandoning Job");
        if (myJob == null) return;
        
        myJob.CancelJob();
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
            else if(currTile == nextTile)
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
            Debug.LogError($"Character::TrySetDestination: {this} cannot get from {currTile.Pos} to {destTile.Pos}. Setting destination tile to current tile.");
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
    


    //Job completed or cancelled
    void OnJobEnded(Job _job)
    {
        if (_job != myJob)
        {
            Debug.LogError($"Character being notified about job {{_job}} that isn't his {myJob}. You forgot to unregister something");
            return;
        }
        
        myJob.OnJobCancelled -= OnJobEnded;
        myJob.OnJobCompleted -= OnJobEnded;

        myJob = null;
    }
    
    public bool TryAssignInventory(Inventory inv, int desiredAmount)
    {
        if (inv == null)
        {
            //Remove Installed Object
            inventory = null;
            //OnTileTypeChanged?.Invoke(this); From tile code
            return true;
        }

        if (inventory != null && inventory.objectType != inv.objectType)
        {
            Debug.LogError($"Tried to Install Inventory {inv.objectType} on tile {X},{Y} that already has an {inventory.objectType} on it!");
            return false;
        }

        inventory = inv.Clone(); //This will cause errors when this is the recipe's maxStackSize! It will be too low and not match the prototype's actual stack size
        inventory.tile = null;
        inventory.stackSize = 0; //Our inventory is the same type, but it's of type 0;
        
        int numToMove = Mathf.Clamp(desiredAmount, 0, inv.stackSize);   //we move amount, capped by stacksize
            if (inventory.stackSize + numToMove > inventory.maxStackSize)
            {   //We'll only add the amount that makes us reach the max stack size
                numToMove = inventory.maxStackSize - inventory.stackSize;
            }

        inventory.stackSize += numToMove;
        inv.stackSize -= numToMove;
        //     
        //     return true;
        // }
        //
        // //THIS DOESN'T GIVE DESIRED BEHAVIOUR BECAUSE IT DOESN'T CHECK AMOUNT
        // //AND SPLIT THE INVENTORY INTO 2
        //
        // //inventory is null. Can't just directly assign it because
        // //Inventory manager needs to know it was created.
        // inventory = inv.Clone();
        // //inventory.tile = this; From tile code
        // if (inventory.tile != null)
        // {
        //     inventory.tile.TryAssignInventory(null);
        // }
        //
        // inventory.tile = null;
        // //inventory.CallOnChanged(); We don't call events on non-floor inventories
        //
        // //We do this as a type of "return value" in InventoryManager.PlaceInventory.
        // //We check if inv.stackSize == 0, then remove it.
        // //inv.stackSize = 0;  
        // return true;
        return true;
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