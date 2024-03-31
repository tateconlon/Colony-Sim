//Things like walls, sofa, doors etc.

using System;
using UnityEngine;

public class Furniture
{
    //This represents the BASE tile of the object
    //However, large objects may actually OCCUPY multiple tiles
    public Tile tileOwner { get; protected set; }

    //This is an id that can be used to query across the game
    public string objectType { get; protected set; }
    
    //Movement speed is 1/movementCost.
    //SPECIAL: 0 -> can't walk through it
    public float movementCost { get; protected set; }

    int width;
    int height;

    public event Action<Furniture> OnChanged;
    
    Func<Tile, bool> funcPositionValidation;    //List of functions?
    
    public bool linksToNeighbours { get; protected set; }
    
    private Furniture(){} //Hide the constructor to force static use
    
    //TODO: Implement bigger objects
    //TODO: Implement rotation
    
    public static Furniture CreatePrototype(string objectType, float movementCost = 1f, int width = 1, int height = 1, bool linksToNeighbour = false)
    {
        Furniture retVal = new Furniture();
        
        retVal.objectType = objectType;
        retVal.movementCost = movementCost;
        retVal.width = width;
        retVal.height = height;
        retVal.linksToNeighbours = linksToNeighbour;
        retVal.funcPositionValidation = retVal.__IsValidPosition;   //Move this into static librariy

        return retVal;
    }

    
    public static Furniture PlaceInstance(Furniture prototype, Tile tileOwner)
    {
        if (!prototype.funcPositionValidation(tileOwner))
        {
            Debug.LogError($"Can't place {prototype.objectType} on tile {tileOwner.X},{tileOwner.Y}");
            return null;
        }
            Furniture retVal = new Furniture();
        
        retVal.objectType = prototype.objectType;
        retVal.movementCost = prototype.movementCost;
        retVal.width = prototype.width;
        retVal.height = prototype.height;
        retVal.linksToNeighbours = prototype.linksToNeighbours;
        retVal.tileOwner = tileOwner;

        if (!tileOwner.TryAssignFurniture(retVal))
        {
            //Couldn't place tile (it was probably occupied)
            return null;
        }

        //When we place it, let our linkedNeighbours know
        //They may need to change Graphically
        if (retVal.linksToNeighbours)
        {
            Tile t = tileOwner;
        
            Tile temp_N = t.world.GetTileAt(t.X, t.Y + 1);
            if (temp_N != null && temp_N.Furniture != null && temp_N.Furniture.objectType == retVal.objectType)
            {
                temp_N.Furniture.OnChanged?.Invoke(temp_N.Furniture);
            }
        
            Tile temp_E = t.world.GetTileAt(t.X + 1, t.Y);
            if (temp_E != null && temp_E.Furniture != null && temp_E.Furniture.objectType == retVal.objectType)
            {
                temp_E.Furniture.OnChanged?.Invoke(temp_E.Furniture);
            }
        
            Tile temp_S = t.world.GetTileAt(t.X, t.Y - 1);
            if (temp_S != null && temp_S.Furniture != null && temp_S.Furniture.objectType == retVal.objectType)
            {
                temp_S.Furniture.OnChanged?.Invoke(temp_S.Furniture);
            }
        
            Tile temp_W = t.world.GetTileAt(t.X - 1, t.Y);
            if (temp_W != null && temp_W.Furniture != null && temp_W.Furniture.objectType == retVal.objectType)
            {
                temp_W.Furniture.OnChanged?.Invoke(temp_W.Furniture);
            }
        }

        return retVal;
    }
    
    public bool IsPositionValid(Tile t)
    {
        return funcPositionValidation(t);
    }

    //TODO: Move these into static position library
    public bool __IsValidPosition(Tile tile)
    {
        if (tile.TileType != TileType.Floor) return false;
        if (tile.Furniture != null) return false;
        
        return true;
    }

    public bool __IsValidPosition_Door(Tile tile)
    {
        return __IsValidPosition(tile);
    }
    
}