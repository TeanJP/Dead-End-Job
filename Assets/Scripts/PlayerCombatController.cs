using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCombatController : MonoBehaviour
{
    private enum SoulAnimation
    {
        None,
        ToCentre,
        ToInside
    }

    private SoulAnimation soulAnimation = SoulAnimation.None;
    private Transform enemySoul = null;
    private Vector2 animationStart;
    private Vector2 animationEnd;
    private Vector2 initialScale;
    private float targetRotation = 0f;
    private float animationSpeed = 3f;
    private float animationTimer = 0f;

    private AudioSource vacuumAudioSource = null;
    private AudioSource suctionAudioSource = null;

    //The game controller.
    private GameController gameController = null;

    //The text that will display the player's stamina.
    [SerializeField]
    private Text staminaText = null;

    private float startingStamina = 50f;
    //The current value of the player's stamina.
    private float stamina;
    //How much the player's starting stamina should be multiplied by.
    [HideInInspector]
    public float staminaMultiplier = 1f;

    //The base damage of the vacuum cleaner.
    private float vacuumDamage = 1f;
    //How much the player's damage should be multiplied by.
    [HideInInspector]
    public float vacuumDamageMultiplier = 1f;
    
    //The progress bar which displays the charge of the vacuum cleaner.
    [SerializeField]
    private Image vacuumChargeDisplay = null;
    //How much charge the vacuum can hold.
    private float actualMaximumCharge;
    [SerializeField]
    private float maximumVacuumCharge = 5f;
    //What the capacity of the vacuum should be multiplied by.
    [HideInInspector]
    public float maximumVacuumChargeMultiplier = 1f;
    //The current amount of charge the vacuum has.
    private float currentVacuumCharge;
    //How quickly the charge should decrease.
    private float vacuumChargeDrainRate = 1f;
    //How quickly the charge should increase.
    private float vacuumChargeRefillRate = 0.8f;
    //Whether the vacuum is recharging completely.
    private bool vacuumRechargingFromZero = false;
    //What colour the charge bar should be normally.
    private Color normalColour = new Color(1f, 0.85f, 0.4f);
    //What colour the charge bar should be when it is fully recharging.
    private Color emptyColour = new Color(1f, 0.5f, 0.4f);
    [HideInInspector]
    public bool vacuumActive = false;

    //The movement speed of the player.
    [SerializeField]
    private float movementSpeed = 3f;
    //What the palyer's movement speed should be multiplied by.
    [HideInInspector]
    public float movementSpeedMultiplier = 1f;

    //The vacuum cleaner object.
    private GameObject vacuumCleaner = null;
    //The poiint to which object are pulled towards.
    private Transform vacuumCleanerEnd = null;
    //The radius of the vacuum cleaner's cone.
    [SerializeField]
    private float vacuumCleanerRadius = 60f;
    //How far the vacuum cleaner can reach.
    [SerializeField]
    private float vacuumCleanerRange = 4f;
    //How quickly the vacuum cleaner pulls objects in.
    [SerializeField]
    private float vacuumCleanerStrength = 1f;
    //A contact filter for use when getting objects within range of the vacuum cleaner.
    ContactFilter2D contactFilter;

    //The object that hold the sprite mask and colldier for the vacuum.
    [SerializeField]
    private Transform vacuumCleanerDestructor = null;
    //The sprite mask to hide objects sucked up by the vacuum cleaner.
    private SpriteMask vacuumSpriteMask = null;
    //The colldier used to destroy destructible projectiles.
    private BoxCollider2D vacuumCollider = null;

    private Vector2 spriteDimensions;

    private List<UpgradesSystem.PlayerUpgrade> conditionalUpgrades = new List<UpgradesSystem.PlayerUpgrade>();
    private List<bool> activeUpgrades = new List<bool>();

    private VacuumEffectManager vacuumEffectManager = null;
    private List<LineRenderer> activeVacuumEffects = new List<LineRenderer>();

    private bool canTakeDamage = true;
    private float invincibilityDuration = 0.5f;
    private float invincibilityTimer = 0f;
    private SpriteRenderer hitPoint = null;

    void Start()
    {
        //Get the game controller.
        vacuumEffectManager = VacuumEffectManager.Instance;
        //gameController = FindObjectOfType<GameController>();

        float height = gameObject.GetComponent<SpriteRenderer>().bounds.size.y;
        float width = gameObject.GetComponent<SpriteRenderer>().bounds.size.x;

        spriteDimensions = new Vector2(width, height);
        spriteDimensions *= 0.5f;

        //Get the vacuum cleaner object.
        vacuumCleaner = transform.GetChild(1).gameObject;
        //Get the end point of the vacuum cleaner.
        vacuumCleanerEnd = vacuumCleaner.transform.GetChild(0).GetChild(0);

        //Get the sprite mask and collider for the vacuum cleaner.
        vacuumSpriteMask = vacuumCleanerDestructor.GetComponent<SpriteMask>();
        vacuumCollider = vacuumCleanerDestructor.GetComponent<BoxCollider2D>();

        stamina = startingStamina;
        //Display the player's current stamina.
        staminaText.text = stamina.ToString();

        actualMaximumCharge = Mathf.Max(maximumVacuumCharge * maximumVacuumChargeMultiplier, 1f);

        //Store the current charge of the vacuum cleaner.
        currentVacuumCharge = actualMaximumCharge;

        //Set the vacuum cleaner charge bar to be the default colour.
        vacuumChargeDisplay.color = normalColour;
    }

    void Update()
    {
        //If the game is not over.
        if (!gameController.gameOver && !gameController.gamePaused && gameController.activeTransition == "NONE")
        {
            if (soulAnimation == SoulAnimation.None)
            {
                CheckConditionalUpgrades();

                if (!canTakeDamage)
                {
                    AnimateIFrames();
                }

                //Get which direction the player is trying to move in.
                float horizontalMovement = Input.GetAxisRaw("Horizontal");
                float verticalMovement = Input.GetAxisRaw("Vertical");

                //Store and normalize the player's direction.
                Vector2 direction = new Vector2(horizontalMovement, verticalMovement);
                direction.Normalize();

                //Move the player in the direction they are trying to move in.
                transform.Translate((direction * Mathf.Clamp(movementSpeed * movementSpeedMultiplier, 1.5f, 5f)) * Time.deltaTime);

                Vector2 halfWorldSize = gameController.GetHalfWorldSize();

                float x = transform.position.x;
                float y = transform.position.y;

                float clampedX = Mathf.Clamp(x, -halfWorldSize.x + spriteDimensions.x, halfWorldSize.x - spriteDimensions.x);
                float clampedY = Mathf.Clamp(y, -halfWorldSize.y + spriteDimensions.y, halfWorldSize.y - spriteDimensions.y);

                transform.position = new Vector2(clampedX, clampedY);

                //Rotate the vacuum cleaner towards the mouse cursor.
                RotateVacuumCleaner();

                //Make the vacuum cleaner destructor follow the vacuum cleaner.
                vacuumCleanerDestructor.position = vacuumCleanerEnd.position;
                vacuumCleanerDestructor.rotation = vacuumCleanerEnd.rotation;
                Vector3 previousScale = vacuumCleanerDestructor.localScale;
                Vector3 flippedScale = new Vector3(Mathf.Abs(previousScale.x) * Mathf.Sign(transform.localScale.x), previousScale.y, previousScale.z);
                vacuumCleanerDestructor.localScale = flippedScale;

                //If the player is trying to use the vacuum cleaner, they have charge and the vaccum cleaner is not doing a full recharge.
                if (Input.GetMouseButton(0) && currentVacuumCharge > 0f && !vacuumRechargingFromZero)
                {
                    if (!vacuumAudioSource.isPlaying)
                    {
                        vacuumAudioSource.Play();
                    }

                    //Enable the vacuum cleaner destructor's components.
                    //vacuumSpriteMask.enabled = true;
                    //vacuumCollider.enabled = true;

                    vacuumActive = true;

                    //Pull objects in with.
                    UseVacuumCleaner();
                    //Decrement the vacuum cleaner's charge.
                    currentVacuumCharge = Mathf.Max(0f, currentVacuumCharge - (Time.deltaTime * vacuumChargeDrainRate));
                    //Update the charge bar.
                    vacuumChargeDisplay.fillAmount = currentVacuumCharge / actualMaximumCharge;

                    //If the player has run out of charge.
                    if (currentVacuumCharge == 0f)
                    {
                        //Set the vacuum as doing a full recharge.
                        vacuumRechargingFromZero = true;
                        vacuumChargeDisplay.color = emptyColour;
                    }
                }
                else
                {
                    if (activeVacuumEffects.Count != 0)
                    {
                        for (int i = 0; i < activeVacuumEffects.Count; i++)
                        {
                            activeVacuumEffects[i].gameObject.SetActive(false);
                        }

                        activeVacuumEffects = new List<LineRenderer>();
                    }

                    if (vacuumAudioSource.isPlaying)
                    {
                        vacuumAudioSource.Stop();
                    }

                    vacuumActive = false;

                    //Disable the vacuum cleaner destructor's components.
                    //vacuumSpriteMask.enabled = false;
                    //vacuumCollider.enabled = false;

                    //Increment the vacuum cleaner's charge.
                    currentVacuumCharge = Mathf.Min(actualMaximumCharge, currentVacuumCharge + (Time.deltaTime * vacuumChargeRefillRate));
                    //Update the charge bar.
                    vacuumChargeDisplay.fillAmount = currentVacuumCharge / actualMaximumCharge;

                    //If the vacuum cleaner was doing a full recharge and it is now full.
                    if (vacuumRechargingFromZero && Mathf.FloorToInt(currentVacuumCharge) == Mathf.FloorToInt(actualMaximumCharge))
                    {
                        //Set the vacuum cleaner as not doing a full recharge.
                        vacuumRechargingFromZero = false;
                        //Set the charge bar colour back to the default.
                        vacuumChargeDisplay.color = normalColour;
                    }
                }
            }
            else
            {
                if (!vacuumAudioSource.isPlaying)
                {
                    vacuumAudioSource.Play();
                }

                if (activeVacuumEffects.Count != 0)
                {
                    activeVacuumEffects[0].material.mainTextureOffset = activeVacuumEffects[0].material.mainTextureOffset + new Vector2(Time.deltaTime, 0);
                    activeVacuumEffects[0].SetPositions(new Vector3[] { vacuumCleanerEnd.position, enemySoul.transform.position });
                }

                switch (soulAnimation)
                {
                    case SoulAnimation.ToCentre:
                        float distance = Vector2.Distance(animationStart, animationEnd);
                        animationTimer += ((Time.deltaTime / distance) * animationSpeed);

                        gameController.FadeSoulStamina(animationTimer / 0.5f);
                        enemySoul.position = Vector2.Lerp(animationStart, animationEnd, animationTimer);
                        enemySoul.localScale = Vector2.Lerp(initialScale, initialScale * 0.2f, animationTimer);
                        float newRotation = Mathf.Lerp(0f, targetRotation, animationTimer);
                        enemySoul.rotation = Quaternion.Euler(new Vector3(0f, 0f, newRotation));

                        if ((Vector2)enemySoul.position == animationEnd)
                        {
                            activeVacuumEffects[0].gameObject.SetActive(false);
                            activeVacuumEffects = new List<LineRenderer>();

                            animationStart = animationEnd;
                            animationEnd = vacuumCleanerDestructor.transform.GetChild(0).transform.position;
                            soulAnimation = SoulAnimation.ToInside;
                            animationTimer = 0f;
                            PlaySuctionSound();
                        }
                        break;
                    case SoulAnimation.ToInside:
                        animationTimer += Time.deltaTime;
                        enemySoul.position = Vector2.Lerp(animationStart, animationEnd, animationTimer / 1f);

                        if ((Vector2)enemySoul.position == animationEnd)
                        {
                            gameController.EndCombat("WIN");
                            gameController.soulAnimationInProgress = false;
                        }
                        break;
                }
            }

        }
        else
        {
            if (vacuumAudioSource.isPlaying)
            {
                vacuumAudioSource.Stop();
            }
        }

        //Make the player's stamina and charge bar follow the player.
        Vector2 screenPosition = Camera.main.WorldToScreenPoint(transform.position);
        Vector2 vacuumUIPosition = screenPosition + GetScaledUISpacing(new Vector2(0f, 140f));
        Vector2 staminaUIPosition = vacuumUIPosition + GetScaledUISpacing(new Vector2(0f, 30f));


        if (staminaUIPosition.y > 1080f)
        {
            vacuumUIPosition = screenPosition - GetScaledUISpacing(new Vector2(0f, 140f));
            staminaUIPosition = vacuumUIPosition - GetScaledUISpacing(new Vector2(0f, 30f));
        }

        vacuumChargeDisplay.transform.parent.GetComponent<RectTransform>().position = vacuumUIPosition;
        staminaText.rectTransform.position = staminaUIPosition;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //Get the projectile script.
        SoulProjectile projectile = other.gameObject.GetComponent<SoulProjectile>();

        //If the object is a projectile.
        if (projectile != null && canTakeDamage)
        {
            //Make the player take damage.
            stamina = Mathf.Max(0f, stamina - projectile.damage);
            //Displat the player's new stamina.
            staminaText.text = Mathf.CeilToInt(stamina).ToString();

            canTakeDamage = false;

            //If the player is out of stamina.
            if (stamina == 0)
            {
                //Set the player as having lost the battle.
                gameController.EndCombat("LOSE");
            }

            Destroy(projectile.gameObject);
        }
    }

    private float GetAngleBetweenObjects(Vector2 from, Vector2 to)
    {
        //Get the angle between two vectors.
        Vector2 direction = to - from;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private void RotateVacuumCleaner()
    {
        //Store the position of the mouse cursor.
        Vector2 mousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        //Convert the mouse position to a world position.
        mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

        //If the mouse is to the left of the player.
        if (mousePosition.x < transform.position.x)
        {
            //Make the player face left.
            transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x) * -1f, transform.localScale.y); 
        }
        else
        {
            //Make the player face right.
            transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x), transform.localScale.y);
        }

        //Work out the angle between the vacuum cleaner and the mouse.
        float angleToMouse = GetAngleBetweenObjects(vacuumCleaner.transform.position, mousePosition);

        //If the player has been flipped to look left.
        if (Mathf.Sign(transform.localScale.x) == -1f)
        {
            //Add 180 degrees to the angle.
            angleToMouse += 180f;
        }

        //Update the rotation of the vacuum cleaner.
        vacuumCleaner.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, angleToMouse));
    }

    private void UseVacuumCleaner()
    {
        //Create an empty list of colliders.
        List<Collider2D> colliders = new List<Collider2D>();

        //Get object with a collider within range of the vacuum cleaner.
        Physics2D.OverlapCircle(vacuumCleanerEnd.position, vacuumCleanerRange, contactFilter.NoFilter(), colliders);

        //Loop through the object.
        for (int i = colliders.Count - 1; i >= 0; i--)
        {
            //Store the game object the collider is attached to.
            GameObject currentObject = colliders[i].gameObject;

            //Get the angle between the vaccuum cleaner and it's end point.
            float angleToEnd = GetAngleBetweenObjects(vacuumCleaner.transform.position, vacuumCleanerEnd.position);
            //Get the angle between the vacuum cleaner end point and the current object.
            float angleToObject = GetAngleBetweenObjects(vacuumCleanerEnd.position, currentObject.transform.position);

            //Work out if the object is within the vacuum cleaner's cone.
            bool withinRadius = angleToEnd + (vacuumCleanerRadius * 0.5) > angleToObject && angleToEnd + (-vacuumCleanerRadius * 0.5f) < angleToObject;

            //Get whether the object can be sucked up.
            bool destrutcible = CheckObjectDestructible(currentObject);

            //If the object can't be sucked up or isn't within range.
            if (!withinRadius || !destrutcible)
            { 
                //Ignore this object.
                colliders.RemoveAt(i);
            }
        }

        if (colliders.Count > activeVacuumEffects.Count)
        {
            activeVacuumEffects = vacuumEffectManager.GetVacuumEffects(colliders.Count);
        }
        else if (colliders.Count < activeVacuumEffects.Count)
        {
            for (int i = activeVacuumEffects.Count - 1; i > colliders.Count - 1; i--)
            {
                activeVacuumEffects[i].gameObject.SetActive(false);
                activeVacuumEffects.RemoveAt(i);
            }
        }

        //Loop through the remaining objects.
        for (int i = 0; i < colliders.Count; i++)
        {
            //Get the game object that the collider is attached to.
            GameObject currentObject = colliders[i].gameObject;

            activeVacuumEffects[i].material.mainTextureOffset = activeVacuumEffects[i].material.mainTextureOffset + new Vector2(Time.deltaTime, 0);
            activeVacuumEffects[i].SetPositions(new Vector3[] { vacuumCleanerEnd.position, currentObject.transform.position});

            //Work out the direction the object is from the vacuum cleaner.
            Vector2 direction = vacuumCleanerEnd.position - currentObject.transform.position;
            direction.Normalize();

            //Pull the object towards the vacuum cleaner.

            SoulCombatController soul = currentObject.GetComponent<SoulCombatController>();
            SoulProjectile soulProjectile = currentObject.GetComponent<SoulProjectile>();

            //If the object is a soul.
            if (soul != null)
            {
                if (!soul.CheckTeleporting())
                {
                    //Damage the soul.
                    bool fatalDamage = soul.TakeDamage(Mathf.Max(vacuumDamage * vacuumDamageMultiplier, 1f));

                    //Debug.Log(fatalDamage);
                    if (fatalDamage)
                    {
                        gameController.soulAnimationInProgress = true;

                        soul.GetComponent<SoulTurretManager>().DeactivateTurrets();
                        GetComponent<CircleCollider2D>().enabled = false;

                        soul.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
                        enemySoul = soul.transform;
                        animationStart = soul.transform.position;
                        animationEnd = vacuumCleanerDestructor.transform.position;

                        initialScale = soul.transform.localScale;

                        Transform destructorTransform = vacuumCleanerDestructor.transform;
                        float destructorRotation = vacuumCleanerDestructor.transform.rotation.eulerAngles.z;

                        if (Mathf.Abs(destructorRotation - 360f) < destructorRotation)
                        {
                            destructorRotation = destructorRotation - 360f;
                        }

                        if ((enemySoul.position.y <= destructorTransform.position.y && enemySoul.position.x >= destructorTransform.position.x) || (enemySoul.position.y >= destructorTransform.position.y && enemySoul.position.x <= destructorTransform.position.x))
                        {
                            targetRotation = 90f + destructorRotation;
                        }
                        else
                        {
                            targetRotation = -90f + destructorRotation;
                        }

                        soulAnimation = SoulAnimation.ToCentre;

                        if (activeVacuumEffects.Count == 0)
                        {
                            activeVacuumEffects = vacuumEffectManager.GetVacuumEffects(1);
                        }
                        else
                        {
                            for (int j = activeVacuumEffects.Count - 1; j > 0; j--)
                            {
                                activeVacuumEffects[j].gameObject.SetActive(false);
                                activeVacuumEffects.RemoveAt(j);
                            }
                        }

                        activeVacuumEffects[0].gameObject.SetActive(true);
                        activeVacuumEffects[0].SetPositions(new Vector3[] { vacuumCleanerEnd.position, enemySoul.transform.position });

                        return;
                    }
                    
                    currentObject.transform.position += (Vector3)direction * vacuumCleanerStrength * Time.deltaTime;
                }
            }
            else if (soulProjectile != null)
            {
                soulProjectile.ChangeVelocityDirection(direction, vacuumCleanerStrength * 10f);
                currentObject.transform.position += (Vector3)direction * vacuumCleanerStrength * Time.deltaTime;
            }
        }
    }

    private bool CheckObjectDestructible(GameObject objectToCheck)
    {
        //Get the projectile script.
        SoulProjectile soulProjectile = objectToCheck.GetComponent<SoulProjectile>();
        //Get the soul script.
        SoulCombatController soul = objectToCheck.GetComponent<SoulCombatController>();

        //If the object is a soul and isn't teleporting, or the object is a projectile and is destructible.
        if ((soul != null) || (soulProjectile != null && soulProjectile.destructible))
        {
            //The object can be sucked up.
            return true;
        }
        else
        {
            //The object can't be sucked up.
            return false;
        }
    }

    public void RestartCombat()
    {
        //Reset the values of the player and the UI associated with it.
        //transform.position = startPosition;

        GetComponent<CircleCollider2D>().enabled = true;

        enemySoul = null;
        soulAnimation = SoulAnimation.None;
        animationTimer = 0f;

        if (hitPoint == null)
        {
            hitPoint = transform.GetChild(0).GetComponent<SpriteRenderer>();
        }

        hitPoint.enabled = true;
        canTakeDamage = true;
        invincibilityTimer = 0f;

        stamina = Mathf.Max(Mathf.Ceil(startingStamina * staminaMultiplier), 1f);
        staminaText.text = stamina.ToString();

        actualMaximumCharge = Mathf.Max(maximumVacuumCharge * maximumVacuumChargeMultiplier, 1f);

        currentVacuumCharge = actualMaximumCharge;
        vacuumChargeDisplay.fillAmount = currentVacuumCharge / actualMaximumCharge;
    }

    public void AddVacuumCharge(float chargeGained)
    {
        currentVacuumCharge = Mathf.Min(actualMaximumCharge, currentVacuumCharge + chargeGained);
        vacuumChargeDisplay.fillAmount = currentVacuumCharge / actualMaximumCharge;
    }

    private Vector2 GetScaledUISpacing(Vector2 originalSpacing)
    {
        Vector2 spacingScale = new Vector2(originalSpacing.x / 1920f, originalSpacing.y / 1080f);
        Vector2 scaledSpacing = new Vector2(Screen.width, Screen.height) * spacingScale;

        return scaledSpacing;
    }

    public void AddBonusUpgrade(float staminaBonus, float damageBonus, float capacityBonus, float speedBonus)
    {
        staminaMultiplier += (staminaBonus * 0.01f);
        vacuumDamageMultiplier += (damageBonus * 0.01f);
        maximumVacuumChargeMultiplier += (capacityBonus * 0.01f);
        movementSpeedMultiplier += (speedBonus * 0.01f);
    }

    public void AddConditionalUpgrade(UpgradesSystem.PlayerUpgrade conditionalUpgrade)
    {
        conditionalUpgrades.Add(conditionalUpgrade);
        activeUpgrades.Add(false);
    }

    private void CheckConditionalUpgrades()
    {
        for (int i = 0; i < conditionalUpgrades.Count; i++)
        {
            float[] bonuses = new float[] { conditionalUpgrades[i].GetSpeed() * 0.01f, conditionalUpgrades[i].GetPower() * 0.01f };


            UpgradesSystem.UpgradeCondition condition = conditionalUpgrades[i].GetCondition();

            float conditionValue = condition.GetValue() * 0.01f;
            float comparisonValue = 0f;

            switch (condition.GetAttribute())
            {
                case UpgradesSystem.ComparisonType.Stamina:
                    comparisonValue = stamina / Mathf.Ceil(100f * staminaMultiplier);
                    break;
                case UpgradesSystem.ComparisonType.Charge:
                    comparisonValue = currentVacuumCharge / actualMaximumCharge;
                    break;
            }

            bool bonusApplied = false;

            switch (condition.GetComparison())
            {
                case UpgradesSystem.ComparisonCondition.Equal:
                    if (comparisonValue == conditionValue)
                    {
                        bonusApplied = true;
                    }
                    break;
                case UpgradesSystem.ComparisonCondition.Greater:
                    if (comparisonValue > conditionValue)
                    {
                        bonusApplied = true;
                    }
                    break;
                case UpgradesSystem.ComparisonCondition.Less:
                    if (comparisonValue < conditionValue)
                    {
                        bonusApplied = true;
                    }
                    break;
            }

            if (bonusApplied && !activeUpgrades[i])
            {
                movementSpeedMultiplier += bonuses[0];
                vacuumDamageMultiplier += bonuses[1];

                activeUpgrades[i] = true;
            }
            else if (!bonusApplied && activeUpgrades[i])
            {
                movementSpeedMultiplier -= bonuses[0];
                vacuumDamageMultiplier -= bonuses[1];

                activeUpgrades[i] = false;
            }
        }
    }

    private void AnimateIFrames()
    {
        invincibilityTimer += Time.deltaTime;

        if (invincibilityTimer < invincibilityDuration * (1f / 3f))
        {
            hitPoint.enabled = false;
        }
        else if (invincibilityTimer < invincibilityDuration * (2f / 3f))
        {
            hitPoint.enabled = true;
        }
        else
        {
            hitPoint.enabled = false;
        }

        if (invincibilityTimer >= invincibilityDuration)
        {
            canTakeDamage = true;
            hitPoint.enabled = true;
            invincibilityTimer = 0f;
        }
    }

    public void PlaySuctionSound()
    {
        suctionAudioSource.Play();
    }

    public void UpdateSFXVolume()
    {
        suctionAudioSource.volume = gameController.masterVolume * gameController.sfxVolume;
        vacuumAudioSource.volume = gameController.masterVolume * gameController.sfxVolume;
    }

    public void Setup()
    {
        gameController = GameController.Instance;

        AudioSource[] audioSources = GetComponents<AudioSource>();
        vacuumAudioSource = audioSources[0];
        suctionAudioSource = audioSources[1];
    }
}
