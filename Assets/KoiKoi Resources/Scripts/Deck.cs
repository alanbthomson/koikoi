using System.Collections.Generic;
using UnityEngine;

public class Deck : MonoBehaviour
{
    private List<Card> _deckCards = new List<Card>();
    private List<Card> _usedCards = new List<Card>();
    
    public int CardCount => _deckCards.Count;
    public bool IsEmpty => CardCount == 0;
    
    public void AddCard(Card card) {
        _deckCards.Add(card);
    }

    public Card DrawCard()
    {
        if (_deckCards.Count == 0) return null;

        Card card = _deckCards[^1]; // The [^n] notation takes the n'th last element from a list, super weird...
        _usedCards.Add(card);
        _deckCards.RemoveAt(_deckCards.Count - 1);
        return card;
    }
    
    public Card GetCardAt(int index)
    {
        if (index < 0 || index >= _deckCards.Count)
            return null;
        return _deckCards[index];
    }

    /// Debug helper: swaps a card so it will be drawn at the given 1-based
    /// draw position (draws come from the end of the list).
    public void SwapToDrawPosition(Card card, int drawPosition)
    {
        int target = _deckCards.Count - drawPosition;
        int current = _deckCards.IndexOf(card);
        if (current < 0 || target < 0 || target >= _deckCards.Count || current == target) return;
        (_deckCards[current], _deckCards[target]) = (_deckCards[target], _deckCards[current]);
    }

    public void Shuffle() {
        List<Card> temp = new List<Card>();
        // Shuffle the deck first
        while (_deckCards.Count != 0) {
            int i_nextCard = Random.Range(0, _deckCards.Count);
            temp.Add(_deckCards[i_nextCard]);
            _deckCards.RemoveAt(i_nextCard);
        }

        // Do it again the other way because why not?
        while (temp.Count != 0) {
            int i_nextCard = Random.Range(0, temp.Count);
            _deckCards.Add(temp[i_nextCard]);
            temp.RemoveAt(i_nextCard);
        }
    }

    public void ResetDeck()
    {
        _deckCards.AddRange(_usedCards);
        _usedCards.Clear();
    }

    public void ClearAllCards()
    {
        _deckCards.Clear();
        _usedCards.Clear();
    }

    public void PrintDebugInfo() {
        foreach (Card card in _deckCards)
            card.printCardDebugInfo();
    }
}
