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
}