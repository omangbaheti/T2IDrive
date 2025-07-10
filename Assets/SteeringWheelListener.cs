using System;
using System.Collections.Generic;
using UnityEngine;

public class SteeringWheelListener : MonoBehaviour
{
    [SerializeField] List<LogitechControls> Inputs = new();
    [SerializeField] LogitechAdapter logitechAdapter;
    [SerializeField] private SelfDrivingManager selfDrivingManager;
    [SerializeField] CarInputManager carInputManager;
    [Range(-1, 1), SerializeField] private float steeringInput;
    private void Start()
    {
        logitechAdapter.InputActionEvents[LogitechControls.Steering].AddListener(PhysicalSteeringWheelInput);
    }

    private void OnDestroy()
    {
        logitechAdapter.InputActionEvents[LogitechControls.Steering].RemoveListener(PhysicalSteeringWheelInput);
    }

    private void Update()
    {
        if (carInputManager.IsSelfDrivingActive)
        {
            SteeringWheelInput(selfDrivingManager.SteerInput);
            return;
        }
    }

    private void PhysicalSteeringWheelInput(float input)
    {
        if (carInputManager.IsSelfDrivingActive)
        {
            return;
        }

        float steerInput = (input * 2) - 1;
        SteeringWheelInput(steerInput);
    }


    private void SteeringWheelInput(float steeringWheel)
    {
        Debug.Log($"Steering Wheel Angle{steeringInput * -450f}");
        steeringInput = steeringWheel;
        transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.y, steeringInput * -450f);


    }
}
