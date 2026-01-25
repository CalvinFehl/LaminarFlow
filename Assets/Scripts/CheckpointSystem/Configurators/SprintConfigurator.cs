using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Scripts.CheckpointSystem.Configurators
{
    public class PlayerSprintInfo : BaseChallengeInfo
    {
        public PlayerSprintInfo(GameObject player, CheckPoint startCheckPoint, float sprintTime, bool sprintCompleted) 
            : base(player, startCheckPoint, sprintTime, sprintCompleted) {}
    }

    public class SprintConfigurator : BaseConfigurator
    {
        // Setting Up Track
        public CheckPoint startCheckPoint;
        public CheckPoint endCheckPoint;

        // Tracking Players
        private Dictionary<GameObject, PlayerSprintInfo> activePlayersSprintInfo;
        private Dictionary<GameObject, List<PlayerSprintInfo>> allPlayersSprintInfo;

        //TODO refactor UI handling.
        // UI
        private SprintTimeViewModel raceTimeViewModel;
        private HighscoreViewModel highscoreViewModel;
        private List<PlayerSprintInfo> highscores;
        private GameObject highscoreUI;


        // Translocator
        private Translocator playerTranslocator;

        public void Start()
        {
            activePlayersSprintInfo = new Dictionary<GameObject, PlayerSprintInfo>();
            allPlayersSprintInfo = new Dictionary<GameObject, List<PlayerSprintInfo>>();
            highscores = new List<PlayerSprintInfo>();

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
            foreach (var player in activePlayersSprintInfo)
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
            if (activePlayersSprintInfo.ContainsKey(player)) { return; } // Check if Player is already participating
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
            var newPlayerSprintInfo = new PlayerSprintInfo(player, startCheckPoint, 0, false); // Adding Player to Info Dictionary

            activePlayersSprintInfo.Add(player,newPlayerSprintInfo);

            if (!allPlayersSprintInfo.ContainsKey(player)) // Add Player to Highscore List
            { allPlayersSprintInfo.Add(player, new List<PlayerSprintInfo>()); }


            // Setting up Player Info
            newPlayerSprintInfo.NextTargetCheckpoint = startCheckPoint;
            newPlayerSprintInfo.ChallengeStarted = true;

            // Toggle GuideMarkers to Start Checkpoint
            foreach (var checkPoint in checkPoints)
            {
                checkPoint.ToggleGuideMarkers(checkPoint == startCheckPoint);
            }

            // Attach TrackUI to Player
            //TODO refactor UI handling.
            var uisocket = player.transform.Find("CheckpointUISocket");
            if (PlayerTrackUI != null)
            {
                var playerUI = Instantiate(PlayerTrackUI);
                if (PlayerTrackUI != null)
                {
                    raceTimeViewModel = playerUI.GetComponent<SprintTimeViewModel>();
                    if (raceTimeViewModel != null) raceTimeViewModel.PlayerSprintInfo = newPlayerSprintInfo;
                    else { Debug.LogWarning("RaceTimeViewModel is not set"); }

                    playerUI.SetActive(true);
                    playerUI.transform.SetParent(uisocket, false);
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
            if (activePlayersSprintInfo.ContainsKey(player))
            {
                allPlayersSprintInfo[player].Add(activePlayersSprintInfo[player]);
                activePlayersSprintInfo.Remove(player);
            }

            base.PlayerLeaveTrack(player);
            Debug.Log("Player Left Track" + player);
        }


        // Reaching Checkpoint
        public override void PlayerEnteredCheckpoint(CheckPointCollision checkPointCollision)
        {            
            if(!activePlayersSprintInfo.ContainsKey(checkPointCollision.Player) ) { return; }

            var playerSprintInfo = activePlayersSprintInfo[checkPointCollision.Player];
            
            // If collided checkpoint is the next checkpoint
            if (checkPointCollision.CheckPoint == playerSprintInfo.NextTargetCheckpoint)
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
                if (!playerSprintInfo.CheckPointTimes.ContainsKey(checkPointCollision.CheckPoint))
                { playerSprintInfo.CheckPointTimes.Add(checkPointCollision.CheckPoint, new List<DateTime>()); } // Add CheckPoint to Dictionary
                if (playerSprintInfo.CheckPointTimes[checkPointCollision.CheckPoint].Count == 0) { playerSprintInfo.CheckPointTimes[checkPointCollision.CheckPoint].Add(DateTime.Now); } // Add Time to List
                else { playerSprintInfo.CheckPointTimes[checkPointCollision.CheckPoint][0] = checkPointCollision.Timestemp; } // Add Time to List


                // Update last  and current checkpoint
                CheckPoint prevCheckpoint = playerSprintInfo.CurrentCheckPoint;
                int currentCheckpointIndex = checkPoints.IndexOf(playerSprintInfo.NextTargetCheckpoint);
                playerSprintInfo.CurrentCheckPoint = checkPointCollision.CheckPoint;

                // Toggle GuideMarkers for Current and Next Checkpoint
                playerSprintInfo.CurrentCheckPoint.ToggleGuideMarkers(false);

                if (checkPointCollision.CheckPoint != endCheckPoint)
                { 
                    // TODO Make it loop the nextckecpoint index with modul so it overflows and starts with 0 again if the new index will be higher as the possible index
                    playerSprintInfo.NextTargetCheckpoint = checkPoints[currentCheckpointIndex + 1];
                    Debug.Log("Next Target Checkpoint: " + playerSprintInfo.NextTargetCheckpoint.name); 
                    // Mark the next CheckPoint
                    playerSprintInfo.NextTargetCheckpoint.ToggleGuideMarkers(true);                
                }
                else // If collided checkpoint is the last checkpoint --> Finish Race
                {
                    // Calculate Time
                    TimeSpan totalDuration = DateTime.Now - playerSprintInfo.CheckPointTimes[startCheckPoint][0];


                    // Clear Player Info
                    playerSprintInfo.ChallengeCompleted = true;
                    playerSprintInfo.ChallengeStarted = false;
                    playerSprintInfo.NextTargetCheckpoint = null;

                    playerSprintInfo.ChallengeTime = (float)totalDuration.TotalSeconds;

                    highscoreUI.SetActive(true);

                    Debug.Log("Finished Sprint with a time of: " + playerSprintInfo.ChallengeTime);
                    PlayerLeaveTrack(checkPointCollision.Player);
                    //highscoreViewModel.HighscoreList = GetBestSprintsDescending();                    
                }
            }
            else
            {
                // Racer Hit the Wrong Checkpoint
            }
        }


        // When Player Teleports to Checkpoint
        public override void ResetTimer(CheckPoint checkPoint, GameObject player = null)
        {
            base.ResetTimer(checkPoint);

            Debug.Log("Resetting Timer for Checkpoint: " + checkPoint.name);

            if (activePlayersSprintInfo.ContainsKey(player))
            {
                var playerLapInfo = activePlayersSprintInfo[player];
                if (playerLapInfo?.CheckPointTimes.ContainsKey(checkPoint) == false) { return; }

                float timeDifference = (float)(DateTime.Now - playerLapInfo.CheckPointTimes[checkPoint][0]).TotalSeconds;
                foreach (var kvp in playerLapInfo.CheckPointTimes)
                {
                    if (kvp.Key == checkPoint)
                    { kvp.Value[0] = DateTime.Now; }
                    else
                    {
                        if (kvp.Value[0] == default) { continue; }
                        kvp.Value[0] = kvp.Value[0].AddSeconds(timeDifference);
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

            if (activePlayersSprintInfo.ContainsKey(player))
            {
                var playerLapInfo = activePlayersSprintInfo[player];
                playerLapInfo.ChallengeTime = 0;
                playerLapInfo.ChallengeCompleted = false;
                playerLapInfo.ChallengeStarted = true;
                playerLapInfo.NextTargetCheckpoint = checkPoints[1];
                playerLapInfo.CurrentCheckPoint = startCheckPoint;
                playerLapInfo.CheckPointTimes.Clear();
            }
        }

        private void UpdateTransLocator(Translocator playerTranslocator, CheckPoint newCheckPoint, Vector3 localPos = default, Quaternion localRot = default, Vector3 localVel = default, CheckPoint startCheckpoint = null)
        {
            if (playerTranslocator.ChallengeLastCheckPoint != newCheckPoint) { playerTranslocator.ChallengeLastCheckPoint = newCheckPoint; }

            if (startCheckpoint != null) { playerTranslocator.ChallengeStartCheckPoint = startCheckpoint; }
            if (localPos != default) { playerTranslocator.LastCheckPointRelativePosition = localPos; }
            if (localRot != default) { playerTranslocator.LastCheckPointRelativeRotation = localRot; }
            if (localVel != default) { playerTranslocator.LastCheckPointVelocity = localVel; }
        }

        public List<PlayerSprintInfo> GetBestSprintsDescending()
        {
            var flattend = allPlayersSprintInfo.Values
         .SelectMany(sprints => sprints);
            var descinding = flattend.OrderByDescending(sprint => sprint.ChallengeTime)
         .ToList();
            return descinding;
        }
    }
}
