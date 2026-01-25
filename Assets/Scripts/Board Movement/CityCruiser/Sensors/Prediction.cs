using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prediction : MonoBehaviour
{
    public float duration = 0.3f;


    void Update()
    {
        if(duration <= 0f)
        {
            Destroy(gameObject);
        }

        duration -= Time.deltaTime;
    }
}
