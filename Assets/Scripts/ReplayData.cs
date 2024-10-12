using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using Unity.Burst.Intrinsics;
using UnityEngine.Rendering;

namespace SpeedGame
{
    public class ReplayData
    {
        public Queue<CharacterInputSet> inputQueue;
    }

    public struct CharacterInputSet
    {
        public CharacterInputSet(Vector2 moveValue, Vector3 rightAxis, Vector3 forwardAxis, bool jumpPressed, bool jumpReleased, float steerValue, float accelerateValue, float brakeValue, long tick) =>
            (MoveValueX, MoveValueY, RightAxisX, RightAxisY, RightAxisZ, ForwardAxisX, ForwardAxisY, ForwardAxisZ, JumpPressed, JumpReleased, SteerValue, AccelerateValue, BrakeValue, Tick) =
            (moveValue.x, moveValue.y, rightAxis.x, rightAxis.y, rightAxis.z, forwardAxis.x, forwardAxis.y, forwardAxis.z, jumpPressed, jumpReleased, steerValue, accelerateValue, brakeValue, tick);

        public long Tick { get; }

        public float MoveValueX { get; }
        public float MoveValueY { get; }

        public float RightAxisX { get; }
        public float RightAxisY { get; }
        public float RightAxisZ { get; }
        public float ForwardAxisX { get; }
        public float ForwardAxisY { get; }
        public float ForwardAxisZ { get; }

        public bool JumpPressed { get; }
        public bool JumpReleased { get; }
        public float SteerValue { get; }
        public float AccelerateValue { get; }
        public float BrakeValue { get; }
    }

    public static class ReplayFunctions
    {
        unsafe public static byte[] ToBinary(CharacterInputSet inputs)
        {
            int size = Marshal.SizeOf(inputs);
            byte[] result = new byte[size];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(inputs, ptr, true);
                Marshal.Copy(ptr, result, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return result;
        }

        public static void WriteReplay(ReplayData replay)
        {
            string fileName = "CurrentReplay.replay";

            using (var stream = File.Open(fileName, FileMode.Create))
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                {
                    while (replay.inputQueue.Count > 0)
                    {
                        writer.Write(ToBinary(replay.inputQueue.Dequeue()));
                    }
                    
                }
            }
        }

        unsafe public static ReplayData ReadReplay(string fileName)
        {
            ReplayData result = new ReplayData();
            result.inputQueue = new Queue<CharacterInputSet>();

            if (File.Exists(fileName))
            {
                using (var stream = File.Open(fileName, FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                    {
                        while (reader.BaseStream.Position != reader.BaseStream.Length)
                        {
                            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(CharacterInputSet)));

                            // Pin the managed memory while, copy it out the data, then unpin it
                            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                            CharacterInputSet inputs = (CharacterInputSet)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(CharacterInputSet));
                            handle.Free();

                            result.inputQueue.Enqueue(inputs);
                        }
                    }
                }
            }

            return result;
        }
    }
}
