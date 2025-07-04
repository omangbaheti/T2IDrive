using System;
using System.Collections.Generic;
using ArtificeToolkit.Runtime.SerializedDictionary;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Serialization;

public class LogitechAdapter : MonoBehaviour
{
    public SerializedDictionary<LogitechControls, UnityEvent<float>> InputActionEvents => InputActions;
    [SerializeField] private SerializedDictionary <string, LogitechControls> InputScheme= new();
    [SerializeField] private SerializedDictionary<LogitechControls, UnityEvent<float>> InputActions = new SerializedDictionary<LogitechControls, UnityEvent<float>>();
    private DIInputManager drivingSimInputManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        InputActions.Clear();
        foreach (KeyValuePair<string, LogitechControls> controlScheme in InputScheme)
        {
            InputActions.Add(controlScheme.Value, new UnityEvent<float>());

        }
    }

    private void OnValidate()
    {
        InputActions.Clear();
        foreach (KeyValuePair<string, LogitechControls> controlScheme in InputScheme)
        {
            InputActions.Add(controlScheme.Value, new UnityEvent<float>());

        }
    }

    void Start()
    {
        drivingSimInputManager = DIInputManager.Instance;
        Debug.Log(drivingSimInputManager.ffbDevice.name);
    }

    // Update is called once per frame
    void Update()
    {

        foreach (var control in drivingSimInputManager.ffbDevice.allControls)
        {
            if (control is AxisControl or InputControl<float>)
            {
                float value = drivingSimInputManager.GetFFBDeviceAxisValue(control.name);
                if (InputScheme.TryGetValue(control.name, out LogitechControls inputValue))
                {
                    InputActions[InputScheme[control.name]].Invoke(value);
                }
            }
        }
    }
}

public enum LogitechControls
{
    Steering = 0,
    Accelerator = 1,
    Brake = 2,
    Clutch = 3,
    A = 4,
    B = 5,
    X = 6,
    Y = 7,
    UP = 8,
    DOWN = 9,
    LEFT = 10,
    RIGHT = 11,
    START = 12,
    SELECT = 13,
    RSB = 14,
    LSB = 15,
    RightPaddle = 16,
    LeftPaddle = 17
}

