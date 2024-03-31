using System;
using UnityEngine;

public class SoundController : MonoBehaviour
{
    //A hacky way to make sure we don't duplicate sound events so sounds don't get loud
    //Because they're called multiple times a frame (ie: putting down lots of floors)
    private float soundCooldown = 0.02f;    //FIXME: make this a const
    void Start()
    {
        WorldController.Instance.World.OnFurnitureCreated += World_OnFurnitureCreated;
        WorldController.Instance.World.OnTileChangedEvent += World_OnTileChangedEvent;
    }

    void Update()
    {
        soundCooldown -= Time.deltaTime;
    }

    private void World_OnTileChangedEvent(Tile obj)
    {
        if (soundCooldown > 0) return;
        
        AudioClip ac = Resources.Load<AudioClip>("Sounds/floor_OnCreated");
        AudioSource.PlayClipAtPoint(ac, Camera.main.transform.position);

        soundCooldown = 0.02f;
    }

    private void World_OnFurnitureCreated(Furniture obj)
    {
        if (soundCooldown > 0) return;

        AudioClip ac = Resources.Load<AudioClip>($"Sounds/{obj.objectType}_OnCreated");
        if (ac == null)
        {
            Debug.Log($"No OnCreated sound for {obj.objectType} ex: Sounds/{obj.objectType}_OnCreated. Defaulting to wall_OnCreated");
            ac = Resources.Load<AudioClip>($"Sounds/wall_OnCreated");
        }
        
        AudioSource.PlayClipAtPoint(ac, Camera.main.transform.position);
        
        soundCooldown = 0.02f;
    }

    private void OnDisable()
    {
        WorldController.Instance.World.OnFurnitureCreated -= World_OnFurnitureCreated;
        WorldController.Instance.World.OnTileChangedEvent -= World_OnTileChangedEvent;
    }
}