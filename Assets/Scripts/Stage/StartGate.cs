using SpeedGame;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class StartGate : MonoBehaviour
{
    public GameObject Character;
    public GameObject Camera;

    private PlayerController activeCharacter;

    private List<GhostController> activeGhosts;

    public void BeginStage()
    {
        activeGhosts = new List<GhostController>();

        SpawnPlayer();
        SpawnGhost(StageManager.CurrentAuthorReplay());
        SpawnGhost(StageManager.CurrentPersonalReplay());
        //StartCoroutine(SpawnGhostDelay(3));
    }

    public void ResetStage()
    {
        activeCharacter.Reset();
        activeCharacter.transform.position = Vector3.zero;
        activeCharacter.transform.rotation = Quaternion.identity;

        foreach (GhostController ghost in activeGhosts)
        {
            ghost.Reset();
            ghost.transform.position = Vector3.zero;
            ghost.transform.rotation = Quaternion.identity;
        }
    }

    void SpawnPlayer()
    {
        GameObject camera = Instantiate(Camera, Vector3.zero, Quaternion.identity);
        GameObject player = Instantiate(Character, Vector3.zero, Quaternion.identity);
        PlayerController controller = player.AddComponent<PlayerController>();
        controller.platformInputSpace = camera.transform;
        camera.GetComponent<CameraManager>().focus = player.GetComponent<Character>();

        activeCharacter = controller;
    }

    void SpawnGhost(ReplayData replay)
    {
        if (replay != null)
        {
            Debug.Log("Reading replay with length - " + replay.inputList.Count);
            GameObject ghost = Instantiate(Character, Vector3.zero, Quaternion.identity);
            GhostController controller = ghost.AddComponent<GhostController>();
            controller.SetReplay(replay);

            activeGhosts.Add(controller);
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
            Debug.Log("Reading replay with length - " + replay.inputList.Count);
            GameObject ghost = Instantiate(Character, new Vector3(0, 0, 0), Quaternion.identity);
            GhostController controller = ghost.AddComponent<GhostController>();
            controller.SetReplay(replay);
        }
    }
}
