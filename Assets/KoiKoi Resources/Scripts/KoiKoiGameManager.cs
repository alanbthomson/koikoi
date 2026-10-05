using System.Collections;
using System.Collections.Generic;
using System.Security.Claims;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using System;
using TMPro;

public class KoiKoiGameManager : MonoBehaviour
{
    [SerializeField] private Card cardPrefab;
    private float offset_x = 0f;
    private float offset_y = 0f;
    private float verticalSpacing = 0.06f;
    private float horizontalSpacing = 0.03f;
    private Transform stackAnchor;
    private float stackHeightStep = 0.0004f;
    [Tooltip("Assign a deck prefab asset or an existing deck in the scene.")]
    [SerializeField] private Deck deck;
    private float xPlayerStartPos = 0.105f;
    private float xCenterStartPos = 0.045f;
    private float xOffset = 0.03f;
    private float cardRestingHeight = 0.7055f;
    public Card selectedCard;
    public bool isDrawingFromDeck = false;
    public List<Card> centerCards = new List<Card>();
    public List<Card> deckCards = new List<Card>();
    public bool timeToDraw = false;
    public Player Player { get; private set; }
    public Player Opponent { get; private set; }
    public GameObject[] topRowMarkers = new GameObject[8];
    public GameObject[] bottomRowMarkers = new GameObject[8];
    public GameObject[] topRowCards = new GameObject[8];
    public GameObject[] bottomRowCards = new GameObject[8];
    private List<Card> tempMatchedCards = new List<Card>();
    private readonly Dictionary<string, Vector3> threeOfAKindBasePositions = new Dictionary<string, Vector3>();

    // Whose turn it is. Turn state is single-source here; the IsTurn flags on
    // the Player objects are kept in sync by SwitchPlayerTurn for Card.cs.
    public Player CurrentPlayer { get; private set; }
    public Player WaitingPlayer { get; private set; }

    [SerializeField] private GameObject viewMatchesButton;
    [SerializeField] private GameObject viewOpponentMatchesButton;
    [Tooltip("Root game UI object — hidden while viewing captured matches")]
    [SerializeField] private GameObject gameUI;
    [Tooltip("Debug: rig the deal so the center gets two same-month cards and the top two deck cards after the deal are the third and fourth of that month")]
    [SerializeField] private bool debugThreeOfAKindDeal = false;
    private bool gameOver = false;
    private bool roundWasInvalid = false;
    private int currentRound = 0;

    private CameraController cameraController;
    [SerializeField] private GameObject newGameButton;
    [SerializeField] private TMP_Text playerScoreText;
    [SerializeField] private TMP_Text opponentScoreText;
    [SerializeField] private TMP_Text currentRoundText;

    [SerializeField] private TMP_Text playerWinText;
    [SerializeField] private TMP_Text opponentWinText;
    [SerializeField] private TMP_Text tempPlayerPointsText;
    [SerializeField] private TMP_Text tempOpponentPointsText;
    [SerializeField] private GameObject playerClaimWinButton;
    [SerializeField] private GameObject opponentClaimWinButton;
    [SerializeField] private GameObject playerKoiKoiButton;
    [SerializeField] private GameObject opponentKoiKoiButton;
    [SerializeField] private TMP_Text drawText;
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private TMP_Text playerWinDataText;
    [SerializeField] private TMP_Text opponentWinDataText;

    void Awake()
    {
        if (Application.isMobilePlatform)
        {
            Application.targetFrameRate = 60;
        }

        // Pile anchors: player at bottom-left, opponent mirrored at top-right.
        Player = new Player("Player", new Vector3(-0.215f, cardRestingHeight, -0.21f));
        Opponent = new Player("Opponent", new Vector3(0.215f, cardRestingHeight, 0.21f), rowDirection: -1f);

        // Point each player at their own UI elements.
        Player.UI = new Player.PlayerUI
        {
            ScoreText = playerScoreText,
            WinText = playerWinText,
            TempPointsText = tempPlayerPointsText,
            WinDataText = playerWinDataText,
            ClaimWinButton = playerClaimWinButton,
            KoiKoiButton = playerKoiKoiButton,
        };
        Opponent.UI = new Player.PlayerUI
        {
            ScoreText = opponentScoreText,
            WinText = opponentWinText,
            TempPointsText = tempOpponentPointsText,
            WinDataText = opponentWinDataText,
            ClaimWinButton = opponentClaimWinButton,
            KoiKoiButton = opponentKoiKoiButton,
        };
    }

    IEnumerator Start()
    {
        CurrentPlayer = Player;
        WaitingPlayer = Opponent;
        Player.IsTurn = true;
        Opponent.IsTurn = false;
        cameraController = FindFirstObjectByType<CameraController>();
        cameraController.OnViewToggled += SetGameUIVisible;
        LoadCards();
        CreateDeckOnScreen();
        currentRoundText.text = CurrentRoundSwitch(currentRound);
        yield return StartCoroutine(DealCardsCoroutine());
        RestructurePlayerHand();
        RestructureOpponentHand();
        CheckIfValidGame();
        CheckAndHandleThreeOfAKindOnBoard();
        StartCoroutine(GameLoop());
    }

    /// Hides the in-game UI while a matches view is open, restores it after.
    void SetGameUIVisible(bool viewingMatches)
    {
        gameUI.SetActive(!viewingMatches);
    }

    public void NewGame()
    {
        
        newGameButton.SetActive(false);
        StopAllCoroutines();
        if (roundWasInvalid)
        {
            // Invalid board deal: replay the same month.
            roundWasInvalid = false;
        }
        else if (currentRound < 11)
        {
            currentRound++;
        }
        else
        {
            currentRound = 0;
            Player.Score = 0;
            playerScoreText.text = "Player Score: 0";
            Opponent.Score = 0;
            opponentScoreText.text = "Opponent Score: 0";
        }        
        currentRoundText.text = CurrentRoundSwitch(currentRound);
        cameraController.ResetView();
        StartCoroutine(NewGameCoroutine());
    }

