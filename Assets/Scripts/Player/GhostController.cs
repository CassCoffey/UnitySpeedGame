using SpeedGame;
using UnityEngine;

[DefaultExecutionOrder(10)]
public class GhostController : Controller
{
    private ReplayData replay;
    private ReplayData originalReplay;
    private bool loadedData = false;

    private CharacterInputSet previousInputs;
    private CharacterInputSet nextInputs;
    private bool repeatingInput = false;

    public void SetReplay(ReplayData replay)
    {
        this.replay = replay;
        originalReplay = replay;
        loadedData = true;
    }

    public override void Reset()
    {
        replay = originalReplay;
        previousInputs = nextInputs = default;
        repeatingInput = false;

        loadedData = true;

        base.Reset();
    }

    protected override void FixedUpdate()
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

        base.FixedUpdate();
    }

    protected override void Finish(EndGate gate, Collider gateTrigger)
    {
        // Done
    }
}