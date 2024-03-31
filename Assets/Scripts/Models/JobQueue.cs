using System;
using System.Collections.Generic;

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

}