    String CurrentRoundSwitch(int round)
    {
        switch (round)
        {
            case 0:
                return "January";
            case 1:
                return "February";
            case 2:
                return "March";
            case 3:
                return "April";
            case 4:
                return "May";
            case 5:
                return "June";
            case 6:
                return "July";
            case 7:
                return "August";
            case 8:
                return "September";
            case 9:
                return "October";
            case 10:
                return "November";
            case 11:
                return "December";
            default:
                return "Unknown";
        }
    }

    public IEnumerator NewGameCoroutine()
    {
        DestroyEverything();
        LoadCards();
        CreateDeckOnScreen();
        yield return StartCoroutine(DealCardsCoroutine());
        RestructurePlayerHand();
        RestructureOpponentHand();
        CheckIfValidGame();
        CheckAndHandleThreeOfAKindOnBoard();
        StartCoroutine(GameLoop());
    }

    public void LoadCards()
    {
        // Prefab assets are templates; keep all mutable deck state on a scene instance.
        if (deck != null && !deck.gameObject.scene.IsValid())
        {
            deck = Instantiate(deck);
        }
        if (deck == null || cardPrefab == null)
        {
            throw new InvalidOperationException("Assign Card Prefab and Deck on KoiKoiGameManager before starting a round.");
        }

        // Load card data from resources and instantiate cards
        CardData[] cardDataArray = Resources.LoadAll<CardData>("GameData/CardData");
        if (cardDataArray.Length < 48)
        {
            throw new InvalidOperationException(
                $"Expected 48 CardData assets under a Resources folder at GameData/CardData, found {cardDataArray.Length}. " +
                "Check that the folder is named exactly 'Resources' and the path is 'GameData/CardData'.");
        }
        if (stackAnchor == null)
        {
            stackAnchor = transform;
        }

        for (int i = 0; i < 48; i++)
        {
            Card card = Instantiate(cardPrefab, stackAnchor.position, stackAnchor.rotation);
            card.gameManager = this;
            card.SetMatchHighlight(false);
            deck.AddCard(card);
            deckCards.Add(card);
            card.LoadCardData(cardDataArray[i]);
            card.gameObject.name = card.MonthName + card.AnimalName + card.BrightName + card.RibbonName;
        }
    }

    public void DestroyEverything()
    {
        if (deck != null)
        {
            deck.ClearAllCards();
        }
        // Clear row arrays
        for (int i = 0; i < 8; i++)
        {
            topRowCards[i] = null;
            bottomRowCards[i] = null;
        }
        HideMarkers();
        deckCards.Clear();
        centerCards.Clear();
        tempMatchedCards.Clear();
        threeOfAKindBasePositions.Clear();
        Player.ResetForNewRound();
        Opponent.ResetForNewRound();

        selectedCard = null;
        timeToDraw = false;

        foreach (Card card in FindObjectsByType<Card>(FindObjectsSortMode.None))
        {
            if (card.gameObject.name == "OG Card" || card.gameObject.name == "Card Template") continue;
            Destroy(card.gameObject);
        }

        // Reset UI
        viewMatchesButton.SetActive(false);
        viewOpponentMatchesButton.SetActive(false);
        gameOverText.gameObject.SetActive(false);
        gameOver = false;
    }

