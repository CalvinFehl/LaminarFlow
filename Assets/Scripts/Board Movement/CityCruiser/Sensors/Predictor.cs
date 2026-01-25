using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.scrible.Sensors
{
    public class Predictor : BaseSensor
    {
        private float groundDistance;

        Vector3 currentPosition;
        Vector3 lastPosition;

        public Vector3 currentGravity;
        public Vector3 gravityNormal;
        public float gravityStrength;

        public Vector3 currentImpulse;
        public Vector3 impulseNormal;
        public float currentSpeed;

        public float relativePitch;
        public float sinusDing;
        public Vector3 relXVector;
        public Vector3 predictorRay;
        public Vector3 prediction;

        public GameObject predictedLanding;
        [SerializeField] float cooldown = 0.2f;
        private float timeSinceLastPrediction = 0f;

        GameObject parentObject;
        Rigidbody parentRigidbody;

        public void ChangeGravity(Vector3 gravityNew)
        {
            currentGravity = gravityNew;
            gravityStrength = currentGravity.magnitude;
        }

        public void PredictLanding()
        {
            currentPosition = transform.position;
            if(lastPosition == null)
            {
                lastPosition = currentPosition;
            }

            currentImpulse = parentRigidbody.linearVelocity + currentGravity;
            currentSpeed = parentRigidbody.linearVelocity.magnitude;
            relativePitch = Vector3.Angle(currentGravity, currentImpulse) - 90f;
            float relativePitchRad = relativePitch * Mathf.Deg2Rad;

            float cosAlphaSpeedSquaredThroughG = Mathf.Pow(Mathf.Cos(relativePitchRad) * currentSpeed, 2f) / gravityStrength;

            float minusHalfP = cosAlphaSpeedSquaredThroughG * Mathf.Tan(relativePitchRad);            

            float minusQ = cosAlphaSpeedSquaredThroughG * groundDistance * 2f;
                        
            float relXDistance = minusHalfP + (Mathf.Sqrt(cosAlphaSpeedSquaredThroughG * cosAlphaSpeedSquaredThroughG + minusQ));
            impulseNormal = currentImpulse.normalized;
            gravityNormal = currentGravity.normalized;
            sinusDing = Mathf.Sin(relativePitchRad);

            relXVector = (impulseNormal + gravityNormal * sinusDing).normalized;

            predictorRay = relXVector * relXDistance + gravityNormal * groundDistance;
            
            prediction = currentPosition + predictorRay;

            lastPosition = currentPosition;


        }

        private void Awake()
        {
            hit = false;
        }

        public void Update()
        {
            if (!parentObject)
            {
                parentObject = transform.parent.gameObject;
                
            }

            if (!parentRigidbody)
            {
                parentRigidbody = parentObject.GetComponent<Rigidbody>();
            }

            if (timeSinceLastPrediction < cooldown)
            {
                timeSinceLastPrediction += Time.deltaTime;
            }
        }

        public void FixedUpdate()
        {
            if (Physics.Raycast(transform.position, currentGravity, out RaycastHit rayHit))
            {
                groundDistance = rayHit.distance;
            }


            if(timeSinceLastPrediction >= cooldown)
            {
                PredictLanding();
                if(Physics.Raycast(transform.position, predictorRay, out RaycastHit predHit, predictorRay.magnitude))
                {
                    hit = true;
                    distance = predHit.distance;
                    groundNormal = predHit.normal;
                    hitTag = predHit.collider.tag;


                    Instantiate(predictedLanding, predHit.point, Quaternion.Euler(groundNormal));
                    timeSinceLastPrediction = 0f;
                }
                else
                {
                    hit = false;
                }
            }


        }
    }
}
