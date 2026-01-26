using UnityEngine;

public interface IReconsileQuaternion
{
    public Quaternion GetValue();
    public void Reconcile(Quaternion value);
}