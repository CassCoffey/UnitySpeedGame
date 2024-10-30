using SpeedGame;
using UnityEngine;

[DefaultExecutionOrder(10)]
public class GhostController : Controller
{
    private ReplayData replay;
    private bool loadedData = false;

    public void SetReplay(ReplayData replay)
    {
        this.replay = replay;
        loadedData = true;
    }

    public override void Reset()
    {
        replay.index = 0;

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

                replay.index++;
            }
            else
            {
                if (replay.index > 0)
                {
                    character.UpdateInputs(replay.inputList[replay.index - 1]);
                }
                else
                {
                    character.UpdateInputs(default);
                }
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