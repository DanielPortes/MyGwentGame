using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class DragDrop : MonoBehaviour
{
    public GameObject Canvas;
    public GameObject DropZone;
    public GameObject EnemyZone;
    public GameManager GameManager;

    private bool isDragging = false;
    private bool isDraggable = true;
    private GameObject startParent;
    private Vector2 startPosition;
    private GameObject dropZone;
    private bool isOverDropZone;

    void Start()
    {
        GameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        Canvas = GameObject.Find("Main Canvas");
        DropZone = GameObject.Find("DropZone2");
        EnemyZone = GameObject.Find("EnemyArea");
        
        startParent = transform.parent.gameObject; // bug null, endDrag estava executando sem StartDrag ter inicializado a posicao inicial da carta
        startPosition = transform.position;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        isOverDropZone = true;
        dropZone = col.gameObject;
    }

    private void OnCollisionExit2D(Collision2D col)
    {
        isOverDropZone = false;
        dropZone = null;
    }

    public void StartDrag()
    {
        if (!isDraggable || transform.IsChildOf(EnemyZone.transform) || PlayerPrefs.GetString("Turn") == "Enemy")
        {
            return;
        }

        isDragging = true;
        startParent = transform.parent.gameObject;
        startPosition = transform.position;
    }

    public void EndDrag()
    {
        if (!isDraggable || transform.IsChildOf(EnemyZone.transform) || PlayerPrefs.GetString("Turn") == "Enemy")
        {
            return;
        }

        isDragging = false;
        if (isOverDropZone)
        {
            transform.SetParent(dropZone.transform, false);

            isDraggable = false;

            if (GetComponent<CardInfo>().hasEffect)
            {
                GameManager.CardsEffects(this.transform);
            }

            GameManager.passTurn();
        }
        else
        {
            transform.position = startPosition;
            transform.SetParent(startParent.transform, false);
        }
    }

    void Update()
    {
        if (isDragging)
        {
            transform.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y);
            transform.SetParent(Canvas.transform, true);
        }
    }
}