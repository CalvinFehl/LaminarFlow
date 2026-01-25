using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.CheckpointSystem.Configurators
{
    public abstract class BaseConfigurator : MonoBehaviour
    {
        public GameObject PlayerTrackUI;
        public GameObject HighscoreUI;
        protected List<CheckPoint> checkPoints;
        public Action<GameObject> OnPlayerFinishedConfiguration;
        public bool SpawnsSpline = false, SplineIsClosed = false;

        public virtual void PlayerParticipateOnTrack(GameObject player)
        {
            foreach (var checkPoint in checkPoints)
            {
                if (!checkPoint.gameObject.activeSelf)
                {
                    checkPoint.gameObject.SetActive(true);
                }
            }
        }

        public virtual void PlayerLeaveTrack(GameObject player)
        {
            foreach (var checkPoint in checkPoints)
            {
                if (checkPoint.gameObject != gameObject)
                {
                    checkPoint.gameObject.SetActive(false);
                }
            }
        }

        public abstract void PlayerEnteredCheckpoint(CheckPointCollision checkPointCollision);

        public virtual void SetCheckPoints(List<CheckPoint> checkPointList)
        {
            checkPoints = checkPointList;
        }

        public virtual void ResetTimer(CheckPoint checkPoint, GameObject player = null)
        {

        }

        public virtual void RestartChallenge(GameObject player = null)
        {

        }
    }
}
