using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Manages the primary survey that asks the player for their information
/// </summary>
public class MenuManager : MonoBehaviour
{
    public InputField playerField; // Interactable field for player's ID
    public InputField genderField; // Interactable field for player's gender
    public InputField ageField; // Interactable field for player's age
    public Text errorText; // Optional error text
    public string pid = "";
    public string gid = "";
    public string error = "";
    public static string playerID;
    public static string gender;
    public static string age;

    /// <summary>
    /// Validates input then loads the next scene
    /// </summary>
    public void PlayGame()
    {
        playerID = playerField.text;
        gender = genderField.text;
        age = ageField.text;
        
        // Checks if age is a number
        var isNumeric = int.TryParse(age, out _);

        // Checks if login fields are null/empty/white space and stops you from continuing
        if (String.IsNullOrEmpty(playerID) || String.IsNullOrWhiteSpace(playerID) ||
            String.IsNullOrEmpty(gender) || String.IsNullOrWhiteSpace(gender) ||
            String.IsNullOrEmpty(age) || String.IsNullOrWhiteSpace(age) || isNumeric == false)
        {
            // Error message to display problem with incorrect fields
            errorText.text = "Please fill in all fields correctly";
        }
        else 
        {
            // Values filled in correctly move to game, carrying over variables
            errorText.text = "";
            PlayerPrefs.SetString("gender", gender);
            PlayerPrefs.SetString("playerID", playerID);
            SceneManager.LoadScene(3);
        }
    }

    /// <summary>
    /// Sets up the text displayed to be blank initially before the application starts
    /// </summary>
    public void Start()
    {
        playerField.text = "";
        genderField.text = "";
        ageField.text = "";
    }
}
