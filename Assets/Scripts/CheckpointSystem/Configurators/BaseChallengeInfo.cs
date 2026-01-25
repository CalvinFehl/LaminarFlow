using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseChallengeInfo
{
    public GameObject Player;
    public CheckPoint CurrentCheckPoint;
    public CheckPoint NextTargetCheckpoint;
    public float ChallengeTime;
    public bool ChallengeCompleted;
    public bool ChallengeStarted;
    public Dictionary<CheckPoint, List<DateTime>> CheckPointTimes;

    public BaseChallengeInfo(GameObject player, CheckPoint startCheckPoint, float challengeTime, bool challengeCompleted)
    {
        this.Player = player;
        this.CurrentCheckPoint = startCheckPoint;
        this.NextTargetCheckpoint = CurrentCheckPoint;
        this.ChallengeTime = challengeTime;
        this.ChallengeCompleted = challengeCompleted;
        this.CheckPointTimes = new Dictionary<CheckPoint, List<DateTime>>();
    }
}
