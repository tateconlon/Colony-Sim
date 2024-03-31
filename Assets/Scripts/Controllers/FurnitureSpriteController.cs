using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

//Part of the "visuals" controller collection
public class FurnitureSpriteController : MonoBehaviour
{
    const string FURNITURE_SORTING_LAYER_NAME = "FURNITURE";
    const string FURNITURE_RESOURCE_PATH = "Images/Furniture/";
    Dictionary<string, Sprite> furnitureSprites;
    public static FurnitureSpriteController Instance { get; protected set; }

    private Dictionary<Furniture, GameObject> furniture_GameObject_Map = new();

    World World => WorldController.Instance.World;

    void Start()
    {
        if (Instance != null)
        {
            Debug.LogError("FurnitureSpriteController Singleton already Exists!");
        }

        Instance = this;

        LoadSprites();

        WorldController.Instance.World.OnFurnitureCreated += OnFurnitureCreated;
    }

    void LoadSprites()
    {
        furnitureSprites = new();
        Sprite[] sprites = Resources.LoadAll<Sprite>(FURNITURE_RESOURCE_PATH);
        foreach (Sprite sprite in sprites)
        {
            // Debug.Log(sprite.name);
            furnitureSprites.Add(sprite.name, sprite);
        }
    }

    public static Vector2Int ScreenToTileCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x + 0.5f);
        int y = Mathf.FloorToInt(coord.y + 0.5f);
        return new Vector2Int(x, y);
    }
    
    [CanBeNull]
    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        Vector2Int tileCoords = ScreenToTileCoord(coord);

        return WorldController.Instance.World.GetTileAt(tileCoords.x, tileCoords.y);
    }

    public void OnFurnitureCreated(Furniture createdObj)
    {
        //Do visuals
        Tile tile_data = World.GetTileAt(createdObj.tileOwner.X, createdObj.tileOwner.Y);
                
        GameObject furn_go = new GameObject();
        furn_go.name = $"{createdObj.objectType}_{tile_data.X}_{tile_data.Y}";
        furn_go.transform.position = new Vector3(tile_data.X, tile_data.Y, 0);
        furn_go.transform.SetParent(transform);
        
        SpriteRenderer furn_sr = furn_go.AddComponent<SpriteRenderer>();
        furn_sr.sortingLayerName = FURNITURE_SORTING_LAYER_NAME;
        furn_sr.sprite = GetSpriteForFurniture(createdObj);

        furniture_GameObject_Map.TryAdd(createdObj, furn_go);

        createdObj.OnChanged += OnFurnitureChanged;
    }

    public Sprite GetSpriteForFurniture(Furniture obj)
    {
        if (!obj.linksToNeighbours)
        {
            return furnitureSprites[obj.objectType];
        }

        string spriteName = obj.objectType + "_";
        Tile t = obj.tileOwner;
        
        Tile temp_N = World.GetTileAt(t.X, t.Y + 1);
        if (temp_N != null && temp_N.Furniture != null)
        {
            spriteName += temp_N.Furniture.objectType == obj.objectType ? "N" : "";
        }
        
        Tile temp_E = World.GetTileAt(t.X + 1, t.Y);
        if (temp_E != null && temp_E.Furniture != null)
        {
            spriteName += temp_E.Furniture.objectType == obj.objectType ? "E" : "";
        }
        
        Tile temp_S = World.GetTileAt(t.X, t.Y - 1);
        if (temp_S != null && temp_S.Furniture != null)
        {
            spriteName += temp_S.Furniture.objectType == obj.objectType ? "S" : "";
        }
        
        Tile temp_W = World.GetTileAt(t.X - 1, t.Y);
        if (temp_W != null && temp_W.Furniture != null)
        {
            spriteName += temp_W.Furniture.objectType == obj.objectType ? "W" : "";
        }

        // bool didNESWTrigger = false;
        // if (!spriteName.EndsWith('_'))
        // {
        //     spriteName += "_";
        //     didNESWTrigger = true;
        // }
        //
        // Tile temp_NE = World.GetTileAt(t.X+1, t.Y + 1);
        // if (temp_NE != null && temp_NE.installedObject != null)
        // {
        //     spriteName += temp_NE.installedObject.objectType == obj.objectType ? "NE" : "";
        // }
        //
        // Tile temp_SE = World.GetTileAt(t.X + 1, t.Y -1);
        // if (temp_SE != null && temp_SE.installedObject != null)
        // {
        //     spriteName += temp_SE.installedObject.objectType == obj.objectType ? "SE" : "";
        // }
        //
        // Tile temp_SW = World.GetTileAt(t.X-1, t.Y - 1);
        // if (temp_SW != null && temp_SW.installedObject != null)
        // {
        //     spriteName += temp_SW.installedObject.objectType == obj.objectType ? "SW" : "";
        // }
        //
        // Tile temp_NW = World.GetTileAt(t.X - 1, t.Y + 1);
        // if (temp_NW != null && temp_NW.installedObject != null)
        // {
        //     spriteName += temp_NW.installedObject.objectType == obj.objectType ? "NW" : "";
        // }
        //
        // if (spriteName.EndsWith('_') && didNESWTrigger)
        // {
        //     spriteName = spriteName.Substring(0, spriteName.Length - 1);
        // }
        //
        if (furnitureSprites.TryGetValue(spriteName, out Sprite s))
        {
            return s;
        }
        
        Debug.LogError($"Could not find sprite {spriteName} -- GetSpriteName");
        return null;
    }

    //Currently ignoring links to neighbours
    public Sprite GetSpriteForFurniture(string furnitureType)
    {
        if (furnitureSprites.TryGetValue(furnitureType, out Sprite s))
        {
            return s;
        }
        
        if (furnitureSprites.TryGetValue(furnitureType + "_", out Sprite s2))
        {
            return s2;
        }

        return null;
    }

    void OnFurnitureChanged(Furniture obj)
    {
        if (!furniture_GameObject_Map.TryGetValue(obj, out GameObject furn_go))
        {
            Debug.LogError("Trying to respond to furniture changes not in our dictionary map");
            return;
        }

        furn_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(obj);
    }
}