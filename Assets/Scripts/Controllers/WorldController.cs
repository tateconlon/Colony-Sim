using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldController : MonoBehaviour
{
    public static WorldController Instance { get; protected set; }
    
    public World World { get; protected set; }

    static bool loadWorld = false;  //statics persist across scene loads so
    void OnEnable()
    {
        if (Instance != null)
        {
            Debug.LogError("WorldController Singleton already Exists!");
        }

        Instance = this;

        if (loadWorld)
        {
            LoadWorldFromSave();
        }
        else
        {
            CreateEmptyWorld();
        }
        
    }

    void Update()
    {
        World.Update(Time.deltaTime);
        // if (World.jobQueue.TryPeek(out Job j))
        // {
        //     j.DoWork(Time.deltaTime);
        // }
    }


    public static Vector2Int ScreenToTileCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x + 0.5f);
        int y = Mathf.FloorToInt(coord.y + 0.5f);
        return new Vector2Int(x, y);
    }

    public void SetupPathfindingTest()
    {
        World.SetupPathfindingExample();

        Path_TileGraph graph = new Path_TileGraph(World);
    }
    
    public void CreateNewWorld(bool fromSave)
    {
        loadWorld = fromSave;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void CreateEmptyWorld()
    {
        World = new World(100, 100);

        Camera.main.transform.position = new Vector3(World.Width / 2, World.Height/2, Camera.main.transform.position.z);
    }

    void LoadWorldFromSave()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(World));
        
        // Reading the XML from the file
        using (FileStream fileStream = new FileStream(CONST.SAVE_FILE_PATH, FileMode.Open))
        {
            World = (World)serializer.Deserialize(fileStream);
        }
        
        Camera.main.transform.position = new Vector3(World.Width / 2, World.Height/2, Camera.main.transform.position.z);
    }

    public void SaveWorld()
    {
        Debug.Log("Save World button was clicked");

        XmlSerializer serializer = new XmlSerializer(typeof(World));
        XmlWriterSettings settings = new() { Indent = true };
        using (XmlWriter xmlWriter = XmlWriter.Create(CONST.SAVE_FILE_PATH, settings))
        {
            xmlWriter.WriteStartDocument();
            serializer.Serialize(xmlWriter, World);
            xmlWriter.WriteEndDocument();
        }
    }
}

