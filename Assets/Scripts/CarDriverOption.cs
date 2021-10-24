using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Specifies the car driver options
/// </summary>
public class CarDriverOption : MonoBehaviour
{
    public Sprite[] CarImage;
    public Sprite[] DriverImage;

    List<int> list = new List<int>();   //  Declare list
    int randVal;
    public static List<int> randomNumberChosen = new List<int>();

    public GameObject[] selectCar;

    public string[] CarDescription;

    [HideInInspector]
    public static int playerCarIndex;
    List<int> chosenDriverOptions = new List<int>();
    public static string vehicleDriverOption0;
    public static string vehicleDriverOption1;
    public static string vehicleDriverOption2;

    private GameManager gameManager;
    private SessionManager sessionManager;

    public Button playerButton;

    public static int driverIndex;
    private static string carName = "";

    /// <summary>
    /// Populates the options before the scene starts
    /// </summary>
    void Awake()
    {

        //  Populate list with values from 0 to number of drivers
        for (int nn = 0; nn < CarImage.Length; nn++)
        {
            list.Add(nn);
        }

        selectCar = new GameObject[CarImage.Length];

        for (int i = 0; i < CarImage.Length; i++)
        {
            int index = Random.Range(0, list.Count - 1);    //  Pick random element from the list
            randVal = list[index];    //  randVal = the number that was randomly picked
            randomNumberChosen.Add(randVal);  //add randVal to a new list 
            chosenDriverOptions.Add(randVal);

            list.RemoveAt(index);   //  Remove chosen element so that it is not chosen randomly again

            // Populate the driver image, car image and car description with the random value chosen
            if (this.gameObject.transform.childCount > i)
            {
                selectCar[i] = transform.GetChild(i).gameObject;

                selectCar[i].transform.GetChild(2).GetComponent<Image>().sprite = DriverImage[randVal];

                selectCar[i].transform.GetChild(3).GetComponent<Image>().sprite = CarImage[randVal];

                selectCar[i].transform.GetChild(4).GetComponent<Text>().text = CarDescription[randVal];
            }
        }
    }

    /// <summary>
    /// Handles button
    /// </summary>
    /// <param name="btn">Button</param>
    public void buttonHandler(Button btn)
    {
        //Debug.Log(btn.name);
        string buttonName = btn.name;       // Retrieve the name of the button that was clicked
                                            //Debug.Log("HERE " + buttonName);

        // check which button was clicked and retrieve the index value of the player
        if (string.Equals(buttonName.Trim(), "confirmButton0"))
        {
            driverIndex = randomNumberChosen[0];
        }
        else if (string.Equals(buttonName.Trim(), "confirmButton1"))
        {
            driverIndex = randomNumberChosen[1];
        }
        else if (string.Equals(buttonName.Trim(), "confirmButton2"))
        {
            driverIndex = randomNumberChosen[2];
        }
        else
        {
            Debug.Log("Error in CarDriverOption script");
        }
        //Debug.Log("driverIndex IS    " + driverIndex);

        //turn back on:
        carSelecter(driverIndex);
    }

    public static string CarName
    {
        get { return carName; }
        set { carName = value; }

    }

    // Use the randomly chosen index value to select the player game object and start the scene
    public void carSelecter(int index)
    {
        //Debug.Log("You selected car number " + index);
        playerCarIndex = index;
        PlayerPrefs.SetInt("Select Car", index);
        SceneManager.LoadScene("MainGame");
        //carName = CarDescription[driverIndex];
    }
}
