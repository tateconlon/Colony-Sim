using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class BuildModeController : MonoBehaviour
{
    //Consider: Should Tile become Bulldoze & Tile?
    //or stay as one mode that uses TileType enums like how furniture
    //uses a string. This way BuildMode is more about "layers" than individual blocks
    //ie:tile layer, furniture layer, looseObject layer etc.
    public enum BuildMode
    {
        Tile,  
        Furniture,
    }
    
    public static BuildModeController Instance { get; protected set; }

    BuildMode buildMode;
    string buildMode_FurnitureType;
    TileType buildMode_TileType;
    
    void Start()
    {
        if (Instance != null)
        {
            Debug.LogError("Two BuildModeController Singletons in Scene!");
        }
        Instance = this;
    }
    public void DoBuild(List<Tile> tiles)
    {
        foreach (Tile tile in tiles)
        {
            if (buildMode == BuildMode.Furniture)
            {
                string furnitureType = buildMode_FurnitureType;   //Avoid using the member type which may change before

                if (WorldController.Instance.World.IsFurniturePlacementValid(furnitureType, tile))
                {
                    
                    Job j;
                    if (WorldController.Instance.World.furnitureJobPrototypes.TryGetValue(furnitureType, out Job jobProto))
                    {
                        //success!
                        j = jobProto.Clone();
                        j.tile = tile;
                    }
                    else
                    {
                        Debug.Log($"There is no Furniture Job Prototype for {furnitureType}");
                        j = new Job(tile,
                        FurnitureActions.JobComplete_FurnitureBuilding,
                        furnitureType, 
                        0.1f,
                        null);
                    }

                    //FIXME: I don't like having to manually and explicitly set flags to prevent conflicts
                    //It's too easy to forget to set/clear them
                    tile.pendingFurnitureJob = j;
                    j.OnJobCancelled += (job) => { job.tile.pendingFurnitureJob = null; };
                        
                    WorldController.Instance.World.jobQueue.Enqueue(j);
                    Debug.Log($"Job Queue Size: {WorldController.Instance.World.jobQueue.Count}");
                }
            }
            else if (buildMode == BuildMode.Tile)
            {
                //Not furniture. eg: Floor vs. empty
                tile.TileType = buildMode_TileType;
            }
        }
    }
    
    public void SetMode_BuildFloor()
    {
        buildMode = BuildMode.Tile;
        buildMode_TileType = TileType.Floor;
    }
    
    public void SetMode_Bulldoze()
    {
        buildMode = BuildMode.Tile;
        buildMode_TileType = TileType.Empty;
    }


    public void SetMode_Furniture(string furnitureTypeStr)
    {
        buildMode = BuildMode.Furniture;
        buildMode_FurnitureType = furnitureTypeStr;
    }
}