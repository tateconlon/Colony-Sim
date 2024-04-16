using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class MouseOverFurnitureIndex : MonoBehaviour
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
            myText.text = $"Furniture Type: N/A";
            return;
        }

        if (t.furniture == null)
        {
            myText.text = $"Furniture Type: N/A";
        }
        else
        {
            myText.text = $"Furniture Type: {t.furniture.objectType}";
        }
    }
}
