using UnityEngine;

public class GateTrigger : MonoBehaviour
{
    public ZoneController zoneController;

    private void OnTriggerEnter(Collider other)
    {
        if (zoneController != null)
        {
            Debug.Log(other.gameObject.name + " entered the gate trigger.");
            zoneController.ObjectEntered(other.gameObject);
        }
    }

}