using UnityEngine;

public class Rotatebyforce : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float force = 10f;
    private bool isAKeyPressed = false;
    private bool isDKeyPressed = false;
    private bool isWKeyPressed = false;
    private bool isSKeyPressed = false;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            isAKeyPressed = true;
        }
        else if (Input.GetKeyUp(KeyCode.Q))
        {
            isAKeyPressed = false;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            isDKeyPressed = true;
        }
        else if (Input.GetKeyUp(KeyCode.E))
        {
            isDKeyPressed = false;
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            isAKeyPressed = true;
        }
        else if (Input.GetKeyUp(KeyCode.W))
        {
            isAKeyPressed = false;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            isDKeyPressed = true;
        }
        else if (Input.GetKeyUp(KeyCode.S))
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
                rb.AddTorque(Vector3.left * force);
            }
            if (isDKeyPressed)
            {
                rb.AddTorque(Vector3.right * force);
            }
            if (isWKeyPressed)
            {
                rb.AddTorque(Vector3.forward * force);
            }
            if (isSKeyPressed)
            {
                rb.AddTorque(Vector3.back * force);
            }
        }
        else
        {
            Debug.LogError("Rigidbody not found");
        }
    }
}
