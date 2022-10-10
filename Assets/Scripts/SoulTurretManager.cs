using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoulTurretManager : MonoBehaviour
{
    [System.Serializable]
    public class Turret
    {
        public List<SoulTurret.Attack> attacks = new List<SoulTurret.Attack>();
    }

    private GameController gameController = null;

    private bool turretsCreated = false;

    [SerializeField]
    private GameObject soulTurret = null;

    public List<Turret> turretsToCreate = new List<Turret>();

    private List<GameObject> turrets = new List<GameObject>();

    [SerializeField]
    private bool makeAreaSquare = false;

    private float areaWidth = 4f;
    private float areaHeight = 4f;

    [SerializeField]
    private int attacksBeforeTeleport = 6;

    void Start()
    {
        gameController = GameController.Instance;
    }

    void Update()
    {
        if (!turretsCreated && gameController.activeTransition == "NONE")
        {
            for (int i = 0; i < turretsToCreate.Count; i++)
            {
                SoulTurret turret = Instantiate(soulTurret).GetComponent<SoulTurret>();
                turret.attacks = turretsToCreate[i].attacks;
                turret.SetAttacksBeforeTeleport(attacksBeforeTeleport);

                turrets.Add(turret.gameObject);
            }

            turretsCreated = true;

            areaHeight = Random.Range(3, 8);

            if (makeAreaSquare)
            {
                areaWidth = areaHeight;
            }
            else
            {
                areaWidth = Random.Range(3, 14);
            }

            SetTeleportDestinations();
        }
        else if (turretsCreated)
        {
            bool moveTurrets = true;

            for (int i = 0; i < turrets.Count; i++)
            {
                SoulTurret currentTurret = turrets[i].GetComponent<SoulTurret>();

                if (!currentTurret.CanTeleport())
                {
                    moveTurrets = false;
                    break;
                }
            }

            if (moveTurrets)
            {
                areaHeight = Random.Range(3, 8);

                if (makeAreaSquare)
                {
                    areaWidth = areaHeight;
                }
                else
                {
                    areaWidth = Random.Range(3, 14);
                }

                SetTeleportDestinations();
            }
        }
    }

    private void SetTeleportDestinations()
    {
        Vector2 currentPosition = new Vector2(0f, areaHeight * 0.5f);

        float spacing = (2f * (areaWidth + areaHeight)) / turrets.Count;

        SoulTurret currentTurret = turrets[0].GetComponent<SoulTurret>();
        currentTurret.StartTeleport(currentPosition);

        for (int i = 1; i < turrets.Count; i++)
        {
            float tempSpacing = spacing;
            float newX;
            float newY;

            if (currentPosition.y == areaHeight * 0.5f)
            {
                newX = Mathf.Min(currentPosition.x + tempSpacing, areaWidth * 0.5f);

                tempSpacing -= Mathf.Abs(newX - currentPosition.x);

                currentPosition.x = newX;
            }
            
            if (currentPosition.x == areaWidth * 0.5f)
            {
                newY = Mathf.Max(currentPosition.y - tempSpacing, areaHeight * -0.5f);

                tempSpacing -= Mathf.Abs(newY - currentPosition.y);

                currentPosition.y = newY;
            }
            
            if (currentPosition.y == areaHeight * -0.5f)
            {
                newX = Mathf.Max(currentPosition.x - tempSpacing, areaWidth * -0.5f);

                tempSpacing -= Mathf.Abs(newX - currentPosition.x);

                currentPosition.x = newX;
            }
            
            if (currentPosition.x == areaWidth * -0.5f)
            {
                newY = Mathf.Max(currentPosition.y + tempSpacing, areaHeight * -0.5f);

                tempSpacing -= Mathf.Abs(newY - currentPosition.y);

                currentPosition.y = newY;
            }

            currentTurret = turrets[i].GetComponent<SoulTurret>();
            currentTurret.StartTeleport(currentPosition);
        }
    }

    public void DeactivateTurrets()
    {
        for (int i = 0; i < turrets.Count; i++)
        {
            turrets[i].GetComponent<SoulTurret>().SetDeactivateTurret();
        }

        this.enabled = false;
    }

    public void IncrementDamage(float damageToAdd)
    {
        for (int i = 0; i < turretsToCreate.Count; i++)
        {
            for (int j = 0; j < turretsToCreate[i].attacks.Count; j++)
            {
                turretsToCreate[i].attacks[j].damage += damageToAdd;
            }
        }
    }
}
