using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

public class CarInputManager : MonoBehaviour
{
    public float SteerInput => steeringAction.action.ReadValue<float>();
    public float AccelerationInput => accelerationAction.action.ReadValue<float>();
    public float BrakeInput => brakeAction.action.ReadValue<float>();
    
    [SerializeField] private InputActionReference steeringAction;
    [SerializeField] private InputActionReference accelerationAction;
    [SerializeField] private InputActionReference brakeAction;
    [SerializeField] private InputActionReference ABXY;
    [SerializeField] private InputActionReference Dpad;
    [SerializeField] private InputActionReference RSB;
    [SerializeField] private InputActionReference LSB;
    [SerializeField] private InputActionReference StartButton;
    [SerializeField] private InputActionReference SelectButton;
    void Start()
    {
        steeringAction.action.Enable();
        accelerationAction.action.Enable();
        brakeAction.action.Enable();
    }
}


