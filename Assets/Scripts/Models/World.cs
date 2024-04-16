using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;
using Random = UnityEngine.Random;

public class World : IXmlSerializable
{
    Tile[,] tiles;
    public List<Character> characters = new();
    public List<Furniture> furnitures = new();
    public List<Room> rooms = new();
    public InventoryManager inventoryManager = new();
    Dictionary<string, Furniture> furniturePrototypes;
    public Dictionary<string, Job> furnitureJobPrototypes;
    

    public Path_TileGraph tileGraph { get; set; } // the pathing graph

    public int Width { get; protected set; }
    public int Height { get; protected set; }

    public event Action<Furniture> OnFurnitureCreated;
    public event Action<Character> OnCharacterCreated;
    public event Action<Inventory> OnInventoryCreated;
    public event Action<Inventory> OnInventoryChanged;
    public event Action<Tile> OnTileChangedEvent;

    public JobQueue jobQueue;
    
    public World()
    {
        //empty constructor needed for XMLWriter
    }
    
    public World(int width = 100, int height = 100)
    {
        // Create empty world, and we'll put a character in it for good measure.
        Init_World(width, height);
        CreateCharacter(GetTileAt(Width/2,     Height/2));

        //Character c1 = CreateCharacter(GetTileAt(Width/2,     Height/2));
        // Character c2 = CreateCharacter(GetTileAt(Width/2 + 1, Height/2 + 1));
        // Character c3 = CreateCharacter(GetTileAt(Width/2 + 2, Height/2 + 2));

        // c1.TrySetDestination(GetTileAt(Width/2, Height - 1));
        // c2.TrySetDestination(GetTileAt(Width/2,  0));
        // c3.TrySetDestination(GetTileAt(0,  Height/2));
    }

