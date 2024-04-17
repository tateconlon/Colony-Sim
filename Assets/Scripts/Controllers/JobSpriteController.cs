using System.Collections.Generic;
using System.Linq;
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
        if (_job.jobType == null)
        {
            //job has no assosiated sprite, just return
            return;
        }
        
        if (job_GameObject_Map.ContainsKey(_job))
        {
            //Debug.Log("OnJobCreated for a jobGO that already exists -- most like due to a job being re-queded instead of created");
            return;
        }
        //Make Sprite
        Sprite sprite = fsc.GetSpriteForFurniture(_job.jobType);
        
        //Do visuals
        Tile tile_data = _job.tile;
                
        GameObject jobGO = new GameObject();
        jobGO.name = $"{_job.jobType}_{tile_data.X}_{tile_data.Y}";
        jobGO.transform.position = new Vector3(tile_data.X, tile_data.Y, 0);
        jobGO.transform.SetParent(transform);
        
        //FIXME: this is hardcoded for doors, but it should not be eventually
        //      Plus it doesn't update at runtime
        if (_job.jobType == "door")
        {
            //If door is placed between north and south walls, rotate it 90 degrees.
            //Doors in the model space do not care about their orientation/rotation
            //It's only a visual thing

            Tile tile_North = _job.tile.world.GetTileAt(_job.tile.X, _job.tile.Y + 1);
            Tile tile_South = _job.tile.world.GetTileAt(_job.tile.X, _job.tile.Y - 1);
            if ((tile_North != null && tile_South != null)
                && (tile_North.furniture != null && tile_South.furniture != null)
                && (tile_North.furniture.objectType == "wall" && tile_South.furniture.objectType == "wall"))
            {
                jobGO.transform.rotation *= Quaternion.AngleAxis(90, Vector3.forward);
            }
        }
        
        SpriteRenderer furn_sr = jobGO.AddComponent<SpriteRenderer>();
        furn_sr.sortingLayerName = JOB_SORTING_LAYER_NAME;
        furn_sr.sprite = sprite;
        furn_sr.color = Color.green - Color.black * 0.75f;   //25% transparency

        job_GameObject_Map.TryAdd(_job, jobGO);


        _job.OnJobCompleted += OnJobEnded;
        _job.OnJobCancelled += OnJobEnded;
    }

    void OnJobEnded(Job _job)
    {
        if (job_GameObject_Map.TryGetValue(_job, out GameObject go))
        {
            Destroy(go);
            job_GameObject_Map.Remove(_job);
            
            _job.OnJobCompleted -= OnJobEnded;
            _job.OnJobCancelled -= OnJobEnded;
            
            return;
        }
        
        
        Debug.LogError($"Could not remove job {_job} from job_GameObject_Map. Could not find it!");
    }
}