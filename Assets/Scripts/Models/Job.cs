 using System;
 using System.Collections.Generic;
 using System.ComponentModel;
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

     public Action<Job> onJobCompleted;
     public Action<Job> OnJobCancelled;
     public Action<Job> OnJobAbandoned; //Use Scriptable objects?

     public Dictionary<string, Inventory> inventoryRequirements;

     public Job(Tile tile, Action<Job> onJobCompleted, string jobType, float jobTime, Inventory[] inventoryReqs)
     {
         this.tile = tile;
         this.onJobCompleted += onJobCompleted;
         this.jobType = jobType;
         this.jobTime = jobTime;

         inventoryRequirements = new Dictionary<string, Inventory>();
         if (inventoryReqs != null)
         {
             foreach (Inventory inventoryReq in inventoryReqs)
             {
                 if (!inventoryRequirements.ContainsKey(inventoryReq.objectType))
                 { 
                     inventoryRequirements[inventoryReq.objectType] = inventoryReq.Clone();
                 }
                 else
                 {
                     inventoryRequirements[inventoryReq.objectType].maxStackSize += inventoryReq.maxStackSize;
                     inventoryRequirements[inventoryReq.objectType].stackSize += inventoryReq.stackSize;    //stackSize for a req should always be 0, but I'm including it anyways just in case.
                 }
             }
         }
     }

     protected Job(Job other)
     {
         this.tile = other.tile;
         this.onJobCompleted = other.onJobCompleted;
         this.jobType = other.jobType;
         this.jobTime = other.jobTime;

         inventoryRequirements = new Dictionary<string, Inventory>();
         foreach (KeyValuePair<string,Inventory> kv in inventoryRequirements)
         {
             inventoryRequirements[kv.Key] = kv.Value.Clone();
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
             onJobCompleted?.Invoke(this);
             tile.pendingFurnitureJob = null;
             //WorldController.Instance.World.jobQueue.TryDequeue();
         }
     }

     public void CancelJob()
     {
         OnJobCancelled?.Invoke(this);
     }
     
     public void AbandonJob(Job j)
     {
         OnJobAbandoned?.Invoke(this);
     }

     public bool HasAllMaterial()
     {
         foreach (Inventory inv in inventoryRequirements.Values)
         {
             if (inv.maxStackSize > inv.stackSize)
             {
                 return false;
             }
         }

         return true;
     }

     public bool DesiresInventoryType(Inventory inv)
     {
         if (inv == null)
         {
             return false;
         }

         if (inventoryRequirements.ContainsKey(inv.objectType) == false)
         {
             return false;
         }

         if (inventoryRequirements[inv.objectType].stackSize >= inventoryRequirements[inv.objectType].maxStackSize)
         {
             return false; //we have enough!
         }

         //inventory is of type we want, and we need more
         return true;
     }
 }