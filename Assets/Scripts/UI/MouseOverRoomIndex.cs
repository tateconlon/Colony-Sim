using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class MouseOverRoomIndex : MonoBehaviour
{
    TextMeshProUGUI myText;

    void Start()
    {
        myText = GetComponent<TextMeshProUGUI>();
        
    }

    void Update()
    {
        Tile t = MouseController.Instance.GetTileUnderMouse();
        if (t == null || t.world == null)
        {
            myText.text = $"Room Index: N/A";
        }
        myText.text = $"Room Index: {t.world.rooms.IndexOf(t.room).ToString()}";
    }
}
