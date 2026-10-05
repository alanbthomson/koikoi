using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    private Dictionary<Card.MonthType, List<Card>> cards;

    public void AddCard(Card card)
    {
        if(cards.ContainsKey(card.Month))
        {
            cards[card.Month].Add(card);
        }
        else
        {
            List<Card> newCards = new List<Card>();
            newCards.Add(card);
            cards.Add(card.Month, newCards);
        }
    }

    public void RemoveCard()
    {
        
    }

    // commented to make compiler happy
    // private Card GetCard(int id)
    // {

    // }

    public void ResetHand()
    {

    }
}
