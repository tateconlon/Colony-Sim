using System.Collections.Generic;
using UnityEngine;
public class CharacterSpriteController : MonoBehaviour
{
    const string CHARACTER_SORTING_LAYER_NAME = "CHARACTER";
    Dictionary<Character, GameObject> character_GameObject_Map = new();

    [SerializeField] Sprite characterSprite;

    World _world;

    void Start()
    {
        _world = WorldController.Instance.World;

        for (int i = 0; i < _world.characters.Count; i++)
        {
            Character char_data = _world.characters[i];
            OnCharacterCreated(char_data);
        }

        _world.OnCharacterCreated += OnCharacterCreated;
    }

    //Create visuals GameObject
    void OnCharacterCreated(Character char_data)
    {
        if (character_GameObject_Map.ContainsKey(char_data))
        {
            Debug.LogError($"Trying to create character {char_data}, but it already exists!");
            return;
        }

        char_data.OnChanged += OnCharacterChanged;
        
        GameObject char_go = new GameObject($"Character {character_GameObject_Map.Count}",
            typeof(SpriteRenderer));
            
        char_go.transform.SetParent(transform);
        char_go.transform.position = char_data.Pos;
            
        SpriteRenderer char_sr = char_go.GetComponent<SpriteRenderer>();
        char_sr.sortingLayerName = CHARACTER_SORTING_LAYER_NAME;
        char_sr.sprite = characterSprite;
        
        character_GameObject_Map.Add(char_data, char_go);
    }
    
    
    void OnCharacterChanged(Character char_data)
    {
        if (!character_GameObject_Map.TryGetValue(char_data, out GameObject char_go))
        {
            Debug.LogError($"Could not find character {char_data}'s Game Object");
            return;
        }

        char_go.transform.position = char_data.Pos;
    }
}