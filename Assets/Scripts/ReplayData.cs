using System.Collections.Generic;
using System;
using UnityEngine;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace SpeedGame
{
    public class ReplayData
    {
        public Queue<CharacterInputSet> inputQueue;
    }

    public struct CharacterInputSet
    {
        public CharacterInputSet(sbyte moveValueX, sbyte moveValueY, sbyte forwardAxisX, sbyte forwardAxisZ, byte buttonMask, sbyte steerValue, byte accelerateValue, byte brakeValue, uint tick) =>
            (MoveValueX, MoveValueY, ForwardAxisX, ForwardAxisZ, ButtonMask, SteerValue, AccelerateValue, BrakeValue, Tick) =
            (moveValueX, moveValueY, forwardAxisX, forwardAxisZ, buttonMask, steerValue, accelerateValue, brakeValue, tick);

        public uint Tick { get; }

        public sbyte MoveValueX { get; }
        public sbyte MoveValueY { get; }

        public sbyte ForwardAxisX { get; }
        public sbyte ForwardAxisZ { get; }

        public byte ButtonMask { get; }
        public sbyte SteerValue { get; }
        public byte AccelerateValue { get; }
        public byte BrakeValue { get; }
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
