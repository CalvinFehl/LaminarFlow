using UnityEngine;

public interface IReconsileVector3
{
    public Vector3 GetValue();
    public void Reconcile(Vector3 value, uint tick = 0);
}
