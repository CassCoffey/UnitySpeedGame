using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct CheckpointData
{
    public uint tick;
    public Checkpoint point;
}

[DefaultExecutionOrder(10)]
public class Controller : MonoBehaviour
{
    public static byte JumpPressedMask = 0b00000001;
    public static byte JumpReleasedMask = 0b00000010;
    public static byte AccelerateMask = 0b00000100;
    public static byte BrakeMask = 0b00001000;

    public static byte CheckpointResetMask = 0b01000000;
    public static byte FullResetMask = 0b10000000;

    protected Character character = null;

    protected uint tick = 0;

    protected List<CheckpointData> checkpoints = new List<CheckpointData>();

    protected virtual void Awake()
    {
        character = GetComponent<Character>();
    }

    protected virtual void FixedUpdate()
    {
        tick++;
    }

    protected virtual void Finish()
    {
        // Nothing
    }

    public void ActivateCheckpoint(Checkpoint checkpoint)
    {
        if (!checkpoints.Any(point => point.point == checkpoint))
        {
            CheckpointData data = new CheckpointData();
            data.tick = tick;
            data.point = checkpoint;
            checkpoints.Add(data);
        }
    }

    public void ActivateEndGate(EndGate gate)
    {
        if (checkpoints.Count >= gate.numCheckpoints)
        {
            Finish();
        }
    }
}