using Unity.VisualScripting;
using UnityEngine;

public class Placeholder : MonoBehaviour
{
    public KoiKoiGameManager gameManager;
    public int placeholderIndex;
    public bool topRow;
    public bool bottomRow;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager = FindFirstObjectByType<KoiKoiGameManager>();
    }

    void OnMouseDown()
    {
        Debug.Log("Placeholder clicked");
        if (gameManager.selectedCard != null)
        {
            gameManager.MoveCardToPlaceholder(transform.position);
        }
        // fill placeholder array
        if (topRow)
        {
            gameManager.topRowCards[placeholderIndex] = gameManager.selectedCard.GameObject();
        }
        if (bottomRow)
        {
            gameManager.bottomRowCards[placeholderIndex] = gameManager.selectedCard.GameObject();
        }
    }
}
