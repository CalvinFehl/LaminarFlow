using Assets.Scripts.CheckpointSystem.Configurators;
using UnityEngine;
using UnityEngine.UIElements;

public class LapRaceTimeViewModel : MonoBehaviour
{
    public UIDocument RaceTimeUIDoc;
    public PlayerLapRaceInfo PlayerLapRaceInfo;

    private Label CurrentLapLabel;
    private Label LapCountLabel;
    private Label TotalTimeLabel;
    private Label CheckpointTimeLabel;
    public float TotalTime
    {
        set
        {
            TotalTimeLabel.text = value.ToString();
        }
        get
        {
            float outVal = 0f;
            var couldParse = float.TryParse(TotalTimeLabel.text, out outVal);


            return outVal;

        }
    }
    public float CheckpointTime
    {
        set
        {
            CheckpointTimeLabel.text = value.ToString();
        }
        get
        {
            float outVal = 0f;
            var couldParse = float.TryParse(CheckpointTimeLabel.text, out outVal);

            return outVal;

        }
    }
    public int CurrentLap
    {
        set
        {
            CurrentLapLabel.text = value.ToString();
        }
        get
        {
            int outVal = 0;
            var couldParse = int.TryParse(CurrentLapLabel.text, out outVal);

            return outVal;
        }
    }
    public int LapCount
    {
        set
        {
            LapCountLabel.text = value.ToString();
        }
        get
        {
            int outVal = 0;
            var couldParse = int.TryParse(LapCountLabel.text, out outVal);

            return outVal;
        }
    }


    void OnEnable()
    {
        CurrentLapLabel = RaceTimeUIDoc.rootVisualElement.Q<Label>("CurrentLapValueLabel");
        LapCountLabel = RaceTimeUIDoc.rootVisualElement.Q<Label>("LapCountValueLabel");
        TotalTimeLabel = RaceTimeUIDoc.rootVisualElement.Q<Label>("TotalTimeValueLabel");
        CheckpointTimeLabel = RaceTimeUIDoc.rootVisualElement.Q<Label>("CheckpointTimeValueLabel");
        TotalTime = 0f;
        CheckpointTime = 0f;
        CurrentLap = 1;
    }

    public void Update()
    {
        if (PlayerLapRaceInfo.ChallengeStarted)
        {
            TotalTime += Time.deltaTime;
            CheckpointTime += Time.deltaTime;
        }
    }

    public void UpdateAllTimes(float addedTime)
    {
        CheckpointTime += addedTime;
        TotalTime += addedTime;
    }
}