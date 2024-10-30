using SpeedGame;
using System;
using System.IO;
using UnityEngine;

public struct StageData
{
    public string stageName;
    public uint stageID;
    public bool published;

    public ReplayData authorGhost;

    public TimeSpan authorTime;
    public TimeSpan goldTime;
    public TimeSpan silverTime;
    public TimeSpan bronzeTime;

    public StageData(string StageName, uint StageID, bool Published, ReplayData AuthorGhost)
    {
        stageName = StageName;
        stageID = StageID;
        published = Published;
        authorGhost = AuthorGhost;

        authorTime = authorGhost != null ? authorGhost.finishTime : TimeSpan.Zero;
        goldTime = TimeSpan.FromSeconds(Math.Ceiling((authorTime * 1.06f).TotalSeconds));
        silverTime = TimeSpan.FromSeconds(Math.Ceiling((authorTime * 1.2f).TotalSeconds));
        bronzeTime = TimeSpan.FromSeconds(Math.Ceiling((authorTime * 1.5f).TotalSeconds));
    }
}

public class StageManager : MonoBehaviour
{
    [HideInInspector]
    public static StageManager Instance;

    [HideInInspector]
    public static string UserName = "TestUser";

    private ReplayData personalBest;

    private StageData currentStageData;

    //This will be included in stagedata later, when maps are fully stored in files
    public StartGate gate;

    void Awake()
    {
        Instance = this;

        GameObject[] objs = GameObject.FindGameObjectsWithTag("StageManager");

        if (objs.Length > 1)
        {
            Destroy(this.gameObject);
        }

        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        LoadStageData();

        gate.BeginStage();

        HUDManager.Init();
    }

    public static void FullReset()
    {
        Instance.gate.ResetStage();
    }

    private void LoadStageData()
    {
        // just make this up for now
        ReplayData replay = ReplayFunctions.ReadReplay(ReplayFunctions.GetReplayFilename(UserName, "Stage01", 0, ReplayTypes.AuthorTime));
        currentStageData = new StageData("Stage01", 0, false, replay);

        personalBest = ReplayFunctions.ReadReplay(ReplayFunctions.GetReplayFilename(UserName, currentStageData.stageName, currentStageData.stageID, ReplayTypes.PersonalBest));
    }

    public static ReplayData CurrentAuthorReplay()
    {
        return Instance.currentStageData.authorGhost;
    }

    public static ReplayData CurrentPersonalReplay()
    {
        return Instance.personalBest;
    }

    public static StageData CurrentStageData()
    {
        return Instance.currentStageData;
    }

    public static void SaveNewPersonalBest(ReplayData replay)
    {
        if (Instance.personalBest != null && replay.finishTime >= Instance.personalBest.finishTime)
        {
            // Not a personal best
            return;
        }

        ReplayFunctions.WriteReplay(ReplayFunctions.GetReplayFilename(UserName, Instance.currentStageData.stageName, Instance.currentStageData.stageID, ReplayTypes.PersonalBest), replay);
    }

    public static string GetStageFilePath(string name, uint id)
    {
        string path = Path.Combine(Application.persistentDataPath, @"data\stages");

        //Create Directory if it does not exist
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        return path;
    }
}
