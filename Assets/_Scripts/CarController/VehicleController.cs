// MIT License
//
// Copyright (c) 2023 Samborlang Pyrtuh
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using ArtificeToolkit.Runtime.SerializedDictionary;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

public class VehicleController : MonoBehaviour
{
    
    [Header("Vehicle Settings")]
    public float motorForce = 50f;
    public float maxSteerAngle = 30f;
    public bool enable4x4 = false; // Option to enable 4-wheel drive
    public float brakeForce = 500f;
    public float[] gearRatios; // Array to store the gear ratios for each gear
    public float shiftThreshold = 5000f; // Threshold value for shifting to a higher gear
    
    [Header("Wheel References")]
    public GameObject centerOfMassObject;

    public SerializedDictionary<WheelPlacement, WheelData> wheelData;
    
    [Header("SFX References")]
    public AudioSource engineStartAudioSource; // Assign this in the Inspector
    public ParticleSystem frontLeftDustParticleSystem, frontRightDustParticleSystem, rearLeftDustParticleSystem, rearRightDustParticleSystem; // References to the dust particle systems for each wheel
    public AudioSource engineAudioSource; // Assign this in the Inspector
    
    private WheelData frontLeft => wheelData[WheelPlacement.FrontLeft];
    private WheelData frontRight => wheelData[WheelPlacement.FrontRight];
    private WheelData rearLeft => wheelData[WheelPlacement.RearLeft];
    private WheelData rearRight => wheelData[WheelPlacement.RearRight];
    
    private Rigidbody carRB;
    private int currentGear = 1; // Variable to track the current gear
    private float stopSpeedThreshold = 1f; // Speed threshold for considering the vehicle stopped
    private Quaternion prevRotation; // Previous rotation of the wheel
    private CarInputController mobileInputController;
    private AudioClip engineSound;
    private float targetPitch;
    private bool hasStartedMoving = false;
    

    void Start()
    {
        carRB = GetComponent<Rigidbody>();
        prevRotation = wheelData[WheelPlacement.FrontLeft].wheelTransform.rotation;
        mobileInputController = FindAnyObjectByType<CarInputController>();
        engineSound = Resources.Load<AudioClip>($"EngineSound");
        targetPitch = engineAudioSource.pitch;
        StartCoroutine(DelayedEngineSound());
    }

    private IEnumerator DelayedEngineSound()
    {
        while (!hasStartedMoving)
        {
            yield return null;
        }

        yield return new WaitForSeconds(2f); // Delay for 2 seconds

        engineAudioSource.Play();
    }


    private void Update()
    {
        if (centerOfMassObject)
        {
            carRB.centerOfMass = transform.InverseTransformPoint(centerOfMassObject.transform.position);
        }

        float v = mobileInputController != null ? mobileInputController.GetVerticalInput() : Input.GetAxis("Vertical") * motorForce;
        float h = mobileInputController != null ? mobileInputController.GetHorizontalInput() : Input.GetAxis("Horizontal") * maxSteerAngle;

        // Apply motor torque to the wheels
        frontLeft.collider.motorTorque = v;
        frontRight.collider.motorTorque = v;
        
        rearLeft.collider.motorTorque = h;
        rearRight.collider.motorTorque = h;

        UpdateWheelPoses();
        
        float _brakeForce = (Input.GetKey(KeyCode.Space) || mobileInputController.brakeButton.IsButtonPressed()) ? brakeForce : 0;
        foreach ((WheelPlacement placement, WheelData _wheelData) in wheelData)
        {
            _wheelData.collider.brakeTorque = _brakeForce;
        }
    }

