using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Controller that tracks the game's time
/// </summary>
public class GameTimer : MonoBehaviour
{

    public Text timerText; // The text to be displayed

	private float timeScale = 1f;
	private float timeLeft = 0;  // default 300 seconds (5 minutes)
    private bool paused = false;

	/// <summary>
    /// On each frame updates time based on timescale
    /// </summary>
    void Update()
    {
    	if(!paused)
		{
			//Scale time but factor timeScale
    		timeLeft += Time.deltaTime * timeScale;
			updateText();
    	}
    }

	/// <summary>
    /// Updates text based on the amount of time left
    /// </summary>
	private void updateText()
	{
		//Get minute and second values (rounded down)
		float minutesLeft = Mathf.Floor(timeLeft / 60);
		float secondsLeft = Mathf.Floor(timeLeft % 60);

		var minuteText = "";
		var secondsText = "";

		minuteText = (minutesLeft).ToString("0");
		secondsText = (secondsLeft).ToString("0");

		//Add '0' in front of single minute/second digits
		if (minutesLeft < 10)
		{
			minuteText  = "0" + minuteText;
		}
		if(secondsLeft < 10)
		{
			secondsText = "0" + secondsText;
		}

		string timeString = minuteText + ":" + secondsText;

		//No negative time
		if (timeLeft <= 0)
		{
			timeString = "00:00";
		}

		//Update text field
		timerText.text =  "Elapsed Time: " + timeString;
	}

	/// <summary>
    /// 
    /// </summary>
    /// <param name="scale"></param>
	public void setTimeScale(float scale)
	{
		timeScale = scale;
	}

	public float getTimer()
	{
    	return timeLeft;
    }
	
    public void setTimer(float time)
	{
		Debug.Log("The time is set at: "+timeLeft);
    	timeLeft = time;
		updateText();
    }

	public void extendTimer(float time)
	{
    	timeLeft += time;
		updateText();
    }

	public bool isPaused()
	{
    	return paused;
    }

    public void pauseTimer()
	{
    	paused = true;
    }

	public void runTimer()
	{
    	paused = false;
    }
    public void toggleTimer()
	{
    	paused = !paused;
    }
}
