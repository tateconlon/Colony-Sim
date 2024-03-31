using System;
using UnityEngine;

public class Character
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
    
    public Character(Tile t, float moveSpeed = 2f)
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
        if (myJob == null && currTile.world.jobQueue.TryPeek(out j))
        {
            if (TrySetJob(j))
            {
                currTile.world.jobQueue.TryDequeue(out j);
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
        
        float distTotal = Vector2Int.Distance(currTile.Pos, nextTile.Pos);
        float distThisFrame = moveSpeed * deltaTime;   //Does this make sense?
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

}