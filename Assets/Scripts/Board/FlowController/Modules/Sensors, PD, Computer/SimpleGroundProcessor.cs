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
