using System;
using UnityEngine;


[Serializable]
public class PIDController
{
    [Header("PID Settings")]
    [SerializeField] private float Kp = 0.5f;
    [SerializeField] private float Ki = 0.1f;
    [SerializeField] private float Kd = 0.05f;
    [SerializeField] private float integratorLimit = 1f;
    private float integral;
    private float lastError;
    
    public float CalculatePIDStep(float error)
    {
        float deltaTime = Time.fixedDeltaTime;
        integral += error * deltaTime;
        // Clamp integral to avoid windup
        integral = Mathf.Clamp(integral, -integratorLimit, integratorLimit);
        float derivative = (error - lastError) / deltaTime;
        float pidOutput = Kp * error + Ki * integral + Kd * derivative;
        lastError = error;
        return pidOutput;
    }
}
