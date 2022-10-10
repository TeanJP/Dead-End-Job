using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoulTurret : MonoBehaviour
{
    [System.Serializable]
    public class Attack
    {
        public float timeBetweenAttacks = 0.05f;

        public float timeBetweenBursts = 0.5f;
        public int attacksPerBurst = 6;

        public bool trackPlayer = true;

        public Vector3 attackRotation = Vector3.zero;

        public int attackGroups = 1;
        public float groupInterval = 0f;
        public int projectilesPerGroup = 1;
        public float groupRange = 0f;

        public int destructibleSpacing = 0;
        public int destructibleGroupSize = 0;
        public bool resetBetweenGroups = true;
        public bool startWithDestructible = false;

        public float projectileSpeed = 2f;

        public float damage = 1;

        public int attacksBeforeChange = 1;
    }

    #region Projectile Prefabs
    [SerializeField]
    private GameObject projectilePrefab = null;
    [SerializeField]
    private GameObject destructibleProjectilePrefab = null;
    #endregion

    #region Attack Controls
    private bool canAttack = false;

    private int attacksLaunched = 0;
    private int burstsLaunched = 0;

    public List<Attack> attacks = new List<Attack>();
    private int currentAttack = 0;
    private int attacksBeforeChange = 1;

    //The coroutine for the burst cooldown.
    private Coroutine burstCooldownCoroutine = null;
    #endregion

    #region Projectile Spawn Points
    private List<Vector3[]> originalProjectileSpawnPoints = new List<Vector3[]>();
    private List<Vector3[]> currentProjectileSpawnPoints = new List<Vector3[]>();
    #endregion

    private PlayerCombatController player = null;
    private GameController gameController = null;

    private SpriteRenderer spriteRenderer = null;

    #region Teleportation Variables
    [SerializeField]
    private int attacksBeforeTeleport = 6;
    private int teleportCountdown;
    private float teleportTransitionLength = 2f;
    private float teleportTransitionTimer = 0f;
    [HideInInspector]
    public bool teleporting = false;
    private Vector2 teleportDestination = Vector2.zero;
    #endregion

    private bool deactivateTurret = false;
    private float fadeOutDuration = 0.5f;
    private float fadeOutTimer = 0f;

    void Start()
    {
        gameController = GameController.Instance;
        //gameController = FindObjectOfType<GameController>();
        player = gameController.combatPlayer.GetComponent<PlayerCombatController>();


        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        
        teleportCountdown = attacksBeforeTeleport;

        for (int i = 0; i < attacks.Count; i++)
        {
            originalProjectileSpawnPoints.Add(GetProjectileSpawnPoints(Vector3.left * 0.25f, attacks[i]));
        }

        currentProjectileSpawnPoints = originalProjectileSpawnPoints;

        attacksBeforeChange = attacks[0].attacksBeforeChange;

        StartCoroutine(AttackCooldown(attacks[0].timeBetweenAttacks));
    }

    void Update()
    {
        if (!gameController.gameOver && !gameController.gamePaused && gameController.activeTransition == "NONE")
        {
            if (deactivateTurret)
            {
                DeactivateTurret();
            }
            else if (teleporting)
            {
                Teleport();
            }
            else if (canAttack)
            {
                PerformAttack(attacks[currentAttack]);
            }
        }
    }

    public bool CanTeleport()
    {
        return teleportCountdown == 0 && !teleporting;
    }

    public void StartTeleport(Vector2 destination)
    {
        teleportDestination = destination;
        teleportCountdown = attacksBeforeTeleport;
        teleporting = true;
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

            if (teleportTransitionTimer == teleportTransitionLength)
            {
                teleporting = false;
                teleportTransitionTimer = 0f;
            }
        }
    }

    private void PerformAttack(Attack attack)
    {
        if (attacksLaunched < attack.attacksPerBurst)
        {
            int normalAttacks = 0;

            if (attack.startWithDestructible)
            {
                normalAttacks = attack.destructibleSpacing;
            }

            int destructibleAttacks = 0;

            if (attack.trackPlayer && attacksLaunched == 0)
            {
                Vector3 updatedGroupMidpoint = RotateAroundPoint(Vector3.zero, Vector3.left * 0.25f, GetAngleToPlayer(Vector3.left * 0.25f));
                currentProjectileSpawnPoints[currentAttack] = GetProjectileSpawnPoints(updatedGroupMidpoint, attack);
            }

            for (int i = 0; i < currentProjectileSpawnPoints[currentAttack].Length; i++)
            {
                GameObject projectileToInstantiate = projectilePrefab;

                if (attack.destructibleGroupSize != 0)
                {
                    if (normalAttacks < attack.destructibleSpacing)
                    {
                        normalAttacks++;
                    }
                    else
                    {
                        projectileToInstantiate = destructibleProjectilePrefab;
                        destructibleAttacks++;

                        if (destructibleAttacks == attack.destructibleGroupSize)
                        {
                            destructibleAttacks = 0;
                            normalAttacks = 0;
                        }
                    }

                    if ((i + 1) % attack.projectilesPerGroup == 0 && attack.resetBetweenGroups)
                    {
                        destructibleAttacks = 0;
                        normalAttacks = 0;
                    }
                }

                SoulProjectile spawnedProjectile = Instantiate(projectileToInstantiate, transform.position + currentProjectileSpawnPoints[currentAttack][i], Quaternion.identity).GetComponent<SoulProjectile>();

                spawnedProjectile.movementSpeed = attack.projectileSpeed;
                spawnedProjectile.damage = attack.damage;
                spawnedProjectile.InitializeVelocity(currentProjectileSpawnPoints[currentAttack][i].normalized);

                if (!attack.trackPlayer && attacksLaunched == 0)
                {
                    currentProjectileSpawnPoints[currentAttack][i] = RotateAroundPoint(Vector3.zero, originalProjectileSpawnPoints[currentAttack][i], attack.attackRotation);
                }
            }

            attacksLaunched++;
        }
        else
        {
            if (burstCooldownCoroutine == null)
            {
                burstCooldownCoroutine = StartCoroutine(BurstCooldown(attack.timeBetweenBursts));
            }
        }

        canAttack = false;
        StartCoroutine(AttackCooldown(attack.timeBetweenAttacks));
    }

    private Vector3[] GetProjectileSpawnPoints(Vector3 initialGroupMidpoint, SoulTurret.Attack attack)
    {
        Vector3[] projectileSpawnPoints = new Vector3[attack.attackGroups * attack.projectilesPerGroup];

        int currentPoint = 0;
        float projectileSpacing = 0;

        if (attack.projectilesPerGroup != 1)
        {
            projectileSpacing = (2 * attack.groupRange) / (attack.projectilesPerGroup - 1);
        }

        for (int i = 0; i < attack.attackGroups; i++)
        {
            Vector3 groupMidpoint = RotateAroundPoint(Vector3.zero, initialGroupMidpoint, new Vector3(0f, 0f, attack.groupInterval * i));

            if (attack.projectilesPerGroup == 1)
            {
                projectileSpawnPoints[i] = groupMidpoint;
            }
            else
            {
                for (int j = 0; j < attack.projectilesPerGroup; j++)
                {
                    projectileSpawnPoints[currentPoint] = RotateAroundPoint(Vector3.zero, groupMidpoint, new Vector3(0f, 0f, attack.groupRange - (projectileSpacing * j)));
                    currentPoint++;
                }
            }
        }

        return projectileSpawnPoints;
    }

    public void SetAttacksBeforeTeleport(int attacksBeforeTeleport)
    {
        this.attacksBeforeTeleport = attacksBeforeTeleport;
        teleportCountdown = this.attacksBeforeTeleport;
    }

    public void SetDeactivateTurret()
    {
        deactivateTurret = true;
    }

    private void DeactivateTurret()
    {
        fadeOutTimer = Mathf.Min(fadeOutDuration, fadeOutTimer + Time.deltaTime);
        spriteRenderer.color = Color.Lerp(Color.white, Color.clear, fadeOutTimer / fadeOutDuration);

        if (fadeOutTimer == fadeOutDuration)
        {
            Destroy(gameObject);
        }
    }

