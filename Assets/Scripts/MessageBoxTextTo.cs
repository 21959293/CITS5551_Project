using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessageBoxTextTo : MonoBehaviour {

    [SerializeField]
    private TMP_Text myText;

    public void SetText(string textString)
    {
        myText.text = textString;
    }
}

