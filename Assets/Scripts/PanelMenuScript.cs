using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu that allows the user to open the driver select menu
/// </summary>
public class PanelMenuScript : MonoBehaviour
{
    public GameObject panelMenu2;

    private GameManager gameManager;

    public static int modeOfTransport;

    public static string[] modeOfTransportChoice = {"Player chose to drive their own vehicle", "Player chose an AV vehicle", "Player chose a vehicle with a rideshare driver" };
    void Start()
    {
        panelMenu2.SetActive(false);


    }
    public void OpenPanelMenu2()
    {
        if(panelMenu2 != null)
        {
            panelMenu2.SetActive(true);
        }
    }

    public void onOwnVehicleButton()
    {
       // Debug.Log("It's on  onOwnVehicleButton!");
        modeOfTransport = 0;
        SceneManager.LoadScene("MainGame");
    }
    public void onAvVehicleButton()
    {
       //Debug.Log("It's on AvVehicleButton!");
       modeOfTransport = 1;
       SceneManager.LoadScene("MainGame");
    }
    public void onDriverVehicleButton()
    {
        //Debug.Log("It's on DriverVehicleButton!");
        modeOfTransport = 2;
        OpenPanelMenu2();
    }
}
