using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowPoints : MonoBehaviour
{
    public Text CardPoints;

    private void Awake()
    {
        CardPoints.text = GetComponentInParent<CardInfo>().strength.ToString();
    }
}
