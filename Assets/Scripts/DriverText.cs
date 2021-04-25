using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows the drivers required
/// </summary>
public class DriverText : MonoBehaviour
{
    public GameObject driverTextBox0;
    public GameObject driverText0;

    public GameObject driverTextBox1;
    public GameObject driverText1;
    
    public GameObject driverTextBox2;
    public GameObject driverText2;

    // @todo: Move to an external text file to make it easier to update for future
    public static string[] drivers =
    {
        "Name: Mathew Smith \n Gender: Male \n License plate: XGH12HH \n Driver’s license: 25239087 \n User rating: 3/5 \n Driving Experience: 7 years \n Rideshare driver experience: 2 years",
        "Name: Jake Smith\n Gender: Male \n License plate: ZGH63JH \n Driver’s license: 22419227 \n User rating: 4/5 \n Driving Experience: 6 years \n Rideshare driver experience: 3 years",
        "Name: Samantha Smith \n Gender: Female \n License plate: DDH12TH \n Driver’s license: 44239011 \n User rating: 5/5 \n Driving Experience: 7 years \n Rideshare driver experience: 2 years",
        "Name: Mary Smith\n Gender: Female \n License plate: XGH12HH \n Driver’s license: 25239087 \n User rating: 3/5 \n Driving Experience: 7 years \n Rideshare driver experience: 2 years",
        "Name: Remy Smith \n Gender: Male \n License plate: ZGH63JH \n Driver’s license: 22419227 \n User rating: 4/5 \n Driving Experience: 6 years \n Rideshare driver experience: 3 years",
        "Name: Will Smith\n Gender: Male \n License plate: DDH12TH \n Driver’s license: 44239011 \n User rating: 5/5 \n Driving Experience: 7 years \n Rideshare driver experience: 2 years",
        "Name: Cosmin Smith\n Gender: Male \n License plate: XGH12HH \n Driver’s license: 25239087 \n User rating: 3/5 \n Driving Experience: 7 years \n Rideshare driver experience: 2 years",
        "Name: Christine Smith\n Gender: Female \n License plate: ZGH63JH \n Driver’s license: 22419227 \n User rating: 4/5 \n Driving Experience: 6 years \n Rideshare driver experience: 3 years",
        "Name: Anthony Jones\n Gender: Male \n License plate: DDH12TH \n Driver’s license: 44239011 \n User rating: 5/5 \n Driving Experience: 7 years \n Rideshare driver experience: 2 years",
        "Name: Paul Smith\n Gender: Male \n License plate: XGH12HH \n Driver’s license: 25239087 \n User rating: 3/5 \n Driving Experience: 7 years \n Rideshare driver experience: 2 years",
        "Name: Jada Smith\n Gender: Female \n License plate: ZGH63JH \n Driver’s license: 22419227 \n User rating: 4/5 \n Driving Experience: 6 years \n Rideshare driver experience: 3 years",
        "Name: Christiano Smith\n Gender: Male \n License plate: DDH12TH \n Driver’s license: 44239011 \n User rating: 5/5 \n Driving Experience: 7 years \n Rideshare driver experience: 2 years",
        "Name: Amal Smith\n Gender: Female \n License plate: DDH12TH \n Driver’s license: 44239011 \n User rating: 5/5 \n Driving Experience: 7 years \n Rideshare driver experience: 2 years",
        "Name: Cory Smith\n Gender: Male \n License plate: DDH12TH \n Driver’s license: 44239011 \n User rating: 5/5 \n Driving Experience: 7 years \n Rideshare driver experience: 2 years",
        "Name: Dennis Smith\n Gender: Male \n License plate: DDH12TH \n Driver’s license: 44239011 \n User rating: 5/5 \n Driving Experience: 7 years \n Rideshare driver experience: 2 years",
        "Name: Leon Smith\n Gender: Male \n License plate: DDH12TH \n Driver’s license: 44239011 \n User rating: 5/5 \n Driving Experience: 7 years \n Rideshare driver experience: 2 years",
        "Name: Jenny Smith\n Gender: Female \n License plate: DDH12TH \n Driver’s license: 44239011 \n User rating: 5/5 \n Driving Experience: 7 years \n Rideshare driver experience: 2 years",
        "Name: Ahmed Smith\n Gender: Male \n License plate: DDH12TH \n Driver’s license: 44239011 \n User rating: 5/5 \n Driving Experience: 7 years \n Rideshare driver experience: 2 years",
        "Name: Nicole Smith\n Gender: Female \n License plate: DDH12TH \n Driver’s license: 44239011 \n User rating: 5/5 \n Driving Experience: 7 years \n Rideshare driver experience: 2 years",
        "Name: Ben Smith\n Gender: Male \n License plate: DDH12TH \n Driver’s license: 44239011 \n User rating: 5/5 \n Driving Experience: 7 years \n Rideshare driver experience: 2 years"









    };

    /// <summary>
    /// Make all text boxes active before the start of the scene
    /// </summary>
    void Start()
    {
        driverTextBox0.SetActive(false);
        driverTextBox1.SetActive(false);
        driverTextBox2.SetActive(false);
    }

    /// <summary>
    /// Handles button by setting it to be active and to have the correct text according to setting
    /// </summary>
    /// <param name="btn">Button for the menu to be set</param>
    public void buttonHandler(Button btn)
    {
        string buttonName = btn.name;       

        if (string.Equals(buttonName.Trim(), "DriverButton0"))
        {
            driverTextBox0.SetActive(true);
            driverText0.GetComponent<Text>().text = drivers[CarDriverOption.randomNumberChosen[0]];
            driverTextBox0.transform.SetAsLastSibling();

        }
        else if (string.Equals(buttonName.Trim(), "DriverButton1"))
        {
            driverTextBox1.SetActive(true);
            driverText1.GetComponent<Text>().text = drivers[CarDriverOption.randomNumberChosen[1]];
            driverTextBox1.transform.SetAsLastSibling();
        }
        else if (string.Equals(buttonName.Trim(), "DriverButton2"))
        {
            driverTextBox2.SetActive(true);
            driverText2.GetComponent<Text>().text = drivers[CarDriverOption.randomNumberChosen[2]];
            driverTextBox2.transform.SetAsLastSibling();
        }
        else
        {
            Debug.Log("Error in DriverText, ButtonHandler Function");
        }


    }

    /// <summary>
    /// Closes the whole text box
    /// </summary>
    /// <param name="driverTextBox">GameObject representing the driver's text box</param>
    public void CloseDriverTextBox(GameObject driverTextBox)
    {
        driverTextBox.SetActive(false);
    }
   

}
