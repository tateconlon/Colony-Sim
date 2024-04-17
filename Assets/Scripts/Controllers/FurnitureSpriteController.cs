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

        foreach (Furniture worldFurniture in WorldController.Instance.World.furnitures)
        {
            OnFurnitureCreated(worldFurniture);
        }
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

    public void OnFurnitureCreated(Furniture createdObj)
    {
        //Do visuals
        Tile tile_data = World.GetTileAt(createdObj.tile.X, createdObj.tile.Y);
                
        GameObject furn_go = new GameObject();

        furn_go.name = $"{createdObj.objectType}_{tile_data.X}_{tile_data.Y}";
        furn_go.transform.position = new Vector3(tile_data.X, tile_data.Y, 0);
        furn_go.transform.SetParent(transform);
        
        //FIXME: this is hardcoded for doors, but it should not be eventually
        //      Plus it doesn't update at runtime
        if (createdObj.objectType == "door")
        {
            //If door is placed between north and south walls, rotate it 90 degrees.
            //Doors in the model space do not care about their orientation/rotation
            //It's only a visual thing

            Tile tile_North = World.GetTileAt(createdObj.tile.X, createdObj.tile.Y + 1);
            Tile tile_South = World.GetTileAt(createdObj.tile.X, createdObj.tile.Y - 1);
            if ((tile_North != null && tile_South != null)
                && (tile_North.furniture != null && tile_South.furniture != null)
                && (tile_North.furniture.objectType == "wall" && tile_South.furniture.objectType == "wall"))
            {
                furn_go.transform.rotation *= Quaternion.AngleAxis(90, Vector3.forward);
            }
        }
        
        SpriteRenderer furn_sr = furn_go.AddComponent<SpriteRenderer>();
        furn_sr.sortingLayerName = FURNITURE_SORTING_LAYER_NAME;
        furn_sr.sprite = GetSpriteForFurniture(createdObj);
        furn_sr.color = createdObj.tint;

        furniture_GameObject_Map.TryAdd(createdObj, furn_go);

        createdObj.OnChanged += OnFurnitureChanged;
    }

    public Sprite GetSpriteForFurniture(Furniture furn)
    {
        string spriteName = "";
        if (!furn.linksToNeighbours)
        {
            if (furn.objectType == "door")
            {
                if (furn.GetParameter("openness") < 0.1f)
                {
                    spriteName = "door_openness_0";
                }
                else if (furn.GetParameter("openness") < 0.4f)
                {
                    spriteName = "door_openness_1";
                }
                else if (furn.GetParameter("openness") < 0.9f)
                {
                    spriteName = "door_openness_2";
                }
                else if (furn.GetParameter("openness") >= 0.9f)
                {
                    spriteName = "door_openness_3";
                }
                
                if (furnitureSprites.TryGetValue(spriteName, out Sprite doorSprite))
                {
                    return doorSprite;
                }
                Debug.LogError($"Could not find sprite {spriteName} for door @ {furn.tile.Pos}");
            }

            return furnitureSprites[furn.objectType];
        }

        spriteName = furn.objectType + "_";
        Tile t = furn.tile;
        
        Tile temp_N = World.GetTileAt(t.X, t.Y + 1);
        if (temp_N != null && temp_N.furniture != null)
        {
            spriteName += temp_N.furniture.objectType == furn.objectType ? "N" : "";
        }
        
        Tile temp_E = World.GetTileAt(t.X + 1, t.Y);
        if (temp_E != null && temp_E.furniture != null)
        {
            spriteName += temp_E.furniture.objectType == furn.objectType ? "E" : "";
        }
        
        Tile temp_S = World.GetTileAt(t.X, t.Y - 1);
        if (temp_S != null && temp_S.furniture != null)
        {
            spriteName += temp_S.furniture.objectType == furn.objectType ? "S" : "";
        }
        
        Tile temp_W = World.GetTileAt(t.X - 1, t.Y);
        if (temp_W != null && temp_W.furniture != null)
        {
            spriteName += temp_W.furniture.objectType == furn.objectType ? "W" : "";
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
        //     spriteName += temp_NE.installedObject.objectType == furn.objectType ? "NE" : "";
        // }
        //
        // Tile temp_SE = World.GetTileAt(t.X + 1, t.Y -1);
        // if (temp_SE != null && temp_SE.installedObject != null)
        // {
        //     spriteName += temp_SE.installedObject.objectType == furn.objectType ? "SE" : "";
        // }
        //
        // Tile temp_SW = World.GetTileAt(t.X-1, t.Y - 1);
        // if (temp_SW != null && temp_SW.installedObject != null)
        // {
        //     spriteName += temp_SW.installedObject.objectType == furn.objectType ? "SW" : "";
        // }
        //
        // Tile temp_NW = World.GetTileAt(t.X - 1, t.Y + 1);
        // if (temp_NW != null && temp_NW.installedObject != null)
        // {
        //     spriteName += temp_NW.installedObject.objectType == furn.objectType ? "NW" : "";
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
        if (furnitureType == "door")
        {
            furnitureType = "door_openness_0";
        }
        
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