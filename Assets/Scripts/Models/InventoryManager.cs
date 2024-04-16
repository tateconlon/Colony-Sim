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

    public bool PlaceInventory(Tile tile, Inventory inv)
    {
        if (tile == null) { return false;}
            
        bool tileWasEmpty = tile._inventory == null;
        if (!tile.TryAssignInventory(inv))
        {
            return false;
        }

        if (!inventories.ContainsKey(inv.objectType))
        {
            inventories[inv.objectType] = new();
        }
        
        // Could happen if we merge inv into the tile's _inventory.
        // ex: placing 10 steel_plates on top of 20 steel_plates
        if (inv.stackSize == 0)
        {
            inventories[inv.objectType].Remove(inv);
        }

        if (tileWasEmpty)
        {
            inventories[inv.objectType].Add(tile._inventory);   //Why don't we just use inv??
        }
        return true;
    }
    
    public bool PlaceInventory(Job job, Inventory inv)
    {
        if (job == null) { return false;}
        if (!job.inventoryRequirements.ContainsKey(inv.objectType)) {return false;} //Do not want the inv

        Inventory jobInv = job.inventoryRequirements[inv.objectType];
        
        int combinedCount = jobInv.stackSize + inv.stackSize;
        jobInv.stackSize = Mathf.Clamp(combinedCount, 0, jobInv.maxStackSize);
        
        int overflow = combinedCount - jobInv.maxStackSize;
        inv.stackSize = Mathf.Max(0, overflow);

        // Could happen if we merge inv into the tile's _inventory.
        // ex: placing 10 steel_plates on top of 20 steel_plates
        // Similar to "deleting" the inventory
        if (inv.stackSize == 0)
        {
            inventories[inv.objectType].Remove(inv);
        }
        
        return true;
    }
}