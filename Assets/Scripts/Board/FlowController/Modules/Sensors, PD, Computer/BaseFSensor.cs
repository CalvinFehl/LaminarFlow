using UnityEngine;

public class BaseFSensor : MonoBehaviour
{
    public virtual GroundData ScanCurrentGround(params object[] args)
    {
        return new GroundData
        {
            Hit = false,
            HitPoint = Vector3.zero,
            HitNormal = Vector3.up,
            HitDistance = 10000f,
            HitTag = "Air"
        };
    }
}
