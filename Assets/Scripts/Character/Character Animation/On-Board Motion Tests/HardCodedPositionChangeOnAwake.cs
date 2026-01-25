using UnityEngine;

public class HardCodedPositionChangeOnAwake : MonoBehaviour
{
    [SerializeField] private Transform parent;
    private void Update()
    {
        if (parent != null)
        {
            transform.position = parent.position;
            transform.rotation = parent.rotation;
        }
    }
}
