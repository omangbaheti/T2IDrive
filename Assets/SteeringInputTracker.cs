using System;
using System.Collections.Generic;
using UnityEngine;
using UXF;

public class CarInputTracker : Tracker 
{
    private CarInputManager carInputManager;
    private VehicleController vehicleController;
    private void Awake()
    {
        carInputManager = GetComponent<CarInputManager>();
        vehicleController = GetComponent<VehicleController>();
        if (String.IsNullOrWhiteSpace(objectName))
        {
            objectName = gameObject.name;
        }
    }
    public override string MeasurementDescriptor => "car_inputs";

    public override IEnumerable<string> CustomHeader => new[]
    {
        "steering_input", "accelerator_input", "brake_input", "speed"
    };
    protected override UXFDataRow GetCurrentValues()
    {
        float steering = carInputManager.SteerInput;
        float accelerator = carInputManager.AccelerationInput;
        float brake = carInputManager.BrakeInput;
        float speed = vehicleController.CurrentSpeed;
        UXFDataRow values = new()
        {
            ("steering_input", steering),
            ("accelerator_input", accelerator),
            ("brake_input", brake),
            ("speed", speed)
        };
        return values;
    }
}
