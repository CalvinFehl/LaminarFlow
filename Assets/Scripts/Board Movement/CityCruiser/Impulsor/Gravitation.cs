using UnityEngine;

namespace Assets.Scripts.scrible.Impulsor
{
    public class Gravitation : BaseImpulsor
    {
        public float gravityDefaultStrength = 15f;
        public float gravityStrength;
        public float magnetFactor = 1f;
        private Rigidbody parentRigidbody;
        public Vector3 gravityDefaultVector = new Vector3(0f,-1f,0f);
        public Vector3 gravityVector;

        public Vector3 DefaultGravity()
        {
            gravityStrength = gravityDefaultStrength;
            gravityVector = gravityDefaultVector * gravityStrength;
            return gravityVector;
        }

        public Vector3 MagnetGravity(Vector3 groundNormal)
        {
            Vector3 magnetPull = groundNormal * -gravityStrength * magnetFactor;
            gravityVector = magnetPull;
            return magnetPull;
        }

        public void Start()
        {
            gravityVector = DefaultGravity();

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
            parentRigidbody.AddForce(gravityVector, ForceMode.Acceleration);            
        }
    }
}
