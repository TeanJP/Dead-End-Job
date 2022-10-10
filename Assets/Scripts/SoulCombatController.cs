using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoulCombatController : MonoBehaviour
{
    public Text staminaText = null;
    [SerializeField]
    private float stamina = 100f;
    private bool canTakeDamage = true;

    [HideInInspector]
    public PlayerCombatController player = null;
    private GameController gameController = null;

    private SpriteRenderer spriteRenderer = null;

    private Vector2 spriteDimensions;

    private float fleeDistance = 3f;
    private float fleeDecay = 0f;
    private float movementSpeed = 2f;

    private Vector2 direction = Vector2.zero;
    private float directionDuration;
    private float directionTimer = 0f;

    private bool teleporting = false;
    private Vector2 teleportDestination = Vector2.zero;
    private float teleportDelay = 2f;
    private float teleportTransitionTimer = 0f;
    private float teleportTransitionLength = 1f;
    private float minTeleportDistance = 4f;
    private float maxTeleportDistance = 6f;

    public int bounty = 100;
    public float timeBonus = 10f;

    private bool setupComplete = false;

    void Start()
    {
        gameController = GameController.Instance;

        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();

        float height = spriteRenderer.bounds.size.y;
        float width = spriteRenderer.bounds.size.x;

        spriteDimensions = new Vector2(width, height);
        spriteDimensions *= 0.5f;
    }

    void Update()
    {
        bool fleeing = false;

        if (!gameController.gameOver && !gameController.gamePaused && stamina != 0f && gameController.activeTransition == "NONE" && setupComplete)
        {
            if (!teleporting)
            {
                Vector2 directionFromPlayer = (transform.position - player.transform.position);
                float distanceFromPlayer = directionFromPlayer.magnitude;

                if (distanceFromPlayer < fleeDistance)
                {
                    fleeDecay = 1f;
                    fleeing = true;

                    if (Mathf.Sign(directionFromPlayer.x) != Mathf.Sign(transform.localScale.x))
                    {
                        transform.localScale = new Vector3(transform.localScale.x * -1f, 1f, 1f);
                    }

                    direction = Vector2.zero;
                    transform.Translate(directionFromPlayer.normalized * movementSpeed * Time.deltaTime);
                }
                else
                {
                    if (direction == Vector2.zero)
                    {
                        GetNewDirection();
                    }

                    if (fleeDecay > 0f)
                    {
                        fleeDecay = Mathf.Max(0f, fleeDecay - Time.deltaTime);
                    }

                    directionTimer += Time.deltaTime;

                    if (directionTimer >= directionDuration)
                    {
                        direction = Vector2.zero;
                    }

                    if (Mathf.Sign(direction.x) != Mathf.Sign(transform.localScale.x))
                    {
                        transform.localScale = new Vector3(transform.localScale.x * -1f, 1f, 1f);
                    }

                    transform.Translate(direction * movementSpeed * Time.deltaTime);
                }
            }
            else
            {

                teleportDelay = Mathf.Max(0f, teleportDelay - Time.deltaTime);

                if (teleportDelay == 0f)
                {
                    Teleport();                    
                }
          
            }
            
            float x = transform.position.x;
            float y = transform.position.y;

            Vector2 clampedPosition = GetClampedPosition(transform.position);

            if (x != clampedPosition.x || y != clampedPosition.y)
            {
                if (fleeDecay > 0f)
                {
                    teleporting = true;
                    GetTeleportDestination();
                }
                else if (!fleeing)
                {
                    Vector2 previousDirection = direction;

                    GetNewDirection();

                    if (Mathf.Sign(direction.x) == Mathf.Sign(previousDirection.x) && Mathf.Sign(direction.y) == Mathf.Sign(previousDirection.y))
                    {
                        direction *= -1f;

                        if (Mathf.Sign(direction.x) != Mathf.Sign(transform.localScale.x))
                        {
                            transform.localScale = new Vector3(transform.localScale.x * -1f, 1f, 1f);
                        }
                    }
                }
            }

            transform.position = clampedPosition;
        }

        Vector2 screenPosition = Camera.main.WorldToScreenPoint(transform.position);
        Vector2 UIPosition = screenPosition + GetScaledUISpacing(new Vector2(0f, 115f));

        if (UIPosition.y > 1080f)
        {
            UIPosition = screenPosition - GetScaledUISpacing(new Vector2(0f, 110f));
        }

        staminaText.rectTransform.position = UIPosition;
    }

    private void GetNewDirection()
    {
        directionDuration = Random.Range(2f, 5f);
        directionTimer = 0f;

        direction = Random.insideUnitCircle;
        direction.Normalize();
    }

    public void UpdateStaminaText()
    {
        staminaText.text = Mathf.CeilToInt(stamina).ToString();
    }
  
    public bool TakeDamage(float damage)
    {
        if (canTakeDamage)
        {
            stamina = Mathf.Max(0f, stamina - damage);
            UpdateStaminaText();

            StartCoroutine(DamageCooldown());

            if (stamina == 0f)
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 GetScaledUISpacing(Vector2 originalSpacing)
    {
        Vector2 spacingScale = new Vector2(originalSpacing.x / 1920f, originalSpacing.y / 1080f);
        Vector2 scaledSpacing = new Vector2(Screen.width, Screen.height) * spacingScale;

        return scaledSpacing;
    }

    private Vector2 GetClampedPosition(Vector2 position)
    {
        Vector2 halfWorldSize = gameController.GetHalfWorldSize();

        float clampedX = Mathf.Clamp(position.x, -halfWorldSize.x + spriteDimensions.x, halfWorldSize.x - spriteDimensions.x);
        float clampedY = Mathf.Clamp(position.y, -halfWorldSize.y + spriteDimensions.y, halfWorldSize.y - spriteDimensions.y);

        return new Vector2(clampedX, clampedY);
    }

    private void GetTeleportDestination()
    {
        Vector2 newDestination = Random.insideUnitCircle.normalized;

        newDestination.x = Mathf.Lerp(minTeleportDistance, maxTeleportDistance, newDestination.x);
        newDestination.y = Mathf.Lerp(minTeleportDistance, maxTeleportDistance, newDestination.y);

        newDestination += (Vector2)transform.position;

        Vector2 halfWorldSize = gameController.GetHalfWorldSize();

        if (newDestination.x > halfWorldSize.x || newDestination.x < -halfWorldSize.x)
        {
            newDestination.x -= transform.position.x;
            newDestination.x *= -1f;
            newDestination.x += transform.position.x;
        }

        if (newDestination.y > halfWorldSize.y || newDestination.y < -halfWorldSize.y)
        {
            newDestination.y -= transform.position.y;
            newDestination.y *= -1f;
            newDestination.y += transform.position.y;
        }

        newDestination = GetClampedPosition(newDestination);

        teleportDestination = newDestination;
    }

    private void Teleport()
    {
        Color oldColour, newColour;
        float alphaValue;

        if ((Vector2)transform.position != teleportDestination)
        {
            teleportTransitionTimer = Mathf.Min(teleportTransitionLength, teleportTransitionTimer + Time.deltaTime);

            oldColour = spriteRenderer.color;

            alphaValue = Mathf.Lerp(1f, 0f, teleportTransitionTimer / teleportTransitionLength);

            newColour = new Color(oldColour.r, oldColour.g, oldColour.b, alphaValue);
            spriteRenderer.color = newColour;

            oldColour = staminaText.color;
            newColour = new Color(oldColour.r, oldColour.g, oldColour.b, alphaValue);
            staminaText.color = newColour;

            if (teleportTransitionTimer == teleportTransitionLength)
            {
                transform.position = teleportDestination;
                teleportTransitionTimer = 0f;
            }
        }
        else
        {
            teleportTransitionTimer = Mathf.Min(teleportTransitionLength, teleportTransitionTimer + Time.deltaTime);

            oldColour = spriteRenderer.color;

            alphaValue = Mathf.Lerp(0f, 1f, teleportTransitionTimer / teleportTransitionLength);

            newColour = new Color(oldColour.r, oldColour.g, oldColour.b, alphaValue);
            spriteRenderer.color = newColour;

            oldColour = staminaText.color;
            newColour = new Color(oldColour.r, oldColour.g, oldColour.b, alphaValue);
            staminaText.color = newColour;

            if (teleportTransitionTimer == teleportTransitionLength)
            {
                teleportDelay = 2f;
                fleeDecay = 0f;
                teleporting = false;
                teleportTransitionTimer = 0f;
            }
        }
    }

    public bool CheckTeleporting()
    {
        return teleportDelay == 0f;
    }

    public void SetSetupComplete()
    {
        setupComplete = true;
    }

    public void ScaleHealth(float multiplier)
    {
        stamina *= multiplier;
    }

    #region Coroutines
    private IEnumerator DamageCooldown()
    {
        canTakeDamage = false;
        yield return new WaitForSeconds(0.1f);
        canTakeDamage = true;
    }
    #endregion
}
