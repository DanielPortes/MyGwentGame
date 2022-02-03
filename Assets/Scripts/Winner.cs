using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Winner : MonoBehaviour
{
    public Text win;

    private void Awake()
    {
        string winner = PlayerPrefs.GetString("Winner");
        win.text = "The Winner is:" + winner;
        
    }
}
