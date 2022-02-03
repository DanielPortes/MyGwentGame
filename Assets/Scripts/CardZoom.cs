using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CardZoom : MonoBehaviour
{
    public GameObject Canvas;
    public GameObject ZoomCard;
    public GameObject EnemyZone;
    public RawImage Clip;

    private RawImage clip;
    private GameObject zoomCard;
    private Sprite zoomSprite;


    public void Awake()
    {
        Canvas = GameObject.Find("Main Canvas");
        EnemyZone = GameObject.Find("EnemyArea");

        zoomSprite = gameObject.GetComponent<Image>().sprite;
    }

    public void OnHoverEnter()
    {
        if (transform.IsChildOf(EnemyZone.transform))
        {
            return;
        }

        if (Clip != null)
        {
            clip = Instantiate(Clip, new Vector2(Input.mousePosition.x, Input.mousePosition.y + 250),
                Quaternion.identity);
            clip.GetComponent<RawImage>().texture = Clip.texture;
            clip.transform.SetParent(Canvas.transform, true);
            RectTransform rect = clip.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240, 344);
        }
        else
        {
            zoomCard = Instantiate(ZoomCard, new Vector2(Input.mousePosition.x, Input.mousePosition.y + 250),
                Quaternion.identity);
            zoomCard.GetComponent<Image>().sprite = zoomSprite;
            zoomCard.transform.SetParent(Canvas.transform, true);
            RectTransform rect = zoomCard.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240, 344);
        }
    }

    public void OnHoverExit()
    {
        if (zoomCard != null)
        {
            DestroyImmediate(zoomCard);
            return;
        }
        if (clip != null)
        {
            DestroyImmediate(clip);
        }

    }
}