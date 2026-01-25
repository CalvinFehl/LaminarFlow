using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Scripts.CheckpointSystem.Configurators
{
    [Serializable]
    public class PlayerLapRaceInfo : BaseChallengeInfo
    {
        public bool lapAlmostCompleted;
        public int lapCount, lapsCompleted;

        public PlayerLapRaceInfo(GameObject player, CheckPoint startCheckPoint, float challengeTime, bool sprintCompleted, int lapCount = 1) 
            : base(player, startCheckPoint, challengeTime, sprintCompleted)
        {
            this.lapCount = lapCount;
            lapsCompleted = 0;
        }
    }

    public class LapRaceConfigurator : BaseConfigurator
    {
        // Setting Up Track
        public CheckPoint startCheckPoint;
        public CheckPoint endCheckPoint;
        public int lapCount;

        // Tracking Players
        private Dictionary<GameObject, PlayerLapRaceInfo> activePlayersLapRaceInfo;
        private Dictionary<GameObject, List<PlayerLapRaceInfo>> allPlayersLapRaceInfo;

        // UI
        private LapRaceTimeViewModel raceTimeViewModel;
        private HighscoreViewModel highscoreViewModel;
        private List<PlayerLapRaceInfo> highscores;
        private GameObject highscoreUI;

        // Translocator
        private Translocator playerTranslocator;
        private bool reachedCheckpointByTeleporting = false;

        public void Start()
        {
            activePlayersLapRaceInfo = new Dictionary<GameObject, PlayerLapRaceInfo>();
            allPlayersLapRaceInfo = new Dictionary<GameObject, List<PlayerLapRaceInfo>>();
            highscores = new List<PlayerLapRaceInfo>();

            if (endCheckPoint == null) 
            { 
                if (checkPoints.Count > 1)
                {
                    endCheckPoint = checkPoints[^1];
                }
                else
                {
                    Debug.LogWarning("Race has not enough Checkpoints");
                }
            }
        }

        public void Update()
        {
            foreach (var player in activePlayersLapRaceInfo)
            {
                if (player.Value.ChallengeStarted == true)
                {
                    player.Value.ChallengeTime += Time.deltaTime;
                }
            }
        }

        public override void SetCheckPoints(List<CheckPoint> checkPointList)
        {
            base.SetCheckPoints(checkPointList);
            if (checkPointList.Count == 0) { return; }
            startCheckPoint = checkPointList[0];
            endCheckPoint = checkPointList[^1];
        }

        // Player Joins Challenge
        public override void PlayerParticipateOnTrack(GameObject player)
        {

            if (activePlayersLapRaceInfo.ContainsKey(player)) { return; } // Check if Player is already participating
            if (endCheckPoint == null) { Debug.LogWarning("Missing EndCheckPoint"); return; } // Check if End Checkpoint is set

            base.PlayerParticipateOnTrack(player); // Activate Checkpoints


            //TODO Refactor the player shouldn't care about any checkpoint state
            // Updating Translocator on Player
            if (player.GetComponentInChildren<Translocator>() != null) 
            { 
                playerTranslocator = player.GetComponentInChildren<Translocator>(); 

                if (playerTranslocator != null)
                {
                    playerTranslocator.IsOnTrack = true;
                    playerTranslocator.Configurator = this;
                    UpdateTransLocator(playerTranslocator, startCheckPoint);
                    playerTranslocator.ChallengeStartCheckPoint = startCheckPoint;
                    playerTranslocator.ChallengeLastCheckPoint = startCheckPoint;
                }
            }


            // Adding Player to Challenge Dictionary
            var newPlayerLapRaceInfo = new PlayerLapRaceInfo(player, startCheckPoint, 0, false, lapCount); // Adding Player to Info Dictionary

            activePlayersLapRaceInfo.Add(player, newPlayerLapRaceInfo);

            if (!allPlayersLapRaceInfo.ContainsKey(player)) // Adding Player to Highscore List
            { allPlayersLapRaceInfo.Add(player, new List<PlayerLapRaceInfo>()); }

            // Setting up Player Info
            newPlayerLapRaceInfo.NextTargetCheckpoint = startCheckPoint;
            newPlayerLapRaceInfo.ChallengeStarted = true;


            // Attach TrackUI to Player
            // TODO refactor UI handling.
            var uisocket = player.transform.Find("CheckpointUISocket");
            if (PlayerTrackUI != null)
            {
                var playerUI = Instantiate(PlayerTrackUI);

                if (playerUI != null)
                {
                    playerUI.SetActive(true);
                    playerUI.transform.SetParent(uisocket, false);

                    raceTimeViewModel = playerUI.GetComponent<LapRaceTimeViewModel>();
                    if (raceTimeViewModel != null)
                    {
                        raceTimeViewModel.PlayerLapRaceInfo = newPlayerLapRaceInfo;
                        raceTimeViewModel.LapCount = lapCount;
                    }
                    else { Debug.LogWarning("RaceTimeViewModel is not set"); }
                }
            }
            else { Debug.LogWarning("PlayerTrackUI is not set"); }
            if (HighscoreUI != null)
            {
                highscoreUI = Instantiate(HighscoreUI);
                highscoreViewModel = highscoreUI.GetComponent<HighscoreViewModel>();

                highscoreUI.transform.SetParent(uisocket, false);
            }
            else { Debug.LogWarning("HighScoreUI is not set"); }

            // TODO this shouldn't be here
            PlayerEnteredCheckpoint(startCheckPoint.CalculateCheckPointCollision(player, startCheckPoint));
        }


        // Player Leaves Track
        public override void PlayerLeaveTrack(GameObject player)
        {
            // Clear Translocator
            if (playerTranslocator != null)
            {
                playerTranslocator.IsOnTrack = false;
                playerTranslocator.Configurator = null;
                playerTranslocator.ChallengeStartCheckPoint = null;
                playerTranslocator.ChallengeLastCheckPoint = null;
            }

            //TODO refactor UI handling.
            // Remove TrackUI from Player
            if (PlayerTrackUI != null)
            {
                var uisocket = player.transform.Find("CheckpointUISocket");
                foreach (Transform child in uisocket)
                {
                    Destroy(child.gameObject);
                }
            }

            // Remove Player from Challenge Dictionary
            if (activePlayersLapRaceInfo.ContainsKey(player))
            {
                allPlayersLapRaceInfo[player].Add(activePlayersLapRaceInfo[player]);
                activePlayersLapRaceInfo.Remove(player);
            }

            base.PlayerLeaveTrack(player);
            Debug.Log("Player Left Track: " + player);
        }


        // Reaching Checkpoint
        public override void PlayerEnteredCheckpoint(CheckPointCollision checkPointCollision)
        {
            if (!activePlayersLapRaceInfo.ContainsKey(checkPointCollision.Player)) { return; }

            var playerLapInfo = activePlayersLapRaceInfo[checkPointCollision.Player];


            // If collided checkpoint is the next checkpoint
            if (checkPointCollision.CheckPoint == playerLapInfo.NextTargetCheckpoint)
            {
                Debug.Log("Player correctly reached checkpoint: " + checkPointCollision.CheckPoint.name);

                //TODO refactor UI handling.
                // Reset CheckPointTimer in UI
                if (raceTimeViewModel != null) { raceTimeViewModel.CheckpointTime = 0f; }

                //TODO Refactor the player shouldn't care about any checkpoint state
                // Update Translocator
                if (playerTranslocator != null)
                { UpdateTransLocator(playerTranslocator, checkPointCollision.CheckPoint, checkPointCollision.LocalPosition, checkPointCollision.LocalRotation, checkPointCollision.LocalVelocity, null); }


                // Add Checkpoint Time to PlayerInfo
                if (!playerLapInfo.CheckPointTimes.ContainsKey(checkPointCollision.CheckPoint))
                { 
                    playerLapInfo.CheckPointTimes.Add(checkPointCollision.CheckPoint, new List<DateTime>());
                } // Add CheckPoint to Dictionary

                if (playerLapInfo.CheckPointTimes[checkPointCollision.CheckPoint].Count < playerLapInfo.lapsCompleted +1) // Add Times to List of CheckPoint, until List is up to date
                { 
                    for (int i = 0; i < playerLapInfo.lapsCompleted +1 - playerLapInfo.CheckPointTimes[checkPointCollision.CheckPoint].Count; i++)
                    {
                        playerLapInfo.CheckPointTimes[checkPointCollision.CheckPoint].Add(DateTime.Now);
                    }
                }

                if (playerLapInfo.CheckPointTimes[checkPointCollision.CheckPoint].Count >= playerLapInfo.lapsCompleted + 1)
                {
                    playerLapInfo.CheckPointTimes[checkPointCollision.CheckPoint][playerLapInfo.lapsCompleted] = checkPointCollision.Timestemp; // Add Time to List
                }


                // Check if Lap is completed
                CheckPoint prevCheckpoint = playerLapInfo.CurrentCheckPoint; 
                bool isFinishingLap = reachedCheckpointByTeleporting == false && prevCheckpoint == endCheckPoint && checkPointCollision.CheckPoint == startCheckPoint;


                // Update Current and next Target Checkpoint
                int currentCheckpointIndex = checkPoints.IndexOf(playerLapInfo.NextTargetCheckpoint);
                playerLapInfo.CurrentCheckPoint = checkPointCollision.CheckPoint;

                if (checkPointCollision.CheckPoint == endCheckPoint)
                { playerLapInfo.NextTargetCheckpoint = startCheckPoint; }
                else
                { playerLapInfo.NextTargetCheckpoint = checkPoints[currentCheckpointIndex + 1]; }
                Debug.Log("Next Target Checkpoint: " + playerLapInfo.NextTargetCheckpoint.name);

                // Toggle Guide Markers for Current Checkpoint
                playerLapInfo.CurrentCheckPoint.ToggleGuideMarkers(false);

                // If collided checkpoint is the start checkpoint after one lap
                if (isFinishingLap) 
                {
                    playerLapInfo.lapsCompleted++;

                    if (playerLapInfo.lapsCompleted < playerLapInfo.lapCount)
                    {
                        PlayerFinishedLap(playerLapInfo.lapsCompleted);
                    }
                    else // If race is finished
                    {
                        // Calculate Time
                        TimeSpan totalDuration = DateTime.Now - playerLapInfo.CheckPointTimes[startCheckPoint][0];

                        playerLapInfo.ChallengeCompleted = true;
                        playerLapInfo.ChallengeStarted = false;
                        playerLapInfo.NextTargetCheckpoint = null;

                        playerLapInfo.ChallengeTime = (float)totalDuration.TotalSeconds;

                        //TODO Refactor UI handling
                        highscoreUI.SetActive(true);

                        // Translocator
                        if (playerTranslocator != null)
                        {
                            playerTranslocator.IsOnTrack = false;
                            playerTranslocator.Configurator = null;
                            playerTranslocator.ChallengeStartCheckPoint = null;
                            playerTranslocator.ChallengeLastCheckPoint = null;
                        }

                        Debug.Log("Finished Race with a time of: " + playerLapInfo.ChallengeTime);
                        PlayerLeaveTrack(checkPointCollision.Player);
                        //highscoreViewModel.HighscroeList = GetBestLapRacesDescending();

                    }
                }
                // Mark the next CheckPoint
                if (playerLapInfo.NextTargetCheckpoint != null)
                { playerLapInfo.NextTargetCheckpoint.ToggleGuideMarkers(true); }
            }
            else
            {
                // Racer Hit the Wrong Checkpoint
            }
            reachedCheckpointByTeleporting = false;
        }


        // When Player Teleports to Checkpoint
        public override void ResetTimer(CheckPoint checkPoint, GameObject player = null)
        {
            base.ResetTimer(checkPoint);
            reachedCheckpointByTeleporting = true;

            Debug.Log("Resetting Timer for Checkpoint: " + checkPoint.name);

            if (activePlayersLapRaceInfo.ContainsKey(player))
            {
                var playerLapInfo = activePlayersLapRaceInfo[player];
                if (playerLapInfo?.CheckPointTimes.ContainsKey(checkPoint) == false) { return; }

                float timeDifference = (float)(DateTime.Now - playerLapInfo.CheckPointTimes[checkPoint][playerLapInfo.lapsCompleted]).TotalSeconds;
                foreach (var kvp in playerLapInfo.CheckPointTimes)
                {
                    if (kvp.Key == checkPoint)
                    { kvp.Value[playerLapInfo.lapsCompleted] = DateTime.Now; }
                    else 
                    {
                        for (int i = 0; i < kvp.Value.Count; i++)
                        {
                            if (kvp.Value[i] == default) { continue; }
                            kvp.Value[i] = kvp.Value[i].AddSeconds(timeDifference);
                        }
                    }
                }

                raceTimeViewModel.CheckpointTime = 0f;
                raceTimeViewModel.TotalTime -= timeDifference;

                playerLapInfo.ChallengeTime -= timeDifference;
                Debug.Log("Time Difference: " + timeDifference);
            }
        }

        public override void RestartChallenge(GameObject player = null)
        {
            base.RestartChallenge();
            Debug.Log("Restarting Challenge");

            // Reset UI Times
            raceTimeViewModel.CheckpointTime = 0f;
            raceTimeViewModel.TotalTime = 0f;

            if (activePlayersLapRaceInfo.ContainsKey(player))
            {
                var playerLapInfo = activePlayersLapRaceInfo[player];
                playerLapInfo.ChallengeTime = 0;
                playerLapInfo.ChallengeCompleted = false;
                playerLapInfo.ChallengeStarted = true;
                playerLapInfo.lapsCompleted = 0;
                playerLapInfo.NextTargetCheckpoint = checkPoints[1];
                playerLapInfo.CurrentCheckPoint = startCheckPoint;
                playerLapInfo.CheckPointTimes.Clear();
            }
        }

        private void PlayerFinishedLap(int lapsCompleted)
        {
            raceTimeViewModel.CurrentLap = lapsCompleted + 1;
            Debug.Log("Laps Completed: " + lapsCompleted + "/" + lapCount);
        }

        private void UpdateTransLocator(Translocator playerTranslocator, CheckPoint newCheckPoint, Vector3 localPos = default, Quaternion localRot = default, Vector3 localVel = default, CheckPoint startCheckpoint = null)
        {
            if (playerTranslocator.ChallengeLastCheckPoint != newCheckPoint) { playerTranslocator.ChallengeLastCheckPoint = newCheckPoint; }

            if(startCheckpoint != null) { playerTranslocator.ChallengeStartCheckPoint = startCheckpoint; }
            if(localPos != default) { playerTranslocator.LastCheckPointRelativePosition = localPos; }
            if(localRot != default) { playerTranslocator.LastCheckPointRelativeRotation = localRot; }
            if(localVel != default) { playerTranslocator.LastCheckPointVelocity = localVel; }
        }

        public List<PlayerLapRaceInfo> GetBestLapRacesDescending()
        {
            var flattend = allPlayersLapRaceInfo.Values
         .SelectMany(lapRaces => lapRaces);
            var descinding = flattend.OrderByDescending(lapRaces => lapRaces.ChallengeTime)
         .ToList();
            return descinding;
        }
    }
}
