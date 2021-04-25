using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the end of the game (doesn't seem to do much though)
/// </summary>
public class EndManager : MonoBehaviour
{

    public Text moneyText;

    void Start () 
    {
        //Get final money earnt and display
        float money = PlayerPrefs.GetFloat("finalMoney");
        moneyText.text = money.ToString("c2");
    }
}
