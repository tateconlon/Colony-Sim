using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using Furniture_1 = Furniture;


public enum TileType
{
    Empty,
    Floor,
    UNINITIALIZED,
}

public enum Enterability
{
    Yes,
    Never,
    Soon,
}

public class Tile : IXmlSerializable
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

    public Room room { get; set; }

    public Inventory inventory { get; protected set; }
    public Furniture furniture { get; protected set; }
    public Job pendingFurnitureJob;

    const float baseTileMovementCost = 1f;

    public float movementCost {
        get
        {
            if (_tileType == TileType.Empty) return 0;//impassable
            if (furniture == null) return 1;
            return baseTileMovementCost * furniture.movementCost;
        }
    }

    public World world { get; protected set; }
    public int X { get; protected set; }
    public int Y { get; protected set; }
    public Vector2Int Pos => new Vector2Int(X, Y);

    public Tile(World world, int x, int y)
    {
        Init_Tile(world, x, y);
    }
    
    public void Init_Tile(World world, int x, int y, TileType tType = TileType.UNINITIALIZED)
    {
        this.world = world;
        this.X = x;
        this.Y = y;
        TileType = tType;
    }

    /// <summary>
    /// Returns true if you can enter this tile in this momemnt
    /// </summary>
    /// <returns></returns>
    public Enterability IsEnterable()
    {
        if (movementCost == 0)
        {
            return Enterability.Never;
        }

        if (furniture != null && furniture.IsEnterable != null)
        {
            return furniture.IsEnterable(furniture);
        } 

        return Enterability.Yes;
    }
    public bool TryAssignFurniture(Furniture objInstance)
    {
        if (objInstance == null)
        {
            //Remove Installed Object
            furniture = null;
            OnTileTypeChanged?.Invoke(this);
            return true;
        }

        if (furniture != null)
        {
            Debug.LogError($"Tried to Install Furniture {objInstance.objectType} on tile {X},{Y} that already has an {furniture.objectType} on it!");
            return false;
        }

        furniture = objInstance;
        return true;
    }
    
    public bool TryAssignInventory(Inventory inv)
    {
        if (inv == null)
        {
            //Remove Installed Object
            inventory = null;
            OnTileTypeChanged?.Invoke(this);
            return true;
        }

        if (inventory != null)
        {
            if (inventory.objectType != inv.objectType)
            {
                Debug.LogError($"Tried to Install Inventory {inv.objectType} on tile {X},{Y} that already has an {inventory.objectType} on it!");
                return false;
            } 

            int numToMove = inv.stackSize;
            if (inventory.stackSize + numToMove > inventory.maxStackSize)
            {   //We'll only add the amount that makes us reach the max stack size
                numToMove = inventory.maxStackSize - inventory.stackSize;
            }

            inventory.stackSize += numToMove;
            inv.stackSize -= numToMove;
            
            return true;
        }

        //inventory is null. Can't just directly assign it because
        //Inventory manager needs to know it was created.
        inventory = inv.Clone();
        inventory.tile = this;
        inventory.CallOnChanged(); //Should this be OnCreated???
        
        //We do this as a type of "return value" in InventoryManager.PlaceInventory.
        //We check if inv.stackSize == 0, then remove it.
        //inv.stackSize = 0;  
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

    #region SAVE_AND_LOAD

    public XmlSchema GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
        reader.MoveToAttribute("Type");
        _tileType = (TileType)reader.ReadContentAsInt();
        OnTileTypeChanged?.Invoke(this);
        
        // We don't init the tile because it's already been given it's world + x & y coordinate
        // During the World's init. SO WE DON'T DO THIS!
        // reader.MoveToAttribute("X");
        // int x = reader.ReadContentAsInt();
        //
        // reader.MoveToAttribute("Y");
        // int y = reader.ReadContentAsInt();
        // Init_Tile(null, x, y, tileType);
    }

    public void WriteXml(XmlWriter writer)
    {
        //Debug.Log("Tile::WriteXML");
        
        writer.WriteAttributeString("X", X.ToString());
        writer.WriteAttributeString("Y", Y.ToString());
        writer.WriteAttributeString("Type", ((int)_tileType).ToString());
    }
    
    #endregion
}
