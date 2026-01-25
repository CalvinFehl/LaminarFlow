using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.scrible.Feedback_moduls
{

    public class PIDRotationFeedbackmodul : Feedbackmodul
    {
        private PIDController xController;
        private PIDController yController;
        private PIDController zController;

        [SerializeField] float xAngleTreshold = 60f;
        [SerializeField] float yAngleTreshold = 60f;
        [SerializeField] float zAngleTreshold = 60f;

        public Vector3 Target;
        private Vector3 groundNormalLocal;
        private Vector3 transformLocal =new Vector3(0f, 1f, 0f);
        //public Vector3 Current;
        private Vector3 ErrorEuler;
        public Vector3 Correction;

        public Vector3 xPIDFactors = new Vector3(1f, 0f, 0.1f);
        public Vector3 yPIDFactors = new Vector3(0, 0f, 0f);
        public Vector3 zPIDFactors = new Vector3(1f, 0f, 0.001f);

        public float xResponse;
        public float xThrottleRange = 0f;

        public float yResponse;
        public float yThrottleRange = 0f;

        public float zResponse;
        public float zThrottleRange = 0f;

        public void OverwritePIDValues(Vector3 xFactors, Vector3 yFactors, Vector3 zFactors, bool multiply)
        {
            if(multiply == true)
            {
                xPIDFactors = Vector3.Scale(xPIDFactors, xFactors);
                yPIDFactors = Vector3.Scale(yPIDFactors, yFactors);
                zPIDFactors = Vector3.Scale(zPIDFactors, zFactors);
            }
            else if (multiply == false)
            {
                xPIDFactors = Vector3.Scale(xPIDFactors, new Vector3(1 / xFactors.x, 1 / xFactors.y, 1 / xFactors.z));
                yPIDFactors = Vector3.Scale(yPIDFactors, new Vector3(1 / yFactors.x, 1 / yFactors.y, 1 / yFactors.z));
                zPIDFactors = Vector3.Scale(zPIDFactors, new Vector3(1 / zFactors.x, 1 / zFactors.y, 1 / zFactors.z));
            }

            xController.pFactor = xPIDFactors.x;
            xController.iFactor = xPIDFactors.y;
            xController.dFactor = xPIDFactors.z;

            yController.pFactor = yPIDFactors.x;
            yController.iFactor = yPIDFactors.y;
            yController.dFactor = yPIDFactors.z;

            zController.pFactor = zPIDFactors.x;
            zController.iFactor = zPIDFactors.y;
            zController.dFactor = zPIDFactors.z;
        }

        public void Start()
        {
            xController = new PIDController(0, 0, 0);
            yController = new PIDController(0, 0, 0);
            zController = new PIDController(0, 0, 0);

            xController.pFactor = xPIDFactors.x;
            xController.iFactor = xPIDFactors.y;
            xController.dFactor = xPIDFactors.z;

            yController.pFactor = yPIDFactors.x;
            yController.iFactor = yPIDFactors.y;
            yController.dFactor = yPIDFactors.z;

            zController.pFactor = zPIDFactors.x;
            zController.iFactor = zPIDFactors.y;
            zController.dFactor = zPIDFactors.z;
        }

        public void FixedUpdate()
        {
            //tranformLocal = transform.InverseTransformVector(transform.up);
            groundNormalLocal = transform.InverseTransformVector(Target);
            ErrorEuler = Quaternion.FromToRotation(transformLocal, groundNormalLocal).eulerAngles;
            Correction = new Vector3(ErrorEuler.x > 180f ? ErrorEuler.x - 360f : ErrorEuler.x, ErrorEuler.y > 180f ? ErrorEuler.y - 360f : ErrorEuler.y, ErrorEuler.z > 180f ? ErrorEuler.z - 360f : ErrorEuler.z);

            if (Mathf.Abs(Correction.x) < xAngleTreshold)
            {
                xResponse = GetXFeedback(Correction.x, 0f) / 360f;
            }
            else
            {
                xResponse = 0f;
            }
            
            if(Mathf.Abs(Correction.y) < yAngleTreshold)
            {
                yResponse = GetYFeedback(Correction.y, 0f) / 360f;
            }
            else
            {
                yResponse = 0f;
            }
            
            if(Mathf.Abs(Correction.z) < zAngleTreshold)
            {
                zResponse = GetZFeedback(Correction.z, 0f) / 360f;
            }
            else
            {
                zResponse = 0f;
            }     
        }

        public float GetXFeedback(float target, float current)
        {
            return xController.FixedUpdate(target, current);
        }

        public float GetYFeedback(float target, float current)
        {
            return yController.FixedUpdate(target, current);
        }

        public float GetZFeedback(float target, float current)
        {
            return zController.FixedUpdate(target, current);
        }

    }
}
