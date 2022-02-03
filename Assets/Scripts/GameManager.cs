using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public GameObject Canvas;
    public GameObject DropZone;
    public GameObject DropZone2;
    public GameObject EnemyZone;
    public GameObject PlayerZone;

    public Text PlayerPoints;
    public Text EnemyPoints;

    private DrawCards drawCards;
    private int lastCountCards = 0;
    private int turnCount = 0;

    private void Awake()
    {
        PlayerPrefs.SetString("Turn", "Player");
    }

    void Start()
    {
        DropZone = GameObject.Find("DropZone");
        DropZone2 = GameObject.Find("DropZone2");
        EnemyZone = GameObject.Find("EnemyArea");
        PlayerZone = GameObject.Find("PlayerArea");
        GetComponent<DrawCards>().draw();
        StartCoroutine(BotIntelligence());
    }

    void Update()
    {
        UpdatePoints();
        EndGame();
    }


    public void CardsEffects(Transform card)
    {
        if (!card.GetComponent<CardInfo>().hasEffect)
        {
            return;
        }

        if (card.GetComponent<CardInfo>().name == "Yen")
        {
            if (card.tag == "PlayerCard")
            {
                GameObject cardClone =
                    Instantiate(GetComponent<DrawCards>().Card2, new Vector2(0, 0), Quaternion.identity);
                cardClone.transform.SetParent(DropZone2.transform, false);
                cardClone.tag = "PlayerCard";
                CardsEffects(cardClone.transform);
            }
            else
            {
                GameObject cardClone =
                    Instantiate(GetComponent<DrawCards>().Card2, new Vector2(0, 0), Quaternion.identity);
                cardClone.transform.SetParent(DropZone.transform, false);
                cardClone.tag = "EnemyCard";
                CardsEffects(cardClone.transform);
            }
        }


        if (card.GetComponent<CardInfo>().name == "Geralt")
        {
            if (card.tag == "PlayerCard")
            {
                var lastChild = DropZone.transform.childCount;
                if (lastChild > 0)
                {
                    Destroy(DropZone.transform.GetChild(lastChild - 1).gameObject);
                }
            }
            else
            {
                var lastChild = DropZone2.transform.childCount;
                if (lastChild > 0)
                {
                    Destroy(DropZone2.transform.GetChild(lastChild - 1).gameObject);
                }
            }
        }

        if (card.GetComponent<CardInfo>().name == "Nekker")
        {
            for (int i = 0; i < 2; i++)
            {
                GameObject cardClone = Instantiate(card.gameObject, new Vector2(0, 0), Quaternion.identity);
                if (card.tag == "PlayerCard")
                {
                    cardClone.transform.SetParent(DropZone2.transform, false);
                }
                else
                {
                    cardClone.transform.SetParent(DropZone.transform, false);
                }
            }
        }
    }

    IEnumerator BotIntelligence()
    {
        if (EnemyZone.transform.childCount == 0)
        {
            yield return null;
        }

        if (PlayerPrefs.GetString("Turn") == "Enemy" || PlayerZone.transform.childCount == 0)
        {
            int cardNumber = Random.Range(0, EnemyZone.transform.childCount);
            Transform card = EnemyZone.transform.GetChild(cardNumber);
            card.SetParent(DropZone.transform, false);
            card.GetComponent<CardFlipper>().Flip();
            if (card.GetComponent<CardInfo>().hasEffect)
            {
                CardsEffects(card);
            }
            passTurn();
        }

        yield return new WaitForSeconds(2);
        StartCoroutine(BotIntelligence());
    }

    void EndGame()
    {
        int cardsNotPlayed = PlayerZone.transform.childCount + EnemyZone.transform.childCount;
        if (cardsNotPlayed != 0)
        {
            return;
        }

        if (int.Parse(PlayerPoints.text) > int.Parse(EnemyPoints.text))
        {
            PlayerPrefs.SetString("Winner", "Player");
            Debug.Log("Player WIN");
        }
        else
        {
            PlayerPrefs.SetString("Winner", "Bot");
            Debug.Log("Enemy WIN");
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(2);
    }

    void UpdatePoints()
    {
        if (lastCountCards != DropZone.transform.childCount || lastCountCards != DropZone2.transform.childCount)
        {
            int playerPoints = 0;
            int enemyPoints = 0;
            for (int i = 0; i < DropZone.transform.childCount; i++)
            {
                if (DropZone.transform.GetChild(i).tag != "PlayerCard")
                {
                    enemyPoints += DropZone.transform.GetChild(i).GetComponent<CardInfo>().strength;
                }
            }

            for (int i = 0; i < DropZone2.transform.childCount; i++)
            {
                if (DropZone2.transform.GetChild(i).tag == "PlayerCard")
                {
                    playerPoints += DropZone2.transform.GetChild(i).GetComponent<CardInfo>().strength;
                }
            }

            PlayerPoints.text = playerPoints.ToString();
            EnemyPoints.text = enemyPoints.ToString();
            lastCountCards++;
        }
    }

    public void passTurn()
    {
        if (PlayerPrefs.GetString("Turn") == "Player")
        {
            PlayerPrefs.SetString("Turn", "Enemy");
        }
        else
        {
            PlayerPrefs.SetString("Turn", "Player");
        }

        turnCount++;
    }
}