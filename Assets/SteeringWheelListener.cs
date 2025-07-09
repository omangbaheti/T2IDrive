using System;
using System.Collections.Generic;
using UnityEngine;

public class SteeringWheelListener : MonoBehaviour
{
    [SerializeField] List<LogitechControls> Inputs = new List<LogitechControls>();
    [SerializeField] LogitechAdapter logitechAdapter;
    [SerializeField] Transform steeringWheelAnchor;
    [SerializeField] private SelfDrivingManager selfDrivingManager;
    [Range(-1, 1), SerializeField] private float steeringInput;
    private void Start()
    {
        logitechAdapter.InputActionEvents[LogitechControls.Steering].AddListener(SteeringWheelInput);

    }

    private void OnDestroy()
    {
        logitechAdapter.InputActionEvents[LogitechControls.Steering].RemoveListener(SteeringWheelInput);
    }

    private void Update()
    {
       SteeringWheelInput(selfDrivingManager.SteerInput);
    }


    private void SteeringWheelInput(float steeringWheel)
    {
        // Debug.Log(steeringWheel);
        steeringInput = steeringWheel;
        transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.y, steeringInput * -450f);
    }
}
