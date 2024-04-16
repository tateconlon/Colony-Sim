 using System;
 using System.Collections.Generic;
 using System.ComponentModel;
 using UnityEngine;
 using UnityEngine.Android;

 public class Job
 {
     //This class holds info for a queued up job, which can include
     //things like placing furniture, moving stored inventory
     // working at a desk, and maybe even fighting enemies.
     
     public Tile tile;
     float jobTime;

     //FIXME: This will change since jobs can be more than just furniture
     public string jobType { get; protected set; }

     public Action<Job> OnJobCompleted;
     public Action<Job> OnJobCancelled;
     public Action<Job> OnJobAbandoned; //Use Scriptable objects?

     public Dictionary<string, Inventory> recipe;

     public Job(Tile tile, Action<Job> onJobCompleted, string jobType, float jobTime, Inventory[] inventoryReqs)
     {
         this.tile = tile;
         this.OnJobCompleted += onJobCompleted;
         this.jobType = jobType;
         this.jobTime = jobTime;

         recipe = new Dictionary<string, Inventory>();
         if (inventoryReqs != null)
         {
             foreach (Inventory inventoryReq in inventoryReqs)
             {
                 if (!recipe.ContainsKey(inventoryReq.objectType))
                 { 
                     recipe[inventoryReq.objectType] = inventoryReq.Clone();
                 }
                 else
                 {
                     recipe[inventoryReq.objectType].maxStackSize += inventoryReq.maxStackSize;
                     recipe[inventoryReq.objectType].stackSize += inventoryReq.stackSize;    //stackSize for a req should always be 0, but I'm including it anyways just in case.
                 }
             }
         }
     }

     protected Job(Job other)
     {
         this.tile = other.tile;
         this.OnJobCompleted = other.OnJobCompleted;
         this.jobType = other.jobType;
         this.jobTime = other.jobTime;

         recipe = new Dictionary<string, Inventory>();
         foreach (KeyValuePair<string,Inventory> kv in other.recipe)
         {
             recipe[kv.Key] = kv.Value.Clone();
         }
     }
     
     virtual public Job Clone()
     {
         return new Job(this);
     }

     public void DoWork(float workTime)
     {
         jobTime -= workTime;
     
         if (jobTime <= 0)
         {
             OnJobCompleted?.Invoke(this);
             tile.pendingFurnitureJob = null;
             //WorldController.Instance.World.jobQueue.TryDequeue();
         }
     }

     public void CancelJob()
     {
         Debug.Log("Cancelling Job");
         OnJobCancelled?.Invoke(this);
     }
     
     public void AbandonJob(Job j)
     {
         OnJobAbandoned?.Invoke(this);
     }

     public bool HasAllMaterial()
     {
         foreach (Inventory inv in recipe.Values)
         {
             if (inv.maxStackSize > inv.stackSize)
             {
                 return false;
             }
         }

         return true;
     }

     public bool DesiresInventoryType(Inventory inv, out Inventory recipeInventory)
     {
         recipeInventory = null;
         if (inv == null)
         {
             return false;
         }

         if (recipe.ContainsKey(inv.objectType) == false)
         {
             return false;
         }

         if (recipe[inv.objectType].stackSize >= recipe[inv.objectType].maxStackSize)
         {
             return false; //we have enough!
         }
         
         //inventory is of type we want, and we need more
         recipeInventory = recipe[inv.objectType].Clone();
         return true;
     }

     public List<Inventory> GetDesiredInventories()
     {
         List<Inventory> retVal = new();
         foreach (Inventory inv in recipe.Values)
         {
             if (inv.stackSize != inv.maxStackSize)
             {
                 retVal.Add(inv);
             }
         }

         return retVal;
     }
 }