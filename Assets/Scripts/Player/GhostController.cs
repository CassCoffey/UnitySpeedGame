using SpeedGame;
using UnityEngine;

[DefaultExecutionOrder(10)]
public class GhostController : MonoBehaviour
{
    private Character character = null;

    private ReplayData replay;
    private bool loadedData = false;

    private uint tick = 0;

    private CharacterInputSet previousInputs;
    private CharacterInputSet nextInputs;
    private bool repeatingInput = false;

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
        if (!loadedData)
        {
            return;
        }

        if (!repeatingInput && replay.inputQueue.TryDequeue(out CharacterInputSet inputs))
        {
            if (inputs.Tick == tick)
            {
                character.UpdateInputs(inputs);
                previousInputs = inputs;
            }
            else
            {
                nextInputs = inputs;
                repeatingInput = true;
            }
        } 
        else if (repeatingInput)
        {
            if (nextInputs.Tick == tick)
            {
                character.UpdateInputs(nextInputs);
                previousInputs = nextInputs;
                repeatingInput = false;
            }
            else
            {
                character.UpdateInputs(previousInputs);
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