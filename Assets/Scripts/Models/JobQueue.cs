using System;
using System.Collections.Generic;
using UnityEngine;

public class JobQueue
{
    Queue<Job> jobQueue;
    public int Count => jobQueue.Count;

    public event Action<Job> OnJobCreated;
    // public event Action<Job> OnJobRemoved;

    public JobQueue()
    {
        jobQueue = new();
    }

    public void Enqueue(Job j)
    {
        if (j.jobTime < 0)
        {   //Negative job times are not supposed to be queued
            j.DoWork(0f);   //Completes the job
            return;
        }
        jobQueue.Enqueue(j);
        
        OnJobCreated?.Invoke(j);
    }
    
    public bool TryDequeue(out Job j)
    {
        bool succ = jobQueue.TryDequeue(out j);
        if (succ)
        {
            j.OnJobAbandoned += Enqueue;
        }

        return succ;
        // OnJobRemoved?.Invoke(j);
    }

    public bool TryPeek(out Job j)
    {
        return jobQueue.TryPeek(out j);
    }

    public void Remove(Job j)
    {
        List<Job> jobs = new List<Job>(jobQueue);
        if (!jobs.Remove(j))
        {
            //Job may have finished being worked by a character 
            Debug.Log($"Tried to remove Job {j.jobType} but it wasn't in the job queue");
        }
        foreach (Job job in jobs)
        {
            jobQueue.Enqueue(job);
        }
    }

}