using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialController : MonoBehaviour
{
    private class TutorialGoal
    {
        private bool setupCompleted;
        private float targetCompletions;
        private float currentCompletions;

        public TutorialGoal(float targetCompletions)
        {
            this.setupCompleted = false;
            this.targetCompletions = targetCompletions;
            this.currentCompletions = 0;
        }

        public void SetSetupCompleted()
        {
            this.setupCompleted = true;
        }

        public bool GetSetupCompleted()
        {
            return this.setupCompleted;
        }

        public void CompleteGoal(float progress)
        {
            this.currentCompletions = Mathf.Min(this.currentCompletions + progress, targetCompletions);
        }

        public bool GetCompleted()
        {
            return this.targetCompletions == this.currentCompletions;
        }
    }

    private List<TutorialGoal> tutorialGoals;

    private int currentGoal = 0;

    private GameController gameController = null;

    private Vector2 playersLastPosition = Vector2.zero;
    private Transform mazePlayer = null;
    private Transform combatPlayer = null;

    private float moveSoulDuration = 0.45f;
    private float moveSoulTimer = 0f;

    void Start()
    {
        gameController = GameController.Instance;

        mazePlayer = gameController.mazePlayer.transform;
        combatPlayer = gameController.combatPlayer.transform;

        tutorialGoals = new List<TutorialGoal>()
        {
            new TutorialGoal(10f),
            new TutorialGoal(1f),
            new TutorialGoal(10f),
            new TutorialGoal(12f),
            new TutorialGoal(1f)
        };
    }

    void Update()
    {
        if (!gameController.gamePaused && gameController.activeTransition == "NONE")
        {
            if (currentGoal < tutorialGoals.Count)
            {
                if (!tutorialGoals[currentGoal].GetSetupCompleted())
                {
                    switch (currentGoal)
                    {
                        case 0:
                            gameController.dialogueSystem.DisplayDialogueLine(DialogueSystem.DialogueType.Tutorial);
                            playersLastPosition = mazePlayer.position;
                            tutorialGoals[currentGoal].SetSetupCompleted();
                            break;
                        case 1:
                            gameController.SpawnSouls(1);
                            tutorialGoals[currentGoal].SetSetupCompleted();
                            break;
                        case 2:
                            playersLastPosition = combatPlayer.position;
                            tutorialGoals[currentGoal].SetSetupCompleted();
                            break;
                        case 3:
                            gameController.combatSoul.GetComponent<SoulTurretManager>().enabled = true;
                            tutorialGoals[currentGoal].SetSetupCompleted();
                            break;
                        case 4:
                            moveSoulTimer += Time.deltaTime;

                            gameController.MoveCombatSoul(moveSoulTimer / moveSoulDuration);

                            if (moveSoulTimer >= moveSoulDuration)
                            {
                                gameController.combatSoul.GetComponent<SoulCombatController>().SetSetupComplete();
                                tutorialGoals[currentGoal].SetSetupCompleted();
                            }
                            break;
                    }
                }
                else
                {
                    float distanceMoved;

                    switch (currentGoal)
                    {
                        case 0:
                            distanceMoved = Vector2.Distance(mazePlayer.position, playersLastPosition);
                            CompleteGoal(currentGoal, distanceMoved);
                            playersLastPosition = mazePlayer.position;
                            break;
                        case 1:

                            break;
                        case 2:
                            distanceMoved = Vector2.Distance(combatPlayer.position, playersLastPosition);
                            CompleteGoal(currentGoal, distanceMoved);
                            playersLastPosition = combatPlayer.position;
                            break;
                        case 3:

                            break;
                        case 4:

                            break;
                    }
                }
            }
        }
    }

    public void CompleteGoal(int goal, float progress)
    {
        if (goal == currentGoal)
        {
            tutorialGoals[currentGoal].CompleteGoal(progress);


            if (tutorialGoals[currentGoal].GetCompleted())
            {
                currentGoal++;
                
                gameController.dialogueSystem.DisplayDialogueLine(DialogueSystem.DialogueType.Tutorial);

                if (currentGoal == tutorialGoals.Count)
                {
                    gameController.dialogueSystem.ToggleDialogueDisplay(false, 1);
                    gameController.dialogueSystem.SetDialogueDisplay(0);
                    gameController.dialogueSystem.ToggleDialogueDisplay(true, 0);

                    PlayerPrefs.SetInt("Tutorial", 0);
                    gameController.SetGameOver();
                }
            }
        }
    }

    public int GetCurrentGoal()
    {
        return currentGoal;
    }
}
