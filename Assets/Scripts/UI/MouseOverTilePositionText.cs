using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class MouseOverTilePositionText : MonoBehaviour
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
            myText.text = $"INVALID TILE";
            return;
        }
        
        myText.text = $"X:{t.X}\tY:{t.Y}";
    }
}