#region Coroutines
private IEnumerator AttackCooldown(float duration)
    {
        yield return new WaitForSeconds(duration);
        canAttack = true;
    }

    private IEnumerator BurstCooldown(float duration)
    {
        yield return new WaitForSeconds(duration);

        burstsLaunched++;
        attacksLaunched = 0;

        if (burstsLaunched == attacksBeforeChange)
        {
            burstsLaunched = 0;
            currentAttack++;

            if (currentAttack == attacks.Count)
            {
                currentAttack = 0;
            }

            attacksBeforeChange = attacks[currentAttack].attacksBeforeChange;

            teleportCountdown = Mathf.Max(teleportCountdown - 1, 0);
            /*
            if (teleportCountdown == 0)
            {
                teleportCountdown = attacksBeforeTeleport;
                GetTeleportDestination();
                teleporting = true;
            }
            */
        }

        burstCooldownCoroutine = null;
    }
    #endregion

    #region Utility Functions
    private Vector3 RotateAroundPoint(Vector3 pivot, Vector3 point, Vector3 angle)
    {
        return Quaternion.Euler(angle) * (point - pivot) + pivot;
    }

    private Vector3 GetAngleToPlayer(Vector3 point)
    {
        Vector3 direction = (player.transform.position - transform.position);
        direction.Normalize();

        float angle = Vector3.SignedAngle(point, direction, new Vector3(0f, 0f, 1f));
        return new Vector3(0f, 0f, angle);
    }

    private Quaternion GetRotationToPoint(Vector3 point, Vector3 direction)
    {
        point.Normalize();
        direction.Normalize();
        return Quaternion.FromToRotation(direction, point);
    }
    #endregion
}
