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
    private float lastPV; // Track process variable instead of error
    private bool firstRun;
    [SerializeField] private float outputMin = float.MinValue;
    [SerializeField] private float outputMax = float.MaxValue;

    public PIDController(float kp, float ki, float kd)
    {
        Kp = kp;
        Ki = ki;
        Kd = kd;
        integral = 0f;
        lastPV = 0f;
        firstRun = true;
    }
    public float Calculate(float setpoint, float input, float deltaTime)
    {
        // Debug.Log($"Previous Angle {input}");
        float error = setpoint - input;
        float absError = Mathf.Abs(error);
        if (Mathf.Abs(error) < 3f) // Adjust threshold as needed
            error = 0f;
        float adaptiveKp = absError < 10f ? Kp * 0.5f : Kp;
        float adaptiveKd = absError < 10f ? Kd * 1.5f : Kd;

        // Proportional term
        float proportional = adaptiveKp * error;

        // Integral term with windup protection
        integral += error * deltaTime;
        integral = Mathf.Clamp(integral, -integratorLimit, integratorLimit);
        float integralTerm = Ki * integral;

        // Derivative term (on measurement to avoid derivative kick)
        float derivative = 0f;
        if (!firstRun)
        {
            derivative = -adaptiveKd * (input - lastPV) / deltaTime;
        }

        lastPV = input;
        firstRun = false;

        float output = proportional + integralTerm + derivative;
        // Debug.Log("Angle Output:" + output);
        return Mathf.Clamp(output, outputMin, outputMax);
    }
}
