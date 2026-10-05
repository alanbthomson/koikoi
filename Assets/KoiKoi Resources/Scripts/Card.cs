using System;
using UnityEngine;

[RequireComponent(typeof(CardRenderer))]
public class Card : MonoBehaviour {

    public bool isSelected = false;
    public KoiKoiGameManager gameManager;

    #region Data Definitions

    public enum RankType {
        First, Second, Third, Fourth
    }
    public enum MonthType {
        January, February, March, April, May, June, July,
        August, September, October, November, December
    }
    public enum BrightType {
        None, Plain, Moon, Sakura, RainMan
    }
    public enum AnimalType {
        None, Plain, Boar, Deer, Butterfly, Sake
    }
    public enum RibbonType {
        None, Red, Text, Blue
    }
    public enum CardType {
        Plain, Bright, Animal, Ribbon
    }

    #endregion

    #region Serialized Fields (Unity Inspector)
    [Tooltip("Texture lookup table to retrieve textures from")]
    [SerializeField] private TextureAtlas textureAtlas;
    [Tooltip("Mesh renderer of the card")]
    [SerializeField] private MeshRenderer meshRenderer;
    #endregion
    
    #region Card Data Properties
    // Each are defined as properties with public getters and private setters
    
    public int BinaryData { get; private set; }

    /// Unique ID consisting of the card's rank and month.
    public int UniqueID { get; private set; }
    
    /// The value that determines which card within a month this card is specifically.
    /// (Mostly used for rendering)
    public RankType Rank { get; private set; }

    /// The month this card is a part of.
    public MonthType Month { get; private set; }

    /// The type of bright card this card is.
    public BrightType Bright { get; private set; }

    /// The type of animal card this card is.
    public AnimalType Animal { get; private set; }

    /// The type of ribbon card this card is.
    public RibbonType Ribbon { get; private set; }

    /// Returns true if the card is a brght card.
    public bool IsBright => Bright != BrightType.None;

    /// Returns true if this card is an animal card.
    public bool IsAnimal => Animal != AnimalType.None;

    /// Returns true if this card is a ribbon card.
    public bool IsRibbon => Ribbon != RibbonType.None;

    /// Returns the name of the month of the card.
    /// (Calls Month.ToString())
    public string MonthName => Month.ToString();

    /// Returns the name of the animal of the card if one exists, otherwise returns an empty string.
    /// (Essentially just calls Animal.ToString())
    public string AnimalName => Animal == AnimalType.None ? "" : Animal.ToString();

    /// Returns the name of the bright type of the card if one exists, otherwise returns an empty string.
    /// (Essentially just calls Bright.ToString())
    public string BrightName => Bright == BrightType.None ? "" : Bright.ToString();
    
    /// Returns the name of the ribbon type of the card if one exists, otherwise returns an empty string.
    /// (Essentially just calls Ribbon.ToString())
    public string RibbonName => Ribbon == RibbonType.None ? "" : Ribbon.ToString();
    
    #endregion
    
    #region Rendering Data
    private MaterialPropertyBlock material_properties;
    #endregion

    /// Set the values of the card using its network-ready binary data.
    /// Recommended to use <see cref="LoadCardData">loadCardData</see> instead as SetBinaryData may have unintended consequences if the data is invalid.
    /// The card's binary data. Will be of the type ushort when networked,
    /// but working with ints is generally easier in the backend.
    public void SetBinaryData(int data) {
        // Set the basic info of the card
        BinaryData = data;
        UniqueID = data & 0b111111;

        // Get the values used with enum types for checking later
        int v_rank = data & 0b11;
        int v_month = (data >> 2) & 0b1111;
        int v_bright = (data >> 6) & 0b11;
        int v_animal = (data >> 8) & 0b111;
        int v_ribbon = (data >> 11) & 0b11;

        // Throw exceptions if any values with associated enums are out of the enums' ranges
        if (v_rank >= 4) throw new ArgumentOutOfRangeException($"Rank value {v_rank} is out of range. Binary Data: {data:b16}");
        if (v_month >= 12) throw new ArgumentOutOfRangeException($"Month value {v_month} is out of range. Binary Data: {data:b16}");
        if (v_bright >= 4) throw new ArgumentOutOfRangeException($"Bright value {v_bright} is out of range. Binary Data: {data:b16}");
        if (v_animal >= 6) throw new ArgumentOutOfRangeException($"Animal value {v_animal} is out of range. Binary Data: {data:b16}");
        if (v_ribbon >= 4) throw new ArgumentOutOfRangeException($"Ribbon value {v_ribbon} is out of range. Binary Data: {data:b16}");
        
        // Assign the enum variables their values
        Rank = (RankType)v_rank;
        Month = (MonthType)v_month;
        Bright = (BrightType)v_bright;
        Animal = (AnimalType)v_animal;
        Ribbon = (RibbonType)v_ribbon;

        // Create and use the material property block
        material_properties ??= new MaterialPropertyBlock();
        material_properties.SetTexture("_BaseMap", textureAtlas.GetTexture(UniqueID));
        meshRenderer.SetPropertyBlock(material_properties);
        OnValidate();
    }

