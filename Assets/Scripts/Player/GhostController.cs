using SpeedGame;
using UnityEngine;

[DefaultExecutionOrder(10)]
public class GhostController : MonoBehaviour
{
    private Character character = null;

    private ReplayData replay;
    private bool loadedData = false;

    private uint tick = 0;

    void Awake()
    {
        replay = ReplayFunctions.ReadReplay("CurrentReplay.replay");
        if (replay != null)
        {
            loadedData = true;
            Debug.Log("Reading replay with length - " + replay.inputQueue.Count);
        }

        character = GetComponent<Character>();
    }

    void FixedUpdate()
    {
        if (loadedData && replay.inputQueue.TryDequeue(out CharacterInputSet inputs))
        {
            if (inputs.Tick == tick)
            {
                character.UpdateInputs(inputs);
            }
        } 
        else
        {
            // Replay is done
            loadedData = false;
        }

        tick++;
    }
}