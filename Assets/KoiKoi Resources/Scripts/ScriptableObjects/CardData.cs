using UnityEngine;

#if UNITY_EDITOR
using System;
using UnityEditor;
#endif


[CreateAssetMenu(fileName = "CardData", menuName = "Scriptable Objects/CardData")]
public class CardData : ScriptableObject {
    
    #region Serialized Fields (Unity Inspector)
    [Header("Main Properties")]
    [SerializeField] private Card.RankType rank;
    [SerializeField] private Card.MonthType month;
    
    [Space]
    [Header("Secondary Properties")]
    [SerializeField] private Card.BrightType bright;
    [SerializeField] private Card.AnimalType animal;
    [SerializeField] private Card.RibbonType ribbon;
    
    public Card.RankType Rank => rank;
    public Card.MonthType Month => month;
    public Card.BrightType Bright => bright;
    public Card.AnimalType Animal => animal;
    public Card.RibbonType Ribbon => ribbon;
    #endregion

    /// <summary>
    /// The packed binary value of a card.
    /// </summary>
    public int BinaryData => (
        (int)Rank +
        ((int)Month << 2) +
        ((int)Bright << 6) +
        ((int)Animal << 8) +
        ((int)Ribbon << 11)
    );

    /// <summary>
    /// The unique ID of a card based on the card's month and rank.
    /// </summary>
    public int UniqueID => (int)Rank + ((int)Month << 2);
}

#if UNITY_EDITOR
[CustomEditor(typeof(CardData)), CanEditMultipleObjects]
public class CardDataEditor : Editor {
    private const string CARD_DATA_EDITOR_ATLAS_KEY = "CardDataEditor.TextureAtlasReference";
    private TextureAtlas textureAtlas;
    private SerializedProperty script;

    private enum CardType {
        Plain, Bright, Animal, Ribbon
    }

    void OnEnable() {
        script = serializedObject.FindProperty("m_Script");
    }

    public override void OnInspectorGUI() {
        GUI.enabled = false;
        EditorGUILayout.PropertyField(script);
        GUI.enabled = true;
        TextureAtlasProperty();
        
        EditorGUILayout.Space(20f);
        
        //DrawDefaultInspector();
        SerializedProperty rank = serializedObject.FindProperty("rank");
        SerializedProperty month = serializedObject.FindProperty("month");
        SerializedProperty bright = serializedObject.FindProperty("bright");
        SerializedProperty animal = serializedObject.FindProperty("animal");
        SerializedProperty ribbon = serializedObject.FindProperty("ribbon");

        CardType cardType = CardType.Plain;
        if ((Card.BrightType)bright.enumValueIndex != Card.BrightType.None) cardType = CardType.Bright;
        if ((Card.AnimalType)animal.enumValueIndex != Card.AnimalType.None) cardType = CardType.Animal;
        if ((Card.RibbonType)ribbon.enumValueIndex != Card.RibbonType.None) cardType = CardType.Ribbon;


        rank.enumValueIndex = (int)(Card.RankType)EditorGUILayout.EnumPopup("Rank", (Card.RankType)rank.enumValueIndex);
        month.enumValueIndex = (int)(Card.MonthType)EditorGUILayout.EnumPopup("Month", (Card.MonthType)month.enumValueIndex);
        
        cardType = (CardType)EditorGUILayout.EnumPopup("Card Type", cardType);

        int temp_bright = cardType==CardType.Bright ? temp_bright = bright.enumValueIndex : (int)Card.BrightType.None;
        int temp_animal = cardType==CardType.Animal ? temp_animal = animal.enumValueIndex : (int)Card.AnimalType.None;
        int temp_ribbon = cardType==CardType.Ribbon ? temp_ribbon = ribbon.enumValueIndex : (int)Card.RibbonType.None;
        
        switch (cardType) {
            case CardType.Bright:
                if ((Card.BrightType)temp_bright == Card.BrightType.None) temp_bright = 1;
                temp_bright = (int)(Card.BrightType)EditorGUILayout.EnumPopup(new GUIContent("Bright Type"), (Card.BrightType)temp_bright, ExcludeNoneBright, false);
                break;
            case CardType.Animal:
                if ((Card.AnimalType)temp_animal == Card.AnimalType.None) temp_animal = 1;
                temp_animal = (int)(Card.AnimalType)EditorGUILayout.EnumPopup(new GUIContent("Animal Type"), (Card.AnimalType)temp_animal, ExcludeNoneAnimal, false);
                break;
            case CardType.Ribbon:
                if ((Card.RibbonType)temp_ribbon == Card.RibbonType.None) temp_ribbon = 1;
                temp_ribbon = (int)(Card.RibbonType)EditorGUILayout.EnumPopup(new GUIContent("Ribbon Type"), (Card.RibbonType)temp_ribbon, ExcludeNoneRibbon, false);
                break;
        }

        bright.enumValueIndex = temp_bright;
        animal.enumValueIndex = temp_animal;
        ribbon.enumValueIndex = temp_ribbon;

        serializedObject.ApplyModifiedProperties();
        CardPreviewDisplay();
    }

    private void TextureAtlasProperty() {
        string initial_path = EditorPrefs.GetString("CardDataEditor", "");
        if (initial_path.Length != 0)
            textureAtlas = AssetDatabase.LoadAssetAtPath<TextureAtlas>(initial_path);
        
        textureAtlas = (TextureAtlas)EditorGUILayout.ObjectField("Texture Atlas", textureAtlas, typeof(TextureAtlas), false);
        string path_new = AssetDatabase.GetAssetPath(textureAtlas);
        if (!initial_path.Equals(path_new))
            EditorPrefs.SetString("CardDataEditor", path_new);
    }

    private void CardPreviewDisplay() {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.Space(20, true);
        EditorGUILayout.EndVertical();
        EditorGUILayout.LabelField("Card Image Preview (Texture atlas must be set at the top of the editor)", EditorStyles.boldLabel);

        if (textureAtlas == null) {
            EditorGUILayout.HelpBox("Texture atlas not set, image preview not available", MessageType.Error);
            return;
        }
        

        Texture2D texture = textureAtlas.GetTexture(((CardData)target).UniqueID);
        if (!texture) {
            EditorGUILayout.HelpBox("Invalid texture ID, image preview not available", MessageType.Error);
            return;
        }
        
        float width = Mathf.Min(EditorGUIUtility.currentViewWidth - 40f, 200f);
        float height = (float)texture.height / (float)texture.width * width;

        Rect lastRect = GUILayoutUtility.GetLastRect();
        
        GUI.DrawTexture(new Rect(lastRect.x, lastRect.y+20f, width, height), texture);
    }

    public bool ExcludeNoneBright(Enum e) => (Card.BrightType)e != Card.BrightType.None;
    public bool ExcludeNoneAnimal(Enum e) => (Card.AnimalType)e != Card.AnimalType.None;
    public bool ExcludeNoneRibbon(Enum e) => (Card.RibbonType)e != Card.RibbonType.None;
}
#endif