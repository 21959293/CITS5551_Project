using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using MongoDB.Driver;
using MongoDB.Bson;
using IniParser;
using IniParser.Model;


/// <summary>
/// Manages the session of the game
/// </summary>
public class SessionManager : MonoBehaviour
{
    public AVController playerCar; // Car controller for the player
    public PlayerController playCon; // Player controller representing the player
    public GameTimer timer; // Keeps time
	public PopupPhone phone; // PopupPhone object representing the phone that allows players to make decisions
    public Text gameText;
    public Text costText;
    public Text totalText;
    public GameObject driver;
    public AudioSource drivingAudio;
    public float startDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases.
    public float endDelay = 3f;               // The delay between the end of RoundPlaying and RoundEnding phases.
    public bool driverReachedDest = false;

    public GameObject car; // Template object representing a car

    // Most of these should have been replaced by the config file
    private int numRounds;    
    private float initialTotal;
    private bool driverVisible;
    private float scaleTime;
    private float scaleCost;

    private float[] baseTime;
    private float[] addedTime;
    private float[] requestTime;
    private float[] baseCost;
    private float[] removedCost;
    private float[] roundDuration;
    private float[] additionalStopDistance;
    private string mapName = "";

    public int roundNumber = 0;              // Which round the game is currently on.
    private float moneyTotal = 0f;
    public float costTotal = 0f;
    private WaitForSeconds startWait;         // Used to have a delay whilst the round starts.
    private WaitForSeconds endWait;           // Used to have a delay whilst the round or game ends.
    //private Connect db = new Connect();
    private List<bool> choices = new List<bool> { false, false, false };
    private List<float> times = new List<float>();
    // private Mongo db;
    private GameManager gameManager;
    private MenuManager menuManager;

    private bool flag = true;
    private float startTime;
    private DateTime startTimeUTC;
    private float waitTime = 0f;

    private GameObject AICarsObject;

    // Used to populate the database with the driver options
    public static string vehicleDriverOption0;
    public static string vehicleDriverOption1;
    public static string vehicleDriverOption2;
    public static string playerDriverChoice;
    public string carName = "";

    /// <summary>
    /// On each frame sets basic objects and starts player after round starts @todo: This is inefficient and should only be done once 
    /// </summary>
    private void Update()
    {
        // Initialises basic objects
        playerCar = GameManager.instantiatedPlayer.transform.GetChild(0).gameObject.GetComponent<AVController>();
        car = GameManager.instantiatedPlayer.transform.GetChild(0).gameObject;
        driver = car.transform.GetChild(4).gameObject;

        // Finds the player
        GameObject PlayerObject = GameObject.FindGameObjectWithTag("Player");
        playCon = PlayerObject.GetComponent<PlayerController>();

        // Spawns the player and starts the game after the round starts
        if (Time.time > startTime + waitTime && flag)
        {
            flag = false;
            StartCoroutine(GameLoop());

        }
       
        //printing passenger pick up choices
        foreach(var xx in choices)
        {
            UnityEngine.Debug.Log(xx.ToString());
        }

    }

    /// <summary>
    /// Upon the start of the game, set up basics and begin session
    /// </summary>
    private void Start()
    {
        // Start time and wait
        drivingAudio.Stop();
        startTime = Time.time;
        startTimeUTC = DateTime.UtcNow;
        startWait = new WaitForSeconds (startDelay);
        endWait = new WaitForSeconds (endDelay);

        // Grab config data
        string configFilepath;
        if (UnityEngine.Debug.isDebugBuild)
        {
            configFilepath = "Assets/Resources/config.ini";
        }
        else
        {
            configFilepath = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"/Resources/config.ini";
        }
        var parser = new FileIniDataParser();
        IniData configData = parser.ReadFile(configFilepath);
        SessionDecoder(configData);

        // Initialize screen
        moneyTotal = initialTotal;
        costTotal = 0;
        updateMoneyText();
        timer.setTimeScale(scaleTime);
        populateDatabase();   


    }

    /// <summary>
    /// Sets up a coroutine representing the gameloop
    /// </summary>
    public void startCoroutineFunction()
    {
        StartCoroutine(GameLoop());
    }

