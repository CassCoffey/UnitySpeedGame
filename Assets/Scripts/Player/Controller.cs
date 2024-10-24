using SpeedGame;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct CheckpointData
{
    public uint tick;
    public TimeSpan time;
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

    protected virtual void Checkpoint(CheckpointData checkpointData)
    {
        // Nothing
    }

    protected virtual void Finish(EndGate gate, Collider gateTrigger)
    {
        // Nothing
    }

    public void ActivateCheckpoint(Checkpoint checkpoint, Collider checkpointTrigger)
    {
        if (!checkpoints.Any(point => point.point == checkpoint))
        {
            CheckpointData data = new CheckpointData();
            data.tick = tick;
            data.time = UtilFunctions.GetTrueTriggerTime(character, checkpointTrigger, tick);
            data.point = checkpoint;
            checkpoints.Add(data);
            Checkpoint(data);
        }
    }

    public void ActivateEndGate(EndGate gate, Collider gateTrigger)
    {
        if (checkpoints.Count >= gate.numCheckpoints)
        {
            Finish(gate, gateTrigger);
        }
    }
}