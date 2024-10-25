using SpeedGame;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class StartGate : MonoBehaviour
{
    public GameObject Character;
    public GameObject Camera;

    public void BeginStage()
    {
        SpawnPlayer();
        SpawnGhost(StageManager.CurrentAuthorReplay());
        SpawnGhost(StageManager.CurrentPersonalReplay());
        //StartCoroutine(SpawnGhostDelay(3));
    }

    void SpawnPlayer()
    {
        GameObject camera = Instantiate(Camera, new Vector3(0, 0, 0), Quaternion.identity);
        GameObject player = Instantiate(Character, new Vector3(0, 0, 0), Quaternion.identity);
        PlayerController controller = player.AddComponent<PlayerController>();
        controller.platformInputSpace = camera.transform;
        camera.GetComponent<CameraManager>().focus = player.GetComponent<Character>();
    }

    void SpawnGhost(ReplayData replay)
    {
        if (replay != null)
        {
            Debug.Log("Reading replay with length - " + replay.inputQueue.Count);
            GameObject ghost = Instantiate(Character, new Vector3(0, 0, 0), Quaternion.identity);
            GhostController controller = ghost.AddComponent<GhostController>();
            controller.SetReplay(replay);
        }
    }

    void SpawnGhost(string fileName)
    {
        SpawnGhost(ReplayFunctions.ReadReplay(fileName));
    }

    IEnumerator SpawnGhostDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

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
