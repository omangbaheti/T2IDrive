using System;
using System.Collections.Generic;
using UnityEngine;

public class SteeringWheelListener : MonoBehaviour
{
    [SerializeField] List<LogitechControls> Inputs = new List<LogitechControls>();
    [SerializeField] LogitechAdapter logitechAdapter;
    [SerializeField] Transform steeringWheelAnchor;
    [SerializeField] private float steeringInput;
    private void Start()
    {
        logitechAdapter.InputActionEvents[LogitechControls.Steering].AddListener(SteeringWheelInput);
    }

    private void OnDestroy()
    {
        logitechAdapter.InputActionEvents[LogitechControls.Steering].RemoveListener(SteeringWheelInput);
    }


    private void SteeringWheelInput(float steeringWheel)
    {
        Debug.Log(steeringWheel);
        steeringInput = steeringWheel;
        Debug.Log(Mathf.Lerp(-300f, -300f, steeringWheel));
        transform.rotation = Quaternion.AngleAxis(Mathf.Lerp(-300f, -300f, steeringWheel), steeringWheelAnchor.forward);
        // transform.Rotate(steeringWheelAnchor.forward, Mathf.Lerp(-450f, -450f, steeringWheel));
        // transform.RotateAround(steeringWheelAnchor.position, steeringWheelAnchor.forward, Mathf.Lerp(-450f, -450f, steeringWheel));
    }
}
