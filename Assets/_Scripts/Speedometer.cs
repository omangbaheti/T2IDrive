using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class Speedometer : MonoBehaviour
{
    private TextMeshPro speedometer;
    [SerializeField] private VehicleController vehicleController;
    private void Awake()
    {
        speedometer = GetComponent<TextMeshPro>();
    }

    private void Update()
    {
        speedometer.text = vehicleController.CurrentSpeed.ToString("F1", CultureInfo.InvariantCulture) + " km/h"; ; 
        
    }
}