using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.scrible.Feedback_moduls
{

    public class PIDFeedbackmodul : Feedbackmodul
    {
        private PIDController controller;

        public float Target;
        public float Current;
        public float feedbackMin = 0f;
        public float feedbackMax = 30000f;

        public float Response;

        public Vector3 PIDFactors = new Vector3(1f, 0f, 0.2f);

        public void OverwritePIDValues(Vector3 factors, bool multiply)
        {
            if (multiply == true)
            {
                PIDFactors = Vector3.Scale(PIDFactors, factors);
                //Debug.Log("new PID " + PIDFactors);

            }
            else if (multiply == false)
            {
                PIDFactors = Vector3.Scale(PIDFactors, new Vector3(1 / factors.x, 1 / factors.y, 1 / factors.z));
                //Debug.Log("new PID " + PIDFactors);
            }

            controller.pFactor = PIDFactors.x;
            controller.iFactor = PIDFactors.y;
            controller.dFactor = PIDFactors.z;
        }

        public void Start()
        {
            controller = new PIDController(0,0,0);

            controller.pFactor = PIDFactors.x;
            controller.iFactor = PIDFactors.y;
            controller.dFactor = PIDFactors.z;
        }

        public void FixedUpdate()
        {
            Response = Mathf.Clamp(GetFeedback(Target, Current), feedbackMin, feedbackMax);
        }

        public float GetFeedback(float target, float current)
        {
            return controller.FixedUpdate(target, current);
        }

    }
}
