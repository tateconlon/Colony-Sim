using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

public class World
{
    Tile[,] tiles;
    public List<Character> characters;
    Dictionary<string, Furniture> furniturePrototypes;

    public Path_TileGraph tileGraph { get; set; } // the pathing graph

    public int Width { get; protected set; }
    public int Height { get; protected set; }

    public event Action<Furniture> OnFurnitureCreated;
    public event Action<Character> OnCharacterCreated;
    public event Action<Tile> OnTileChangedEvent;

    public JobQueue jobQueue;
    
    public World(int width = 100, int height = 100)
    {
        jobQueue = new();
        Width = width;
        Height = height;

        tiles = new Tile[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                tiles[i, j] = new Tile(this, i, j);
                tiles[i, j].TileType = TileType.Empty;
                tiles[i, j].OnTileTypeChanged += OnTileTypeTileChangedFunc;
            }
        }

        Debug.Log($"World created with {width}, {height}");

        characters = new();
        Character c1 = CreateCharacter(GetTileAt(Width/2,     Height/2));
        //c1.TrySetDestination(GetTileAt(0, 0));
        Character c2 = CreateCharacter(GetTileAt(Width/2 + 1, Height/2 + 1));
        Character c3 = CreateCharacter(GetTileAt(Width/2 + 2, Height/2 + 2));

        // c1.TrySetDestination(GetTileAt(Width/2, Height - 1));
        // c2.TrySetDestination(GetTileAt(Width/2,  0));
        // c3.TrySetDestination(GetTileAt(0,  Height/2));

        InitFurniturePrototypes();

        tileGraph = new Path_TileGraph(this);
    }

    public void Update(float deltaTime)
    {
        foreach (Character c in characters)
        {
            c.Update(deltaTime);
        }
    }

    void InitFurniturePrototypes()
    {
        furniturePrototypes = new();
        
        Furniture wallPrototype = Furniture.CreatePrototype(
            "wall", 
            0 ,  //impassable
            1,
            1,
            true);
        furniturePrototypes.Add("wall", wallPrototype);
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
    public bool TryPlaceFurniture(string objectType, Tile t)
    {
        if (furniturePrototypes.TryGetValue(objectType, out Furniture obj))
        {
            Furniture newObj = Furniture.PlaceInstance(obj, t);
            
            //TODO: Handle the null as a destruction. This currently doesn't make sense
            if (newObj == null) return false;
            
            OnFurnitureCreated?.Invoke(newObj);
            InvalidateTileGraph();
            
            return newObj != null;
        }

        return false;
    }

    //When a tile tells us it's changed, World broadcasts OnTileChanged too
    //FIXME: May result in duplicate events firing??
    void OnTileTypeTileChangedFunc(Tile t)
    {
        OnTileChangedEvent?.Invoke(t);
        
        InvalidateTileGraph();
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
}