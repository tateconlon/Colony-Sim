using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
public class InventorySpriteController : MonoBehaviour
{
    const string INVENTORY_SORTING_LAYER_NAME = "INVENTORY";
    const string INVENTORY_RESOURCE_PATH = "Images/Inventory/";

    public GameObject InventoryUIPrefab;
    
    Dictionary<Inventory, GameObject> inventory_GameObject_Map = new();
    
    Dictionary<string, Sprite> inventorySprites;

    World _world;

    void Start()
    {
        _world = WorldController.Instance.World;
        
        LoadSprites();

        foreach (string inventoryType in _world.inventoryManager.inventories.Keys)
        {
            foreach (Inventory inv in _world.inventoryManager.inventories[inventoryType])
            {
                OnInventoryCreated(inv);
            }
        }

        _world.inventoryManager.OnInventoryCreated += OnInventoryCreated;
        _world.inventoryManager.OnInventoryDeleted += OnInventoryDeleted;
    }
    
    void LoadSprites()
    {
        inventorySprites = new();
        Sprite[] sprites = Resources.LoadAll<Sprite>(INVENTORY_RESOURCE_PATH);
        foreach (Sprite sprite in sprites)
        {
            // Debug.Log(sprite.name);
            inventorySprites.Add(sprite.name, sprite);
        }
    }

    //Create visuals GameObject
    void OnInventoryCreated(Inventory inv)
    {
        if (inventory_GameObject_Map.ContainsKey(inv))
        {
            Debug.LogError($"Trying to create inventory {inv}, but it already exists!");
            return;
        }

        GameObject inv_go = new GameObject($"Inventory {inv.objectType}",
            typeof(SpriteRenderer));
            
        inv_go.transform.SetParent(transform);
        inv_go.transform.position = inv.tile.Pos.V3();
            
        SpriteRenderer inv_sr = inv_go.GetComponent<SpriteRenderer>();
        inv_sr.sortingLayerName = INVENTORY_SORTING_LAYER_NAME;
        inv_sr.sprite = inventorySprites[inv.objectType];

        if (inv.maxStackSize > 1)
        {
            GameObject ui_go = Instantiate(InventoryUIPrefab, inv_go.transform);
            TextMeshProUGUI text = ui_go.GetComponentInChildren<TextMeshProUGUI>();
            text.text = inv.stackSize.ToString();
        }
        
        inventory_GameObject_Map.Add(inv, inv_go);
        inv.OnChanged += OnInventoryChanged;
    }
    
    //Create visuals GameObject
    void OnInventoryDeleted(Inventory inv)
    {
        if (!inventory_GameObject_Map.ContainsKey(inv))
        {
            Debug.LogError($"Trying to destroy inventory {inv}, but it already exists!");
            return;
        }

        Destroy(inventory_GameObject_Map[inv]);
        inv.OnChanged -= OnInventoryChanged;
    }
    
    
    void OnInventoryChanged(Inventory inv)
    {
        if (!inventory_GameObject_Map.TryGetValue(inv, out GameObject inv_go))
        {
            Debug.LogError($"Could not find Inventory {inv}'s Game Object");
            return;
        }

        inv_go.transform.position = inv.tile.Pos.V3();
        if (inv.maxStackSize > 1)
        {
            TextMeshProUGUI text = inv_go.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = inv.stackSize.ToString();
            }
        }
        
    }
}