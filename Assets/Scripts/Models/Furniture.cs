//Things like walls, sofa, doors etc.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;

public class Furniture : IXmlSerializable
{
    protected Dictionary<string, float> furnParams = new();
    public Action<Furniture, float> updateActions;
    public Func<Furniture, Enterability> IsEnterable;

    //This represents the BASE tile of the object
    //However, large objects may actually OCCUPY multiple tiles
    public Tile tile { get; protected set; }

    //This is an id that can be used to query across the game
    public string objectType { get; protected set; }
    
    //Movement speed is 1/movementCost.
    //SPECIAL: 0 -> can't walk through it
    public float movementCost { get; protected set; }

    public bool roomEnclosure; //Does this help define the border of a room? ex: Wall, door, window

    int width;
    int height;

    public Color tint = Color.white;

    public Action<Furniture> OnChanged;

    List<Job> jobs = new(); //The furniture's jobs (like a workbench wanting someone to make something)
    
    Func<Tile, bool> funcPositionValidation;    //List of functions?
    
    public bool linksToNeighbours { get; protected set; }
    
    private Furniture(){} //Hide the constructor to force static use.

    //Copy Constructor
    protected Furniture(Furniture other)
    {
        this.objectType = other.objectType;
        this.movementCost = other.movementCost;
        this.width = other.width;
        this.height = other.height;
        this.linksToNeighbours = other.linksToNeighbours;
        this.roomEnclosure = other.roomEnclosure;
        this.tint = other.tint;

        this.furnParams = new Dictionary<string, float>(other.furnParams);
        if (other.updateActions != null)
        {
            foreach (Delegate del in other.updateActions.GetInvocationList())
            {
                this.updateActions += (Action<Furniture, float>)del;
            }
        }
        if (other.IsEnterable != null)
        {
            foreach (Delegate del in other.IsEnterable.GetInvocationList())
            {
                this.IsEnterable += (Func<Furniture, Enterability>)del;
            }
        }

        jobs = new List<Job>();
        foreach (Job otherJob in other.jobs)
        {
            jobs.Add(otherJob);
        }
    }

    public virtual Furniture Clone()
    {
        return new(this);
    }

    //TODO: Implement bigger objects
    //TODO: Implement rotation
    
    // This should only be used to make prototypes. Create furnitures via PlaceInstance.
    public Furniture(string objectType, float movementCost = 1f, int width = 1, int height = 1, bool linksToNeighbours = false, bool roomEnclosure = false)
    {
        this.objectType = objectType;
        this.movementCost = movementCost;
        this.width = width;
        this.height = height;
        this.linksToNeighbours = linksToNeighbours;
        this.roomEnclosure = roomEnclosure;
        this.funcPositionValidation = __IsValidPosition;   //Move these validation functions into static librariy

        furnParams = new();
    }

    public static Furniture PlaceInstance(Furniture prototype, Tile tileOwner)
    {
        if (!prototype.funcPositionValidation(tileOwner))
        {
            Debug.LogError($"Can't place {prototype.objectType} on tile {tileOwner.X},{tileOwner.Y}");
            return null;
        }
            
        Furniture retVal = new Furniture(prototype);
        retVal.tile = tileOwner;

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
            if (temp_N != null && temp_N.furniture != null && temp_N.furniture.objectType == retVal.objectType)
            {
                temp_N.furniture.OnChanged?.Invoke(temp_N.furniture);
            }
        
            Tile temp_E = t.world.GetTileAt(t.X + 1, t.Y);
            if (temp_E != null && temp_E.furniture != null && temp_E.furniture.objectType == retVal.objectType)
            {
                temp_E.furniture.OnChanged?.Invoke(temp_E.furniture);
            }
        
            Tile temp_S = t.world.GetTileAt(t.X, t.Y - 1);
            if (temp_S != null && temp_S.furniture != null && temp_S.furniture.objectType == retVal.objectType)
            {
                temp_S.furniture.OnChanged?.Invoke(temp_S.furniture);
            }
        
            Tile temp_W = t.world.GetTileAt(t.X - 1, t.Y);
            if (temp_W != null && temp_W.furniture != null && temp_W.furniture.objectType == retVal.objectType)
            {
                temp_W.furniture.OnChanged?.Invoke(temp_W.furniture);
            }
        }

        return retVal;
    }
    
    public bool IsPositionValid(Tile t)
    {
        return funcPositionValidation(t);
    }

    public void Update(float deltaTime)
    {
        updateActions?.Invoke(this, deltaTime);
    }

    public float GetParameter(string key, float defaultVal = 0)
    {
        if (furnParams.TryGetValue(key, out float retVal))
        {
            return retVal;
        }

        return defaultVal;
    }

    public void SetParameter(string key, float value)
    {
        furnParams["key"] = value;
    }
    
    public void ChangeParameter(string key, float value)
    {
        if (furnParams.ContainsKey("key"))
        {
            furnParams["key"] += value;
            return;
        }

        furnParams["key"] = value;
    }

    //TODO: Move these into static position library
    public bool __IsValidPosition(Tile tile)
    {
        if (tile.TileType != TileType.Floor) return false;
        if (tile.furniture != null) return false;
        
        return true;
    }

    public bool __IsValidPosition_Door(Tile tile)
    {
        return __IsValidPosition(tile);
    }

    public int JobCount()
    {
        return jobs.Count();
    }

    public void AddJob(Job j)
    {
        jobs.Add(j);
        tile.world.jobQueue.Enqueue(j);
    }

    public void RemoveJob(Job j)
    {
        if (jobs.Remove(j))
        {
            j.CancelJob();
            tile.world.jobQueue.Remove(j);
        }
    }

    public void ClearJobs()
    {
        foreach (Job j in jobs)
        {
            //j.CancelJob() ???
            RemoveJob(j);
        }
    }

    public bool IsStockpile()
    {
        return objectType == "stockpile";
    }

    #region SAVING_AND_LOADING
    
    public XmlSchema GetSchema()
    {
        throw new NotImplementedException();
    }

    public void ReadXml(XmlReader reader)
    {
        //X, Y, and objectType have already bee set, and we should already
        //be assigned to a tile. So just populate the extra data.
        
        //movementCost = int.Parse(reader.GetAttribute("movementCost")); We get this from the Prototype/Config file

        if (reader.ReadToDescendant("Param"))
        {
            do
            {
                string k = reader.GetAttribute("name");
                float v = float.Parse(reader.GetAttribute("value"));
                furnParams[k] = v;
            } while (reader.ReadToNextSibling("Param"));
        }
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", tile.X.ToString());
        writer.WriteAttributeString("Y", tile.Y.ToString());
        writer.WriteAttributeString("objectType", objectType);
        //writer.WriteAttributeString("movementCost", movementCost.ToString());

        foreach (string k in furnParams.Keys)
        {
            writer.WriteStartElement("Param");
            writer.WriteAttributeString("name", k);
            writer.WriteAttributeString("value", furnParams[k].ToString());
            writer.WriteEndElement();
        }
    }
    
    #endregion
}