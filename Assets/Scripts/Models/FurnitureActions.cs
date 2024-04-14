using UnityEngine;

public static class FurnitureActions 
{
    public static void Door_UpdateAction(Furniture furn, float deltaTime)
    {
        if (furn.furnParams["is_opening"] >= 1)
        {
            furn.furnParams["openness"] += 4 * deltaTime;
            if (furn.furnParams["openness"] >= 1f)
            {
                furn.furnParams["is_opening"] = 0;
            }
        }
        else
        {
            furn.furnParams["openness"] -= 4 * deltaTime;
        }
        
        furn.OnChanged?.Invoke(furn);

        furn.furnParams["openness"] = Mathf.Clamp01(furn.furnParams["openness"]);
        //Debug.Log($"Door updated: {deltaTime}");
    }

    public static Enterability Door_IsEnterable(Furniture furn)
    {
        furn.furnParams["is_opening"] = 1;
        
        if (furn.furnParams["openness"] >= 1f)
        {
            return Enterability.Yes;
        }
        else
        {
            return Enterability.Soon;
        }
    }
}