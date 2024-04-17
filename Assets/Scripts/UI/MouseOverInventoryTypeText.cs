using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class MouseOverInventoryTypeText : MonoBehaviour
{
    TextMeshProUGUI myText;

    void Start()
    {
        myText = GetComponent<TextMeshProUGUI>();
        
    }

    void Update()
    {
        Tile t = MouseController.Instance.GetTileUnderMouse();
        if (t == null)
        {
            myText.text = $"Inventory Type: N/A";
            return;
        }
        
        if (t.inventory == null)
        {
            myText.text = $"Inventory Type: NULL";
            return;
        }
        
        myText.text = $"Inventory Type: {t.inventory.objectType.ToString()}";
    }
}
