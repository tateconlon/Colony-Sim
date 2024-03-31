using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

public class WorldController : MonoBehaviour
{
    public static WorldController Instance { get; protected set; }
    
    public World World { get; protected set; }

    void OnEnable()
    {
        if (Instance != null)
        {
            Debug.LogError("WorldController Singleton already Exists!");
        }

        Instance = this;
        
        World = new World();

        Camera.main.transform.position = new Vector3(World.Width / 2, World.Height/2, Camera.main.transform.position.z);
    }

    void Update()
    {
        World.Update(Time.deltaTime);
        // if (World.jobQueue.TryPeek(out Job j))
        // {
        //     j.DoWork(Time.deltaTime);
        // }
    }
    
    [CanBeNull]
    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        Vector2Int tileCoords = ScreenToTileCoord(coord);

        return WorldController.Instance.World.GetTileAt(tileCoords.x, tileCoords.y);
    }


    public static Vector2Int ScreenToTileCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x + 0.5f);
        int y = Mathf.FloorToInt(coord.y + 0.5f);
        return new Vector2Int(x, y);
    }

    public void SetupPathfindingTest()
    {
        World.SetupPathfindingExample();

        Path_TileGraph graph = new Path_TileGraph(World);
    }
}

