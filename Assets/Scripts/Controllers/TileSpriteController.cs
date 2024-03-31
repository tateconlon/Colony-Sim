using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

//Part of the "visuals" controller collection
public class TileSpriteController : MonoBehaviour
{
    const string TILE_SORTING_LAYER_NAME = "TILE";
    public static TileSpriteController Instance { get; protected set; }

    public Sprite floorSprite;
    public Sprite emptySprite;

    Dictionary<Tile, GameObject> tileGameObjectDict = new();
    Dictionary<string, Sprite> installedObjectSprites = new();

    World World => WorldController.Instance.World;

    void Start()
    {
        if (Instance != null)
        {
            Debug.LogError("TileSpriteController Singleton already Exists!");
        }

        Instance = this;

        LoadSprites();
        
        for (int x = 0; x < World.Width; x++)
        {
            for (int y = 0; y < World.Height; y++)
            {
                Tile tile_data = World.GetTileAt(x, y);
                
                GameObject tile_go = new GameObject();
                tile_go.name = $"Tile_{x}_{y}";
                tile_go.transform.position = new Vector3(tile_data.X, tile_data.Y, 0);
                tile_go.transform.SetParent(transform);
                
                SpriteRenderer tile_sr = tile_go.AddComponent<SpriteRenderer>();
                tile_sr.sortingLayerName = TILE_SORTING_LAYER_NAME;
                tileGameObjectDict.Add(tile_data, tile_go);
                
                UpdateTileSprite(tile_data);
            }
        }
        WorldController.Instance.World.OnTileChangedEvent += OnTileChanged;
    }

    void LoadSprites()
    {
    }

    void OnTileChanged(Tile tile_data)
    {
        
        if (tile_data == null)
        {
            Debug.LogError("How did a tile_data become null for this callback?");
            return;
        }

        UpdateTileSprite(tile_data);
    }

    void UpdateTileSprite(Tile tile_data)
    {
        GameObject tile_go = GetGameObjectFromTile(tile_data);
        if (tile_go == null)
        {
            Debug.LogError($"tileGameObjectDict does not contain tile_data. Did you forget to unregist a callback?");
            return;
        }

        if (tile_data.TileType == TileType.Floor)
        {
            tile_go.GetComponent<SpriteRenderer>().sprite = floorSprite;
        }
        else if (tile_data.TileType == TileType.Empty)
        {
            tile_go.GetComponent<SpriteRenderer>().sprite = emptySprite;
        }
        else
        {
            Debug.Log($"OnTileTypeChanged - Unrecognized Tile Type. {tile_data.TileType}");
        }
    }
    
    [CanBeNull]
    public GameObject GetGameObjectFromTile(Tile t)
    {
        if (tileGameObjectDict.ContainsKey(t))
        {
            return tileGameObjectDict[t];
        }
        else
        {
            return null;
        }
    }
    
    
    
    void DestroyAllTileGameObjects()
    {
        foreach (KeyValuePair<Tile,GameObject> keyValuePair in tileGameObjectDict)
        {
            keyValuePair.Key.OnTileTypeChanged -= OnTileChanged;
            Destroy(keyValuePair.Value);
        }
        tileGameObjectDict.Clear();
    }
}