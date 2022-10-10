using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMazeMovement : MazeMovement
{
    //The direction the player is travelling in.
    [SerializeField]
    private Vector2 direction = new Vector2(0f, 0f);
    //The direction the player has input that they want to move in.
    [SerializeField]
    private Vector2 inputDirection = new Vector2(0f, 0f);
    //Which directions the player can access.
    private List<Vector2> accessibleDirections = new List<Vector2>();

    void Start()
    {
        //Get the game controller.
        gameController = GameController.Instance;
        //gameController = FindObjectOfType<GameController>();
        //Set the player's speed.
        speed = 2.5f;
    }

    void Update()
    {
        //If the game is not over.
        if (!gameController.gameOver && !gameController.gamePaused && !gameController.playerStunned &&  gameController.activeTransition == "NONE")
        {
            //Get the player's input.
            Vector2 tempInputDirection = GetInputDirection();

            //If the player did input a direction.
            if (tempInputDirection != new Vector2(0f, 0f))
            {
                //If the player input the direction which is opposite to the one they are currently travelling in.
                if (tempInputDirection == direction * -1f)
                {
                    //Flip direction.
                    direction *= -1f;
                    //Store the player's input.
                    inputDirection = tempInputDirection;
                    //Change the direction that the player is travelling in.
                    ChangeDirection();
                }
                //If the player is at a waypoint.
                else if (currentWaypoint != null)
                {
                    //Store the player's input.
                    inputDirection = tempInputDirection;
                }
            }

            //Move the player to their target waypoint.
            MoveToWaypoint();

            //If the player has arrived at their target.
            if (currentWaypoint != null && transform.position == currentWaypoint.transform.position)
            {
                //Get whether one of the accessible waypoints is in the same direction that the player is travelling in.
                int index = NextWaypoint();
                //Update the start.
                Vector2 newStart = currentWaypoint.transform.position;
                //Store the current target as the new target in case the player doesn't pick a valid direction to move in.
                Vector2 newEnd = end;

                //If the player chsoe a direction to move in.
                if (inputDirection != new Vector2(0f, 0f))
                {
                    //Loop through the accessible directions.
                    for (int i = 0; i < accessibleDirections.Count; i++)
                    {
                        //If the direction is the same as the one the player wants to move in.
                        if (inputDirection == accessibleDirections[i])
                        {
                            //Update the player's target to the waypoint that matches the input direction.
                            newEnd = currentWaypoint.GetAccessibleWaypoints()[i];
                            //Store the new direction that the player is moving in.
                            direction = newEnd - newStart;
                            direction.Normalize();
                            //Stop looping through the directions.
                            break;
                        }
                    }
                }
                //If player didn't input a direction and they can just continue in the direction they are moving in.
                else if (index != -1 && inputDirection == new Vector2(0f, 0f))
                {
                    //Update the player's target.
                    newEnd = currentWaypoint.GetAccessibleWaypoints()[index];
                }

                //If the player's target has changed.
                if (newEnd != end)
                {
                    //Store the new waypoints.
                    UpdateWaypoints(newStart, newEnd);
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //If the player has collided with a waypoint.
        if (other.gameObject.GetComponent<Waypoint>())
        {
            //Update which waypoint the player is at.
            currentWaypoint = other.gameObject.GetComponent<Waypoint>();

            //Reset the player's input direction.
            inputDirection = new Vector2(0f, 0f);

            //Get which direction the player can move in from the waypoint.
            GetAvailableDirections(currentWaypoint);

        }
        //If the player collided with a soul.
        else if (other.gameObject.GetComponent<SoulMazeMovement>() && !gameController.playerStunned)
        {
            //Store the script attached to the soul object.
            SoulMazeMovement soul = other.GetComponent<SoulMazeMovement>();

            //Start combat with the soul.
            gameController.StartCombat(soul);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        //Get the waypoint that the player just left.
        Waypoint waypoint = other.gameObject.GetComponent<Waypoint>();

        //If the player did just leave a waypoint and it is the same as the one they were at.
        if (waypoint != null && waypoint == currentWaypoint)
        {
            //Set the waypoint that the player is at to null.
            currentWaypoint = null;
        }
    }

    private Vector2 GetInputDirection()
    {
        //Set the input direction to default to none.
        Vector2 newInputDirection = new Vector2(0f, 0f);

        //If the player is pressing the "W" key.
        if (Input.GetKey(KeyCode.W))
        {
            //Set the direction to up.
            newInputDirection = new Vector2(0f, 1f);
        }
        //If the player is pressing the "S" key.
        else if (Input.GetKey(KeyCode.S))
        {
            //Set the direction to down.
            newInputDirection = new Vector2(0f, -1f);
        }
        //If the player is pressing the "A" key.
        else if (Input.GetKey(KeyCode.A))
        {
            //Set the direction to left.
            newInputDirection = new Vector2(-1f, 0f);
        }
        //If the player is pressing the "D" key.
        else if (Input.GetKey(KeyCode.D))
        {
            //Set the direction to right.
            newInputDirection = new Vector2(1f, 0f);
        }

        //Return the direction that the player input.
        return newInputDirection;
    }

    private void GetAvailableDirections(Waypoint waypoint)
    {
        //Store the position of the waypoint as a vector 2.
        Vector2 waypointPosition = new Vector2(waypoint.transform.position.x, waypoint.transform.position.y);

        //Get the waypoints that are accessible from the waypoint.
        List<Vector2> accessibleWaypoints = waypoint.GetAccessibleWaypoints();

        //Reset the directions which the player can access.
        accessibleDirections.Clear();

        //Loop through the accessible waypoints.
        for (int i = 0; i < accessibleWaypoints.Count; i++)
        {
            //Work out the direction between the current accessible waypoint and the waypoint.
            Vector2 waypointDirection = accessibleWaypoints[i] - waypointPosition;
            waypointDirection.Normalize();

            //Store the direction.
            accessibleDirections.Add(waypointDirection);
        }
    }

    private int NextWaypoint()
    {
        //Loop through the accessible directions.
        for (int i = 0; i < accessibleDirections.Count; i++)
        {
            //If the current direction is the same as the one the player is travelling in.
            if (direction == accessibleDirections[i])
            {
                //Return the index of the direction.
                return i;
            }
        }

        //Return -1 if there is no waypoint in the direction the player is travelling.
        return -1;
    }

    public void SetDirection(Vector2 direction)
    {
        //Update the direction that the player is travelling in.
        this.direction = direction;
    }
}
