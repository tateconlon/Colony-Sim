using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class MouseOverTileTypeText : MonoBehaviour
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
            myText.text = $"TileType: N/A";
        }
        myText.text = $"TileType: {t.TileType.ToString()}";
    }
}
