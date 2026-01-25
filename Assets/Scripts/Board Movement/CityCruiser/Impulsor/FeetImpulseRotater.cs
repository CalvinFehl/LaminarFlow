using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.scrible.Impulsor
{
    public class FeetImpulseRotater : BaseImpulsor
    {
        private Rigidbody parentRigidbody;
        public bool isStabilizer = false;
        public bool isPIDControlled = false;
        public bool usesLeftTriggerToSquash = true;

        public float pitchSpeed = 100f;
        public float rollSpeed = 100f;
        public float yawSpeed = 100f;
        [SerializeField] float turnForceMultiplyer = 1f;
        [SerializeField] float distanceDifferenceMultiplyer = 1000f;

        [SerializeField] float pelvisHeight = 1f;

        public float legLength;
        public Vector4 AxesInput;
        public Vector4 PreviousAxesInput;
        [SerializeField] float maxAnkleAngle = 45f;
        [SerializeField] float gettingBackSpeed = 1f;
        [SerializeField] float stretchSpeed = 0.1f;
        [SerializeField] float stretchMin = 0.1f;
        [SerializeField] float stretchMax = 1.2f;

        public float ankleAngle;
        public float kneeBentness;
        [SerializeField] float footLength = 0.25f;

        public int isAirborneAffected = -1;
        public float airborneTresholdFactor = 3f;
        public float airborneMultiplyer = 1f;
        public float airborneAffectedMultiplyerMinimum = 0f;
        public float airborneAffectedMultiplyer = 1f;

        public GameObject leftHeelPressurePoint;
        public GameObject leftHeel;
        public GameObject leftToes;
        public GameObject righHeelPressurePoint;
        public GameObject rightHeel;
        public GameObject rightToes;
        public GameObject[] pressurePoints = new GameObject[4];

        public GameObject middleBetweenFeet;
        public GameObject massCenterGoal;
        public GameObject massCenter;
        public Vector3 massCenterPreviousPosition;

        public float[] distances = new float[4];
        public float[] previousDistances = new float[4];

        public float[] DeltaDistances = new float[4];
        public Vector3[] DeltaForces = new Vector3[4];

        public Vector3 gravity;
        [SerializeField] float gravityMultiplyer = 2f;

        public Vector3 rotationPoint;
        private float f;

        public Vector3 virtualImpulse;

        void moveMassCenter()
        {
            ankleAngle = (Vector3.Angle(middleBetweenFeet.transform.up, massCenter.transform.position - leftHeelPressurePoint.transform.position));            
            
            if(new Vector2(AxesInput.x, AxesInput.z) != new Vector2(PreviousAxesInput.x, PreviousAxesInput.z))
            {
                f = 0f;

                if (Vector3.Distance(massCenterGoal.transform.position, leftHeelPressurePoint.transform.position) < legLength && Vector3.Distance(massCenterGoal.transform.position, righHeelPressurePoint.transform.position) < legLength)
                {
                    rotationPoint = middleBetweenFeet.transform.position; 
                }
                else if (Vector3.Distance(leftHeelPressurePoint.transform.position, massCenterGoal.transform.position) < Vector3.Distance(righHeelPressurePoint.transform.position, massCenterGoal.transform.position))
                {
                    rotationPoint = righHeelPressurePoint.transform.position;
                }
                else
                {
                    rotationPoint = leftHeelPressurePoint.transform.position;
                }

                if(Mathf.Abs(ankleAngle) < maxAnkleAngle || Mathf.Abs(AxesInput.z) < Mathf.Abs(PreviousAxesInput.z))
                {
                    massCenterGoal.transform.RotateAround(rotationPoint, middleBetweenFeet.transform.forward, (AxesInput.z - PreviousAxesInput.z) * rollSpeed * Time.fixedDeltaTime);
                }
                if (Mathf.Abs(ankleAngle) < maxAnkleAngle || Mathf.Abs(AxesInput.x) < Mathf.Abs(PreviousAxesInput.x))
                {
                    massCenterGoal.transform.RotateAround(rotationPoint, middleBetweenFeet.transform.right, (AxesInput.x - PreviousAxesInput.x) * pitchSpeed * Time.fixedDeltaTime);
                }

                PreviousAxesInput = AxesInput;
            }

            else if(massCenterGoal.transform.localPosition != middleBetweenFeet.transform.localPosition + middleBetweenFeet.transform.up * pelvisHeight && new Vector2(AxesInput.x, AxesInput.z) == new Vector2(0f,0f))
            {
                massCenterGoal.transform.position = Vector3.Lerp(massCenterGoal.transform.position, middleBetweenFeet.transform.position + middleBetweenFeet.transform.up * pelvisHeight, f);
                if (f < 1) 
                { f += Time.deltaTime / gettingBackSpeed; }
            }

            massCenter.transform.position = Vector3.Lerp(middleBetweenFeet.transform.position, massCenterGoal.transform.position, Mathf.Clamp(1f + AxesInput.w, stretchMin, stretchMax));
        }

        void populateVariables()
        {
            if (parentRigidbody == null)
            {
                parentRigidbody = transform.parent?.GetComponent<Rigidbody>();
            }

            if(leftHeelPressurePoint == null)
            {
                leftHeelPressurePoint = GameObject.Find("LeftFoot");
                if(leftHeelPressurePoint != null)
                {
                    leftHeel = new GameObject("leftHeel");
                    leftHeel.transform.position = leftHeelPressurePoint.transform.position - leftHeelPressurePoint.transform.right * footLength / 2;
                    leftHeel.transform.rotation = leftHeelPressurePoint.transform.rotation;
                    leftHeel.transform.parent = leftHeelPressurePoint.transform;
                    leftToes = new GameObject("leftToes");
                    leftToes.transform.position = leftHeelPressurePoint.transform.position + leftHeelPressurePoint.transform.right * footLength / 2;
                    leftToes.transform.rotation = leftHeelPressurePoint.transform.rotation;
                    leftToes.transform.parent = leftHeelPressurePoint.transform;
                }
            }

            if (righHeelPressurePoint == null)
            {
                righHeelPressurePoint = GameObject.Find("RightFoot");
                if(righHeelPressurePoint != null)
                {
                    rightHeel = new GameObject("rightHeel");
                    rightHeel.transform.position = righHeelPressurePoint.transform.position - righHeelPressurePoint.transform.right * footLength / 2;
                    rightHeel.transform.rotation = righHeelPressurePoint.transform.rotation;
                    rightHeel.transform.parent = righHeelPressurePoint.transform;
                    rightToes = new GameObject("rightToes");
                    rightToes.transform.position = righHeelPressurePoint.transform.position + righHeelPressurePoint.transform.right * footLength / 2;
                    rightToes.transform.rotation = righHeelPressurePoint.transform.rotation;
                    rightToes.transform.parent = righHeelPressurePoint.transform;
                }
            }

            if(leftHeelPressurePoint != null && righHeelPressurePoint != null)
            {
                pressurePoints[0] = leftHeel;
                pressurePoints[1] = leftToes;
                pressurePoints[2] = rightHeel;
                pressurePoints[3] = rightToes;
            }

            if (middleBetweenFeet == null && righHeelPressurePoint != null && leftHeelPressurePoint != null)
            {
                middleBetweenFeet = new GameObject("middleBetweenFeet");
                middleBetweenFeet.transform.parent = transform;
                updateVariables();
            }

            if(massCenter == null)
            {
                massCenterGoal = GameObject.Find("MassCenterGoal");
                massCenter = GameObject.Find("MassCenter");
                legLength = Vector3.Distance(massCenter.transform.position, leftHeelPressurePoint.transform.position);
            }
        }

        Vector3 calculateForce(int i)
        {
            Vector3 force;

            force = new Vector3(0f, 0f, 0f);

            distances[i] = Vector3.Distance(pressurePoints[i].transform.position, massCenter.transform.position) * distanceDifferenceMultiplyer;

            if (Mathf.Abs(distances[i] - previousDistances[i]) > 1 / distanceDifferenceMultiplyer && massCenterPreviousPosition != null)
            {
                // Debug.Log("case " + 1 + ": " + distances[i] + " is so much more than " + previousDistances[i]);

                DeltaDistances[i] = Mathf.Clamp((previousDistances[i] - distances[i]), - distanceDifferenceMultiplyer, distanceDifferenceMultiplyer);
                force = (massCenter.transform.position - transform.position - massCenterPreviousPosition) * DeltaDistances[i] * turnForceMultiplyer / distanceDifferenceMultiplyer;
                previousDistances[i] = distances[i];
                // Debug.Log("Actually Updating distance " + i);
            }

            force += gravity * gravityMultiplyer * (0.25f - (distances[i] / (distanceDifferenceMultiplyer * (Vector3.Distance(pressurePoints[0].transform.position, massCenter.transform.position) + Vector3.Distance(pressurePoints[1].transform.position, massCenter.transform.position) + Vector3.Distance(pressurePoints[2].transform.position, massCenter.transform.position) + Vector3.Distance(pressurePoints[3].transform.position, massCenter.transform.position)))));
            DeltaForces[i] = force;
            return force * airborneAffectedMultiplyer;
        }

        void updateVariables()
        {
            middleBetweenFeet.transform.position = Vector3.Lerp(leftHeelPressurePoint.transform.position, righHeelPressurePoint.transform.position, 0.5f);
            middleBetweenFeet.transform.rotation = leftHeelPressurePoint.transform.rotation;
        }

        public void Start()
        {
            populateVariables();
            updateVariables();
            massCenterPreviousPosition = massCenter.transform.position - transform.localPosition;
        }

        public void Update()
        {
            populateVariables();
            updateVariables();

        }

        public void FixedUpdate()
        {
            moveMassCenter();
            parentRigidbody.AddRelativeTorque(new Vector3(0f, AxesInput.y * yawSpeed, 0f), ForceMode.Impulse);
            if(pressurePoints != null)
            {
                for(int i = 0; i < pressurePoints.Length; i++)
                {
                    parentRigidbody.AddForceAtPosition(calculateForce(i), pressurePoints[i].transform.position, ForceMode.Impulse);               
                }
            }


            massCenterPreviousPosition = massCenter.transform.localPosition;
        }
    }
}
