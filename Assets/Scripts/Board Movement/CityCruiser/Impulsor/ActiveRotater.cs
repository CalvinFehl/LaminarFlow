using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.scrible.Impulsor
{
    public class ActiveRotater : BaseImpulsor
    {
        private Rigidbody parentRigidbody;
        public Vector3 RotThrottle;
        public bool isStabilizer = false;

        public float pitchSpeed = 100f;
        public float rollSpeed = 100f;
        public float yawSpeed = 100f;

        public bool isPIDControlled = false;

/*        public float coneFactor;
        public float tiltCompensation;
        public float newHeight; */

        public int isAirborneAffected = 0;
        public float airborneTresholdFactor = 1f;
        public float airborneMultiplyer = 1f;
        public float airborneAffectedMultiplyerMinimum = 0f;
        public float airborneAffectedMultiplyer = 1f;

        public void Start()
        {

            if (parentRigidbody == null)
            {
                parentRigidbody = transform.parent?.GetComponent<Rigidbody>();
            }
        }

        public void Update()
        {

            if (parentRigidbody == null)
            {
                parentRigidbody = transform.parent?.GetComponent<Rigidbody>();
            }
        }

        public void FixedUpdate()
        {
            parentRigidbody.AddRelativeTorque(RotThrottle * airborneAffectedMultiplyer, ForceMode.Impulse);
        }
    }
}
