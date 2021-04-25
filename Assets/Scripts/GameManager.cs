using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the state of the overall game
/// </summary>
public class GameManager : MonoBehaviour
{
    public GameObject[] CarList;
    public GameObject AVcar;
    public GameObject playersOwnCar;

    public static GameObject instantiatedPlayer;
    
    /// <summary>
    /// Generates the player's car based on transport selected before scene is loaded
    /// </summary>
    private void Start()
    {
        switch (PanelMenuScript.modeOfTransport)
        {
            case 0:
                generateOwnCar();
                break;
            case 1:
                // Instantiate a driverless car into the scene
                generateAVcar();
                break;
            case 2:
                // Instantiate a car with a driver into the scene
                generateDriverCar();
                break;
            default:
                Debug.Log("Error");
                return;
        }
    }

    /// <summary>
    /// Sets the instantiated player to have its own car
    /// </summary>
    public void generateOwnCar()
    {
        instantiatedPlayer = Instantiate(playersOwnCar, transform.localPosition, Quaternion.identity);
    }

    /// <summary>
    /// Sets the instantiated player to use the car it selected in the menu
    /// </summary>
    public void generateDriverCar()
    {
        instantiatedPlayer = Instantiate(CarList[PlayerPrefs.GetInt("Select Car", 0)], transform.localPosition, Quaternion.identity);
    }

    /// <summary>
    /// Sets the instantiated player to use a default AV car
    /// </summary>
    public void generateAVcar()
    {
        instantiatedPlayer = Instantiate(AVcar, transform.localPosition, Quaternion.identity);
    }

}
