using Assets.Scripts.CheckpointSystem.Configurators;
using UnityEngine;
using UnityEngine.UIElements;

public class SprintTimeViewModel : MonoBehaviour
{
    public UIDocument RaceTimeUIDoc;
    public PlayerSprintInfo PlayerSprintInfo;
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

    private Label TotalTimeLabel;   
    private Label CheckpointTimeLabel;

    public void Awake()
    {
        TotalTimeLabel = RaceTimeUIDoc.rootVisualElement.Q<Label>("TotalTimeValueLabel");
        CheckpointTimeLabel = RaceTimeUIDoc.rootVisualElement.Q<Label>("CheckpointTimeValueLabel");
        TotalTime = 0f;
        CheckpointTime = 0f;
    }

    public void Update()
    {
        if(PlayerSprintInfo.ChallengeStarted)
        {
            TotalTime += Time.deltaTime;
        }
    }

}
