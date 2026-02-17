using System;
using UnityEngine;

public class BaseTracker : MonoBehaviour
{
    public Action<Vector3, float> OnPositionUpdated;
    public Action<Quaternion, float> OnRotationUpdated;
    public Action<Vector3, float> OnVelocityUpdated;
    public Action<Vector3, float> OnAngularVelocityUpdated;
    public virtual void Track(RigidbodyData rigidbodyData, float deltaTime) { }
}
