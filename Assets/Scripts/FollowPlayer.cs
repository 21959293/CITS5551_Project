using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller that follows the player to film the minimap
/// </summary>
public class FollowPlayer : MonoBehaviour
{
    public GameObject player; // GameObject representing the player
    public bool rotateWithPlayer = true; // True if the minimap should rotate with the player (we thought it looks better as true)
    public float heightAbovePlayer = 100.0f; // Height above player controls how much is within frame

    private Following following; // Object used to ensure the player is tracked
    
    private CarDriverOption carDriverOption;
    private GameManager gameManager; // Gamemanager that controls the game state

    /// <summary>
    /// Sets up required objects before the scene is loaded
    /// </summary>
    private void Start()
    {
        //player = GameManager.instantiatedPlayer.transform.GetChild(0).gameObject;
        
        carDriverOption = GameObject.FindObjectOfType<CarDriverOption>();
        gameManager = GameObject.FindObjectOfType<GameManager>();

       

        if (following == null)
        {
            following = new Following(rotateWithPlayer, heightAbovePlayer);
        }
    }

    /// <summary>
    /// Update is called once per frame to update the position of the camera
    /// </summary>
    private void Update()
    {
        player = GameManager.instantiatedPlayer.transform.GetChild(0).gameObject;

        if (player == null)
        {
            Debug.Log("Player in FollowPlayer doesn't exist" + player);
        }
        else
        {
            //Debug.Log("PLayer in FollowPlayer does exist" + player);
        }

        transform.position = following.CalculateMovement(player.transform.position);
        transform.rotation = following.CalculateRotation(player.transform.rotation, transform.rotation);
    }
}