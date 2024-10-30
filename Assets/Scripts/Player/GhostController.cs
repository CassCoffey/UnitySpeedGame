using SpeedGame;
using UnityEngine;

[DefaultExecutionOrder(10)]
public class GhostController : Controller
{
    private ReplayData replay;
    private bool loadedData = false;

    private CharacterInputSet previousInputs;

    public void SetReplay(ReplayData replay)
    {
        this.replay = replay;
        loadedData = true;
    }

    public override void Reset()
    {
        replay.index = 0;
        previousInputs = default;

        loadedData = true;

        base.Reset();
    }

    protected override void FixedUpdate()
    {
        if (!loadedData)
        {
            return;
        }

        if (replay.index < replay.inputList.Count)
        {
            CharacterInputSet inputs = replay.inputList[replay.index];

            if (inputs.Tick == tick) 
            {
                character.UpdateInputs(inputs);
                previousInputs = inputs;

                replay.index++;
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

        base.FixedUpdate();
    }

    protected override void Finish(EndGate gate, Collider gateTrigger)
    {
        // Done
    }
}