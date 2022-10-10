using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoulProjectile : MonoBehaviour
{
    protected enum VacuumState
    {
        None,
        ToCentre,
        ToInside
    }

    protected VacuumState vacuumState = VacuumState.None;

    //The speed of the projectile.
    public float movementSpeed = 2f;

    private Vector2 velocity = Vector2.zero;

    //The sprite renderer attached to the projectile.
    protected SpriteRenderer spriteRenderer = null;

    //How much damage the projectile should do to the player.
    public float damage = 1;

    //Whether the projectile can be vacuumed up by the player.
    public bool destructible = false;

    protected GameController gameController = null;
    private PlayerCombatController player = null;

    //private SpriteMask vacuumMask = null;

    private Transform vacuumEnd = null;
    private float insideDuration = 0.25f;
    private float insideTimer = 0f;
    protected Transform vacuumInside;

    protected bool destroyProjectile = false;

    protected virtual void Start()
    {
        gameController = GameController.Instance;
        //gameController = FindObjectOfType<GameController>();
        player = gameController.combatPlayer;

        //Get the projetile's sprite renderer.
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        spriteRenderer.maskInteraction = SpriteMaskInteraction.None;

    }

    protected virtual void Update()
    {
        MoveProjectile();

        if (destroyProjectile)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void MoveProjectile()
    {
        if (!gameController.gameOver && !gameController.gamePaused)
        {
            switch (vacuumState)
            {
                case VacuumState.None:
                    //Move the projectile in the direction it is heading.
                    transform.Translate(velocity * Time.deltaTime);

                    //If the projectile is not in view.
                    if (!spriteRenderer.isVisible)
                    {
                        //Destroy the projectile.
                        destroyProjectile = true;
                    }
                    break;
                case VacuumState.ToCentre:
                    transform.position = Vector2.MoveTowards(transform.position, vacuumEnd.position, movementSpeed * Time.deltaTime);

                    if (transform.position == vacuumEnd.position)
                    {
                        player.PlaySuctionSound();
                        vacuumState = VacuumState.ToInside;
                    }
                    break;
                case VacuumState.ToInside:
                    insideTimer = Mathf.Min(insideDuration, insideTimer + Time.deltaTime);
                    transform.position = Vector2.Lerp(vacuumEnd.position, vacuumInside.position, insideTimer / insideDuration);
                    transform.localScale = Vector2.Lerp(Vector2.one, new Vector2(0.5f, 0.5f), insideTimer / insideDuration);

                    if (transform.position == vacuumInside.position)
                    {
                        gameController.combatPlayer.AddVacuumCharge(1f);
                        destroyProjectile = true;
                    }
                    break;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //If the projetile is destructible and has collided with the vacuum cleaner.        
        if (other.gameObject.GetComponentInChildren<SpriteMask>() != null && destructible && player.vacuumActive)
        {
            vacuumState = VacuumState.ToCentre;
            vacuumEnd = other.gameObject.transform;
            vacuumInside = vacuumEnd.GetChild(0);
            //vacuumMask = other.gameObject.GetComponent<SpriteMask>();
            spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        }
    }


    public void InitializeVelocity(Vector2 direction)
    {
        velocity = direction * movementSpeed;
    }

    public void ChangeVelocityDirection(Vector2 newDirection, float pullForce)
    {
        Vector2 difference = (newDirection * movementSpeed) - velocity;
        difference.Normalize();

        velocity += (difference * pullForce * Time.deltaTime);
        Vector2.ClampMagnitude(velocity, movementSpeed);
    }
}
