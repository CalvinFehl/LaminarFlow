using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.scrible.Sensors
{
    public class RaycastSensor : BaseSensor
    {
        private void Awake()
        {
            hit = false;
        }

        public void FixedUpdate()
        {
            if (Physics.Raycast(transform.position, -transform.up, out RaycastHit rayhit))
            {
                hit = true;
                this.distance = rayhit.distance;
                this.groundNormal = rayhit.normal;
                this.hitTag= rayhit.collider.tag;
            }
            else
            {
                hit = false;
                this.distance = 100000f;
            }
        }
    }
}
