public interface IReconsileBool
{
    public bool GetValue();
    public void Reconcile(bool value, uint tick = 0);
}
