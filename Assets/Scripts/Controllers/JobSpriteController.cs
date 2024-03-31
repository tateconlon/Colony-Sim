using System.Collections.Generic;
using UnityEngine;
public class JobSpriteController : MonoBehaviour
{
    const string JOB_SORTING_LAYER_NAME = "JOB";
    FurnitureSpriteController fsc;

    Dictionary<Job, GameObject> job_GameObject_Map = new();

    void Start()
    {
        fsc = GameObject.FindObjectOfType<FurnitureSpriteController>();
        
        WorldController.Instance.World.jobQueue.OnJobCreated += OnJobCreated;
    }

    void OnJobCreated(Job _job)
    {
        //Make Sprite
        Sprite sprite = fsc.GetSpriteForFurniture(_job.jobType);
        
        //Do visuals
        Tile tile_data = _job.tile;
                
        GameObject jobGO = new GameObject();
        jobGO.name = $"{_job.jobType}_{tile_data.X}_{tile_data.Y}";
        jobGO.transform.position = new Vector3(tile_data.X, tile_data.Y, 0);
        jobGO.transform.SetParent(transform);
        
        SpriteRenderer furn_sr = jobGO.AddComponent<SpriteRenderer>();
        furn_sr.sortingLayerName = JOB_SORTING_LAYER_NAME;
        furn_sr.sprite = sprite;
        furn_sr.color = Color.green - Color.black * 0.75f;   //25% transparency

        job_GameObject_Map.TryAdd(_job, jobGO);


        _job.onJobCompleted += OnJobEnded;
        _job.OnJobCancelled += OnJobEnded;
    }

    void OnJobEnded(Job _job)
    {
        if (job_GameObject_Map.TryGetValue(_job, out GameObject go))
        {
            Destroy(go);
            job_GameObject_Map.Remove(_job);
            
            _job.onJobCompleted -= OnJobEnded;
            _job.OnJobCancelled -= OnJobEnded;
            
            return;
        }
        
        
        Debug.LogError($"Could not remove job {_job} from job_GameObject_Map. Could not find it!");
    }
}