    /// Load the card data directly from a cardData scriptable object.
    /// <param name="cardData">Scriptable object containing the card data.</param>
    public void LoadCardData(CardData cardData) {
        BinaryData = cardData.BinaryData;
        UniqueID = cardData.UniqueID;
        Rank = cardData.Rank;
        Month = cardData.Month;
        Bright = cardData.Bright;
        Animal = cardData.Animal;
        Ribbon = cardData.Ribbon;
        // Create and use the material property block
        material_properties ??= new MaterialPropertyBlock();
        material_properties.SetTexture("_BaseMap", textureAtlas.GetTexture(UniqueID));
        meshRenderer.SetPropertyBlock(material_properties);
        OnValidate();
    }


    /// Log the debug info of the card to the console.
    public void printCardDebugInfo()
    {
        Debug.Log(
            $"Binary data: {BinaryData}\n" +
            $"Unique ID: {UniqueID}\n" +
            $"Rank: {Rank}\n" +
            $"Month: {Month}, {Month.ToString()}\n" +
            $"Bright: {Bright}, {Bright.ToString()}\n" +
            $"Animal: {Animal}, {Animal.ToString()}\n" +
            $"Ribbon: {Ribbon}, {Ribbon.ToString()}"
        );
    }
    
    #if UNITY_EDITOR
    [SerializeField, TextArea] private string debugInfo;
    #endif

    private void OnValidate()
    {
    #if UNITY_EDITOR
        debugInfo =
            $"Binary data: {BinaryData}\n" +
            $"Unique ID: {UniqueID}\n" +
            $"Rank: {Rank}\n" +
            $"Month: {MonthName}\n" +
            $"Animal: {AnimalName}\n" +
            $"Bright: {BrightName}\n" +
            $"Ribbon: {RibbonName}";
    #endif
    }

    private ParticleSystem[] matchParticles;

    public void SetMatchHighlight(bool highlighted)
    {
        matchParticles ??= GetComponentsInChildren<ParticleSystem>(true);
        Transform highlightRoot = transform.Find("Particle shape");
        if (highlighted && highlightRoot != null)
        {
            highlightRoot.gameObject.SetActive(true);
        }
        foreach (ParticleSystem effect in matchParticles)
        {
            if (highlighted)
            {
                // A particle child cannot play while any parent under the card is inactive.
                for (Transform parent = effect.transform.parent; parent != null && parent != transform; parent = parent.parent)
                {
                    parent.gameObject.SetActive(true);
                }
                effect.gameObject.SetActive(true);
                if (!effect.isPlaying) effect.Play(false);
            }
            else
            {
                effect.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                effect.gameObject.SetActive(false);
            }
        }
        if (!highlighted && highlightRoot != null)
        {
            highlightRoot.gameObject.SetActive(false);
        }
    }

    public void Start()
    {
        if (gameManager == null) gameManager = GameObject.FindFirstObjectByType<KoiKoiGameManager>();
    }

    public void OnMouseDown()
    {
        // select current player's card
        if (gameManager.CurrentPlayer.HandCards.Contains(this) && !gameManager.timeToDraw)
        {
            gameManager.SetCardSelectedBool(this);
        }
        // match card
        if (gameManager.selectedCard != null && this.MonthName == gameManager.selectedCard.MonthName 
            && this != gameManager.selectedCard && !gameManager.timeToDraw)
        {
            // prevent matching opponent's card during player's turn and vice versa
            // also the deck?? Wow.
            if (!gameManager.WaitingPlayer.HandCards.Contains(this) &&
                !gameManager.deckCards.Contains(this))
            {
                gameManager.Match(this);   
            }
            return;
        }
        // draw from deck
        if (gameManager.timeToDraw && gameManager.deckCards.Contains(this) && gameManager.selectedCard == null && !gameManager.isDrawingFromDeck)
        {
            gameManager.DrawFromDeck();
            return;
        }
        // match from deck to center
        if (gameManager.timeToDraw && this != gameManager.selectedCard && gameManager.centerCards.Contains(this) 
        && this.MonthName == gameManager.selectedCard.MonthName)
        {
            gameManager.Match(this);
            return;
        }
    }
}
