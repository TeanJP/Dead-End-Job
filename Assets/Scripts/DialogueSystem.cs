using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
    public enum DialogueType
    {
        Tutorial,
        GameOver,
        UpgradeUnlock,
        UpgradeReroll,
        UpgradePurcahse,
        UpgradeSkip
    }

    [System.Serializable]
    public class DialogueDisplay
    {
        public Text textOutput;
        public GameObject textBackground;
        public Animator grimReaper;

        public void Toggle(bool display)
        {
            ActivateUIElement(textOutput.transform, display);
            ActivateUIElement(textBackground.transform, display);
            ActivateUIElement(grimReaper.transform, display);

            grimReaper.enabled = display;
        }

        private void ActivateUIElement(Transform transform, bool display)
        {
            while (transform.parent != null && transform.parent.GetComponent<Canvas>() == null)
            {
                transform = transform.parent;
            }

            transform.gameObject.SetActive(display);
        }
    }

    [SerializeField]
    private List<DialogueDisplay> dialogueDisplays = new List<DialogueDisplay>();

    private DialogueDisplay activeDialogueDisplay = null;

    private Dictionary<DialogueType, List<string>> dialogueLines = new Dictionary<DialogueType, List<string>>();

    private bool animating = false;
    private int currentLetter = 1;
    private float animationDuration = 0.04f;
    private float animationTimer = 0f;
    private string currentLine = "";

    private GameController gameController = null;

    void Start()
    {
        gameController = GameController.Instance;

        StoreDialogueLines();
    }

    void Update()
    {
        if (animating && !gameController.GetPauseScreenActive())
        {
            AnimateDialogueLine();
        }
        else
        {
            if (!activeDialogueDisplay.grimReaper.GetCurrentAnimatorStateInfo(0).IsName("Grim Reaper Idle"))
            {
                activeDialogueDisplay.grimReaper.Play("Grim Reaper Idle");
            }
        }
    }

    private void StoreDialogueLines()
    {
        List<string> tutorialLines = new List<string>()
        {
            "Welcome to training! Let's start by moving around using the W, A, S and D keys.",
            "To get in range of a soul you first have to catch up to it. Run into the soul to start a fight with it!",
            "In the soul realm you can fly around. Give it a go by using the W, A, S and D keys!",
            "You can aim and use your vacuum cleaner with the mouse. Try using it to suck up some soul energy.",
            "There's a soul nearby. Try putting everything I've taught you together to capture it.",
            "That concludes your basic training. Now get out there and catch some souls!"
        };

        List<string> gameOverLines = new List<string>()
        {
            "I know you can do better than this!",
            "Is this really the best you can do?"
        };

        List<string> upgradeUnlockLines = new List<string>()
        {
            "Nice work out there!",
            "Looks like you're due a promotion!"
        };

        List<string> upgradeRerollLines = new List<string>()
        {
            "Let me see what I can find.",
            "Is there nothing to your liking?",
            "Are these bonuses really not good enough for you?"
        };

        List<string> upgradePurchaseLines = new List<string>()
        {
            "I hope you make good use of this!",
            "An excellent choice!",
            "I was thinking of getting that for myself!"
        };

        List<string> upgradeSkipLines = new List<string>()
        {
            "Keep up the good work!",
            "Good luck out there!",
            "See you around!"
        };

        dialogueLines.Add(DialogueType.Tutorial, tutorialLines);
        dialogueLines.Add(DialogueType.GameOver, gameOverLines);
        dialogueLines.Add(DialogueType.UpgradeUnlock, upgradeUnlockLines);
        dialogueLines.Add(DialogueType.UpgradeReroll, upgradeRerollLines);
        dialogueLines.Add(DialogueType.UpgradePurcahse, upgradePurchaseLines);
        dialogueLines.Add(DialogueType.UpgradeSkip, upgradeSkipLines);

    }

    public void SetDialogueDisplay(int i)
    {
        activeDialogueDisplay = dialogueDisplays[i];
    }

    public void ToggleDialogueDisplay(bool display, int i)
    {
        dialogueDisplays[i].Toggle(display);
    }

    public void DisplayDialogueLine(DialogueType dialogueType)
    {
        List<string> selectedType = dialogueLines[dialogueType];

        if (dialogueType == DialogueType.Tutorial && gameController.tutorialController.enabled)
        {
            currentLine = selectedType[gameController.tutorialController.GetCurrentGoal()];
        }
        else
        {
            currentLine = selectedType[Random.Range(0, selectedType.Count)];
        }

        animating = true;
        currentLetter = 1;
        animationTimer = 0f;
    }

    private void AnimateDialogueLine()
    {
        if (!activeDialogueDisplay.grimReaper.GetCurrentAnimatorStateInfo(0).IsName("Grim Reaper Talking"))
        {
            activeDialogueDisplay.grimReaper.Play("Grim Reaper Talking");
        }

        animationTimer += Time.deltaTime;

        if (animationTimer >= animationDuration * gameController.textSpeed)
        {
            string animatedLine = currentLine.Substring(0, currentLetter);
            
            currentLetter++;

            if (currentLetter > currentLine.Length)
            {
                activeDialogueDisplay.grimReaper.Play("Grim Reaper Idle");
                animating = false;
            }

            activeDialogueDisplay.textOutput.text = animatedLine;

            animationTimer = 0f;

        }
    }
}
