using UnityEngine;

[DefaultExecutionOrder(150)]
public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Character character = other.GetComponent<Character>();
        character.ActivateCheckpoint();
    }
}