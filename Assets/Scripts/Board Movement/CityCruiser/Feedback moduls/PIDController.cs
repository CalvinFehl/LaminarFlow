using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PIDController 
{
    public float pFactor, iFactor, dFactor;

	private float _integral;
	private float _lastError;
	public PIDController(float pFactor, float iFactor, float dFactor)
	{
		this.pFactor = pFactor;
		this.iFactor = iFactor;
		this.dFactor = dFactor;
	}
	public float FixedUpdate(float target, float current)
	{
		float error = target - current;
		//_integral += error * Time.fixedDeltaTime;
		float derivative = (error - _lastError) / Time.fixedDeltaTime;
		_lastError = error;
		return error * pFactor + derivative * dFactor;
	}
}
