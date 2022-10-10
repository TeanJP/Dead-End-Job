using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeMovement : MonoBehaviour
{
    //A reference to the game controller.
    public GameController gameController = null;

    public Waypoint currentWaypoint = null;

    //The movement speed of the object.
    public float speed = 1f;

    //The position the object is starting from.
    public Vector2 start;
    //The target position for the object.
    public Vector2 end;

    //The distance between the start and end positions.
    private float pathDistance;

    //A timer for controlling movement between the start and end points.
    private float timer = 0f;

    public void UpdateWaypoints(Vector2 start, Vector2 end)
    {
        //Store the new start point.
        this.start = start;
        //Store the new end point.
        this.end = end;

        //Recalculate the distance between the points.
        pathDistance = Vector2.Distance(this.start, this.end);

        //Reset the timer.
        timer = 0f;
    }

    public void ChangeDirection()
    {
        //Save the previous target temporarily.
        Vector2 tempEnd = end;
        //Set the target to the previous start point.
        end = start;
        //Set the start position ot the previous target.
        start = tempEnd;

        //Reverse how far between the points the object was.
        timer = 1f - timer;
    }

    public void MoveToWaypoint()
    {
        //Increment the timer.
        timer += ((Time.deltaTime / pathDistance ) * speed);
        //Prevent the timer from going above 1.
        timer = Mathf.Min(timer, 1f);

        //Move the object using the timer.
        transform.position = Vector2.Lerp(start, end, timer);
    }
}
