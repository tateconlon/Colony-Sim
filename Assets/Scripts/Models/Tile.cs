using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum TileType
{
    Empty,
    Floor,
    UNINITIALIZED,
}


public class Tile
{
    public event Action<Tile> OnTileTypeChanged;    //Anytime our data changes

    private TileType _tileType = TileType.Empty;
    public TileType TileType
    {
        get => _tileType;
        set
        {
            TileType oldType = _tileType;
            _tileType = value; 
            if(oldType != _tileType) OnTileTypeChanged?.Invoke(this);}
    }

    private Inventory _inventory;
    public Furniture Furniture { get; protected set; }
    public Job pendingFurnitureJob;

    public float movementCost {
        get
        {
            if (_tileType == TileType.Empty) return 0;//impassable
            if (Furniture == null) return 1;
            return 1 * Furniture.movementCost;
        }
    }

    public World world { get; protected set; }
    public int X { get; protected set; }
    public int Y { get; protected set; }
    public Vector2Int Pos => new Vector2Int(X, Y);

    public Tile(World world, int x, int y)
    {
        this.world = world;
        this.X = x;
        this.Y = y;
        TileType = TileType.UNINITIALIZED;
    }

    public bool TryAssignFurniture(Furniture objInstance)
    {
        if (objInstance == null)
        {
            //Remove Installed Object
            Furniture = null;
            OnTileTypeChanged?.Invoke(this);
            return true;
        }

        if (Furniture != null)
        {
            Debug.LogError($"Tried to Install Object {objInstance} on tile {X},{Y} that already has an {Furniture} on it!");
            return false;
        }

        Furniture = objInstance;
        return true;
    }

    //We may want to cache this
    public Tile[] GetNeighbours(bool includeDiagonals)
    {
        Tile[] retVal;

        if (includeDiagonals == false)
        {
            retVal = new Tile[4];   // N E S W
        }
        else
        {
            retVal = new Tile[8];   // N E S W   NE SE SW NW
        }

        retVal[0] = world.GetTileAt(X, Y + 1);
        retVal[1] = world.GetTileAt(X+ 1, Y);
        retVal[2] = world.GetTileAt(X, Y - 1);
        retVal[3] = world.GetTileAt(X - 1, Y);

        if (includeDiagonals)
        {
            retVal[4] = world.GetTileAt(X + 1, Y + 1);
            retVal[5] = world.GetTileAt(X + 1, Y - 1);
            retVal[6] = world.GetTileAt(X - 1, Y - 1);
            retVal[7] = world.GetTileAt(X - 1, Y + 1);
        }

        return retVal;
    }
}