    /// <summary>
    /// This is called from start and will run each phase of the game one after another.
    /// </summary>
    /// <returns></returns>
    public IEnumerator GameLoop ()
    {

        // Start off by running the 'RoundStarting' coroutine but don't return until it's finished.
        yield return StartCoroutine (RoundStarting());

        // Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished.
        yield return StartCoroutine (RoundPlaying());

        // Once execution has returned here, run the 'RoundEnding' coroutine, again don't return until it's finished.
        yield return StartCoroutine (RoundEnding());

        if (roundNumber < numRounds)
        {
            StartCoroutine (GameLoop());
        } 
        else 
        {
            string gameID = PlayerPrefs.GetString("gameID");
            string playerID = PlayerPrefs.GetString("playerID");

            //db.Init();
            //db.InsertConfig(gameID, playerID, moneyTotal, choices, times);
            dataBaseMethod();

            PlayerPrefs.SetFloat("finalMoney", moneyTotal);
            SceneManager.LoadScene("EndScreen", LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// Sets up the next round
    /// </summary>
    /// <returns></returns>
    private IEnumerator RoundStarting ()
    {
        // Increment the round number and display text showing the players what round it is.
        roundNumber++;
        gameText.text = "RIDE " + roundNumber;

        if(baseCost.Length > (roundNumber - 1))
        {
            costTotal = baseCost[roundNumber - 1];
        }
        updateMoneyText();

        driver.SetActive(driverVisible);
        

        timer.pauseTimer();
        timer.setTimer(0);

        // redefine path for new round (to resolve path change in pickup passanger)
        playCon.Pathing(roundNumber - 1);

        // Set player to go to selected start-end nodes
        playerCar.TP2Start();
        playerCar.currentNode = 0;

        // Reset all AI cars
        AVController AICarAV;
        AICarsObject = GameObject.Find("AICars");
        for (int i = 0 ; i < AICarsObject.transform.childCount ; i++)
        {
            AICarAV = AICarsObject.transform.GetChild(i).gameObject.transform.GetChild(0).GetComponent<AVController>();
            AICarAV.TP2Start();
        }

        yield return startWait;
    }

    /// <summary>
    /// Plays the round until the player reaches its destination
    /// </summary>
    /// <returns></returns>
    private IEnumerator RoundPlaying ()
    {
        drivingAudio.Play();
        timer.runTimer();
        gameText.text = string.Empty;
        bool popupComplete = false;

        playerCar.roundOver = false;

        // Set all AICars to go
        AVController AICarAV;
        for (int i = 0; i < AICarsObject.transform.childCount; i++)
        {
            AICarAV = AICarsObject.transform.GetChild(i).gameObject.transform.GetChild(0).GetComponent<AVController>();
            AICarAV.roundOver = false;

        }

        //while (timer.getTimer() < 15 && !driverReachedDest)
        while (!driverReachedDest)
        {
            //Show phone if popup isn't done and request time has elapsed 
            if (!popupComplete && timer.getTimer() > requestTime[roundNumber - 1])
            {
                popupComplete = true;

                if (PanelMenuScript.modeOfTransport == 1 | PanelMenuScript.modeOfTransport == 2)
                {
                    phone.openPhone(removedCost[roundNumber - 1], addedTime[roundNumber - 1]);
                }
            }

            yield return null;
        }

        // Indicate to player car to stop
        driverReachedDest = false;
        playerCar.roundOver = true;

        // Set all AICars to stop
        for (int i = 0; i < AICarsObject.transform.childCount; i++)
        {
            AICarAV = AICarsObject.transform.GetChild(i).gameObject.transform.GetChild(0).GetComponent<AVController>();
            AICarAV.roundOver = true;
        }

    }

    /// <summary>
    /// Finishes round and cleans up
    /// </summary>
    /// <returns></returns>
    private IEnumerator RoundEnding ()
    {
        drivingAudio.Stop();
        phone.fastClosePhone();

        //Check if phone has had no inputs
        if (choices.Count < roundNumber)
        {
            choices[roundNumber-1] = false;
            times.Add(-1f);
        }
        
        moneyTotal -= costTotal;
        costTotal = 0;
        updateMoneyText();

        timer.pauseTimer();

        gameText.text = "YOU HAVE ARRIVED AT YOUR DESTINATION";

        roundDuration[roundNumber-1] = timer.getTimer();

        yield return endWait;
    }

    /// <summary>
    /// Updates screen to indicate the player has accepted the prompt
    /// </summary>
    /// <param name="timeTaken">Float representing how long was taken for the player to make the choice</param>
    public void acceptPrompt(float timeTaken)
    {
        choices[roundNumber-1] = true;
        times.Add(timeTaken);

        //timer.extendTimer(addedTime[roundNumber-1]);
        costTotal -= removedCost[roundNumber-1];
        updateMoneyText();
    }

    /// <summary>
    /// Updates state to indicate the player has rejected the prompt
    /// </summary>
    /// <param name="timeTaken">Float representing how long was taken for the player to make the choice</param>
    public void rejectPrompt(float timeTaken)
    {
        choices[roundNumber-1] = false;
        times.Add(timeTaken);
    }

    /// <summary>
    /// Updates the text with the current totals
    /// </summary>
    private void updateMoneyText()
    {
        costText.text = "Cost: " + costTotal.ToString("c2");
        totalText.text = "Total: " + moneyTotal.ToString("c2");
    }

    /// <summary>
    /// Decodes the config data to set up the game
    /// </summary>
    /// <param name="configData">IniData object representing the parameters the game should use</param>
    private void SessionDecoder(IniData configData)
    {
        float.TryParse(configData["Session"]["startingMoney"], out initialTotal);
        float.TryParse(configData["Session"]["timeScale"], out scaleTime);
        float.TryParse(configData["Session"]["moneyScale"], out scaleCost);
        int.TryParse(configData["Session"]["numberOfRounds"], out numRounds);
        mapName = configData["Map"]["name"];
        

        requestTime = new float[numRounds];
        baseTime    = new float[numRounds];
        addedTime   = new float[numRounds];
        baseCost    = new float[numRounds];
        removedCost = new float[numRounds];
        roundDuration = new float[numRounds];
        additionalStopDistance = new float[numRounds];

        for(int i = 0; i < numRounds; i++)
        {
            string sessionRoundString = "SessionRound" + (i + 1);

            float.TryParse(configData[sessionRoundString]["requestTime"], out requestTime[i]);
            float.TryParse(configData[sessionRoundString]["baseTime"], out baseTime[i]);
            float.TryParse(configData[sessionRoundString]["additionalStopTime"], out addedTime[i]);
            float.TryParse(configData[sessionRoundString]["baseCost"], out baseCost[i]);
            float.TryParse(configData[sessionRoundString]["additionalCostSaved"], out removedCost[i]);
            float.TryParse(configData[sessionRoundString]["additionalStopDistance"], out additionalStopDistance[i]);
        }

        driverVisible = configData["Session"]["driverVisible"].ToLower() == "true";
    }

    /// <summary>
    /// Returns the new cost of the ride
    /// </summary>
    /// <param name="addedcost">Float representing the cost to add to the total</param>
    /// <returns>Float representing the total cost of the ride</returns>
    public float getNewCost(float addedcost)
    {
        costTotal += addedcost;
        return costTotal;
    }

    /// <summary>
    /// Updates and returns the time I guess?? What a pointless function
    /// </summary>
    /// <param name="time">Float representing time</param>
    /// <param name="addedTime">Float representing additional time to add</param>
    /// <returns>Float for the new time</returns>
    public float getNewTime(float time, float addedTime)
    {
        time += addedTime;
        return time;
    }

    /// <summary>
    /// Populates the second screen with valid random options
    /// </summary>
    public void populateDatabase()
    {

        if (PanelMenuScript.modeOfTransport == 0 | PanelMenuScript.modeOfTransport == 1)
        {
            vehicleDriverOption0 = "N/A";
            vehicleDriverOption1 = "N/A";
            vehicleDriverOption2 = "N/A";
            playerDriverChoice = "N/A";
        }
        else
        {
            vehicleDriverOption0 = DriverText.drivers[CarDriverOption.randomNumberChosen[0]].Replace("\n", "").Replace("\r", "");
            vehicleDriverOption1 = DriverText.drivers[CarDriverOption.randomNumberChosen[1]].Replace("\n", "").Replace("\r", "");
            vehicleDriverOption2 = DriverText.drivers[CarDriverOption.randomNumberChosen[2]].Replace("\n", "").Replace("\r", "");
            playerDriverChoice = DriverText.drivers[CarDriverOption.driverIndex].Replace("\n", "").Replace("\r", "");
        }
    
    }

    /// <summary>
    /// Validates and updates the menu based on settings chosen
    /// </summary>
    public void dataBaseMethod()
    {
        carName = CarDriverOption.CarName;

        MongoClient dbClient = new MongoClient("mongodb+srv://DefaultClient:P5n8isH6GzjWMF1R@grandtraffic.cy1ic.azure.mongodb.net/GTA2-2020?retryWrites=true&w=majority");
        var database = dbClient.GetDatabase("GTA2-2020");
        var collection = database.GetCollection<BsonDocument>("Results");

        // Null checks for variables sent to database
        if (String.IsNullOrEmpty(MenuManager.playerID))
        {
            MenuManager.playerID = "No playerID provided";
        }
        if (String.IsNullOrEmpty(MenuManager.gender))
        {
            MenuManager.gender = "No gender provided";
        }
        if (String.IsNullOrEmpty(MenuManager.age))
        {
            MenuManager.age = "No age provided";
        }
        if (String.IsNullOrEmpty(PanelMenuScript.modeOfTransportChoice[PanelMenuScript.modeOfTransport]))
        {
            PanelMenuScript.modeOfTransportChoice[PanelMenuScript.modeOfTransport] = "Not provided";
        }
        if (String.IsNullOrEmpty(vehicleDriverOption0))
        {
            vehicleDriverOption0 = "Not provided";
        }
        if (String.IsNullOrEmpty(vehicleDriverOption1))
        {
            vehicleDriverOption1 = "Not provided";
        }
        if (String.IsNullOrEmpty(vehicleDriverOption2))
        {
            vehicleDriverOption2 = "Not provided";
        }
        if (String.IsNullOrEmpty(playerDriverChoice))
        {
            playerDriverChoice = "Not provided";
        }

        // Generates menu content
        var document = new BsonDocument
            {
                { "Session", new BsonDocument
                    {
                        { "mapName", mapName },
                        { "sessionStartDateTime", startTimeUTC },
                        { "playerId", MenuManager.playerID },
                        { "playerGender", MenuManager.gender },
                        { "playerAge", MenuManager.age },
                        { "startingMoney", initialTotal },
                        { "endingMoney", moneyTotal },
                        { "moneyScale", scaleCost },
                        { "timeScale", scaleTime },
                        { "numberOfRounds", numRounds },
                        //{ "weather", "mj" } 
                    }
                },
                { "Decisions", new BsonDocument
                    {
                        { "modeOfTransport", PanelMenuScript.modeOfTransportChoice[PanelMenuScript.modeOfTransport]},
                        { "vehicleDriverOption0", vehicleDriverOption0 },
                        { "vehicleDriverOption1", vehicleDriverOption1 },
                        { "vehicleDriverOption2", vehicleDriverOption2 },
                        //{ "vehicleChoice", carName },
                        { "playerDriverChoice", playerDriverChoice},
                        { "Rounds",  new BsonArray
                            {
                                "Round1", new BsonDocument
                                    {
                                        {"requestTime", requestTime[0] },
                                        {"baseCost", baseCost[0] },
                                        {"additionalCostSaved", removedCost[0] },
                                        {"additionalStopDistance", additionalStopDistance[0] },
                                        {"passengerAccept", choices[0] },
                                        {"roundDuration", roundDuration[0] }
                                    },
                                "Round2", new BsonDocument
                                    {
                                        {"requestTime", requestTime[1] },
                                        {"baseCost", baseCost[1] },
                                        {"additionalCostSaved", removedCost[1] },
                                        {"additionalStopDistance", additionalStopDistance[1] },
                                        {"passengerAccept", choices[1] },
                                        {"roundDuration", roundDuration[1] }
                                    },
                                "Round3", new BsonDocument
                                    {
                                        {"requestTime", requestTime[2] },
                                        {"baseCost", baseCost[2] },
                                        {"additionalCostSaved", removedCost[2] },
                                        {"additionalStopDistance", additionalStopDistance[2] },
                                        {"passengerAccept", choices[2] },
                                        {"roundDuration", roundDuration[2] }
                                    },
                            }
                        }
                    }
                }
            };
        collection.InsertOne(document);

        database = null;
        collection = null;
    }

}

    //DEBUG EXAMPLE
    // 50&15&1&3&15,30,45&300, 360, 420&60,120,180&10,15,25&10,12.5,13.25&T&F&F
    // arguments[0] starting money
    // arguments[1] time scale
    // arguments[2] money scale
    // arguments[3] number of trials
    // arguments[4] request times
    // arguments[5] base time
    // arguments[6] added time
    // arguments[7] base costs
    // arguments[8] savings
    // arguments[9] driver
    // arguments[10] friend
    // arguments[11] multiple passengers

