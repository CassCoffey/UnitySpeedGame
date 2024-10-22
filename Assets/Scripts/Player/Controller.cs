using UnityEngine;

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

    protected virtual void Awake()
    {
        character = GetComponent<Character>();
    }

    protected virtual void FixedUpdate()
    {
        tick++;
    }

    public void ActivateCheckpoint(Checkpoint checkpoint)
    {
        Debug.Log("Checkpoint Wahoo!");
    }
}