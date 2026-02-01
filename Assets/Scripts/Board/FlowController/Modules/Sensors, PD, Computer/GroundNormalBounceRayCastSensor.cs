using UnityEngine;

public class GroundNormalBounceRayCastSensor : BaseFSensor
{
    [Tooltip("The raycast shoots in the Transform Y Direction")]
    [SerializeField] private Transform raycastOrigin;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private bool usesSimpleRay = false;

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
            GroundData groundData = new GroundData
            {
                Hit = true,
                HitPoint = rayhit.point,
                HitNormal = rayhit.normal,
                HitDistance = rayhit.distance,
                HitTag = rayhit.collider.tag
            };

            if (usesSimpleRay)
            {
                return groundData;
            }

            if (Physics.Raycast(raycastOrigin.position, -rayhit.normal, out RaycastHit rayhit2, _maxDistance * 2f))
            {
                if (rayhit.distance < rayhit2.distance)
                {
                    groundData.HitDistance = rayhit2.distance;
                    groundData.HitNormal = rayhit2.normal;
                    groundData.HitPoint = rayhit2.point;
                }
            }

            return groundData;
        }
        else
        {
            return base.ScanCurrentGround(args);
        }
    }
}
