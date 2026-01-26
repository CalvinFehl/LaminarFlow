public interface IReconsileFloat
{
    public float GetValue();
    public void Reconcile(float value, uint tick = 0);
}