    void Init_World(int width, int height)
    {
        jobQueue = new();
        Width = width;
        Height = height;

        inventoryManager = new();
        rooms = new();
        Room outside = new Room();
        rooms.Add(outside); //TODO: Add the "outside" room.

        tiles = new Tile[width, height];
        Tile t;
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                t = new Tile(this, i, j);
                t.TileType = TileType.Empty;
                t.OnTileTypeChanged += OnTileTypeTileChangedFunc;
                tiles[i, j] = t;
                outside.AssignTile(t);
            }
        }

        Debug.Log($"World created with {width}, {height}");

        characters = new();
        furnitures = new();
        InitFurniturePrototypes();
        foreach (Furniture furn in furnitures)
        {
            OnFurnitureCreated(furn);
        }

        tileGraph = new Path_TileGraph(this);
    }

    public void Update(float deltaTime)
    {
        foreach (Character c in characters)
        {
            c.Update(deltaTime);
        }

        foreach (Furniture f in furnitures)
        {
            f.Update(deltaTime);
        }
    }

    void InitFurniturePrototypes()
    {
        // This will be replaced by a function that reads all of our furniture data
        // from a text file.
        furniturePrototypes = new();
        furnitureJobPrototypes = new();
        
        Furniture wallPrototype = new Furniture(
            "wall", 
            0 ,  //impassable
            1,
            1,
            true,
            true);
        furniturePrototypes.Add(wallPrototype.objectType, wallPrototype);
        furnitureJobPrototypes.Add(wallPrototype.objectType,
            new Job(null, 
                FurnitureActions.JobComplete_FurnitureBuilding,
                wallPrototype.objectType,
                1f,
                new Inventory[]{ new Inventory("steel_plate", 0, 2) }));
        
        Furniture doorPrototype = new Furniture(
            "door", 
            1 ,  //movement Cost
            1,
            1,
            false,
            true);
        furniturePrototypes.Add(doorPrototype.objectType, doorPrototype);
        // furnitureJobPrototypes.Add(doorPrototype.objectType,
        //     new Job(null, 
        //         (j) => {},
        //         doorPrototype.objectType,
        //         0.1f,
        //         null));

        doorPrototype.SetParameter("openness", 0f);
        doorPrototype.SetParameter("is_opening", 0f);
        doorPrototype.updateActions += FurnitureActions.Door_UpdateAction;
        doorPrototype.IsEnterable += FurnitureActions.Door_IsEnterable;
        
        
    }

    public void SetupPathfindingExample()
    {
        Debug.Log($"SetupPathfindingExample");

        int l = Width / 2 - 5;
        int b = Height / 2 - 5;
        
        for (int x = l-5; x < l + 15; x++)
        {
            for (int y = b - 5; y < b + 15; y++)
            {
                tiles[x, y].TileType = TileType.Floor;

                if (x == l || x == (l + 9) || y == b || y == (b + 9))
                {
                    if (x != (l + 9) && y != (b + 4))
                    {
                        TryPlaceFurniture("wall", tiles[x, y]);
                    }
                }
            }
        }
    }

    public Character CreateCharacter(Tile t)
    {
        Character c = new Character(t);
        characters.Add(c);
        OnCharacterCreated?.Invoke(c);
        
        return c;
    }

    public Room GetOutsideRoom()
    {
        return rooms[0];
    }

    public void AddRoom(Room room)
    {
        if (!rooms.Contains(room))
        {
            rooms.Add(room);
        }
    }

    public void DeleteRoom(Room r)
    {
        if (r == GetOutsideRoom())
        {
            Debug.LogError("Tried to delete outside room");
            return;
        }

        Tile[] unassignedTiles = r.UnAssignAllTiles();
        foreach (Tile tile in unassignedTiles)
        {
            tile.world.GetOutsideRoom().AssignTile(tile);
        }
        rooms.Remove(r);
    }
    
    public Tile GetTileAt(int x, int y)
    {
        if (x >= Width || x < 0 || y >= Height || y < 0)
        {
            //Debug.LogError($"Tile ({x},{y} is out of range. width = {Width}, height = {Height}");
            return null;
        }
        return tiles[x, y];
    }

    //TODO: This function assumes 1x1 tiles with no rotation
    public Furniture TryPlaceFurniture(string objectType, Tile t)
    {
        if (furniturePrototypes.TryGetValue(objectType, out Furniture obj))
        {
            Furniture newFurn = Furniture.PlaceInstance(obj, t);
            
            //TODO: Handle the null as a destruction. This currently doesn't make sense
            if (newFurn == null) return null;

            OnFurnitureCreated?.Invoke(newFurn);

            if (newFurn.roomEnclosure)
            {
                Room.DoRoomFloodFill(newFurn);
            }

            if (newFurn.movementCost != 1)
            {
                //Since tiles return movement cost as their base cost multiplied
                //by the furniture's movement cost, a furniture movement cost
                //of exactly 1 doesn't impact our pathfinding system, so we can
                //optimize by not having to invalidate tilegraph when the movementCost is 1 (equal to empty floor tile)
                InvalidateTileGraph();
            }
            
            furnitures.Add(newFurn);
            
            return newFurn;
        }

        return null;
    }

    //When a tile tells us it's changed, World broadcasts OnTileChanged too
    //FIXME: May result in duplicate events firing??
    void OnTileTypeTileChangedFunc(Tile t)
    {
        OnTileChangedEvent?.Invoke(t);
        
        InvalidateTileGraph();
        Room.DoRoomFloodFill(t);
    }

    //Should be called whenever a change to the world that
    //affects the pathfinding should be called
    public void InvalidateTileGraph()
    {
        tileGraph = null;
    }

    public bool IsFurniturePlacementValid(string furnitureType, Tile t)
    {
        if (furniturePrototypes.TryGetValue(furnitureType, out Furniture furn))
        {
            return furn.IsPositionValid(t);
        }
        return false;
    }

    public Furniture GetFurniturePrototype(string furnitureType)
    {
        if (furniturePrototypes.TryGetValue(furnitureType, out Furniture furn))
        {
            return furn;
        }
        
        return null;
    }

    #region SAVING_AND_LOADING
    ////////////////////////////////////////////////
    ///
    ///         SAVING AND LOADING
    ///
    ///////////////////////////////////////////////
    
    public XmlSchema GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
        Debug.Log("World::ReadXML");
        //Read data
        int width = int.Parse(reader.GetAttribute("Width"));
        int height = int.Parse(reader.GetAttribute("Height"));
        
        Init_World(width, height);

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Tiles":
                    ReadXml_Tiles(reader);
                    break;
                case "Furnitures":
                    ReadXml_Furnitures(reader);
                    break;
                case "Characters":
                    ReadXml_Characters(reader);
                    break;
            }
        }
        
        //DEBUG ONLY: remove me later
        Inventory inventoryItem = new Inventory("steel_plate", 15, 50);
        Tile t = GetTileAt(51, 51);
        inventoryManager.PlaceInventory(t, inventoryItem);
        OnInventoryCreated?.Invoke(t._inventory);
        
        Inventory inventoryItem2 = new Inventory("steel_plate", 10, 50);
        t = GetTileAt(50, 54);
        inventoryManager.PlaceInventory(t, inventoryItem2);
        OnInventoryCreated?.Invoke(t._inventory);
        
        Inventory inventoryItem4 = new Inventory("steel_plate", 20, 50);
        t = GetTileAt(49, 51);
        inventoryManager.PlaceInventory(t, inventoryItem4);
        OnInventoryCreated?.Invoke(t._inventory);

    }

    void ReadXml_Tiles(XmlReader reader)
    {
        //No Tile nodes in the Tiles list
        if (reader.ReadToDescendant("Tile") == false) return;

        do
        {
            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));
            tiles[x, y].ReadXml(reader);
        } while (reader.ReadToNextSibling("Tile"));
    }

    void ReadXml_Furnitures(XmlReader reader)
    {
        //No Furniture nodes in the Furnitures list
        if (reader.ReadToDescendant("Furniture") == false) return;

        do
        {
            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));
            Furniture furn = TryPlaceFurniture(reader.GetAttribute("objectType"), tiles[x, y]);
            furn.ReadXml(reader);
        } while (reader.ReadToNextSibling("Furniture"));
    }
    
    void ReadXml_Characters(XmlReader reader)
    {
        //No Character nodes in the Characters list
        if (reader.ReadToDescendant("Character") == false) return;

        do
        {
            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));
            
            Character c = CreateCharacter(tiles[x, y]);
            c.ReadXml(reader);
        } while (reader.ReadToNextSibling("Character"));
    }

    public void WriteXml(XmlWriter writer)
    {
        Debug.Log("World::WriteXML");
        writer.WriteAttributeString("Width", Width.ToString());
        writer.WriteAttributeString("Height", Height.ToString());
        
        writer.WriteStartElement("Tiles");
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (tiles[x, y].TileType != TileType.Empty)
                {
                    writer.WriteStartElement("Tile");
                    tiles[x,y].WriteXml(writer);
                    writer.WriteEndElement();
                }
            }
        }
        writer.WriteEndElement();

        if (furnitures.Count > 0)
        {
            writer.WriteStartElement("Furnitures");
            foreach (Furniture furn in furnitures)
            {
                writer.WriteStartElement("Furniture");
                furn.WriteXml(writer);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        if (characters.Count > 0)
        {
            writer.WriteStartElement("Characters");
            foreach (Character c in characters)
            {
                writer.WriteStartElement("Character");
                c.WriteXml(writer);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        
        
    }

    #endregion

}