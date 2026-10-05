using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Holds all per-player state for a Koi-Koi game: turn, hand, captured
/// card piles, pile layout positions, and scoring.
///
/// This is a plain data holder for now. Behavior (playing cards, claiming
/// matches, yaku checks) still lives in KoiKoiGameManager and can move here
/// in later refactor steps.
/// </summary>
public class Player
{
    /// References to this player's score/win UI elements.
    [System.Serializable]
    public class PlayerUI
    {
        public TMP_Text ScoreText;
        public TMP_Text WinText;
        public TMP_Text TempPointsText;
        public TMP_Text WinDataText;
        public GameObject ClaimWinButton;
        public GameObject KoiKoiButton;
    }

    // Vertical spacing between the captured-card piles on the table.
    private const float PileSpacingZ = 0.04f;

    public string Name;
    public PlayerUI UI;

    // Turn state.
    public bool IsTurn;

    // Cards currently in hand.
    public List<Card> HandCards = new List<Card>();

    // All cards this player has captured this round.
    public List<Card> MatchedCards = new List<Card>();

    // Captured piles, sorted by card type.
    public List<Card> BrightCards = new List<Card>();
    public List<Card> AnimalCards = new List<Card>();
    public List<Card> RibbonCards = new List<Card>();
    public List<Card> ShitCards = new List<Card>();

    // Where the next captured card of each type is placed on the table.
    public Vector3 BrightCardsPosition;
    public Vector3 AnimalCardsPosition;
    public Vector3 RibbonCardsPosition;
    public Vector3 ShitCardsPosition;

    private readonly Vector3 initialBrightCardsPosition;
    private readonly Vector3 initialAnimalCardsPosition;
    private readonly Vector3 initialRibbonCardsPosition;
    private readonly Vector3 initialShitCardsPosition;
    private readonly float rowDirection;

    // Scoring.
    public int Score;
    public int TempScore;
    public int PreviousTempScore;

    // Round outcome flags.
    public bool Win;
    public bool KoiKoi;

    /// <summary>
    /// Creates a player whose captured-card piles start at
    /// <paramref name="shitCardsPosition"/> and fan out along the z-axis in the
    /// given direction (default +z; the opponent passes -1 to mirror the rows).
    /// </summary>
    public Player(string name, Vector3 shitCardsPosition, float rowDirection = 1f)
    {
        Name = name;
        this.rowDirection = rowDirection;
        float spacing = PileSpacingZ * rowDirection;
        initialShitCardsPosition = shitCardsPosition;
        initialRibbonCardsPosition = shitCardsPosition + new Vector3(0f, 0f, spacing);
        initialAnimalCardsPosition = shitCardsPosition + new Vector3(0f, 0f, spacing * 2f);
        initialBrightCardsPosition = shitCardsPosition + new Vector3(0f, 0f, spacing * 3f);
        ResetForNewRound();
    }

    /// <summary>
    /// Clears all per-round state: hands, captured piles, pile positions,
    /// temp scores, and win/koi-koi flags. Total Score is preserved.
    /// </summary>
    public void ResetForNewRound()
    {
        HandCards.Clear();
        MatchedCards.Clear();
        BrightCards.Clear();
        AnimalCards.Clear();
        RibbonCards.Clear();
        ShitCards.Clear();

        ShitCardsPosition = initialShitCardsPosition;
        RibbonCardsPosition = initialRibbonCardsPosition;
        AnimalCardsPosition = initialAnimalCardsPosition;
        BrightCardsPosition = initialBrightCardsPosition;

        TempScore = 0;
        PreviousTempScore = 0;
        Win = false;
        KoiKoi = false;
    }

    /// True if this player has captured any cards this round.
    public bool HasCapturedCards =>
        BrightCards.Count > 0 || AnimalCards.Count > 0 ||
        RibbonCards.Count > 0 || ShitCards.Count > 0;

    /// Adds a captured card to the matching pile and returns the table
    /// position the card should be moved to. The pile's placement position
    /// then advances for the next card: 0.03 between cards in a column,
    /// 0.04 after every third card, in the player's row direction.
    public Vector3 CaptureCard(Card card)
    {
        if (card.IsBright) return Capture(card, BrightCards, ref BrightCardsPosition);
        if (card.IsAnimal) return Capture(card, AnimalCards, ref AnimalCardsPosition);
        if (card.IsRibbon) return Capture(card, RibbonCards, ref RibbonCardsPosition);
        return Capture(card, ShitCards, ref ShitCardsPosition);
    }

    private Vector3 Capture(Card card, List<Card> pile, ref Vector3 position)
    {
        Vector3 cardPosition = position;
        pile.Add(card);
        position.x += (pile.Count % 3 == 0 ? 0.04f : 0.03f) * rowDirection;
        return cardPosition;
    }
}
