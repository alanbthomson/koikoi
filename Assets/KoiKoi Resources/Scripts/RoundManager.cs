using System.Collections.Generic;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    #region Singleton Implementation
    public static RoundManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    #endregion


    /// <summary>
    /// Dictionary of <int Month, List[Card] Cards> for all cards in Host Player's Hand
    /// </summary>
    private Hand playerOneCards;
    private Hand playerTwoCards;
    private Hand fieldCards;
    public Deck deck { get; private set; }

    [SerializeField] private Card cardPrefab;
    [SerializeField] private Deck deckPrefab;
    [SerializeField] private Transform stackAnchor;
    

    enum Player
    {
        One = 1,
        Two = 2,
    }
    private Player currentPlayer;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerOneCards = new Hand();
        playerTwoCards = new Hand();
        fieldCards = new Hand();
        
        currentPlayer = Player.One;

        deck = Instantiate(deckPrefab);
        BuildDeck();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void BuildDeck()
    {
        CardData[] cardDataArray = Resources.LoadAll<CardData>("GameData/CardData");
        if (stackAnchor == null) {
            stackAnchor = transform;
        }

        for (int i = 0; i < 48; i++) {
            Card card = Instantiate(cardPrefab, stackAnchor.position, stackAnchor.rotation);
            deck.AddCard(card);
            Debug.Log($"Loaded Card Data: {cardDataArray[i].name}");
            card.LoadCardData(cardDataArray[i]);
            card.gameObject.name = card.MonthName + card.Rank + card.AnimalName + card.BrightName + card.RibbonName;
        }
    }
    
    void StartRound()
    {
        // Decide first player for round
        
        currentPlayer = Player.One;
        
        // Group and shuffle deck
        deck.ResetDeck();
        deck.Shuffle();
        
        // Deal cards (call drawcard on deck?)
        Hand firstHand, secondHand;
        if (currentPlayer == Player.One)
        {
            firstHand = playerOneCards;
            secondHand = playerTwoCards;
        }
        else
        {
            firstHand = playerTwoCards;
            secondHand = playerOneCards;
        }
        Card dealtCard;
        
        for (int i = 0; i < 4; i++)
        {
            // Deal Current Player
            for (int j = 0; j < 2; j++)
            {
            	dealtCard = deck.DrawCard();
                firstHand.AddCard(dealtCard);
            }
            // Deal Field
            for (int j = 0; j < 2; j++)
            {
                dealtCard = deck.DrawCard();
                fieldCards.AddCard(dealtCard);
            }
            // Deal Other Player
            for (int j = 0; j < 2; j++)
            {
                dealtCard = deck.DrawCard();
                secondHand.AddCard(dealtCard);
            }
        }
        
        // Check board for conditions that end round instantly
        // foreach (KeyValuePair<Card.MonthType, Card> pair in firstDeck)
        // {
        //     if pair.value
        // }
        
        // Enter round loop
    }

    void RoundLoop()
    {
        // Current player turn
        
        // Highlight cards in hand that have match
        
        // Wait for player card selection
        
        // Change highlights to show matches of currently selected
        
        // Wait for player to select match (or empty space on field)
        
        // Draw top from deck and check for triple condition / present to player (highlight potential matches)
        
        // IF triple condition, go next turn
        
        // IF not, wait player input for match
        
        // Player gets card(s)
        
        // Check win conditions
        
        // IF wincon present, ask player what they want to do
        
        // ELSE go next round
        
        
    }
}
