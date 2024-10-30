using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SpeedGame
{
    public class ReplayData
    {
        public TimeSpan finishTime;

        public int index = 0;

        public List<CharacterInputSet> inputList;
    }

    public struct CharacterInputSet : IEquatable<CharacterInputSet>
    {
        public CharacterInputSet(sbyte moveValueX, sbyte moveValueY, byte buttonMask, sbyte steerValue, uint tick) =>
            (MoveValueX, MoveValueY, ButtonMask, SteerValue, Tick) =
            (moveValueX, moveValueY, buttonMask, steerValue, tick);

        public uint Tick { get; }

        public sbyte MoveValueX { get; }
        public sbyte MoveValueY { get; }

        public byte ButtonMask { get; }
        public sbyte SteerValue { get; }

        public override bool Equals(object? obj) => obj is CharacterInputSet other && this.Equals(other);

        public bool Equals(CharacterInputSet i) 
        {
            if (MoveValueX == i.MoveValueX && MoveValueY == i.MoveValueY && 
                ButtonMask == i.ButtonMask && SteerValue == i.SteerValue)
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode() => (MoveValueX, MoveValueY, ButtonMask, SteerValue).GetHashCode();

        public static bool operator ==(CharacterInputSet lhs, CharacterInputSet rhs) => lhs.Equals(rhs);

        public static bool operator !=(CharacterInputSet lhs, CharacterInputSet rhs) => !(lhs == rhs);
    }

    public enum ReplayTypes
    {
        PersonalBest,
        AuthorTime,
    }

    public static class ReplayFunctions
    {
        public static byte[] ToBinary(CharacterInputSet inputs)
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

        public static void WriteReplay(string fileName, ReplayData replay)
        {
            string path = Path.Combine(GetReplayFilepath(), fileName);

            using (var stream = File.Open(path, FileMode.Create))
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                {
                    // Write Header
                    writer.Write(replay.finishTime.Ticks);

                    // Write Input Queue
                    for (int i = 0; i < replay.inputList.Count; i++)
                    {
                        writer.Write(ToBinary(replay.inputList[i]));
                    }
                }
            }
        }

        public static ReplayData ReadReplay(string fileName)
        {
            ReplayData result = new ReplayData();
            result.inputList = new List<CharacterInputSet>();

            string path = Path.Combine(GetReplayFilepath(), fileName);

            if (File.Exists(path))
            {
                using (var stream = File.Open(path, FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                    {
                        result.finishTime = new TimeSpan(reader.ReadInt64());

                        while (reader.BaseStream.Position != reader.BaseStream.Length)
                        {
                            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(CharacterInputSet)));

                            // Pin the managed memory while, copy it out the data, then unpin it
                            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                            CharacterInputSet inputs = (CharacterInputSet)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(CharacterInputSet));
                            handle.Free();

                            result.inputList.Add(inputs);
                        }
                    }
                }
            } 
            else
            {
                return null;
            }

            return result;
        }

        public static string GetReplayFilename(string user, string stage, uint id, ReplayTypes type)
        {
            return $"{user}_{stage}{id}_{type}";
        }

        public static string GetReplayFilepath()
        {
            string path = Path.Combine(Application.persistentDataPath, @"data\replays");

            //Create Directory if it does not exist
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }
    }
}
