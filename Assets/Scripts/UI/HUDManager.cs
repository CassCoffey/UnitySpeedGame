using SpeedGame;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public Transform MedalsPanel;
    public Transform SpeedometerPanel;
    public Transform TimerPanel;
    public Transform CheckpointPanel;

    [HideInInspector]
    public static HUDManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public static void Init()
    {
        Instance.CheckpointPanel.gameObject.SetActive(false);
        Instance.UpdateMedalTimes();
    }

    private void UpdateMedalTimes()
    {
        Transform Author = MedalsPanel.Find("AuthorCard");
        Transform Gold = MedalsPanel.Find("GoldCard");
        Transform Silver = MedalsPanel.Find("SilverCard");
        Transform Bronze = MedalsPanel.Find("BronzeCard");
        Transform Personal = MedalsPanel.Find("PersonalCard");

        TimeSpan AuthorTime = StageManager.CurrentStageData().authorTime;
        TimeSpan GoldTime = StageManager.CurrentStageData().goldTime;
        TimeSpan SilverTime = StageManager.CurrentStageData().silverTime;
        TimeSpan BronzeTime = StageManager.CurrentStageData().bronzeTime;

        if (AuthorTime == TimeSpan.Zero)
        {
            // No author time, no medals, map in progress
            Author.gameObject.SetActive(false);
            Gold.gameObject.SetActive(false);
            Silver.gameObject.SetActive(false);
            Bronze.gameObject.SetActive(false);
        } 
        else
        {
            Author.Find("Time").GetComponent<TextMeshProUGUI>().text = AuthorTime.ToString(@"mm\:ss\.fff");
            Gold.Find("Time").GetComponent<TextMeshProUGUI>().text = GoldTime.ToString(@"mm\:ss\.fff");
            Silver.Find("Time").GetComponent<TextMeshProUGUI>().text = SilverTime.ToString(@"mm\:ss\.fff");
            Bronze.Find("Time").GetComponent<TextMeshProUGUI>().text = BronzeTime.ToString(@"mm\:ss\.fff");
        }

        if (StageManager.CurrentPersonalReplay() == null)
        {
            // No personal time yet
            Personal.gameObject.SetActive(false);
        } 
        else
        {
            TimeSpan PersonalTime = StageManager.CurrentPersonalReplay().finishTime;

            Personal.Find("Time").GetComponent<TextMeshProUGUI>().text = PersonalTime.ToString(@"mm\:ss\.fff");

            if (StageManager.CurrentAuthorReplay() != null)
            {
                // Find personal rank compared to medals
                if (PersonalTime < AuthorTime)
                {
                    Personal.SetSiblingIndex(0);
                }
                else if (PersonalTime < GoldTime)
                {
                    Personal.SetSiblingIndex(1);
                }
                else if (PersonalTime < SilverTime)
                {
                    Personal.SetSiblingIndex(2);
                }
                else if (PersonalTime < BronzeTime)
                {
                    Personal.SetSiblingIndex(3);
                }
            }
        }
    }

    public static void UpdateSpeed(float velocity)
    {
        int speed = (int)Mathf.Round(velocity);

        Transform SpeedometerNumber = Instance.SpeedometerPanel.Find("Number");
        SpeedometerNumber.GetComponent<TextMeshProUGUI>().text = speed.ToString();
    }

    public static void UpdateTime(uint tick)
    {
        string time = UtilFunctions.TickToTimespan(tick).ToString(@"mm\:ss\.ff");

        Transform TimeText = Instance.TimerPanel.Find("Text");
        TimeText.GetComponent<TextMeshProUGUI>().text = time;
    }

    public static void UpdateCheckpoint(CheckpointData data)
    {
        Instance.UpdateAndDisplayCheckpoint(data);
    }

    public void UpdateAndDisplayCheckpoint(CheckpointData data)
    {
        Transform TimeText = Instance.CheckpointPanel.Find("TimePanel").Find("Text");
        TimeText.GetComponent<TextMeshProUGUI>().text = data.time.ToString(@"mm\:ss\.fff");

        Transform SpeedText = Instance.CheckpointPanel.Find("SpeedPanel").Find("Text");
        SpeedText.GetComponent<TextMeshProUGUI>().text = data.speed.ToString("n2") + " mph";

        StartCoroutine(DisplayCheckpointPanel(1));
    }

    IEnumerator DisplayCheckpointPanel(float seconds)
    {
        CheckpointPanel.gameObject.SetActive(true);

        yield return new WaitForSeconds(seconds);

        CheckpointPanel.gameObject.SetActive(false);
    }
}
