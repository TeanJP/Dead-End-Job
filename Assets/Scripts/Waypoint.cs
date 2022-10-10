using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    //A list of all the waypoints that can be reached from this one.
    [SerializeField]
    private List<Waypoint> connectedWaypoints;

    public List<Vector2> GetAccessibleWaypoints()
    {
        //Create an empty list for the waypoints.
        List<Vector2> accessibleWaypoints = new List<Vector2>();

        //Loop through the connected waypoints.
        for (int i = 0; i < connectedWaypoints.Count; i++)
        {
            //Add the position of the waypoints to the list.
            accessibleWaypoints.Add(connectedWaypoints[i].transform.position);
        }

        //Return the list.
        return accessibleWaypoints;
    }
}
