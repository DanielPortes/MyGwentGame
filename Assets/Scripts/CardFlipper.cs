using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;

public class CardFlipper : MonoBehaviour
{
    public Sprite CardFront;
    public Sprite CardBack;

    public void Flip()
    {
        Sprite currentSprite = gameObject.GetComponent<Image>().sprite;
        if (currentSprite == CardFront)
        {
            gameObject.GetComponent<Image>().sprite = CardBack;
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                gameObject.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
        else
        {
            gameObject.GetComponent<Image>().sprite = CardFront;
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                gameObject.transform.GetChild(i).gameObject.SetActive(true);
            }
        }
    }
}