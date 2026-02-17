using System;
using UnityEngine;

public class BaseGroundProcessor : MonoBehaviour
{
    public Action<bool, float> OnHitUpdated;
    public Action<Vector3, float> OnHitPointUpdated;
    public Action<Vector3, float> OnHitNormalUpdated;
    public Action<float, float> OnHitDistanceUpdated;
    public Action<string, float> OnHitTagUpdated;
    public virtual void ProcessGroundData(GroundData groundData, float deltaTime) { }
}
