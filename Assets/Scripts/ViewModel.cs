using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Class that triggers whether to show the fullscreen map view
/// </summary>
public class ViewModel : MonoBehaviour {

    public GameObject fullmap; // Gameobject representing the full map
    public Toggle maptoggle; // 
    private bool isShowing; // Boolean indicating whether the full map is currently being shown

    public void Toggle_Full_Map(bool state)
    {
        fullmap.SetActive(state);
        isShowing = state;
    }

    /// <summary>
    /// Checks every frame if the 'M' key is being pressed and toggles the display if so
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            isShowing = !isShowing;
            maptoggle.isOn = isShowing;
        }
    }
}
