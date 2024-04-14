 using System;
 using System.ComponentModel;

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

     public Job(Tile tile, Action<Job> onJobCompleted, string jobType, float jobTime = 0.5f)
     {
         this.tile = tile;
         this.onJobCompleted += onJobCompleted;
         this.jobType = jobType;
         this.jobTime = jobTime;
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
 }