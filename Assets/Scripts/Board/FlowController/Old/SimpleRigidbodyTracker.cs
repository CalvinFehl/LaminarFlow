using System;
using UnityEngine;

public class SimpleRigidbodyTracker : BaseTracker
{
    public override void Track(RigidbodyData rigidbodyData, float deltaTime)
    {
        OnPositionUpdated?.Invoke(rigidbodyData.Position, deltaTime);
        OnRotationUpdated?.Invoke(rigidbodyData.Rotation, deltaTime);
        OnVelocityUpdated?.Invoke(rigidbodyData.Velocity, deltaTime);

        OnAngularVelocityUpdated?.Invoke(rigidbodyData.AngularVelocity, deltaTime);
    }
}

public class BaseTracker : MonoBehaviour
{
    public Action<Vector3, float> OnPositionUpdated;
    public Action<Quaternion, float> OnRotationUpdated;
    public Action<Vector3, float> OnVelocityUpdated;
    public Action<Vector3, float> OnAngularVelocityUpdated;
    public virtual void Track(RigidbodyData rigidbodyData, float deltaTime) { }
}

public class SimpleGroundProcessor : BaseGroundProcessor
{
    public override void ProcessGroundData(GroundData groundData, float deltaTime)
    {
        OnHitUpdated?.Invoke(groundData.Hit, deltaTime);
        OnHitPointUpdated?.Invoke(groundData.HitPoint, deltaTime);
        OnHitNormalUpdated?.Invoke(groundData.HitNormal, deltaTime);
        OnHitDistanceUpdated?.Invoke(groundData.HitDistance, deltaTime);
        OnHitTagUpdated?.Invoke(groundData.HitTag, deltaTime);
    }
}

public class BaseGroundProcessor : MonoBehaviour
{
    public Action<bool, float> OnHitUpdated;
    public Action<Vector3, float> OnHitPointUpdated;
    public Action<Vector3, float> OnHitNormalUpdated;
    public Action<float, float> OnHitDistanceUpdated;
    public Action<string, float> OnHitTagUpdated;
    public virtual void ProcessGroundData(GroundData groundData, float deltaTime) { }
}
