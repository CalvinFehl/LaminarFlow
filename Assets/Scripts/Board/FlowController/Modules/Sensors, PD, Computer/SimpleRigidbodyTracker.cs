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
