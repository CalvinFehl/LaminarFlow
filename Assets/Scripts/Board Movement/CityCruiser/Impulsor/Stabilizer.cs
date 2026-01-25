using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.scrible.Impulsor
{
    public class Stabilizer : BaseImpulsor
    {
        private Rigidbody parentRigidbody;
        public Vector3 RotThrottle;

        public float ClampedThrottle;
        public float pitchThrottleFactor = 1;
        public float yawThrottleFactor = 0;
        public float rollThrottleFactor = 1;


        public void Update()
        {
            if (parentRigidbody == null)
            {
                parentRigidbody = transform.parent?.GetComponent<Rigidbody>();
            }
            transform.parent.GetComponent<Rigidbody>().AddTorque(RotThrottle, ForceMode.Impulse);
        }
    }
}
