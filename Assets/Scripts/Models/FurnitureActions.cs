using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class FurnitureActions 
{
    public static void Door_UpdateAction(Furniture furn, float deltaTime)
    {
        if (furn.GetParameter("is_opening") >= 1)
        {
            furn.ChangeParameter("openness",4 * deltaTime );
            if (furn.GetParameter("openness") >= 1f)
            {
                furn.SetParameter("is_opening", 0);
            }
        }
        else
        {
            furn.ChangeParameter("openness", - 4 * deltaTime );
        }
        
        furn.OnChanged?.Invoke(furn);

        furn.SetParameter("openness", Mathf.Clamp01(furn.GetParameter("openness")));
        //Debug.Log($"Door updated: {deltaTime}");
    }

    public static Enterability Door_IsEnterable(Furniture furn)
    {
        furn.SetParameter("is_opening", 1);
        
        if (furn.GetParameter("openness") >= 1f)
        {
            return Enterability.Yes;
        }
        else
        {
            return Enterability.Soon;
        }
    }
    
    public static void JobComplete_FurnitureBuilding(Job theJob)
    {
        WorldController.Instance.World.TryPlaceFurniture(theJob.jobType,
            theJob.tile);

        theJob.tile.pendingFurnitureJob = null;
    }

    public static Inventory[] Stockpile_Inventory()
    {
        return new Inventory[] { new Inventory("steel_plate", 0, 50) };
    }

    public static void Stockpile_UpdateAction(Furniture furn, float deltaTime)
    {
        //Make sure there is always a job for us if:
        // a) If we're empty, get ANY loose inventory
        // b) If we have something, get us more.
        
        //TODO: this function doesn't need to be run each update.
        //Once we get a lot of furniture in a running game, this will run a LOT more than required
        //Instead it only really needs to run whenever:
        //      -- It gets created
        //      -- A good gets delivered (at which point we reset the job)
        //      -- A good gets picked up (at which point we reset the job)
        //      -- The UI's filter of allowed items gets changed
        
        Inventory currInv = furn.tile.inventory;
        if (currInv != null && currInv.UnfilledStackSize <= 0 )
        {
            //We are full
            furn.ClearJobs();
            return;
        }
        
        //Do we have a job queued up?
        if (furn.JobCount() > 0)
        {   //Great, all done here
            return;
        }
        
        //We currently are not full and don't have a job
        //We either have some inventory or none

        //Error if we have both some inventory but none of it
        if (currInv != null && currInv.stackSize == 0)
        {
            Debug.LogError("Stockpile has a zero size stack. This is clearly WRONG!");
            furn.ClearJobs();
            return;
        }
        
        //TODO: In the future, stockpiles -- rather than being a bunch
        // 1x1 tiles -- should manifest themselves as single, large objects. This
        // would represent our first and probably only VARIABLE sized "furniture" --
        // at what happens if there's a hole in ourstockpile be cause we have actual piece
        // of furniture (like a cooking station) installed in the middle of our stockpile?
        // In any case, once we implement "mega stockpiles". then the job-creation system
        // could be a lot smarter, in that even if the stockpile has some stuff in it,
        // it can also request different object types in its job creation.

        Inventory[] itemsDesired;

        if (currInv == null)
        {
            //We have nothing, go get anything.
            itemsDesired = Stockpile_Inventory();
        }
        else
        {   //We have something started, but we're not full yet
            Inventory desiredInv = currInv.Clone();
            desiredInv.maxStackSize -= desiredInv.stackSize;
            desiredInv.stackSize = 0;
            desiredInv.tile = null;

            itemsDesired = new[] { desiredInv };
        }
            
        //We have nothing, go get anything.
        Job j = new Job(furn.tile,
            StockPile_JobComplete,
            null,
            0,
            itemsDesired);
        j.OnJobWorked += StockPile_JobWorked;
        
        //Later on, add stockpile priorities, so that we can take from a lower priority
        //stockpile for a higher one.
        j.canTakeFromStockpile = false;
            
        furn.AddJob(j);
    }
    
    static void StockPile_JobComplete(Job j)
    {
        j.tile.furniture.RemoveJob(j);
        
        //Change this when it could be any
        foreach (Inventory inv in j.recipe.Values)
        {
            //There should be no way that we end up with more than 1 inventory requirement
            //
            if (inv.stackSize > 0)
            {
                j.tile.world.inventoryManager.PlaceInventory(j.tile, inv);
                return; 
            }
        }
    }
    
    static void StockPile_JobWorked(Job j)
    {
        j.tile.furniture.RemoveJob(j);  //We need to do this incase it's the first job.
        
        //Change this when it could be any
        foreach (Inventory inv in j.recipe.Values)
        {
            //There should be no way that we end up with more than 1 inventory requirement
            //
            if (inv.stackSize > 0)
            {
                j.tile.world.inventoryManager.PlaceInventory(j.tile, inv);
                return; 
            }
        }
    }
}