using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InventoryManager
{
    // static InventoryManager _instance;
    // public static InventoryManager Instance
    // {
    //     get
    //     {
    //         if (_instance == null)
    //         {
    //             _instance = new InventoryManager();
    //         }
    //         return _instance;
    //     }
    // }

    public InventoryManager()
    {
        inventories = new();
    }

    //List of all "live" inventories.
    //Later can be organized into rooms to supplement "master" list
    public Dictionary<string, List<Inventory>> inventories;

    public event Action<Inventory> OnInventoryCreated;
    public event Action<Inventory> OnInventoryDeleted;

    //We delete stacks that are 0
    public void CleanupInventory(Inventory inv)
    {
        if (inv.stackSize > 0)
        {
            return;
        }
        
        if (inventories.ContainsKey(inv.objectType))    //Only need to check key because then there's a list we can remove from
        {
            inventories[inv.objectType].Remove(inv);
        }

        if (inv.tile != null)
        {
            inv.tile.TryAssignInventory(null);
            inv.tile = null;
        }

        if (inv.character != null)
        {
            inv.character.inventory = null;
        }
        
        Debug.Log($"Deleted Inventory {inv.objectType}");
        OnInventoryDeleted?.Invoke(inv);
    }
    
    public bool PlaceInventory(Tile tile, Inventory sourceInv)
    {
        if (tile == null) { return false;}
            
        bool tileWasEmpty = tile._inventory == null;
        if (!tile.TryAssignInventory(sourceInv))
        {
            return false;
        }
        
        //We deleted inventory
        if(sourceInv == null) {return true;}

        if (tileWasEmpty)
        {
            OnInventoryCreated?.Invoke(tile._inventory);
        }

        if (!inventories.ContainsKey(sourceInv.objectType))
        {
            inventories[sourceInv.objectType] = new();
        }
        
        // Could happen if we merge inv into the tile's _inventory.
        // ex: placing 10 steel_plates on top of 20 steel_plates
        if (sourceInv.stackSize == 0)
        {
            CleanupInventory(sourceInv);
        }

        if (tileWasEmpty)
        {
            inventories[sourceInv.objectType].Add(tile._inventory);   //Why don't we just use inv??
        }
        return true;
    }
    
    public bool PlaceInventory(Job job, Inventory sourceInv)
    {
        if (job == null) { return false;}

        //It doesn't make sense to "deposit" nothing into a job
        if (sourceInv == null)
        {
            Debug.LogError("Trying to add null Inventory to job"); 
            return false;
        }   
        
        if (!job.recipe.ContainsKey(sourceInv.objectType)) {return false;} //Do not want the inv

        Inventory jobInv = job.recipe[sourceInv.objectType];
        
        int combinedCount = jobInv.stackSize + sourceInv.stackSize;
        jobInv.stackSize = Mathf.Clamp(combinedCount, 0, jobInv.maxStackSize);
        
        int overflow = combinedCount - jobInv.maxStackSize;
        sourceInv.stackSize = Mathf.Max(0, overflow);

        // Could happen if we merge inv into the tile's _inventory.
        // ex: placing 10 steel_plates on top of 20 steel_plates
        // Similar to "deleting" the inventory
        if (sourceInv.stackSize == 0)
        {
            CleanupInventory(sourceInv);
        }
        
        return true;
    }
    
    public bool PlaceInventory(Character character, Inventory sourceInv, int amount = -1)
    {
        if (character == null) { return false;}
        if (amount == -1)
        {
            amount = sourceInv.stackSize;
        }

        bool characterWasEmpty = character.inventory == null;
        if (!character.TryAssignInventory(sourceInv, amount))
        {
            return false;
        }
        
        //We deleted inventory
        if(sourceInv == null) {return true;}
        
        
        // We don't keep track of inventories that characters are holding
        // if (characterWasEmpty && character.inventory != null)
        // {
        //     OnInventoryCreated?.Invoke(character.inventory);
        // }

        // Could happen if we merge inv into the character's _inventory.
        // ex: placing 10 steel_plates on top of 20 steel_plates
        if (sourceInv.stackSize == 0)
        {
            CleanupInventory(sourceInv);
        }

        // if (characterWasEmpty)
        // {
        //     inventories[sourceInv.objectType].Add(character.inventory);   //Why don't we just use inv??
        // }
        return true;
    }
    
    

    /// <summary>
    /// Returns the closest Inventory that satisfied the desiredAmount of objectType.
    /// If it can't find the desired amont, then returns the biggest 
    /// </summary>
    /// <param name="objectType"></param>
    /// <param name="currTile"></param>
    /// <returns></returns>
    public Inventory GetClosestInventoryOfType(string objectType, Tile currTile, int desiredAmount)
    {
        //FIXME:
        //   a) We are LYING about returning the closest item
        //   b) There's no way to return the closest item in an optimal manner
        //      until our "inventories" database is more sophisticated.
        //      (ie: seperate tile inventory from character inventory and maybe
        //          has room content optimaization)
        
        //TODO: Check if inventory is reachable
        if (!inventories.ContainsKey(objectType)) {return null;} //No inventories in map!

        foreach (Inventory inv in inventories[objectType])
        {
            if (inv.tile != null)
            {
                return inv;
            }
        }

        return null;
    }
}