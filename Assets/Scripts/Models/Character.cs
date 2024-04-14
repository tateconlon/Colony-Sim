using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;

public class Character : IXmlSerializable
{
    public float X => Mathf.Lerp(currTile.X, nextTile.X, movePercentage);
    public float Y => Mathf.Lerp(currTile.Y, nextTile.Y, movePercentage);

    public Vector2 Pos => new Vector3(X, Y);
    
    Tile currTile;
    Tile destTile;
    Tile nextTile;
    float movePercentage;
    float moveSpeed;    // tiles/sec

    Job myJob;

    Path_AStar pathing;

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

    public void Update(float deltaTime)
    {
        //Try to grab a job and set our job destination to it
        Job j;
        if (myJob == null)
        {
            //Cycle through all jobs in the queue
            //Dequeue the first job, if you can't set it, then 
            //Re-enqueue it and try the next until you've cycled the full Job Queue.
            int jobQueueCount = currTile.world.jobQueue.Count;
            for (int i = 0; i < jobQueueCount; i++)
            {
                currTile.world.jobQueue.TryDequeue(out j);
                if (TrySetJob(j))
                {
                    break;
                }
                
                currTile.world.jobQueue.Enqueue(j);
            }
        }

        //Have we arrived?
        if (myJob != null && currTile == myJob.tile)    
        {
            myJob.DoWork(deltaTime);
            pathing = null;
            return; //FIXME: This seems sketch
        }

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

    bool TrySetJob(Job job)
    {
        if (TrySetDestination(job.tile))
        {
            myJob = job;
            
            myJob.OnJobCancelled += OnJobEnded;
            myJob.onJobCompleted += OnJobEnded;
            return true;
        }

        return false;
    }
    
    public bool TrySetDestination(Tile destTile)
    {
        pathing = new Path_AStar(currTile.world, currTile, destTile);
        if (pathing.Length() == 0)
        {
            Debug.LogError($"Character::TrySetDestination: {this} cannot get from {currTile} to {this.destTile}. Setting destination tile to current tile.");
            this.destTile = currTile;
            pathing = null;
            return false;
        }

        this.destTile = destTile;
        this.nextTile = pathing.DequeueNextTile();
        
        while (nextTile == currTile)
        {
            Debug.Log("TrySetDestination: nextTile == currTile, advancing to next tile in path.");
            this.nextTile = pathing.DequeueNextTile();
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