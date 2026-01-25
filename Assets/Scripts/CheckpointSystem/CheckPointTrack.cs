using Assets.Scripts.CheckpointSystem.Configurators;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;


public class CheckPointTrack : MonoBehaviour
{
    public BaseConfigurator baseConfigurators;

    [SerializeField] private float leaveTrackDistance = 10f, leaveTrackTime = 5f;
    private int secondsToReturnToTrack = 5;

    [SerializeField] private Transform pivot;
    public List<CheckPoint> CheckPoints = new List<CheckPoint>();
    private List<CheckPoint> checkPointsCheck = new List<CheckPoint>();

    [SerializeField] private SplineContainer pathContainer;
    public Spline path => pathContainer[0];

    [SerializeField] private bool IsRefreshed = true;

    [SerializeField] private Dictionary<Transform, float> playerOutOfBoundsTimes = new Dictionary<Transform, float>();

    [SerializeField] private Dictionary<CheckPoint, int> knotIndexByCheckPoint = new Dictionary<CheckPoint, int>();


    private void Awake()
    {
        if (pivot == null)
        {
            pivot = transform;
        }
        SubscribeCheckPoints();
        if (baseConfigurators != null)
        {
            baseConfigurators.SetCheckPoints(CheckPoints);
        }
    }

    // Recurring Methods
    public void RefreshPath()
    {
        // Debug.Log("Refreshing Path");

        path.Clear();

        foreach (var checkPoint in CheckPoints)
        {
            int checkPointIndex = CheckPoints.IndexOf(checkPoint); // Alignt nur nachdem path gecleart wurde!!
            path.Insert(checkPointIndex, createKnotForCheckPoint(checkPoint, pivot));
        }

        if (baseConfigurators != null)
        {
            baseConfigurators.SetCheckPoints(CheckPoints);
        }
        
    }

    public void SubscribeCheckPoints()
    {
        foreach (var checkPoint in CheckPoints)
        {
            checkPoint.OnPlayerEnter += OnPlayerEnterCheckPoint;
        }
    }

    private void Update()
    {
        
        TrackPlayerOutOfBounds();
    }


    public void TrackPlayerOutOfBounds()
    {
        var playersOnTrack = playerOutOfBoundsTimes.Keys.ToList();

        foreach (var playerObjectTransform in playersOnTrack)
        {
            var playerTimeOutOfBounds = playerOutOfBoundsTimes[playerObjectTransform];
            SplineUtility.GetNearestPoint(path, playerObjectTransform.position, out var lastPointOnPath, out var splineT);

            float distanceToSpline = Vector3.Distance(playerObjectTransform.position, lastPointOnPath);
            if (distanceToSpline > leaveTrackDistance)
            {
                int playerSecondsOutOfBounds = Mathf.FloorToInt(playerTimeOutOfBounds);

                if (playerSecondsOutOfBounds != secondsToReturnToTrack)
                {
                    secondsToReturnToTrack = playerSecondsOutOfBounds;
                    Debug.Log("Return to Track!! " + (leaveTrackTime - secondsToReturnToTrack)); // Warnung an Player die Track verlassen
                }

                playerTimeOutOfBounds += Time.deltaTime;
                playerOutOfBoundsTimes[playerObjectTransform] = playerTimeOutOfBounds;

                if (playerTimeOutOfBounds > leaveTrackTime) // Remove Players, die zu lange außerhalb des Tracks sind
                {
                    baseConfigurators.PlayerLeaveTrack(playerObjectTransform.gameObject);
                    playerOutOfBoundsTimes.Remove(playerObjectTransform);
                }
            }
            else if (playerTimeOutOfBounds > 0) // Wenn wieder On Track, Timer zurücksetzen
            {
                playerOutOfBoundsTimes[playerObjectTransform] = 0;
            }
        }
    }


    // Setting Up CheckPoints
    private BezierKnot createKnotForCheckPoint(CheckPoint checkPoint, Transform pivot = null)
    {
        if (pivot != null) return new BezierKnot(pivot.InverseTransformPoint(checkPoint.transform.position), default, default, checkPoint.transform.rotation);
        return new BezierKnot(checkPoint.transform.position, default, default, checkPoint.transform.rotation);
    }

    // Player Interaction
    private void playerfinishedConfiguration(GameObject player)
    {

    }

    protected void OnPlayerEnterCheckPoint(CheckPointCollision collision)
    {
        baseConfigurators?.PlayerEnteredCheckpoint(collision);
    }

    public void PlayerParticipateOnTrack(InteractionEventData playerInteraction)
    {
        if (playerInteraction.Interactor == null) return;
        GameObject player = playerInteraction.Interactor;
        if (player.tag != "Player") return;
        baseConfigurators?.PlayerParticipateOnTrack(player);

        Debug.Log(player + " Participates on Challenge: " + baseConfigurators);

        if (!playerOutOfBoundsTimes.ContainsKey(player.transform)) playerOutOfBoundsTimes.Add(player.transform, 0f);
    }
}
