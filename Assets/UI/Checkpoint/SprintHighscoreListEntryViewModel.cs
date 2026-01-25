using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SprintHighscoreListEntryViewModel 
{
    private Label playerNameLabel;
    private Label rankLabel;
    private Label timeLabel;

    private string playerName;
    private int rank;
    private float time;

    public string PlayerName
    {
        get
        {
            return playerName;
        }
        set
        {
            playerName = value;
            if (playerNameLabel != null)
            {
                playerNameLabel.text = playerName;
            }
        }
    }
    public int Rank
    {
        get
        {
            return rank;
        }
        set
        {
            rank = value;
            if (rankLabel != null)
            {
                rankLabel.text = rank.ToString();
            }
        }
    }
    public float Time
    {
        get
        {
            return time;
        }
        set
        {
            time = value;
            if (timeLabel != null)
            {
                timeLabel.text = time.ToString();
            }
        }
    }

    public SprintHighscoreListEntryViewModel()
    {
       
    }
    // Start is called before the first frame update
    public void Bind(VisualElement rootElement)
    {
        playerNameLabel = rootElement.Q<Label>("PlayerNameLabel");
        rankLabel = rootElement.Q<Label>("RankLabel");
        timeLabel = rootElement.Q<Label>("TimeLabel");
        
        playerNameLabel.text = playerName; 
        rankLabel.text = rank.ToString();
        timeLabel.text = time.ToString();
    }
}
