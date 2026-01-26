using UnityEngine;

public class SingleRayCastSensor : BaseFSensor
{
    [Tooltip("The raycast shoots in the Transform Y Direction")]
    [SerializeField] private Transform raycastOrigin;
    [SerializeField] private float maxDistance = 10f;

    /// <summary>
    /// args[0] = maxDistance
    public override GroundData ScanCurrentGround(params object[] args)
    {
        float _maxDistance = maxDistance;

        if (args.Length > 0 && args[0] is float tempMaxDistance)
        {
            _maxDistance = tempMaxDistance;
        }

        if (Physics.Raycast(raycastOrigin.position, -raycastOrigin.up, out RaycastHit rayhit, _maxDistance))
        {
            return new GroundData
            {
                Hit = true,
                HitPoint = rayhit.point,
                HitNormal = rayhit.normal,
                HitDistance = rayhit.distance,
                HitTag = rayhit.collider.tag
            };
        }
        else
        {            
            return base.ScanCurrentGround(args);
        }
    }
}