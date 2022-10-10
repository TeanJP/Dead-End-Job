using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpgradesSystem : MonoBehaviour
{
    private enum PlayerAttirbute
    {
        Stamina,
        Speed,
        Power,
        Capacity
    }

    private List<PlayerAttirbute> playerAttirbutes = new List<PlayerAttirbute>();

    public enum ComparisonCondition
    {
        Equal,
        Less,
        Greater
    }

    public enum ComparisonType
    {
        Stamina,
        Charge
    }

    public class UpgradeCondition
    {
        private ComparisonType type;
        private ComparisonCondition comparison;
        private float value;

        public UpgradeCondition(ComparisonType type, ComparisonCondition comparison, float value)
        {
            this.type = type;
            this.comparison = comparison;
            this.value = value;
        }

        #region Condition Getters
        public ComparisonType GetAttribute()
        {
            return this.type;
        }

        public ComparisonCondition GetComparison()
        {
            return this.comparison;
        }

        public float GetValue()
        {
            return this.value;
        }
        #endregion
    }

    public class PlayerUpgrade
    {
        private UpgradeCondition condition;
        private float staminaBonus;
        private float speedBonus;
        private float powerBonus;
        private float capacityBonus;
        private int cost;

        public PlayerUpgrade(UpgradeCondition condition, float staminaBonus, float speedBonus, float powerBonus, float capacityBonus, int cost)
        {
            this.condition = condition;
            this.staminaBonus = staminaBonus;
            this.speedBonus = speedBonus;
            this.powerBonus = powerBonus;
            this.capacityBonus = capacityBonus;
            this.cost = cost;
        }

        #region Upgrade Getters
        public UpgradeCondition GetCondition()
        {
            return this.condition;
        }

        public float GetStamina()
        {
            return this.staminaBonus;
        }

        public float GetSpeed()
        {
            return this.speedBonus;
        }

        public float GetPower()
        {
            return this.powerBonus;
        }

        public float GetCapacity()
        {
            return this.capacityBonus;
        }
        
        public int GetCost()
        {
            return this.cost;
        }
        #endregion
    }

    private GameController gameController = null;

    float conditionalWeight = 0.25f;

    private List<PlayerUpgrade> availableUpgrades = new List<PlayerUpgrade>();

    private List<Text> upgradeDisplays = new List<Text>();

    private int rerollCost = 25;

    [HideInInspector]
    public int purchasesRemaining = 0;

    [SerializeField]
    private Text moneyText = null;
    [SerializeField]
    private Text purchasesText = null;

    private int bonusCap = 20;

    void Start()
    {
        gameController = GameController.Instance;

        for (int i = 0; i < transform.childCount - 2; i++)
        {
            Text upgradeText = null;

            if (transform.GetChild(i).childCount != 0)
            {
                upgradeText = transform.GetChild(i).GetChild(0).GetComponent<Text>();
            }

            if (upgradeText != null)
            {
                upgradeDisplays.Add(upgradeText);
            }
        }

        for (int i = 0; i < 4; i++)
        {
            playerAttirbutes.Add((PlayerAttirbute)i);
        }

        GetUpgrades();

        purchasesText.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }

    private void GetUpgrades()
    {
        availableUpgrades = new List<PlayerUpgrade>();

        for (int i = 0; i < upgradeDisplays.Count; i++)
        {
            availableUpgrades.Add(GenerateUpgrade());
        }

        DisplayUpgrades();
    }

    private PlayerUpgrade GenerateUpgrade()
    {
        int maxBonus = (bonusCap / gameController.GetTotalLevels()) * gameController.GetClampedLevel();

        int cost = 50;

        List<PlayerAttirbute> availableAttributes = new List<PlayerAttirbute>();

        for (int i = 0; i < playerAttirbutes.Count; i++)
        {
            availableAttributes.Add(playerAttirbutes[i]);
        }

        PlayerAttirbute positiveAttribute;
        float positiveBonus;

        UpgradeCondition upgradeCondition = null;

        float[] bonuses = new float[] { 0f, 0f, 0f, 0f};

        if (Random.Range(0f, 1f) < conditionalWeight)
        {
            availableAttributes.Remove(PlayerAttirbute.Stamina);
            availableAttributes.Remove(PlayerAttirbute.Capacity);

            positiveAttribute = availableAttributes[Random.Range(0, availableAttributes.Count)];

            if (positiveAttribute == PlayerAttirbute.Speed)
            {
                positiveBonus = Random.Range(2, 4 + 1) * 5f;
            }
            else
            {
                positiveBonus = Random.Range(2, maxBonus + 1) * 5f;
            }

            bonuses[(int)positiveAttribute] = positiveBonus;

            ComparisonType comparisonType = (ComparisonType)Random.Range(0, 1 + 1);
            ComparisonCondition comparisonCondition;
            float comparisonValue = 50f;

            if (comparisonType == ComparisonType.Stamina)
            {
                comparisonCondition = (ComparisonCondition)Random.Range(0, 2 + 1);

                switch (comparisonCondition)
                {
                    case ComparisonCondition.Equal:
                        comparisonValue = 100f;
                        break;
                    case ComparisonCondition.Greater:
                        comparisonValue = 100f - (Random.Range(5, 10 + 1) * 5f);
                        break;
                    case ComparisonCondition.Less:
                        comparisonValue = Random.Range(5, 10 + 1) * 5f;
                        break;
                }       
            }
            else
            {
                comparisonCondition = (ComparisonCondition)Random.Range(1, 2 + 1);

                switch (comparisonCondition)
                {
                    case ComparisonCondition.Greater:
                        comparisonValue = 100f - (Random.Range(2, 10 + 1) * 5f);
                        break;
                    case ComparisonCondition.Less:
                        comparisonValue = Random.Range(2, 10 + 1) * 5f;
                        break;
                }
            }

            switch (comparisonCondition)
            {
                case ComparisonCondition.Equal:
                    cost = Mathf.RoundToInt(positiveBonus * 2.5f);
                    cost -= cost % 5;
                    break;
                case ComparisonCondition.Greater:
                    cost = Mathf.RoundToInt(positiveBonus * 2.5f) + (100 - (int)comparisonValue);
                    cost -= cost % 5;
                    break;
                case ComparisonCondition.Less:
                    cost = Mathf.RoundToInt(positiveBonus * 2.5f) + (int)comparisonValue;
                    cost -= cost % 5;
                    break;
            }

            upgradeCondition = new UpgradeCondition(comparisonType, comparisonCondition, comparisonValue);
        }
        else
        {
            positiveAttribute = availableAttributes[Random.Range(0, availableAttributes.Count)];

            if (positiveAttribute == PlayerAttirbute.Speed)
            {
                positiveBonus = Random.Range(2, 4 + 1) * 5f;
            }
            else
            {
                positiveBonus = Random.Range(2, maxBonus + 1) * 5f;

            }

            bonuses[(int)positiveAttribute] = positiveBonus;

            if (positiveBonus >= 50f && Random.Range(0f, 1f) > 0.5f)
            {
                availableAttributes.Remove(positiveAttribute);

                PlayerAttirbute negativeAttribute = availableAttributes[Random.Range(0, availableAttributes.Count)];
                float negativePenalty = Mathf.Floor((positiveBonus * 0.5f) / 10f) * 10f;
                negativePenalty *= -1f;

                bonuses[(int)negativeAttribute] = negativePenalty;

                cost = Mathf.RoundToInt(positiveBonus * 2.5f);
                cost -= cost % 5;
            }
            else
            {
                cost = (int)positiveBonus * 5;
            }
        }

        PlayerUpgrade generatedUpgrade = new PlayerUpgrade(upgradeCondition, bonuses[0], bonuses[1], bonuses[2], bonuses[3], cost);
        return generatedUpgrade;
    }

    private void DisplayUpgrades()
    {
        UpdateMoneyDisplay();

        for (int i = 0; i < upgradeDisplays.Count; i++)
        {
            float[] bonuses = new float[] { availableUpgrades[i].GetStamina(), availableUpgrades[i].GetSpeed(), availableUpgrades[i].GetPower(), availableUpgrades[i].GetCapacity() };

            string upgradeDescription = "";

            if (availableUpgrades[i].GetCondition() != null)
            {
                UpgradeCondition upgradeCondition = availableUpgrades[i].GetCondition();

                string comparisonTypeText = "";
                string comparisonConditionText = "";

                ComparisonType comparisonType = upgradeCondition.GetAttribute();

                switch (comparisonType)
                {
                    case ComparisonType.Stamina:
                        comparisonTypeText = "health";
                        break;
                    case ComparisonType.Charge:
                        comparisonTypeText = "vacuum cleaner charge";
                        break;
                }

                ComparisonCondition comparisonCondition = upgradeCondition.GetComparison();

                switch (comparisonCondition)
                {
                    case ComparisonCondition.Equal:
                        comparisonConditionText = "=";
                        break;
                    case ComparisonCondition.Greater:
                        comparisonConditionText = ">";
                        break;
                    case ComparisonCondition.Less:
                        comparisonConditionText = "<";
                        break;
                }

                string comparisonValue = upgradeCondition.GetValue().ToString() + "%";

                upgradeDescription = "When " + comparisonTypeText + " " + comparisonConditionText + " " + comparisonValue;

                for (int j = 0; j < bonuses.Length; j++)
                {
                    if (bonuses[j] != 0f)
                    {
                        string bonusType;

                        if (j == 0)
                            bonusType = "health";
                        else if (j == 1)
                            bonusType = "movement speed";
                        else if (j == 2)
                            bonusType = "damage";
                        else
                            bonusType = "charge capacity";

                        upgradeDescription += " +" + bonuses[j] + "% " + bonusType + "\n";
                        break;
                    }
                }
            }
            else
            {
                for (int j = 0; j < bonuses.Length; j++)
                {
                    if (bonuses[j] != 0f)
                    {
                        string bonusType;

                        if (j == 0)
                            bonusType = "health";
                        else if (j == 1)
                            bonusType = "movement speed";
                        else if (j == 2)
                            bonusType = "damage";
                        else
                            bonusType = "charge capacity";

                        string bonusSign = "+";

                        if (Mathf.Sign(bonuses[j]) == -1f)
                            bonusSign = "-";

                        upgradeDescription += bonusSign + Mathf.Abs(bonuses[j]) + "% " + bonusType + "\n";
                    }
                }
            }

            upgradeDescription += "\n" + "£" + availableUpgrades[i].GetCost();

            upgradeDisplays[i].text = upgradeDescription;
        }
    }

    public void RerollUpgrades()
    {
        if (gameController.money >= rerollCost)
        {
            gameController.dialogueSystem.DisplayDialogueLine(DialogueSystem.DialogueType.UpgradeReroll);
            gameController.menuAudioSource.Play();
            gameController.SpendMoney(rerollCost);
            GetUpgrades();
        }
    }

    public void ApplyUpgrade(int index)
    {
        PlayerUpgrade selectedUpgrade = availableUpgrades[index];

        //&& purchasesRemaining != 0

        if (gameController.money >= selectedUpgrade.GetCost())
        {
            gameController.dialogueSystem.DisplayDialogueLine(DialogueSystem.DialogueType.UpgradePurcahse);

            if (selectedUpgrade.GetCondition() != null)
            {
                gameController.combatPlayer.AddConditionalUpgrade(selectedUpgrade);
            }
            else
            {
                gameController.combatPlayer.AddBonusUpgrade(selectedUpgrade.GetStamina(), selectedUpgrade.GetPower(), selectedUpgrade.GetCapacity(), selectedUpgrade.GetSpeed());
            }

            gameController.SpendMoney(selectedUpgrade.GetCost());
            availableUpgrades[index] = GenerateUpgrade();
            DisplayUpgrades();

            purchasesRemaining--;
            purchasesText.text = "REMAINING: " + purchasesRemaining;
        }

        gameController.menuAudioSource.Play();
    }

    public void UpdateMoneyDisplay()
    {
        purchasesText.text = "REMAINING: " + purchasesRemaining;
        moneyText.text = "£" + gameController.money;
    }
}
