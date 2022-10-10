using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField]
    private GameObject canvas = null;

    //Which menu screen is currently open.
    [SerializeField]
    GameObject currentScreen = null;
    private AudioSource menuAudioSource = null;
    private AudioSource musicAudioSource = null;

    private float masterVolume;
    private float musicVolume;
    private float sfxVolume;
    private float textSpeed;

    [SerializeField]
    private SpriteRenderer background = null;
    [SerializeField]
    private Color combatColourOne = Color.red;
    [SerializeField]
    private Color combatColourTwo = Color.blue;

    private Color startColour;
    private Color endColour;
    private float colourFadeTimer = 0f;
    private float colourFadeDuration = 10f;

    void Start()
    {
        musicAudioSource = canvas.GetComponent<AudioSource>();
        menuAudioSource = GetComponent<AudioSource>();

        GetSettings();

        startColour = combatColourOne;
        endColour = combatColourTwo;
    }

    void Update()
    {
        colourFadeTimer += Time.deltaTime;

        float colourFadeProgress = colourFadeTimer / colourFadeDuration;

        background.color = Color.Lerp(startColour, endColour, colourFadeProgress);

        if (colourFadeProgress >= 1f)
        {
            Color tempColour = startColour;

            startColour = endColour;
            endColour = tempColour;

            colourFadeTimer = 0f;
        }
    }

    public void OpenScreen(GameObject nextScreen)
    {
        menuAudioSource.Play();
        //Hide the current screen.
        currentScreen.SetActive(false);
        //Show the specified screen.
        nextScreen.SetActive(true);
        //Update which screen is currently active.
        currentScreen = nextScreen;
    }

    private int ConvertBoolToInt(bool value)
    {
        if (value == true)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    public void LoadScene(string scene)
    {
        menuAudioSource.Play();
        StartCoroutine(LoadSceneCoroutine(scene));
    }

    public void UpdateMasterVolume(float volume)
    {
        masterVolume = volume;
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);

        musicAudioSource.volume = masterVolume * musicVolume;
        menuAudioSource.volume = masterVolume * sfxVolume;
    }

    public void UpdateSFXVolume(float volume)
    {
        sfxVolume = volume;
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);

        menuAudioSource.volume = masterVolume * sfxVolume;
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

        Transform settingsScreen = canvas.transform.GetChild(3);

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

    IEnumerator LoadSceneCoroutine(string scene)
    {
        yield return new WaitForSeconds(menuAudioSource.clip.length);
        //If no scene is selected.
        if (scene == "")
        {
            //Quit the game.
            Application.Quit();
        }
        else
        {
            int loadTutorial = 0;

            if (scene == "Tutorial")
            {
                loadTutorial = 1;
            }

            //Load the specified scene.
            PlayerPrefs.SetInt("Tutorial", loadTutorial);
            SceneManager.LoadScene("Game");
        }
    }
}
