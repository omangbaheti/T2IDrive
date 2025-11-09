using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public class Speedometer : MonoBehaviour
{
    // private TextMeshPro speedometerTmp;
    private TextMeshProUGUI speedometerTmpUgui;
    [SerializeField] private VehicleController vehicleController;
    
    private void Awake()
    {
        // speedometerTmp = GetComponent<TextMeshPro>();
        speedometerTmpUgui = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        speedometerTmpUgui.text = vehicleController.CurrentSpeed.ToString("F1", CultureInfo.InvariantCulture) + " km/h"; ; 
    }
}