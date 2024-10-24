using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    [HideInInspector]
    public static HUDManager Instance;

    public TextMeshProUGUI AuthorTime;

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

    public static void UpdateMedalDisplay()
    {
        Instance.AuthorTime.text = "";
        if (StageManager.CurrentAuthorReplay() != null)
        {
            Instance.AuthorTime.text = "\nAuthor Time - " + StageManager.CurrentAuthorReplay().finishTime.ToString(@"mm\:ss\.fff");
        }
        if (StageManager.CurrentUserBestTime() != null)
        {
            Instance.AuthorTime.text += "\nPersonal Best - " + StageManager.CurrentUserBestTime().finishTime.ToString(@"mm\:ss\.fff");
        }
    }
}
