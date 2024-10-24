using UnityEngine;

[DefaultExecutionOrder(150)]
public class Checkpoint : MonoBehaviour
{
    public Collider checkpointTrigger;

    private void OnTriggerEnter(Collider other)
    {
        Transform player = other.transform.parent;
        Controller controller = player.GetComponent<Controller>();

        if (controller != null)
        {
            controller.ActivateCheckpoint(this, checkpointTrigger);
        }
    }
}