    public void MoveCards()
    {
        // Display all cards in a grid to verify correct loading
        deck.Shuffle();
        const int columns = 12;
        const int rows = 4;
        for (int i = 0; i < 48; i++)
        {
            Card card = deck.DrawCard();
            float xIndex = (i % columns) - (columns - 1) * 0.5f;
            float zIndex = (i / columns) - (rows - 1) * 0.5f;
            float xPos = xIndex * horizontalSpacing + offset_x;
            float zPos = zIndex * verticalSpacing + offset_y;
            card.transform.position = new Vector3(xPos, 0.01f, zPos);
            card.transform.rotation = Quaternion.Euler(-90, 180, 0);
            if (card.GetComponent<Rigidbody>() == null) card.gameObject.AddComponent<Rigidbody>();
            BoxCollider boxCollider = card.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = card.AddComponent<BoxCollider>();
            }
            boxCollider.size = new Vector3(0.61f, 1f, 0.06f);
            card.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // card.transform.position = new Vector3(id_x + id_x * offset_x, id_y + id_y * offset_y);
            // card.transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }

    public void CreateDeckOnScreen()
    {
        deck.Shuffle();
        if (debugThreeOfAKindDeal)
        {
            RigDeckForThreeOfAKind();
        }
        Vector3 basePosition = stackAnchor != null ? stackAnchor.position : Vector3.zero;
        Quaternion baseRotation = Quaternion.Euler(180, 0, 0);
        for (int i = 0; i < 48; i++)
        {
            Card card = deck.GetCardAt(i);
            if (card == null)
            {
                break;
            }
            Vector3 stackedPosition = basePosition + Vector3.up * (stackHeightStep * i);
            card.transform.SetPositionAndRotation(stackedPosition, baseRotation);
            if (card.GetComponent<Rigidbody>() == null) card.gameObject.AddComponent<Rigidbody>();
            BoxCollider boxCollider = card.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = card.AddComponent<BoxCollider>();
            }
            boxCollider.size = new Vector3(0.024f, 0.001f, 0.034f);
            card.GetComponent<Rigidbody>().freezeRotation = true;
            card.GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    /// Debug rig: forces a three-of-a-kind scenario. Two cards of one month are
    /// dealt to the center board (center draws are #3 and #4 in the deal order),
    /// the third card sits on top of the deck after the deal (draw #17), and the
    /// fourth is right below it (draw #18).
    void RigDeckForThreeOfAKind()
    {
        var fourOfAMonth = deckCards
            .GroupBy(c => c.MonthName)
            .First(g => g.Count() == 4)
            .Take(4)
            .ToList();
        Debug.Log($"Debug deal rigged: {fourOfAMonth[0].MonthName} — two to the board, third/fourth on top of the deck");
        deck.SwapToDrawPosition(fourOfAMonth[0], 3);
        deck.SwapToDrawPosition(fourOfAMonth[1], 4);
        deck.SwapToDrawPosition(fourOfAMonth[2], 9); // 24
        deck.SwapToDrawPosition(fourOfAMonth[3], 10); // 25
    }

    IEnumerator DealCardsCoroutine()
    {
        float moveDuration = 0.1f;
        float delayBetweenCards = 0.1f;
        Vector3 topPlayerDealPosition = new Vector3(xPlayerStartPos, cardRestingHeight, 0.09f);
        Vector3 bottomPlayerDealPosition = new Vector3(xPlayerStartPos, cardRestingHeight, -0.09f);
        Vector3 centerDealPositionTop = new Vector3(xCenterStartPos, cardRestingHeight, 0.025f);
        Vector3 centerDealPositionBottom = new Vector3(xCenterStartPos, cardRestingHeight, -0.025f);
        // to track the array of cards to use with no match indicators later
        int arrayIndex = 5;

        // Deal two cards at a time, four times
        for (int y = 0; y < 4; y++)
        {
            // Deal top player's cards
            for (int i = 0; i < 2; i++)
            {
                Card card = deck.DrawCard();
                if (card == null)
                {
                    break;
                }
                Opponent.HandCards.Insert(0, card);
                deckCards.Remove(card);
                Rigidbody rb = card.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = card.gameObject.AddComponent<Rigidbody>();
                }
                rb.isKinematic = false;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                Quaternion dealRotation = Quaternion.Euler(180, 0, 0);
                yield return StartCoroutine(MoveRigidbodyToPosition(rb, topPlayerDealPosition, dealRotation, moveDuration));
                topPlayerDealPosition -= new Vector3(xOffset, 0f, 0f);
                rb.position = new Vector3(rb.position.x, cardRestingHeight, rb.position.z);
                yield return new WaitForSeconds(delayBetweenCards);
            }

            // Deal to the center
            for (int i = 0; i < 1; i++)
            {
                Card card = deck.DrawCard();
                if (card == null)
                {
                    break;
                }
                centerCards.Add(card);
                deckCards.Remove(card);
                topRowCards[arrayIndex] = card.gameObject;
                Rigidbody rb = card.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = card.gameObject.AddComponent<Rigidbody>();
                }
                rb.isKinematic = false;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                Quaternion dealRotation = Quaternion.Euler(0, 180, 0);
                yield return StartCoroutine(MoveRigidbodyToPosition(rb, centerDealPositionTop, dealRotation, moveDuration));
                centerDealPositionTop -= new Vector3(xOffset, 0f, 0f);
                rb.position = new Vector3(rb.position.x, cardRestingHeight, rb.position.z);
                yield return new WaitForSeconds(delayBetweenCards);

                Card card2 = deck.DrawCard();
                if (card2 == null)
                {
                    break;
                }
                centerCards.Add(card2);
                deckCards.Remove(card2);
                bottomRowCards[arrayIndex] = card2.gameObject;
                arrayIndex--;
                Rigidbody rb2 = card2.GetComponent<Rigidbody>();
                if (rb2 == null)
                {
                    rb2 = card2.gameObject.AddComponent<Rigidbody>();
                }
                rb2.isKinematic = false;
                rb2.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb2.interpolation = RigidbodyInterpolation.Interpolate;
                yield return StartCoroutine(MoveRigidbodyToPosition(rb2, centerDealPositionBottom, dealRotation, moveDuration));
                centerDealPositionBottom -= new Vector3(xOffset, 0f, 0f);
                rb2.position = new Vector3(rb2.position.x, cardRestingHeight, rb2.position.z);
                yield return new WaitForSeconds(delayBetweenCards);
            }

            // Deal bottom player's cards
            for (int i = 0; i < 2; i++)
            {
                Card card = deck.DrawCard();
                if (card == null)
                {
                    break;
                }
                Player.HandCards.Insert(0, card);
                deckCards.Remove(card);
                Rigidbody rb = card.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = card.gameObject.AddComponent<Rigidbody>();
                }
                rb.isKinematic = false;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                Quaternion dealRotation = Quaternion.Euler(0, 180, 0);
                yield return StartCoroutine(MoveRigidbodyToPosition(rb, bottomPlayerDealPosition, dealRotation, moveDuration));
                bottomPlayerDealPosition -= new Vector3(xOffset, 0f, 0f);
                rb.position = new Vector3(rb.position.x, cardRestingHeight, rb.position.z);
                yield return new WaitForSeconds(delayBetweenCards);
            }
        }
    }

    IEnumerator MoveRigidbodyToPosition(Rigidbody rb, Vector3 target, Quaternion rotation, float duration)
    {
        Vector3 start = rb.position;
        Quaternion startRot = rb.rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rb.MovePosition(Vector3.Lerp(start, target, t));
            rb.MoveRotation(Quaternion.Slerp(startRot, rotation, t));
            yield return new WaitForFixedUpdate(); // physics step
        }
        rb.isKinematic = true;
        if (rb.position.y < cardRestingHeight)
        {
            rb.position = new Vector3(rb.position.x, cardRestingHeight, rb.position.z);
        }
    }

    void CheckIfValidGame()
    {
        // Four of a kind or four pairs on the board, invalid
        bool invalidGame = CheckLucky(centerCards);
        if (invalidGame)
        {
            Debug.Log("Invalid game detected due to four of a kind or four pairs on the board.");
            gameOverText.text = "Invalid game: four of a kind or four pairs on the board";
            gameOverText.gameObject.SetActive(true);
            gameOver = true;
            roundWasInvalid = true;
            DeactivateParticlesOnCards();
            HideMarkers();
            newGameButton.SetActive(true);
            return;
        }

        // Four of a kind or four pairs in hand, instant win 6 points
        bool playerInstantWin = CheckLucky(Player.HandCards);
        bool opponentInstantWin = CheckLucky(Opponent.HandCards);
        if (playerInstantWin ^ opponentInstantWin)
        {
            // exactly one player has an instant win
            Player luckyPlayer = playerInstantWin ? Player : Opponent;
            luckyPlayer.TempScore = 6;
            luckyPlayer.UI.TempPointsText.text = "Winning points: 6";
            luckyPlayer.UI.WinDataText.text = "Lucky hand! Four of a kind or four pairs: 6  ";
            luckyPlayer.UI.ScoreText.text = luckyPlayer.Name + " Score: " + (luckyPlayer.Score + 6).ToString();
            luckyPlayer.Win = true;
            Debug.Log(luckyPlayer.Name + " wins instantly with a lucky hand!");
            luckyPlayer.Score += 6;
            ShowWinScreen(luckyPlayer);
            gameOver = true;
            DeactivateParticlesOnCards();
            HideMarkers();
            newGameButton.SetActive(true);
        }
    }

    bool CheckLucky(List<Card> hand)
    {
        Dictionary<string, int> monthCounts = new Dictionary<string, int>();

        // Count months
        foreach (Card card in hand)
        {
            if (!monthCounts.ContainsKey(card.MonthName))
                monthCounts[card.MonthName] = 0;

            monthCounts[card.MonthName]++;
        }

        // Detect four of a kind
        bool fourOfAKind = monthCounts.ContainsValue(4);

        // Detect four pairs (4+ distinct month pairs)
        bool fourPairs = monthCounts.Values.Count(v => v == 2) >= 4;

        return fourOfAKind || fourPairs;
    }

    void CheckAndHandleThreeOfAKindOnBoard()
    {
        Dictionary<string, int> monthCounts = new Dictionary<string, int>();

        // Count months
        foreach (Card card in centerCards)
        {
            if (!monthCounts.ContainsKey(card.MonthName))
                monthCounts[card.MonthName] = 0;

            monthCounts[card.MonthName]++;
        }

        // Detect three of a kind
        bool threeOfAKind = monthCounts.ContainsValue(3);
        if (threeOfAKind)
        {
            foreach (var pair in monthCounts)
            {
                if (pair.Value == 3)
                {
                    StartCoroutine(MatchThreeOfAKind(pair.Key));
                }
            }
        }
    }

    IEnumerator MatchThreeOfAKind(string month)
    {
        // locate first card of that month in center cards
        Card firstCard = centerCards.Find(card => card.MonthName == month);
        // locate second card of that month in center cards
        Card secondCard = centerCards.Find(card => card.MonthName == month && card != firstCard);
        // locate third card of that month in center cards
        Card thirdCard = centerCards.Find(card => card.MonthName == month && card != firstCard && card != secondCard);
        if (firstCard != null && secondCard != null && thirdCard != null)
        {
            // Anchor all stacked cards to the first card's position with absolute
            // offsets so the pile is uniform regardless of prior animation drift.
            threeOfAKindBasePositions[month] = firstCard.transform.position;
            Vector3 basePos = threeOfAKindBasePositions[month];
            yield return StartCoroutine(MoveRigidbodyToPosition(secondCard.GetComponent<Rigidbody>(),
                new Vector3(basePos.x, cardRestingHeight + 0.002f, basePos.z - 0.005f),
                Quaternion.Euler(0, 180, 0),
                0.1f));
            RemoveFromCenterGrid(secondCard);
            yield return StartCoroutine(MoveRigidbodyToPosition(thirdCard.GetComponent<Rigidbody>(),
                new Vector3(basePos.x, cardRestingHeight + 0.004f, basePos.z - 0.010f),
                Quaternion.Euler(0, 180, 0),
                0.1f));
            RemoveFromCenterGrid(thirdCard);
        }
    }

    private void RemoveFromCenterGrid(Card card)
    {
        for (int i = 0; i < topRowCards.Length; i++)
        {
            if (topRowCards[i] != null && topRowCards[i].GetComponent<Card>() == card)
            {
                topRowCards[i] = null;
                return;
            }
            if (bottomRowCards[i] != null && bottomRowCards[i].GetComponent<Card>() == card)
            {
                bottomRowCards[i] = null;
                return;
            }
        }
    }

    void CheckHands()
    {
        bool canMatch = false;
        List<Card> opponentHandMatches = new List<Card>();
        List<Card> centerCardMatches = new List<Card>();
        if (timeToDraw)
        {
            // opponentHandMatches.Add(selectedCard);
            foreach (Card centerCard in centerCards)
            {
                if (selectedCard.MonthName == centerCard.MonthName)
                {
                    if (CurrentPlayer == Opponent)
                    {
                        centerCardMatches.Add(centerCard);
                    }
                    else
                    {
                        selectedCard.SetMatchHighlight(true);
                        centerCard.SetMatchHighlight(true);
                    }
                    canMatch = true;
                }
            }
        }
        else
        {
            bool highlightMatches = CurrentPlayer == Player;
            foreach (Card handCard in CurrentPlayer.HandCards)
            {
                if (handCard == null) continue;

                foreach (Card centerCard in centerCards)
                {
                    if (centerCard == null) continue;

                    if (handCard.MonthName == centerCard.MonthName)
                    {
                        canMatch = true;
                        if (highlightMatches)
                        {
                            handCard.SetMatchHighlight(true);
                            centerCard.SetMatchHighlight(true);
                        }
                        else
                        {
                            opponentHandMatches.Add(handCard);
                            centerCardMatches.Add(centerCard);
                        }
                    }
                }
            }
        }
        if (!canMatch && CurrentPlayer == Player)
        {
            ShowMarkers();
        }
        else if (!canMatch && CurrentPlayer == Opponent)
        {
            // ensure selected card exists
            if (selectedCard == null)
                selectedCard = Opponent.HandCards[UnityEngine.Random.Range(0, Opponent.HandCards.Count)];

            ShowMarkers();

            // collect available marker positions
            List<Vector3> availableMarkers = new List<Vector3>();
            for (int i = 0; i < topRowMarkers.Length; i++)
            {
                if (topRowMarkers[i].activeSelf)
                    availableMarkers.Add(topRowMarkers[i].transform.position);
                if (bottomRowMarkers[i].activeSelf)
                    availableMarkers.Add(bottomRowMarkers[i].transform.position);
            }

            if (availableMarkers.Count > 0)
            {
                // pick one and immediately place the card, enforcing correct Y height
                Vector3 chosen = availableMarkers[UnityEngine.Random.Range(0, availableMarkers.Count)];
                chosen = new Vector3(chosen.x, cardRestingHeight, chosen.z);
                MoveCardToPlaceholder(chosen);
            }

            return; // prevent Match() from running
        }
        if (CurrentPlayer == Opponent && canMatch)
        {
            if (!timeToDraw)
            {
                selectedCard = opponentHandMatches[UnityEngine.Random.Range(0, opponentHandMatches.Count)];
            }
            List<Card> possibleCenterMatches = centerCards.Where(c => c.MonthName == selectedCard.MonthName).ToList();
            Card centerMatch = possibleCenterMatches[UnityEngine.Random.Range(0, possibleCenterMatches.Count)];
            Match(centerMatch);
        }
    }

    public void SetCardSelectedBool(Card card)
    {
        if (selectedCard != null)
        {
            selectedCard.isSelected = false;
            StartCoroutine(MoveRigidbodyToPosition(selectedCard.GetComponent<Rigidbody>(),
                new Vector3(selectedCard.transform.position.x, cardRestingHeight, selectedCard.transform.position.z),
                selectedCard.transform.rotation,
                0.1f));
        }
        selectedCard = card;
        card.isSelected = true;
        StartCoroutine(MoveRigidbodyToPosition(card.GetComponent<Rigidbody>(),
            new Vector3(card.transform.position.x, cardRestingHeight + 0.01f, card.transform.position.z),
            selectedCard.transform.rotation,
            0.1f));
    }

    public void Match(Card centerCard)
    {
        StartCoroutine(MatchCoroutine(centerCard));
    }

    public IEnumerator MatchCoroutine(Card centerCard)
    {
        // If the played card completes a stacked trio on the board, land it on the
        // pile using the trio's base position with one more 0.002 y / 0.005 z step
        // (0.006 / 0.015). Otherwise use the normal pair offset from the target card.
        bool completesTrio = centerCards.Count(card => card.Month == selectedCard.Month) >= 3;
        Vector3 landingPosition;
        if (completesTrio && threeOfAKindBasePositions.TryGetValue(selectedCard.MonthName, out Vector3 trioBase))
        {
            landingPosition = trioBase + new Vector3(0f, 0.006f, -0.015f);
        }
        else
        {
            landingPosition = centerCard.transform.position + new Vector3(0f, 0.002f, -0.01f);
        }
        yield return StartCoroutine(MoveRigidbodyToPosition(selectedCard.GetComponent<Rigidbody>(),
            landingPosition,
            Quaternion.Euler(0, 180, 0),
            0.1f));

        DeactivateParticlesOnCards();

        // The acting player claims the match.
        Player actingPlayer = CurrentPlayer;
        // Three of a kind on board, claim all cards of that month
        if (completesTrio)
        {
            foreach (Card card in centerCards)
            {
                if (card.MonthName == selectedCard.MonthName)
                {
                    actingPlayer.MatchedCards.Add(card);
                    tempMatchedCards.Add(card);
                }
            }
        }
        else
        {
            actingPlayer.MatchedCards.Add(centerCard);
            tempMatchedCards.Add(centerCard);
        }
        actingPlayer.HandCards.Remove(selectedCard);
        actingPlayer.MatchedCards.Add(selectedCard);
        centerCards.Add(selectedCard);
        tempMatchedCards.Add(selectedCard);

        selectedCard = null;

        if (timeToDraw)
        {
            yield return new WaitForSeconds(0.8f); // short pause before claiming matches
            ClaimMatches();
            RestructurePlayerHand();
            RestructureOpponentHand();
            timeToDraw = false;
            isDrawingFromDeck = false;
        }
        else
        {
            drawText.gameObject.SetActive(true);
            timeToDraw = true;
            // Auto-draw for opponent so their turn completes without player input
            if (CurrentPlayer == Opponent)
            {
                DrawFromDeck();
            }
        }
        HideMarkers();
    }

    public void MoveCardToPlaceholder(Vector3 position)
    {
        StartCoroutine(MoveCardToPlaceholderCoroutine(position));
    }

    IEnumerator MoveCardToPlaceholderCoroutine(Vector3 position)
    {
        yield return StartCoroutine(MoveRigidbodyToPosition(selectedCard.GetComponent<Rigidbody>(),
            new Vector3(position.x, cardRestingHeight, position.z),
            Quaternion.Euler(0, 180, 0),
            0.1f));

        centerCards.Add(selectedCard);
        if (Player.HandCards.Contains(selectedCard))
        {
            Player.HandCards.Remove(selectedCard);
        }
        if (Opponent.HandCards.Contains(selectedCard))
        {
            Opponent.HandCards.Remove(selectedCard);
        }

        // Repopulate grid arrays based on where the card was placed
        for (int i = 0; i < topRowMarkers.Length; i++)
        {
            Vector3 tm = topRowMarkers[i].transform.position;
            Vector3 bm = bottomRowMarkers[i].transform.position;

            // Match by x/z (ignore y)
            if (Mathf.Approximately(position.x, tm.x) && Mathf.Approximately(position.z, tm.z))
            {
                topRowCards[i] = selectedCard.gameObject;
                break;
            }
            if (Mathf.Approximately(position.x, bm.x) && Mathf.Approximately(position.z, bm.z))
            {
                bottomRowCards[i] = selectedCard.gameObject;
                break;
            }
        }
        DeactivateParticlesOnCards();
        HideMarkers();
        selectedCard = null;

        if (timeToDraw)
        {
            yield return new WaitForSeconds(0.8f); // short pause before claiming matches
            ClaimMatches();
            RestructurePlayerHand();
            RestructureOpponentHand();
            timeToDraw = false;
            isDrawingFromDeck = false;
        }
        else
        {
            if (CurrentPlayer == Player)
            {
                drawText.gameObject.SetActive(true);
                timeToDraw = true;    
            }
            else
            {
                timeToDraw = true;
                DrawFromDeck();
            }
        }
    }

    public void DrawFromDeck()
    {
        if (isDrawingFromDeck) return;
        isDrawingFromDeck = true;
        StartCoroutine(DrawFromDeckCoroutine());
    }

    IEnumerator DrawFromDeckCoroutine()
    {
        drawText.gameObject.SetActive(false);
        Card card = deck.DrawCard();
        selectedCard = card;
        deckCards.Remove(card);
        Rigidbody rb = card.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        Quaternion dealRotation = Quaternion.Euler(0, 180, 0);
        yield return StartCoroutine(MoveRigidbodyToPosition(rb,
        new Vector3(card.transform.position.x + 0.05f, card.transform.position.y + 0.02f, card.transform.position.z),
        dealRotation, 0.1f));
        // If the board holds exactly two of the drawn month (from the deal or
        // from a pair matched from hand earlier this turn), the drawn card
        // completes a three-of-a-kind: stack all three and leave them on the
        // board. They are claimed when someone plays or draws the fourth.
        if (centerCards.Count(c => c.MonthName == selectedCard.MonthName) == 2)
        {
            string month = selectedCard.MonthName;
            selectedCard = null;
            centerCards.Add(card);
            // Un-claim any of THIS month's cards pending claim from the hand-play
            // phase so they stay on the board with the stack. Other pending
            // matches are unaffected and still get claimed.
            foreach (Card c in tempMatchedCards.Where(c => c.MonthName == month).ToList())
            {
                CurrentPlayer.MatchedCards.Remove(c);
                tempMatchedCards.Remove(c);
            }
            yield return StartCoroutine(MatchThreeOfAKind(month));
            // Claim any remaining pending matches from the hand-play phase.
            // (ClaimMatches ends the turn itself when it runs.)
            if (tempMatchedCards.Count > 0)
            {
                yield return new WaitForSeconds(0.8f); // short pause before claiming matches
                ClaimMatches();
            }
            else
            {
                SwitchPlayerTurn();
            }
            timeToDraw = false;
            isDrawingFromDeck = false;
            RestructurePlayerHand();
            RestructureOpponentHand();
        }
        // If the board already holds a completed month (3+ cards), the drawn card
        // claims all four cards of that month.
        else if (centerCards.Count(c => c.MonthName == selectedCard.MonthName) >= 3)
        {
            string month = selectedCard.MonthName;
            Debug.Log($"{CurrentPlayer.Name} drew the fourth {month} — claiming all four.");
            selectedCard = null;
            centerCards.Add(card);
            if (threeOfAKindBasePositions.TryGetValue(month, out Vector3 trioBase))
            {
                yield return StartCoroutine(MoveRigidbodyToPosition(rb,
                    trioBase + new Vector3(0f, 0.006f, -0.015f), dealRotation, 0.1f));
            }
            foreach (Card c in centerCards)
            {
                if (c.MonthName == month)
                {
                    CurrentPlayer.MatchedCards.Add(c);
                    tempMatchedCards.Add(c);
                }
            }
            yield return new WaitForSeconds(0.8f); // short pause before claiming matches
            ClaimMatches();
            // ClaimMatches ends the turn via its yaku/turn logic.
            timeToDraw = false;
            isDrawingFromDeck = false;
            RestructurePlayerHand();
            RestructureOpponentHand();
        }
        else
        {
            CheckHands();
        }
    }

    public void DeactivateParticlesOnCards()
    {
        foreach (Player p in new[] { Player, Opponent })
        {
            foreach (Card handCard in p.HandCards)
            {
                handCard.SetMatchHighlight(false);
            }
        }
        foreach (Card centerCard in centerCards)
        {
            centerCard.SetMatchHighlight(false);
        }
        if (selectedCard != null)
        {
            selectedCard.SetMatchHighlight(false);
        }
    }

    public void ShowMarkers()
    {
        if (CurrentPlayer == Opponent && !timeToDraw)
        {
            selectedCard = Opponent.HandCards[UnityEngine.Random.Range(0, Opponent.HandCards.Count)];
        }
        bool hadSpaceInCenter = false;
        // show inside markers first
        for (int i = 2; i < topRowCards.Length - 2; i++)
        {
            if (topRowCards[i] == null)
            {
                topRowMarkers[i].SetActive(true);
                hadSpaceInCenter = true;
            }
            if (bottomRowCards[i] == null)
            {
                bottomRowMarkers[i].SetActive(true);
                hadSpaceInCenter = true;
            }
        }
        // show neighboring markers if no space in center
        if (!hadSpaceInCenter)
        {
            for (int i = 0; i < topRowCards.Length; i++)
            {
                bool hasNeighbor =
                    (i > 0 && topRowCards[i - 1] != null) ||
                    (i < topRowCards.Length - 1 && topRowCards[i + 1] != null);
                if (hasNeighbor && topRowCards[i] == null)
                {
                    topRowMarkers[i].SetActive(true);
                }
            }
            for (int i = 0; i < bottomRowCards.Length; i++)
            {
                bool hasNeighbor =
                    (i > 0 && bottomRowCards[i - 1] != null) ||
                    (i < bottomRowCards.Length - 1 && bottomRowCards[i + 1] != null);
                if (hasNeighbor && bottomRowCards[i] == null)
                {
                    bottomRowMarkers[i].SetActive(true);
                }
            }
        }
    }

    public void HideMarkers()
    {
        foreach (GameObject marker in topRowMarkers)
        {
            marker.SetActive(false);
        }
        foreach (GameObject marker in bottomRowMarkers)
        {
            marker.SetActive(false);
        }
    }

    void ClaimMatches()
    {
        Player claimingPlayer = CurrentPlayer;
        foreach (Card card in tempMatchedCards)
        {
            centerCards.Remove(card);
            Vector3 pilePosition = claimingPlayer.CaptureCard(card);
            StartCoroutine(MoveRigidbodyToPosition(card.GetComponent<Rigidbody>(),
                pilePosition,
                card.transform.rotation,
                0.1f));
            // clear the indices for the markers
            for (int i = 0; i < topRowCards.Length; i++)
            {
                if (topRowCards[i] != null && topRowCards[i].GetComponent<Card>() == card)
                {
                    topRowCards[i] = null;
                }
                if (bottomRowCards[i] != null && bottomRowCards[i].GetComponent<Card>() == card)
                {
                    bottomRowCards[i] = null;
                }
            }
        }
        tempMatchedCards.Clear();
        if (Player.HasCapturedCards)
        {
            viewMatchesButton.SetActive(true);
        }
        if (Opponent.HasCapturedCards)
        {
            viewOpponentMatchesButton.SetActive(true);
        }
        if (CurrentPlayer == Player)
        {
            Debug.Log("Player turn, figuring out what to do");
            // Koi-Koi only pays out if a new yaku (higher score) is formed;
            // the threshold is the score they called Koi-Koi with.
            int threshold = Player.KoiKoi ? Player.PreviousTempScore : 0;
            int playerScoreThisTurn = CheckYaku(Player, threshold, tempPlayerPointsText, playerWinDataText);
            if (playerScoreThisTurn > 0)
            {
                ShowWinScreen(Player);
                return;
            }
            if (Player.KoiKoi)
            {
                Debug.Log("Player did not achieve a new Yaku during Koi-Koi.");
            }
            if (Player.HandCards.Count == 0 && Opponent.HandCards.Count == 0 && !Player.Win && !Opponent.Win)
            {
                ExhuastiveDraw();
            }
            else
            {
                SwitchPlayerTurn();
            }
            return;
        }
        else if (CurrentPlayer == Opponent)
        {
            Debug.Log("Opponent turn, figuring out what to do");
            // Koi-Koi only pays out if a new yaku (higher score) is formed;
            // the threshold is the score they called Koi-Koi with.
            int threshold = Opponent.KoiKoi ? Opponent.PreviousTempScore : 0;
            int opponentScoreThisTurn = CheckYaku(Opponent, threshold, tempOpponentPointsText, opponentWinDataText);
            if (opponentScoreThisTurn > 0)
            {
                ShowWinScreen(Opponent);
                return;
            }
            if (Opponent.KoiKoi)
            {
                Debug.Log("Opponent did not achieve a new Yaku during Koi-Koi.");
            }
            if (Player.HandCards.Count == 0 && Opponent.HandCards.Count == 0 && !Player.Win && !Opponent.Win)
            {
                ExhuastiveDraw();
            }
            else
            {
                SwitchPlayerTurn();
            }
            return;
        }
    }

    void SwitchPlayerTurn()
    {
        (CurrentPlayer, WaitingPlayer) = (WaitingPlayer, CurrentPlayer);
        Player.IsTurn = CurrentPlayer == Player;
        Opponent.IsTurn = CurrentPlayer == Opponent;
        Debug.Log(CurrentPlayer == Player ? "PLAYER TURN" : "OPPONENT TURN");
    }

    void RestructureHand(Player player, float handZ)
    {
        if (player.HandCards.Count == 0) return;

        // Determine total width based on spacing (0.03 between each card)
        float spacing = 0.03f;
        float totalWidth = (player.HandCards.Count - 1) * spacing;

        // Center cards around x = 0
        float startX = -totalWidth / 2f;

        for (int i = 0; i < player.HandCards.Count; i++)
        {
            Card card = player.HandCards[i];
            if (card == null) continue;

            Rigidbody rb = card.GetComponent<Rigidbody>();

            // Calculate the new target position
            Vector3 targetPos = new Vector3(startX + i * spacing, cardRestingHeight, handZ);

            // Smoothly move cards back into place
            StartCoroutine(MoveRigidbodyToPosition(rb, targetPos, card.transform.rotation, 0.1f));
        }
    }

    void RestructurePlayerHand() => RestructureHand(Player, -0.09f);

    void RestructureOpponentHand() => RestructureHand(Opponent, 0.09f);

    IEnumerator GameLoop()
    {
        // Main game loop logic
        while (!gameOver)
        {
            yield return new WaitForSeconds(1.0f); // brief pause before the turn
            yield return StartCoroutine(TurnCoroutine(CurrentPlayer));
        }
        Debug.Log("Game Over!");
    }

    IEnumerator TurnCoroutine(Player turnPlayer)
    {
        CheckHands();
        // Wait until the turn passes to someone else (or the game ends).
        yield return new WaitUntil(() => CurrentPlayer != turnPlayer || gameOver);
    }

    /// Evaluates the player's captured piles for yaku and returns the points
    /// won this turn (0 = no win). During Koi-Koi, pass thresholdScore =
    /// PreviousTempScore: a win then requires a new yaku (score above the
    /// threshold) and the points are doubled.
    int CheckYaku(Player player, int thresholdScore, TMP_Text pointsText, TMP_Text winDataText)
    {
        string winData = "";
        player.TempScore = 0;
        int winnings = 0;

        // 10 shit cards
        if (player.ShitCards.Count >= 10)
        {
            // 10 shit cards = 1 point + 1 point for each additional
            winnings = player.ShitCards.Count - 9;
            player.TempScore += winnings;
            winData += $"Shit cards: {winnings}  ";
        }
        // Poetry AND Blue ribbons
        if (player.RibbonCards.Count(c => c.RibbonName == "Text") >= 3 &&
            player.RibbonCards.Count(c => c.RibbonName == "Blue") >= 3)
        {
            player.TempScore += 12;
            winData += "Poetry AND Blue ribbons: 12  ";
            // +1 for each additional ribbon
            if (player.RibbonCards.Count > 6)
            {
                player.TempScore += player.RibbonCards.Count - 6;
                winData += $" +{player.RibbonCards.Count - 6} for additional ribbons  ";
            }
        }
        // Red poetry ribbons
        else if (player.RibbonCards.Count(c => c.RibbonName == "Text") >= 3)
        {
            player.TempScore += 6;
            winData += "Red poetry ribbons: 6  ";
            // +1 for each additional ribbon
            if (player.RibbonCards.Count > 3)
            {
                player.TempScore += player.RibbonCards.Count - 3;
                winData += $" +{player.RibbonCards.Count - 3} for additional ribbons  ";
            }
        }
        // Blue ribbons
        else if (player.RibbonCards.Count(c => c.RibbonName == "Blue") >= 3)
        {
            player.TempScore += 6;
            winData += "Blue ribbons: 6  ";
            // +1 for each additional ribbon
            if (player.RibbonCards.Count > 3)
            {
                player.TempScore += player.RibbonCards.Count - 3;
                winData += $" +{player.RibbonCards.Count - 3} for additional ribbons  ";
            }
        }
        // 5 ribbon cards
        else if (player.RibbonCards.Count >= 5)
        {
            // 5 ribbon cards = 1 point + 1 point for each additional
            winnings = player.RibbonCards.Count - 4;
            player.TempScore += winnings;
            winData += $"Ribbon cards: {winnings}  ";
        }
        // Ino-shika-cho
        bool hasBoar = player.AnimalCards.Any(c => c.AnimalName == "Boar");
        bool hasDeer = player.AnimalCards.Any(c => c.AnimalName == "Deer");
        bool hasButterfly = player.AnimalCards.Any(c => c.AnimalName == "Butterfly");

        if (hasBoar && hasDeer && hasButterfly)
        {
            player.TempScore += 5;
            winData += "Ino-shika-cho: 5  ";
            // +1 for each additional animal
            if (player.AnimalCards.Count > 3)
            {
                player.TempScore += player.AnimalCards.Count - 3;
                winData += $" +{player.AnimalCards.Count - 3} for additional animals  ";
            }
        }
        // 5 animal cards
        else if (player.AnimalCards.Count >= 5)
        {
            // 5 animal cards = 1 point + 1 point for each additional
            winnings = player.AnimalCards.Count - 4;
            player.TempScore += winnings;
            winData += $"Animal cards: {winnings}  ";
        }
        // Brights
        int brightCount = player.BrightCards.Count;
        if (brightCount >= 5)
        {
            player.TempScore += 15;
            winData += "Five brights: 15  ";
        }
        else if (brightCount == 4 && !player.BrightCards.Any(c => c.BrightName == "RainMan"))
        {
            player.TempScore += 10;
            winData += "Four brights (no RainMan): 10  ";
        }
        else if (brightCount == 4 && player.BrightCards.Any(c => c.BrightName == "RainMan"))
        {
            player.TempScore += 8;
            winData += "Four brights (with RainMan): 8  ";
        }
        else if (brightCount == 3 && !player.BrightCards.Any(c => c.BrightName == "RainMan"))
        {
            player.TempScore += 6;
            winData += "Three brights (no RainMan): 6  ";
        }
        // Flower/Moon viewing
        bool hasSakura = player.BrightCards.Any(c => c.BrightName == "Sakura");
        bool hasMoon = player.BrightCards.Any(c => c.BrightName == "Moon");
        bool hasSakeCup = player.AnimalCards.Any(c => c.AnimalName == "Sake");
        if (hasMoon && hasSakeCup)
        {
            player.TempScore += 5;
            winData += "Moon viewing: 5  ";
        }
        if (hasSakura && hasSakeCup)
        {
            player.TempScore += 5;
            winData += "Sakura viewing: 5  ";
        }
        // Cards of the Month
        string currentRoundMonth = CurrentRoundSwitch(currentRound);
        if (player.MatchedCards.Count(c => c.MonthName == currentRoundMonth) >= 4)
        {
            player.TempScore += 4;
            winData += "Cards of the month: 4  ";
        }
        // Double points for 7 or more
        if (player.TempScore >= 7)
        {
            winData += $"\n>=7 points, doubled: {player.TempScore} * 2 = {player.TempScore * 2}  ";
            player.TempScore *= 2; // double points for 7 or more
        }
        if (player.PreviousTempScore == 0 && player.TempScore > 0)
        {
            player.PreviousTempScore = player.TempScore;
            pointsText.text = "Winning points: " + player.TempScore.ToString();
        }
        winDataText.text = winData;

        // During Koi-Koi only a new yaku (score above the threshold) wins.
        if (player.TempScore <= thresholdScore)
        {
            return 0;
        }
        if (thresholdScore > 0)
        {
            player.TempScore *= 2; // double points for Koi-Koi
            winDataText.text += "\nKoi-Koi! Points doubled to: " + player.TempScore.ToString();
            pointsText.text = "Winning points: " + player.TempScore.ToString();
            player.PreviousTempScore = player.TempScore;
        }
        return player.TempScore;
    }

    void ShowWinScreen(Player player)
    {
        player.Win = true;
        player.UI.WinText.gameObject.SetActive(true);
        player.UI.TempPointsText.gameObject.SetActive(true);
        player.UI.ClaimWinButton.SetActive(true);
        player.UI.WinDataText.gameObject.SetActive(true);
        if (player.HandCards.Count > 0)
        {
            player.UI.KoiKoiButton.SetActive(true);
        }
    }

    void DisableWinScreen(Player player)
    {
        player.Win = false;
        player.UI.WinText.gameObject.SetActive(false);
        player.UI.TempPointsText.gameObject.SetActive(false);
        player.UI.ClaimWinButton.SetActive(false);
        player.UI.KoiKoiButton.SetActive(false);
        player.UI.WinDataText.gameObject.SetActive(false);
    }

    /// Claims the win: banks the temp score and advances to the next round
    /// (or ends the match after December).
    void Win(Player player)
    {
        player.Score += player.TempScore;
        player.UI.ScoreText.text = player.Name + " Score: " + player.Score.ToString();
        DisableWinScreen(player);
        if (currentRound < 11)
        {
            NewGame();
        }
        else
        {
            gameOverText.gameObject.SetActive(true);
            newGameButton.SetActive(true);
        }
    }

    /// Calls Koi-Koi: snapshots the current score as the threshold to beat,
    /// then play continues with the turn passing to the other player.
    void CallKoiKoi(Player player)
    {
        player.PreviousTempScore = CheckYaku(player, 0, player.UI.TempPointsText, player.UI.WinDataText);
        player.KoiKoi = true;
        DisableWinScreen(player);
        player.TempScore = 0;
        player.UI.TempPointsText.text = "Winning points: 0";
        SwitchPlayerTurn();
    }

    // Button wrappers (bound in the Unity Inspector).
    public void PlayerWin() => Win(Player);
    public void PlayerKoiKoi() => CallKoiKoi(Player);
    public void OpponentWin() => Win(Opponent);
    public void OpponentKoiKoi() => CallKoiKoi(Opponent);
    
    void ExhuastiveDraw()
    {
        Debug.Log("Exhaustive Draw! No more cards in hands.");
        if (currentRound < 11)
        {
            NewGame();
        }
        else
        {
            gameOverText.gameObject.SetActive(true);
            newGameButton.SetActive(true);
        }
    }

}