    private void FixedUpdate()
    {
        float v = mobileInputController != null ? mobileInputController.GetVerticalInput() * motorForce : 0f;
        float h = mobileInputController != null ? mobileInputController.GetHorizontalInput() * maxSteerAngle : 0f;

        // Calculate the current wheel speed in km/h
        float currentSpeedKmph =  frontLeft.collider.radius  * Mathf.PI * frontLeft.collider.rpm * 60f / 1000f;
        Debug.Log("Current Speed: " + currentSpeedKmph + " Kmph");

        // Calculate the current engine RPM based on the wheel speed and gear ratio
        float currentRPM = frontLeft.collider.rpm * gearRatios[Mathf.Clamp(currentGear - 1, 0, gearRatios.Length - 1)];

        // Check if it's time to shift to a higher gear
        if (currentRPM > shiftThreshold && currentGear < gearRatios.Length)
        {
            currentGear++; // Shift to the next gear
        }
        else if (currentSpeedKmph < stopSpeedThreshold && currentGear > 1)
        {
            currentGear--; // Shift to the previous gear when slowing down
        }

        // Adjust the motor torque based on the current gear ratio
        float adjustedTorque = v * gearRatios[Mathf.Clamp(currentGear - 1, 0, gearRatios.Length - 1)];

        // Apply motor torque to the wheels
        if (enable4x4)
        {
            foreach ((WheelPlacement placement, WheelData _wheelData)  in wheelData)
            {
                _wheelData.collider.motorTorque = adjustedTorque;
            }
        }
        else
        {
            frontLeft.collider.motorTorque = adjustedTorque;
            frontRight.collider.motorTorque = adjustedTorque;
            rearLeft.collider.motorTorque = 0f;
            rearRight.collider.motorTorque = 0f;
        }

        frontLeft.collider.steerAngle = h;
        frontRight.collider.steerAngle = h;

        UpdateWheelPoses();

        // Calculate the wheel's angular velocity
        Quaternion currentRotation = frontLeft.wheelTransform.rotation;
        float angularVelocity = Quaternion.Angle(prevRotation, currentRotation) / Time.fixedDeltaTime;
        prevRotation = currentRotation;

        // Check if the vehicle is in motion
        bool isMoving = carRB.linearVelocity.magnitude > 0.1f;

        // Check if any of the wheels are slipping or drifting
        foreach ((WheelPlacement placement, WheelData _wheelData) in wheelData)
        {
            bool shouldPlayDustParticles =
                _wheelData.isWheelSlipping || _wheelData.isWheelBraking || _wheelData.isWheelDrifting;
            SetDustParticleSystemState(frontLeftDustParticleSystem, shouldPlayDustParticles);
        }

        // Calculate the target pitch based on the current speed and direction
        float targetPitch = currentSpeedKmph > 0.1f ? Mathf.Lerp(0.5f, 2f, currentSpeedKmph / 100f) : 0.5f;

        // Check if the vehicle is moving in reverse
        if (currentSpeedKmph < -0.1f)
        {
            targetPitch = Mathf.Lerp(0.5f, 2f, Mathf.Abs(currentSpeedKmph) / 100f);
        }
       
        // Smoothly adjust the pitch towards the target pitch
        engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, targetPitch, Time.deltaTime * 5f);


        // Play the engine start sound if the vehicle just starts moving
        if (!hasStartedMoving && currentSpeedKmph > 0.1f)
        {
            engineStartAudioSource.Play();
            hasStartedMoving = true;
        }
    }

    private void SetDustParticleSystemState(ParticleSystem dustParticleSystem, bool shouldPlay)
    {
        if (shouldPlay)
        {
            if (!dustParticleSystem.isPlaying)
            {
                dustParticleSystem.Play();
            }
        }
        else
        {
            if (dustParticleSystem.isPlaying)
            {
                dustParticleSystem.Stop();
            }
        }
    }

    private void UpdateWheelPoses()
    {
        frontLeft.UpdateWheelPose();
        frontRight.UpdateWheelPose(true);
        rearLeft.UpdateWheelPose();
        rearRight.UpdateWheelPose(true);
    }
}

[Serializable]
public class WheelData
{
    [FormerlySerializedAs("wheel")] public WheelCollider collider;
    public Transform wheelTransform;
    [HideInInspector] public bool isWheelSlipping;
    [HideInInspector] public bool isWheelDrifting;
    [HideInInspector] public bool isWheelBraking;
    
    public bool IsWheelSlipping()
    {
        return collider.GetGroundHit(out WheelHit hit) && hit.sidewaysSlip > 0.1f;
    }

    public bool IsWheelDrifting()
    {
        return collider.GetGroundHit(out WheelHit hit) && hit.forwardSlip > 0.1f;
    }

    public bool IsWheelBraking()
    {
        return collider.isGrounded && Mathf.Abs(collider.rpm) < 1f && collider.brakeTorque > 0f;
    }
    
    public void UpdateWheelPose(bool flip = false)
    {
        collider.GetWorldPose(out Vector3 pos, out Quaternion quat);

        if (flip)
        {
            quat *= Quaternion.Euler(0, 180, 0);
        }

        wheelTransform.position = pos;
        wheelTransform.rotation = quat;
    }

}
public enum WheelPlacement
{
    None = 0,
    FrontLeft = 1,
    FrontRight = 2,
    RearLeft = 3,
    RearRight = 4,
}
