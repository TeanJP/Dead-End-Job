using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [System.Serializable]
    public class Level
    {
        public List<GameObject> souls = new List<GameObject>();
    }

    [SerializeField]
    private List<Level> levels = new List<Level>();

    private static GameController gameControllerInstance;

    public static GameController Instance
    {
        get
        {
            return gameControllerInstance;
        }
    }

    public AudioSource menuAudioSource = null;
    [SerializeField]
    private AudioSource combatAudioSource = null;
    [SerializeField]
    private AudioSource musicAudioSource = null;

    [SerializeField]
    private SpriteRenderer combatBackground = null;
    [SerializeField]
    private Color combatColourOne = Color.red;
    [SerializeField]
    private Color combatColourTwo = Color.blue;

    private Color startColour;
    private Color endColour;
    private float colourFadeTimer = 0f;
    private float colourFadeDuration = 10f;

    //The soul prefab.
    [SerializeField]
    private GameObject soulPrefab = null;
    [SerializeField]
    private GameObject tutorialSoulPrefab = null;
    //A list for storing all of the combat souls.
    private List<List<GameObject>> combatSouls = new List<List<GameObject>>();

    //The prefab for the maze player.
    [SerializeField]
    private GameObject playerPrefab = null;

    //A list for all the waypoints.
    private List<Waypoint> waypoints;

    //The text to use for the soul's stamina in combat.
    [SerializeField]
    private Text soulStaminaText = null;
    //The version of the player for combat.
    [HideInInspector]
    public PlayerCombatController combatPlayer = null;
    //The version of the player for the maze.
    [HideInInspector]
    public GameObject mazePlayer = null;

    //The maximum number of souls that can be spawned at any one time.
    private int maximumSouls = 4;
    //How many souls are currently active.
    private int spawnedSouls = 0;
    //How long to wait before spawning more souls.
    private float spawnDelay = 5f;
    //The current timer until more souls are spawned.
    private float spawnTimer = 0f;

    //Which soul the player started combat with.
    private GameObject soulInCombatWith = null;
    [HideInInspector]
    public GameObject combatSoul = null;

    //The spawn positions for the player and soul in combat.
    private Vector2 playerStart = new Vector2(-6f, 0f);
    private Vector2 soulStart = new Vector2(6f, 0f);

    private string activeSection;

    //A list of objects only in the maze.
    private List<GameObject> mazeObjects;
    //A list of objects only in combat.
    private List<GameObject> combatObjects;

    //Whether the game has eneded.
    [HideInInspector]
    public bool gameOver = false;
    [HideInInspector]
    public bool gamePaused = false;

    //The game over screen.
    [SerializeField]
    private GameObject gameOverScreen = null;
    [SerializeField]
    private GameObject pauseScreen = null;
    //The upgrade screen.
    [SerializeField]
    private GameObject upgradeScreen = null;

    //Which stage of the game the player has progressed to.
    private int level = 1;
    //The text for displaying the player's progress on their quota.
    [SerializeField]
    private Text targetScoreText = null;
    //The player's quota.
    private int targetScore = 2;
    //How many souls towards the quota the player has captured.
    private int currentScore = 0;
    
    private int totalScore = 0;
    private Text finalScoreText = null;
    private Text bestScoreText = null;

    //The text for displaying how long the player has left.
    [SerializeField]
    private Text timerText = null;
    //How long the player has to complete their quota.
    private float timerLimit = 60f;
    //How much time the player has left.
    private float timer;

    public bool playerStunned = false;
    [SerializeField]
    private Text stunTimerText = null;
    private float stunDuration = 3f;
    private float stunTimer;

    [SerializeField]
    Image transitionBackground = null;
    [HideInInspector]
    public string activeTransition = "NONE";
    private float transitionTimer = 0f;
    private float transitionInDuration = 3f;
    private float transitionOutDuration = 2f;

    [HideInInspector]
    public bool soulAnimationInProgress = false;

    public int money = 0;
    [SerializeField]
    private Text moneyText = null;

    [HideInInspector]
    public DialogueSystem dialogueSystem = null;

    public TutorialController tutorialController = null;

    public float masterVolume = 1f;
    public float sfxVolume = 1f;
    public float musicVolume = 1f;
    public float textSpeed = 1f;

    [SerializeField]
    private GameObject currentScreen = null;

    void Awake()
    {
        if (gameControllerInstance != null && gameControllerInstance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            gameControllerInstance = this;
        }

        for (int i = 0; i < levels.Count; i++)
        {
            combatSouls.Add(levels[i].souls);
        }

        tutorialController = GetComponent<TutorialController>();

        bool loadTutorial = false;

        if (PlayerPrefs.HasKey("Tutorial"))
        {
            loadTutorial = (0 != PlayerPrefs.GetInt("Tutorial"));
        }

        tutorialController.enabled = loadTutorial;

        //Get the combat version of the player.
        combatPlayer = FindObjectOfType<PlayerCombatController>();
        combatPlayer.Setup();

        //Sort the object into the scene into being for the game or maze.
        GetObjectCatergories();
        //Get all the waypoints.
        GetWaypoints();
        //Spawn the player.
        SpawnPlayer();
        //Spawn the initial group of souls.

        if (!tutorialController.enabled)
        {
            SpawnSouls(maximumSouls);
        }

        //Set the maze as being active.
        SwitchSection("Maze");

        //Set and display the time left.
        timer = timerLimit;

        if (!tutorialController.enabled)
        {
            UpdateTimer();
        }
        else
        {
            timerText.gameObject.SetActive(false);
            targetScoreText.gameObject.SetActive(false);
            moneyText.gameObject.SetActive(false);
        }

        //Display the player's quota.
        targetScoreText.text = currentScore.ToString() + "/" + targetScore.ToString();

        stunTimer = stunDuration;
        stunTimerText.gameObject.SetActive(false);

        finalScoreText = gameOverScreen.transform.GetChild(4).GetComponent<Text>();
        bestScoreText = gameOverScreen.transform.GetChild(5).GetComponent<Text>();

        //Hide the game over and upgrade screens.
        gameOverScreen.SetActive(false);
        pauseScreen.SetActive(false);

        transitionBackground.gameObject.SetActive(false);

        startColour = combatColourOne;
        endColour = combatColourTwo;

        dialogueSystem = GetComponent<DialogueSystem>();

        if (!tutorialController.enabled)
        {
            dialogueSystem.SetDialogueDisplay(0);
            dialogueSystem.ToggleDialogueDisplay(false, 0);
            dialogueSystem.ToggleDialogueDisplay(false, 1);

        }
        else
        {
            dialogueSystem.SetDialogueDisplay(1);
            dialogueSystem.ToggleDialogueDisplay(true, 1);
            dialogueSystem.ToggleDialogueDisplay(false, 0);
        }

        GetSettings();

        Time.timeScale = 1f;
    }

    void Update()
    {      
        if (!gameOver)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {

                if (!upgradeScreen.activeInHierarchy)
                {
                    gamePaused = !gamePaused;
                }

                pauseScreen.SetActive(!pauseScreen.activeInHierarchy);
            }

            if (!gamePaused && !soulAnimationInProgress)
            {
                if (activeTransition == "NONE")
                {
                    if (!tutorialController.enabled)
                    {
                        //Decrement the timer and display the new value.
                        timer = Mathf.Max(0f, timer - Time.deltaTime);
                        UpdateTimer();
                    }

                    //If the timer has expired.
                    if (timer == 0f)
                    {
                        SetGameOver();
                    }
                    else
                    {
                        //If the game is in the maze section and the maximum number of souls has not been spawned.
                        if (mazePlayer.activeInHierarchy && spawnedSouls < maximumSouls && !tutorialController.enabled)
                        {
                            //Update the cooldown for spawning new souls.
                            UpdateSpawnTimer();
                        }
                    }

                    if (playerStunned || stunTimer <= 0)
                    {
                        stunTimer -= Time.deltaTime;

                        UpdateStunTimer();

                        if (stunTimer <= -1f)
                        {
                            stunTimerText.gameObject.SetActive(false);
                            stunTimer = stunDuration;
                        }
                        else if (stunTimer <= 0f)
                        {
                            playerStunned = false;
                        }

                    }
                }
                else
                {
                    if (activeTransition == "IN")
                    {
                        PlayTransitionIn();
                    }
                    else if (activeTransition == "OUT")
                    {
                        PlayTransitionOut();
                    }
                }
            }
        }

        if (activeSection == "Combat")
        {
            AnimateCombatBackground();
        }
    }

    public void SetGameOver()
    {
        //Set the game over screen as visible.
        dialogueSystem.ToggleDialogueDisplay(true, 0);
        GameObject newPersonalBestText = gameOverScreen.transform.GetChild(6).gameObject;

        if (!tutorialController.enabled)
        {
            finalScoreText.text = "Souls Captured: " + totalScore;

            int personalBest = totalScore;

            if (PlayerPrefs.HasKey("PersonalBest"))
            {
                personalBest = PlayerPrefs.GetInt("PersonalBest");

                if (personalBest < totalScore)
                {
                    personalBest = totalScore;
                    PlayerPrefs.SetInt("PersonalBest", personalBest);

                    newPersonalBestText.SetActive(true);
                }
                else
                {
                    newPersonalBestText.SetActive(false);
                }
            }
            else
            {
                PlayerPrefs.SetInt("PersonalBest", personalBest);
                newPersonalBestText.SetActive(true);
            }

            bestScoreText.text = "Personal Best: " + personalBest;

            dialogueSystem.DisplayDialogueLine(DialogueSystem.DialogueType.GameOver);
        }
        else
        {
            Text screenHeading = gameOverScreen.transform.GetChild(3).GetComponent<Text>();
            screenHeading.text = "TUTORIAL COMPLETE";

            gameOverScreen.transform.GetChild(4).gameObject.SetActive(false);
            gameOverScreen.transform.GetChild(5).gameObject.SetActive(false);

            newPersonalBestText.SetActive(false);

            GameObject retryButton = gameOverScreen.transform.GetChild(7).gameObject;
            retryButton.transform.GetChild(0).GetComponent<Text>().text = "PLAY";
        }

        gameOverScreen.SetActive(true);
        gameOver = true;
    }

    private void AnimateCombatBackground()
    {
        colourFadeTimer += Time.deltaTime;

        float colourFadeProgress = colourFadeTimer / colourFadeDuration;

        combatBackground.color = Color.Lerp(startColour, endColour, colourFadeProgress);

        if (colourFadeProgress >= 1f)
        {
            Color tempColour = startColour;

            startColour = endColour;
            endColour = tempColour;

            colourFadeTimer = 0f;
        }
    }

    public void MoveCombatSoul(float progress)
    {
        Vector2 spawnPoint = Vector2.right * 10f;
        combatSoul.transform.position = Vector2.Lerp(spawnPoint, soulStart, progress);
    }

    private void StartTransition(string direction)
    {
        transitionBackground.gameObject.SetActive(true);
        activeTransition = direction;

        transitionTimer = 0f;

        if (direction == "IN")
        {
            transitionBackground.fillAmount = 0f;
            transitionBackground.color = Color.black;
        }
        else if (direction == "OUT")
        {
            transitionBackground.fillAmount = 1f;
            transitionBackground.color = Color.clear;
        }

    }

    private void PlayTransitionIn()
    {
        transitionTimer += Time.deltaTime;

        float spiralDuration = transitionInDuration * 0.75f;
        float fadeDuration = transitionInDuration * 0.1f;
        float moveDuration = transitionInDuration * 0.15f;

        float spiralProgress = transitionTimer / spiralDuration;
        float fadeProgress = (transitionTimer - spiralDuration) / fadeDuration;
        float moveProgress = (transitionTimer - (spiralDuration + fadeDuration)) / moveDuration;

        if (spiralProgress >= 0f && spiralProgress <= 1f)
        {            
            transitionBackground.fillAmount = Mathf.Lerp(0f, 1f, spiralProgress);
        }
        else if (fadeProgress >= 0f && fadeProgress <= 1f)
        {   
            if (activeSection != "Combat")
            {
                SwitchSection("Combat");
            }

            transitionBackground.color = Color.Lerp(Color.black, Color.clear, fadeProgress);
        }
        else if (moveProgress >= 0f && moveProgress <= 1f)
        {
            combatPlayer.transform.position = Vector2.Lerp(Vector2.left * 10f, playerStart, moveProgress);

            if (!tutorialController.enabled)
            {
                MoveCombatSoul(moveProgress);
            }
        }

        if (transitionTimer >= transitionInDuration)
        {
            if (!tutorialController.enabled)
            {
                combatSoul.GetComponent<SoulCombatController>().SetSetupComplete();
            }

            transitionBackground.gameObject.SetActive(false);
            activeTransition = "NONE";
        }
    }

    private void PlayTransitionOut()
    {
        transitionTimer += Time.deltaTime;

        float fadeInDuration = transitionOutDuration * 0.3f;
        float fadeOutDuration = transitionOutDuration * 0.7f;

        float fadeInProgress = transitionTimer / fadeInDuration;
        float fadeOutProgress = (transitionTimer - fadeInDuration) / fadeOutDuration;

        if (fadeInProgress >= 0f && fadeInProgress <= 1f)
        {
            transitionBackground.color = Color.Lerp(Color.clear, Color.black, fadeInProgress);
        }
        else if (fadeOutProgress >= 0f && fadeOutProgress <= 1f)
        {
            if (activeSection != "Maze")
            {
                if (combatSoul != null)
                {
                    //Remove the soul from the list of combat objects.
                    combatObjects.Remove(combatSoul);
                    //Destroy the soul.
                    Destroy(combatSoul);
                    combatSoul = null;
                }

                SwitchSection("Maze");
            }

            transitionBackground.color = Color.Lerp(Color.black, Color.clear, fadeOutProgress);
        }

        if (transitionTimer >= transitionOutDuration)
        {
            if (playerStunned)
            {
                stunTimerText.gameObject.SetActive(true);
                UpdateStunTimer();
            }

            transitionBackground.gameObject.SetActive(false);
            activeTransition = "NONE";
        }
    }

    private void UpdateStunTimer()
    {
        int stunTimerValue = Mathf.CeilToInt(stunTimer);

        string textOutput;

        if (stunTimerValue <= 0)
        {
            textOutput = "GO!";
        }
        else
        {
            textOutput = stunTimerValue.ToString();
        }

        stunTimerText.text = textOutput;
    }

    private void UpdateTimer()
    {
        //Convert the timer value to an integer.
        int timerValue = Mathf.CeilToInt(timer);

        //Set the number of minutes to 0.
        int minutes = 0;

        //Which at least 60 seconds are left.
        while (timerValue >= 60)
        {
            //Decrement the number of seconds by a minute.
            timerValue -= 60;
            //Increment how many minutes there are.
            minutes++;
        }

        //Convert the number of remaining seconds to a string.
        string seconds = timerValue.ToString();

        //If less than 10 seconds are left.
        if (timerValue < 10)
        {
            //Add 0 infront of the number of seconds.
            seconds = "0" + seconds;
        }

        //Display the timer's new value.
        timerText.text = minutes.ToString() + ":" + seconds;

        if (minutes == 0 && timerValue < 15 + 1)
        {
            if ((timer - Mathf.Floor(timer)) < 0.5f)
            {
                timerText.color = Color.red;
            }
            else
            {
                timerText.color = Color.white;
            }
        }

    }

    private void UpdateSpawnTimer()
    {
        //Increment the spawn timer.
        spawnTimer += Time.deltaTime;

        //If the cooldown on spawning has ended.
        if (spawnTimer >= spawnDelay)
        {
            //Spawn enough souls to reach the maximum number of active souls.
            SpawnSouls(maximumSouls - spawnedSouls);
            //Reset the spawn cooldown.
            spawnTimer = 0f;
        }
    }

    private void GetObjectCatergories()
    {
        //Get the objects tagged with being for the maze.
        GameObject[] mazeObjectsArray = GameObject.FindGameObjectsWithTag("Maze");
        //Reset the list of maze objects.
        mazeObjects = new List<GameObject>();

        //Add the maze objects to the list.
        for (int i = 0; i < mazeObjectsArray.Length; i++)
        {
            mazeObjects.Add(mazeObjectsArray[i]);
        }

        //Get the objects tagged with being for combat.
        GameObject[] combatObjectsArray = GameObject.FindGameObjectsWithTag("Combat");
        //Reset the list of combat objects.
        combatObjects = new List<GameObject>();

        //Add the combat objects to the list.
        for (int i = 0; i < combatObjectsArray.Length; i++)
        {
            combatObjects.Add(combatObjectsArray[i]);
        }
    }

    private void GetWaypoints()
    {
        //Convert the array of waypoints to a list.
        Waypoint[] tempWaypoints = FindObjectsOfType<Waypoint>();

        waypoints = new List<Waypoint>();

        for (int i = 0; i < tempWaypoints.Length; i++)
        {
            waypoints.Add(tempWaypoints[i]);
        }
    }

    private void SpawnPlayer()
    {
        //Create the maze version of the player and initialize their direction.
        mazePlayer = Instantiate(playerPrefab, Vector3.zero, transform.rotation);
        mazePlayer.GetComponent<PlayerMazeMovement>().UpdateWaypoints(Vector2.zero, Vector2.zero);

        //Add the player to the list of maze objects.
        mazeObjects.Add(mazePlayer);
    }

    public void SpawnSouls(int amount)
    {
        //Create a temporary list of waypoints.
        List<Waypoint> tempWaypoints = new List<Waypoint>();

        //Store the waypoints.
        for (int i = 0; i < waypoints.Count; i++)
        {
            tempWaypoints.Add(waypoints[i]);
        }

        //Remove waypoints from the list if the player is too close to them.
        for (int i = tempWaypoints.Count - 1; i >= 0; i--)
        {
            float distanceFromPlayer = Vector2.Distance(mazePlayer.transform.position, tempWaypoints[i].transform.position);

            if (distanceFromPlayer <= 1f)
            {
                tempWaypoints.RemoveAt(i);
            }
        }


        //If less waypoints are available than there are souls to spawn.
        if (tempWaypoints.Count < amount)
        {
            //Only spawn as many souls as there are available waypoints.
            amount = tempWaypoints.Count;
        }

        //Loop for as many souls are to be spawned.
        for (int i = 0; i < amount; i++)
        {
            //Select a random waypoint.
            Waypoint waypoint = tempWaypoints[Random.Range(0, tempWaypoints.Count)];
            //Spawn a soul at the waypoint.
            SpawnSoul(waypoint);
            //Don't reuse this waypoint.
            tempWaypoints.Remove(waypoint);
            //Increment the number of active souls.
            spawnedSouls++;
        }
    }

    private void SpawnSoul(Waypoint start)
    {
        GameObject instance = soulPrefab;

        if (tutorialController.enabled)
        {
            instance = tutorialSoulPrefab;
        }

        //Instantiate the soul.
        GameObject soul = Instantiate(instance, start.transform.position, transform.rotation);

        //Set the waypoints for the soul.
        soul.GetComponent<SoulMazeMovement>().SetWaypoints(start);
        //Assign a random combat soul to the newly created soul.

        if (!tutorialController.enabled)
        {
            List<GameObject> selectedLevel = combatSouls[Random.Range(0, GetClampedLevel())];
            soul.GetComponent<SoulMazeMovement>().combatPrefab = selectedLevel[Random.Range(0, selectedLevel.Count)];
        }

        //Add the soul to the list of maze objects.
        mazeObjects.Add(soul);
    }

    public void StartCombat(SoulMazeMovement soul)
    {
        //If the player has not already triggered combat with a soul.
        if (soulInCombatWith == null)
        {
            combatAudioSource.Play();

            //Store the soul the player started combat with.
            soulInCombatWith = soul.gameObject;

            //Create the soul the player will fight.
            combatSoul = Instantiate(soul.combatPrefab, Vector2.right * 10f, transform.rotation);

            soulStaminaText.color = Color.white;

            //Display the soul's stamina.
            combatSoul.GetComponent<SoulCombatController>().staminaText = soulStaminaText;
            combatSoul.GetComponent<SoulCombatController>().player = combatPlayer;

            if (tutorialController.enabled)
            {
                tutorialController.CompleteGoal(1, 1f);
                combatSoul.GetComponent<SoulTurretManager>().enabled = false;
            }
            else
            {
                int levelDifference = level - GetCombatSoulLevel(soul.combatPrefab);
                float scaleRate = 0.5f;

                combatSoul.GetComponent<SoulCombatController>().ScaleHealth(1f + (scaleRate * levelDifference));
                combatSoul.GetComponent<SoulTurretManager>().IncrementDamage(level - 1);
            }

            combatSoul.GetComponent<SoulCombatController>().UpdateStaminaText();

            //Add the soul to the list of combat objects.
            combatObjects.Add(combatSoul);

            //Reset the player's values.
            combatPlayer.RestartCombat();
            combatPlayer.transform.position = Vector2.left * 10f;

            StartTransition("IN");
        }
    }

    private int GetCombatSoulLevel(GameObject soulPrefab)
    {
        for (int i = 0; i < combatSouls.Count; i++)
        {
            for (int j = 0; j < combatSouls[i].Count; j++)
            {
                if (combatSouls[i][j] == soulPrefab)
                {
                    return i + 1;
                }
            }
        }

        return 1;
    }

    public void EndCombat(string result)
    {
        combatSoul.GetComponent<SoulTurretManager>().enabled = false;

        //Loop through all the projectiles.
        foreach (SoulProjectile projectile in FindObjectsOfType<SoulProjectile>())
        {
            //Destroy the projectiles.
            Destroy(projectile.gameObject);
        }

        foreach (SoulTurret turret in FindObjectsOfType<SoulTurret>())
        {
            Destroy(turret.gameObject);
        }

        //Switch to the maze section.
        //SwitchSection("Maze");

        //If the player won.
        if (result == "WIN")
        {
            if (tutorialController.enabled)
            {
                tutorialController.CompleteGoal(4, 1f);
            }

            money += combatSoul.GetComponent<SoulCombatController>().bounty;
            moneyText.text = "£" + money;

            timer += combatSoul.GetComponent<SoulCombatController>().timeBonus;
            timerText.color = Color.white;
            UpdateTimer();

            //Remove the soul from the list of combat objects.
            combatObjects.Remove(combatSoul);
            //Destroy the soul.
            Destroy(combatSoul);

            combatSoul = null;

            soulStaminaText.gameObject.SetActive(false);

            //Remove the soul the player fought from the list of maze objects.
            mazeObjects.Remove(soulInCombatWith);
            //Destroy the soul.
            soulInCombatWith.SetActive(false);
            Destroy(soulInCombatWith);
            //Decrement the number of active souls.
            spawnedSouls--;

            totalScore++;
            //Increment the player's score.
            currentScore++;
            //Display the new score.
            targetScoreText.text = currentScore.ToString() + "/" + targetScore.ToString();

            //If the player has met their target.
            if (currentScore == targetScore)
            {
                combatPlayer.transform.position = Vector2.left * 10f;

                //Set the upgrade screen as active and pause the game.
                upgradeScreen.SetActive(true);
                upgradeScreen.GetComponent<UpgradesSystem>().purchasesRemaining++;
                upgradeScreen.GetComponent<UpgradesSystem>().UpdateMoneyDisplay();
                dialogueSystem.ToggleDialogueDisplay(true, 0);
                dialogueSystem.DisplayDialogueLine(DialogueSystem.DialogueType.UpgradeUnlock);
                gamePaused = true;
            }
        }
        else
        {
            playerStunned = true;
        }

        //Set the player as not being in combat with any soul.
        soulInCombatWith = null;

        StartTransition("OUT");
    }

    private void SwitchSection(string sectionToSwitchTo)
    {
        activeSection = sectionToSwitchTo;

        //Set combat as not being active by default.
        bool combatActive = false;

        //If the game is switching to the maze.
        if (sectionToSwitchTo == "Maze")
        {
            //Set combat as not being active.
            combatActive = false;
        }
        //If the game is switching to combat.
        else if (sectionToSwitchTo == "Combat")
        {
            //Set combat as being active.
            combatActive = true;
        }

        //Loop through the maze objects and set them as active as appropriate.
        for (int i = 0; i < mazeObjects.Count; i++)
        {
            mazeObjects[i].SetActive(!combatActive);
        }
        
        //Loop through the combat objects and set them as active as appropriate.
        for (int i = 0; i < combatObjects.Count; i++)
        {
            combatObjects[i].SetActive(combatActive);
        }
    }

    public void LoadScene(string scene)
    {
        menuAudioSource.Play();
        //Load the selected scene.
        StartCoroutine(LoadSceneCoroutine(scene));
    }

    public void CloseUpgradeScreen()
    {
        menuAudioSource.Play();

        //targetScore += Mathf.CeilToInt(targetScore * 0.5f);
        //targetScore = Mathf.Min(targetScore, 8);

        targetScore = Mathf.Min(targetScore + 1, 6);

        currentScore = 0;

        targetScoreText.text = currentScore.ToString() + "/" + targetScore.ToString();

        //level = Mathf.Min(level + 1, combatSouls.Count);
        level++;

        dialogueSystem.ToggleDialogueDisplay(false, 0); ;
        upgradeScreen.SetActive(false);
        gamePaused = false;
    }

    public Vector2 GetHalfWorldSize()
    {
        float aspectRatio = (float)Screen.width / (float)Screen.height;

        float halfWorldHeight = Camera.main.orthographicSize;
        float halfWorldWidth = halfWorldHeight * aspectRatio;

        return new Vector2(halfWorldWidth, halfWorldHeight);
    }

    public void FadeSoulStamina(float progress)
    {
        soulStaminaText.color = Color.Lerp(Color.white, Color.clear, progress);
    }

    public void SpendMoney(int cost)
    {
        money -= cost;
        moneyText.text = "£" + money;
    }

    public bool GetPauseScreenActive()
    {
        return pauseScreen.activeInHierarchy;
    }

    public int GetTotalLevels()
    {
        return combatSouls.Count;
    }

    public void UpdateMasterVolume(float volume)
    {
        masterVolume = volume;
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);

        musicAudioSource.volume = masterVolume * musicVolume;
        combatAudioSource.volume = masterVolume * sfxVolume;
        menuAudioSource.volume = masterVolume * sfxVolume;
        combatPlayer.UpdateSFXVolume();
    }

    public void UpdateSFXVolume(float volume)
    {
        sfxVolume = volume;
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);

        combatAudioSource.volume = masterVolume * sfxVolume;
        menuAudioSource.volume = masterVolume * sfxVolume;
        combatPlayer.UpdateSFXVolume();
    }

    public void UpdateMusicVolume(float volume)
    {
        musicVolume = volume;
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);

        musicAudioSource.volume = masterVolume * musicVolume;
    }

    public void UpdateTextSpeed(float speed)
    {
        textSpeed = speed;
        PlayerPrefs.SetFloat("TextSpeed", textSpeed);
    }

    private void GetSettings()
    {
        List<Slider> settingSliders = new List<Slider>();

        Transform settingsScreen = pauseScreen.transform.GetChild(4);

        for (int i = 0; i < settingsScreen.childCount; i++)
        {
            if (settingsScreen.GetChild(i).GetComponent<Slider>() != null)
            {
                settingSliders.Add(settingsScreen.GetChild(i).GetComponent<Slider>());
            }
        }

        if (PlayerPrefs.HasKey("MasterVolume"))
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume");
            settingSliders[0].value = masterVolume;
        }

        if (PlayerPrefs.HasKey("SFXVolume"))
        {
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume");
            settingSliders[1].value = sfxVolume;
        }

        if (PlayerPrefs.HasKey("MusicVolume"))
        {
            musicVolume = PlayerPrefs.GetFloat("MusicVolume");
            settingSliders[2].value = musicVolume;
        }

        if (PlayerPrefs.HasKey("TextSpeed"))
        {
            textSpeed = PlayerPrefs.GetFloat("TextSpeed");
            settingSliders[3].value = textSpeed;
        }
    }

    public void OpenScreen(GameObject nextScreen)
    {
        menuAudioSource.Play();
        currentScreen.SetActive(false);
        nextScreen.SetActive(true);
        currentScreen = nextScreen;
    }

    public int GetClampedLevel()
    {
        return Mathf.Min(level, combatSouls.Count);
    }

    IEnumerator LoadSceneCoroutine(string scene)
    {
        yield return new WaitForSeconds(menuAudioSource.clip.length);
        SceneManager.LoadScene(scene);
    }
}
