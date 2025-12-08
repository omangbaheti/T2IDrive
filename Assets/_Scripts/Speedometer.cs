using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class Speedometer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speedometerTmpUgui;
    [SerializeField] private VehicleController vehicleController;
    
    private void OnEnable()
    {
        speedometerTmpUgui = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        speedometerTmpUgui.text = vehicleController.CurrentSpeed.ToString("F0", CultureInfo.InvariantCulture) + " km/h"; ; 
    }
}