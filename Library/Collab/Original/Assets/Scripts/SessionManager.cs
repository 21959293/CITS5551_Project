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


public class SessionManager : MonoBehaviour
{
    public AVController playerCar;
    public PlayerController playCon;
    public GameTimer timer;
	public PopupPhone phone;
    public Text gameText;
    public Text costText;
    public Text totalText;
    public GameObject driver;
    public AudioSource drivingAudio;
    public float startDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases.
    public float endDelay = 3f;               // The delay between the end of RoundPlaying and RoundEnding phases.
    public bool driverReachedDest = false;

    public GameObject car;

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

    public int roundNumber = 0;              // Which round the game is currently on.
    private float moneyTotal = 0f;
    public float costTotal = 0f;
    private WaitForSeconds startWait;         // Used to have a delay whilst the round starts.
    private WaitForSeconds endWait;           // Used to have a delay whilst the round or game ends.
    //private Connect db = new Connect();
    private List<bool> choices = new List<bool> { false, false, false, false };
    private List<float> times = new List<float>();
    // private Mongo db;
    private GameManager gameManager;
    private MenuManager menuManager;

    private bool flag = true;
    private float startTime;
    private float waitTime = 0f;

    private GameObject AICarsObject;

    // Used to populate the database with the driver options
    public static string vehicleDriverOption0;
    public static string vehicleDriverOption1;
    public static string vehicleDriverOption2;
    public static string playerDriverChoice;

    private void Update()
    {
        playerCar = GameManager.instantiatedPlayer.transform.GetChild(0).gameObject.GetComponent<AVController>();
        car = GameManager.instantiatedPlayer.transform.GetChild(0).gameObject;
        driver = car.transform.GetChild(4).gameObject;

        GameObject PlayerObject = GameObject.FindGameObjectWithTag("Player");
        playCon = PlayerObject.GetComponent<PlayerController>();

               
        if (Time.time > startTime + waitTime && flag)
        {
            playerCar.SpawnCar();
            flag = false;
            StartCoroutine(GameLoop());

        }
       
        //printing passenger pick up choices
        foreach(var xx in choices)
        {
            UnityEngine.Debug.Log(xx.ToString());
        }

    }
    private void Start()
    {
        drivingAudio.Stop();


        startTime = Time.time;

        //playerCar = GameManager.instantiatedPlayer.transform.GetChild(0).gameObject.GetComponent<AVController>();
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

       

        // @TODO: HARDCODED FOR TESTING
        string str = "50&1&1 &3&15,30,45&100, 15, 15&60,120,180&10,15,25&10,12.5,13.25&T&F&F";
        //string str = "50&1&1 &1&15 &30 &60 &10&10 &T&F&F";

        SessionDecoder(configData);
        //SessionDecoder(PlayerPrefs.GetString("gameID"));

        moneyTotal = initialTotal;
        costTotal = 0;
        updateMoneyText();

        timer.setTimeScale(scaleTime);

        
        populateDatabase();

        // StartCoroutine (GameLoop ());

        


    }

    public void startCoroutineFunction()
    {
        StartCoroutine(GameLoop());
    }

    // This is called from start and will run each phase of the game one after another.
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
        timer.setTimer(baseTime[roundNumber-1]);

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

