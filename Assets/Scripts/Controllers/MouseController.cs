using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class MouseController : MonoBehaviour
{
    public static MouseController Instance;
    [SerializeField] GameObject selectionCursorPrefab;
    [SerializeField] float zoomSpeed = 0.2f;
    
    Vector3 currFramePos;
    Vector3 lastFramePos;
    
    Vector3 dragStartPos;

    bool isBuildModeObjects = false;
    TileType buildModeTile = TileType.Floor;
    string buildMode_FurnitureType = "";

    public void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Two MouseControllers present in Scene!");
        }
        Instance = this;
    }

    public void Start()
    {
        SimplePool.Preload(selectionCursorPrefab, 100);
    }
    
    public void Update()
    {
        currFramePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currFramePos.z = 0;

        //UpdateCursor_UPDATE();
        UpdateCursorDrag_UPDATE();
        UpdateCameraDrag_UPDATE();
        UpdateCameraZoom_UPDATE();

        //COOL
        //TODO: Camera.main will not work with multiple cameras if they're not tagged with "Main Camera" tag.
        //Have to recast incase the camera has moved
        lastFramePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastFramePos.z = 0;
    }
    //
    // void UpdateCursor_UPDATE()
    // {
    //     //Update selector position
    //     Tile tileUnderMouse = WorldController.Instance.GetTileAtWorldCoord(currFramePos);
    //     if (tileUnderMouse == null)
    //     {
    //         selectionCursor.SetActive(false);
    //     }
    //     else
    //     {
    //         selectionCursor.SetActive(true);
    //         Vector3 selectionCursorPos = new Vector3(tileUnderMouse.X, tileUnderMouse.Y, 0);
    //         selectionCursor.transform.position = selectionCursorPos;
    //     }
    // }

    private Dictionary<Tile, GameObject> dragPreviewGO = new();
    private bool isDragging = false;
    void UpdateCursorDrag_UPDATE()
    { //Start Left Drag
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            isDragging = true;
            dragStartPos = currFramePos;
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 dragEndPos = currFramePos;

            Vector2Int dragStartTileCoords = WorldController.ScreenToTileCoord(dragStartPos);
            Vector2Int dragEndTileCoords = WorldController.ScreenToTileCoord(dragEndPos);

            int x_start = Mathf.Min(dragStartTileCoords.x, dragEndTileCoords.x);
            int y_start = Mathf.Min(dragStartTileCoords.y, dragEndTileCoords.y);
            int x_end = Mathf.Max(dragStartTileCoords.x, dragEndTileCoords.x);
            int y_end = Mathf.Max(dragStartTileCoords.y, dragEndTileCoords.y);

            foreach (KeyValuePair<Tile,GameObject> tile_GO in dragPreviewGO)
            {
                SimplePool.Despawn(tile_GO.Value);
            }
            dragPreviewGO.Clear();
            
            for (int x = x_start; x <= x_end; x++)
            {
                for (int y = y_start; y <= y_end; y++)
                {
                    Tile tile = WorldController.Instance.World.GetTileAt(x, y);
                    if (tile != null && !dragPreviewGO.ContainsKey(tile))
                    {
                        GameObject go = SimplePool.Spawn(selectionCursorPrefab, new Vector3(x, y), Quaternion.identity);
                        dragPreviewGO.Add(tile, go);
                    }
                }
            }
        }
        
        //End Left Drag
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            Vector3 dragEndPos = currFramePos;

            List<Tile> tiles = dragPreviewGO.Keys.ToArray().ToList();
            BuildModeController.Instance.DoBuild(tiles.ToList());

            foreach (GameObject tile_GO in dragPreviewGO.Values)
            {
                SimplePool.Despawn(tile_GO);
            }
            
            dragPreviewGO.Clear();
        }
    }

    public Vector3 GetMousePosition()
    {
        return currFramePos;
    }

    public Tile GetTileUnderMouse()
    {
        Vector2Int tileCoords = WorldController.ScreenToTileCoord(currFramePos);
        return WorldController.Instance.World.GetTileAt(tileCoords.x, tileCoords.y);
    }

    void UpdateCameraDrag_UPDATE()
    {
        //Handle screen dragging
        if (Input.GetMouseButton(2) || Input.GetMouseButton(1))    //Middle or right click
        {
            Vector3 displacement = -(currFramePos - lastFramePos);
            Camera.main.transform.Translate(displacement);
        }
    }

    void UpdateCameraZoom_UPDATE()
    {
        float zoom = Camera.main.orthographicSize + Camera.main.orthographicSize * -Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        Camera.main.orthographicSize = Mathf.Clamp(zoom, 3f, 25);
    }
}