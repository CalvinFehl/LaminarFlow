using UnityEngine;

public class Pushbyforce : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float force = 10f;
    private bool isAKeyPressed = false;
    private bool isDKeyPressed = false;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            isAKeyPressed = true;
        }
        else if (Input.GetKeyUp(KeyCode.A))
        {
            isAKeyPressed = false;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            isDKeyPressed = true;
        }
        else if (Input.GetKeyUp(KeyCode.D))
        {
            isDKeyPressed = false;
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            if (isAKeyPressed)
            {
                rb.AddForce(Vector3.forward * force);
            }
            else if (isDKeyPressed)
            {
                rb.AddForce(Vector3.back * force);
            }
        }
        else
        {
            Debug.LogError("Rigidbody not found");
        }
    }
}
