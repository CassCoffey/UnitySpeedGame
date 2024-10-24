using Newtonsoft.Json.Converters;
using SpeedGame;
using System;
using UnityEngine;

public class StageInfo : MonoBehaviour
{
    string stageName = null;
    uint stageID = 0;
    bool published = false;
    ReplayData authorGhost = null;

    TimeSpan authorTime = TimeSpan.MinValue;
    TimeSpan goldTime = TimeSpan.MinValue;
    TimeSpan silverTime = TimeSpan.MinValue;
    TimeSpan bronzeTime = TimeSpan.MinValue;

    void Awake()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("StageManager");

        if (objs.Length > 1)
        {
            Destroy(this.gameObject);
        }

        DontDestroyOnLoad(this.gameObject);
    }
}
