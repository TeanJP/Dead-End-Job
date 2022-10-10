using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoulMazeMovement : MazeMovement
{
    //The distance from the player at which the soul should turn around.
    private float fleeDistance = 2.5f;
    //The maze version of the player.
    private GameObject player = null;

    //The soul to spawn in the combat section when this soul is caught.
    public GameObject combatPrefab = null;

    void Start()
    {
        //Get the game controller.
        gameController = GameController.Instance;
        //gameController = FindObjectOfType<GameController>();
        //Get the player.
        player = FindObjectOfType<PlayerMazeMovement>().gameObject;
        //Set the movement speed of the soul.
        speed = 2f;
    }

    void Update()
    {
        if (!gameController.gameOver && !gameController.gamePaused && gameController.activeTransition == "NONE")
        {
            //If soul is moving too close to the player.
            if (PlayerInRange())
            {
                //Change direction.
                ChangeDirection();
            }

            //Move the soul towards it's target position.
            MoveToWaypoint();

            //If the soul has arrived at it's target.
            if (currentWaypoint != null && transform.position == currentWaypoint.transform.position)
            {
                //Get a new target.
                SetWaypoints(currentWaypoint);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //If the soul has entered a waypoint.
        if (other.gameObject.GetComponent<Waypoint>())
        {
            //Store the waypoint the soul is at.
            currentWaypoint = other.gameObject.GetComponent<Waypoint>();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        //Get the waypoint which the soul just left.
        Waypoint waypoint = other.gameObject.GetComponent<Waypoint>();

        //If the waypoint the soul is leaving was the one it was at.
        if (waypoint != null && waypoint == currentWaypoint)
        {
            //Set the waypoint the soul is at to null.
            currentWaypoint = null;
        }
    }

    public void SetWaypoints(Waypoint start)
    {
        //Store the current start position.
        Vector2 previousStart = this.start;
        //Store the position of the new start.
        Vector2 newStart = start.transform.position;

        //Get the waypoints that can be accessed from the new start.
        List<Vector2> accessibleWaypoints = start.GetAccessibleWaypoints();
        //Remove the waypoint that is in the direction that the soul just came from.
        accessibleWaypoints.Remove(previousStart);

        //Select a random waypoint for the soul to go to.
        Vector2 newEnd = accessibleWaypoints[Random.Range(0, accessibleWaypoints.Count)];

        //Store the new waypoints for use.
        UpdateWaypoints(newStart, newEnd);
    }

    private bool PlayerInRange()
    {
        //Get the distance between the soul and the player.
        float distanceFromPlayer = Vector2.Distance(transform.position, player.transform.position);

        //A boolean to store whether the soul is on the same row or column as the player.
        bool onSamePath = false;

        //If the x or y position of the soul is equal to that of the player.
        if (transform.position.x == player.transform.position.x || transform.position.y == player.transform.position.y)
        {
            //Set the soul as being in the same row or column as the player.
            onSamePath = true;
        }

        //Work out the direction from the soul to it's target.
        Vector2 directionToEnd = end - (Vector2)transform.position;
        directionToEnd.Normalize();

        //Work out the direction the player is in from the soul.
        Vector2 directionToPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        directionToPlayer.Normalize();

        //If the player is too close and the player is in the same direction as the soul's target.
        if (distanceFromPlayer < fleeDistance && onSamePath && directionToEnd == directionToPlayer)
        {
            //The soul should change direction.
            return true;
        }

        //The soul should not change direction.
        return false;
    }
}
