using UnityEngine;

[DefaultExecutionOrder(150)]
public class EndGate : MonoBehaviour
{
    public int numCheckpoints;

    private void OnTriggerEnter(Collider other)
    {
        Transform player = other.transform.parent;
        Controller controller = player.GetComponent<Controller>();

        if (controller != null)
        {
            controller.ActivateEndGate(this);
        }
    }
}