        while (timer.getTimer() > 0 && !driverReachedDest)
            {
            //Show phone if popup isn't done and request time has elapsed 
            if (!popupComplete && timer.getTimer() < baseTime[roundNumber - 1] - requestTime[roundNumber - 1])
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

    private IEnumerator RoundEnding ()
    {
        drivingAudio.Stop();
        phone.fastClosePhone();

        //Check if phone has had no inputs
        if (choices.Count < roundNumber)
        {
            choices[roundNumber] = false;
            times.Add(-1f);
        }
        
        moneyTotal -= costTotal;
        costTotal = 0;
        updateMoneyText();

        timer.pauseTimer();
        gameText.text = "YOU HAVE ARRIVED AT YOUR DESTINATION";
       
        yield return endWait;
    }

    public void acceptPrompt(float timeTaken)
    {
        choices[roundNumber] = true;
        times.Add(timeTaken);

        timer.extendTimer(addedTime[roundNumber-1]);
        costTotal -= removedCost[roundNumber-1];
        updateMoneyText();
    }

    public void rejectPrompt(float timeTaken)
    {
        choices[roundNumber] = false;
        times.Add(timeTaken);
    }

    private void updateMoneyText()
    {
        costText.text = "Cost: " + costTotal.ToString("c2");
        totalText.text = "Total: " + moneyTotal.ToString("c2");
    }

    private void SessionDecoder(IniData configData)
    {
        float.TryParse(configData["Session"]["startingMoney"], out initialTotal);
        float.TryParse(configData["Session"]["timeScale"], out scaleTime);
        float.TryParse(configData["Session"]["moneyScale"], out scaleCost);
        int.TryParse(configData["Session"]["numberOfRounds"], out numRounds);
        

        requestTime = new float[numRounds];
        baseTime    = new float[numRounds];
        addedTime   = new float[numRounds];
        baseCost    = new float[numRounds];
        removedCost = new float[numRounds];

        for(int i = 0; i < numRounds; i++)
        {
            string sessionRoundString = "SessionRound" + (i + 1);

            float.TryParse(configData[sessionRoundString]["requestTime"], out requestTime[i]);
            float.TryParse(configData[sessionRoundString]["baseTime"], out baseTime[i]);
            float.TryParse(configData[sessionRoundString]["additionalStopTime"], out addedTime[i]);
            float.TryParse(configData[sessionRoundString]["baseCost"], out baseCost[i]);
            float.TryParse(configData[sessionRoundString]["additionalCostSaved"], out removedCost[i]);
        }

        driverVisible = configData["Session"]["driverVisible"].ToLower() == "true";
    }

    public float getNewCost(float addedcost)
    {
        costTotal += addedcost;
        return costTotal;
    }
    public float getNewTime(float time, float addedTime)
    {
        time += addedTime;
        return time;
    }

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
    public void dataBaseMethod()
    {
        MongoClient dbClient = new MongoClient("mongodb+srv://DefaultClient:P5n8isH6GzjWMF1R@grandtraffic.cy1ic.azure.mongodb.net/GTA2-2020?retryWrites=true&w=majority");
        var database = dbClient.GetDatabase("GTA2-2020");
        var collection = database.GetCollection<BsonDocument>("Results");


        //string test = "test";
        //String UUID = Guid.NewGuid().ToString();
        DateTime startTime = DateTime.UtcNow;
        DateTime endTime = DateTime.UtcNow.AddDays(1);

        var document = new BsonDocument
            {
                { "Session", new BsonDocument
                    {
                        { "sessionId", 28 },
                        { "sessionStartTime", startTime },
                        { "sessionEndTime", endTime },
                        { "sessionTotalTime", (endTime-startTime).TotalMinutes  },
                        { "playerId", MenuManager.playerID },
                        { "playerGender", MenuManager.gender },
                        { "playerAge", MenuManager.age },
                        { "playerName", "cm" },
                        { "moneyStart", 35.5 },
                        { "moneyEnd", "cm" },
                        { "moneySavings", 35.5 },
                        { "moneyScale", "cm" },
                        { "weather", "cm" }
                    }
                },
                { "Decisions", new BsonDocument
                    {
                        { "modeOfTransport", PanelMenuScript.modeOfTransportChoice[PanelMenuScript.modeOfTransport]},
                        { "vehicleDriverOption0", vehicleDriverOption0 },
                        { "vehicleDriverOption1", vehicleDriverOption1 },
                        { "vehicleDriverOption2", vehicleDriverOption2 },
                        { "vehicleChoice", 35.5 },
                        { "playerDriverChoice", playerDriverChoice}, 
                        { "passengers",  new BsonArray
                            {
                                "passenger_1", new BsonDocument
                                    {
                                        {"accept", choices[0] }
                                    },
                                "passenger_2", new BsonDocument
                                    {
                                        {"accept", choices[1] }
                                    },
                                "passenger_3", new BsonDocument
                                    {
                                        {"accept", choices[2] }
                                    }
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

