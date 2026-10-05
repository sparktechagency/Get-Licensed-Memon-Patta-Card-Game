using MemonPatta.Rules;
using UnityEngine;

namespace MemonPatta.UI
{
    // 52 face sprites indexed by suit * 13 + (rank - 1); suits are Clubs, Diamonds, Hearts, Spades.
    public class CardSprites : MonoBehaviour
    {
        public Sprite[] faces = new Sprite[52];

        public Sprite Get(int cardId)
        {
            int index = Cards.Suit(cardId) * 13 + Cards.Rank(cardId) - 1;
            return index >= 0 && index < faces.Length ? faces[index] : null;
        }
    }
}
