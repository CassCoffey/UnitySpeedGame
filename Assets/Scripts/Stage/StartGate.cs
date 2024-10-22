using UnityEngine;

public class StartGate : MonoBehaviour
{
    public GameObject Character;
    public GameObject Camera;

    void Awake()
    {
        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        GameObject camera = Instantiate(Camera, new Vector3(0, 0, 0), Quaternion.identity);
        GameObject player = Instantiate(Character, new Vector3(0, 0, 0), Quaternion.identity);
        PlayerController controller = player.AddComponent<PlayerController>();
        controller.platformInputSpace = camera.transform;
        camera.GetComponent<CameraManager>().focus = player.GetComponent<Character>();
    }
}
