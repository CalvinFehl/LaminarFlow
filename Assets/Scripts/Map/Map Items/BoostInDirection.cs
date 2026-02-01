using System.Collections;
using UnityEngine;

public class BoostInDirection : MonoBehaviour
{
    [SerializeField] private float boost = 1000f;
    [SerializeField] private bool oneDirectional = false, cancelsMomentum = false;
    [SerializeField] private float cooldown = 0.5f;
    private bool canBoost = true;

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();

        if (rb != null && canBoost)
        {
            Vector3 boostDirection = transform.up;

            if (Vector3.Dot(rb.linearVelocity, boostDirection) > 0f)
            {
                ApplyBoost(rb, boostDirection);
            }
            else if (!oneDirectional)
            {
                ApplyBoost(rb, -boostDirection);
            }
        }
    }

    private void ApplyBoost(Rigidbody rb, Vector3 direction)
    {
        if (cancelsMomentum)
        {
            rb.linearVelocity = Vector3.zero;
        }

        rb.AddForce(direction * boost);
        StartCoroutine(Cooldown());
    }

    IEnumerator Cooldown()
    {
        canBoost = false;
        yield return new WaitForSeconds(cooldown);
        canBoost = true;
    }
}