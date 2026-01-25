using System;
using System.Collections.Generic;
using UnityEngine;

public class CheckPointCollision
{
    public GameObject Player { get; set; }
    public CheckPoint CheckPoint { get; set; }
    public DateTime Timestemp { get; set; }
    public Vector3 LocalPosition { get; set; }
    public Vector3 LocalVelocity { get; set; }
    public Quaternion LocalRotation { get; set; }
}

public class CheckPoint : MonoBehaviour
{
    public Action<CheckPointCollision> OnPlayerEnter { get; internal set; }
    private Dictionary<GameObject, int> collisionsPerPlayer = new Dictionary<GameObject, int>();

    [SerializeField] private List<GameObject> guideMarkers = new List<GameObject>();

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag != "Player") { return; }

        int simultaneousCollisions = 0;

        if (!collisionsPerPlayer.ContainsKey(other.gameObject))
        {
            collisionsPerPlayer.Add(other.gameObject, 1);
        }
        else
        {
            simultaneousCollisions = collisionsPerPlayer[other.gameObject] + 1;
            collisionsPerPlayer[other.gameObject] = simultaneousCollisions;
        }

        if(simultaneousCollisions > 1) { return; } // if the player is already colliding with the checkpoint, ignore the event

        // create the Checkpoint Collision Event data package
        var checkpointCollision = new CheckPointCollision();

        // check if other is the player
        var boardController = other.GetComponent<CitycruseController>();

        if(boardController == null) { return; }

        checkpointCollision.Timestemp = DateTime.Now;
        checkpointCollision.Player = other.gameObject;
        checkpointCollision.CheckPoint = this;
        checkpointCollision.LocalPosition = this.gameObject.transform.InverseTransformPoint(other.transform.position);
        checkpointCollision.LocalRotation = Quaternion.Inverse(this.gameObject.transform.rotation) * other.gameObject.transform.rotation;

        Rigidbody playerRb = other.GetComponent<Rigidbody>();
        checkpointCollision.LocalVelocity = playerRb == null ? Vector3.zero : this.gameObject.transform.InverseTransformPoint(playerRb.linearVelocity);

        OnPlayerEnter?.Invoke(checkpointCollision);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag != "Player") { return; }

        if (collisionsPerPlayer.ContainsKey(other.gameObject))
        {
            int simultaneousCollisions = collisionsPerPlayer[other.gameObject];
            if (simultaneousCollisions == 1)
            {
                collisionsPerPlayer.Remove(other.gameObject);
            }
            else
            {
                collisionsPerPlayer[other.gameObject] = simultaneousCollisions - 1;
            }
        }
    }

    public CheckPointCollision CalculateCheckPointCollision(GameObject _other, CheckPoint _checkPoint = null, DateTime _timestemp = default)
    {
        var checkpointCollision = new CheckPointCollision();

        checkpointCollision.Timestemp = _timestemp == default ? DateTime.Now : _timestemp;
        checkpointCollision.Player = _other;
        _checkPoint = _checkPoint ?? this;
        checkpointCollision.CheckPoint = _checkPoint;
        checkpointCollision.LocalPosition = _checkPoint.gameObject.transform.InverseTransformPoint(_other.transform.position);
        checkpointCollision.LocalRotation = Quaternion.Inverse(_checkPoint.gameObject.transform.rotation) * _other.gameObject.transform.rotation;

        return checkpointCollision;
    }

    public void ToggleGuideMarkers(bool on = default)
    {
        if (guideMarkers.Count == 0) return;
        if (on) 
        {
            foreach (var marker in guideMarkers)
            {
                if (!marker.gameObject.activeSelf)
                {
                    marker.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            foreach (var marker in guideMarkers)
            {
                if (marker.gameObject.activeSelf)
                {
                    marker.gameObject.SetActive(false);
                }
            }
        }
    }
}
