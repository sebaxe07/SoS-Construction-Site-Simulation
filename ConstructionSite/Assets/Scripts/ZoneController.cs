using UnityEngine;
using System.Collections.Generic;

public class ZoneController : MonoBehaviour
{
    private HashSet<GameObject> objectsInZone = new HashSet<GameObject>();

    public void ObjectEntered(GameObject obj)
    {
        if (!objectsInZone.Contains(obj))
        {
            objectsInZone.Add(obj);
            Debug.Log(obj.name + " entered the zone.");
            MachineBehavior machineBehavior = obj.GetComponent<MachineBehavior>();
            // Check if the machine was moving
            if (machineBehavior != null && machineBehavior.Moving)
            {
                Debug.Log(obj.name + " is moving.");
                machineBehavior.OnEnterZone();
            }
        }
        else
        {
            objectsInZone.Remove(obj);
            Debug.Log(obj.name + " exited the zone.");

            // Get the Behavior component of the object and call the OnExitZone method
            MachineBehavior machineBehavior = obj.GetComponent<MachineBehavior>();
            if (machineBehavior != null)
            {
                // Get the name of the zone 
                string zoneName = gameObject.name;
                machineBehavior.OnExitZone(zoneName);
            }
        }
    }
    public HashSet<GameObject> GetObjectsInZone()
    {
        return objectsInZone;
    }

    public void ClearZone()
    {
        objectsInZone.Clear();
    }

    // Add object to the zone
    public void AddObjectToZone(GameObject obj)
    {
        Debug.Log("Adding " + obj.name + " to the zone.");
        if (!objectsInZone.Contains(obj))
        {
            objectsInZone.Add(obj);
        }
    }
}