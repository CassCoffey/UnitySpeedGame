using SpeedGame;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public Transform MedalsPanel;

    [HideInInspector]
    public static HUDManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public static void Init()
    {
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
                if (PersonalTime > AuthorTime)
                {
                    Personal.SetSiblingIndex(0);
                }
                else if (PersonalTime > GoldTime)
                {
                    Personal.SetSiblingIndex(1);
                }
                else if (PersonalTime > SilverTime)
                {
                    Personal.SetSiblingIndex(2);
                }
                else if (PersonalTime > BronzeTime)
                {
                    Personal.SetSiblingIndex(3);
                }
            }
        }
    }
}
