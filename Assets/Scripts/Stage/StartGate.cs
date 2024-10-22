using SpeedGame;
using UnityEngine;
using UnityEngine.UIElements;

public class StartGate : MonoBehaviour
{
    public GameObject Character;
    public GameObject Camera;

    void Awake()
    {
        SpawnPlayer();
        SpawnGhost();
    }

    void SpawnPlayer()
    {
        GameObject camera = Instantiate(Camera, new Vector3(0, 0, 0), Quaternion.identity);
        GameObject player = Instantiate(Character, new Vector3(0, 0, 0), Quaternion.identity);
        PlayerController controller = player.AddComponent<PlayerController>();
        controller.platformInputSpace = camera.transform;
        camera.GetComponent<CameraManager>().focus = player.GetComponent<Character>();
    }

    void SpawnGhost()
    {
        ReplayData replay = ReplayFunctions.ReadReplay("CurrentReplay.replay");
        if (replay != null)
        {
            Debug.Log("Reading replay with length - " + replay.inputQueue.Count);
            GameObject ghost = Instantiate(Character, new Vector3(0, 0, 0), Quaternion.identity);
            GhostController controller = ghost.AddComponent<GhostController>();
            controller.SetReplay(replay);
        }
    }
}
