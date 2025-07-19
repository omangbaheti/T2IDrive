using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

[Serializable]
[InputControlLayout(displayName = "Shift Processor")]
[UnityEngine.Scripting.Preserve]
public class ShiftProcessor : InputProcessor<float>
{
    public float shift = 0.5f;

    public override float Process(float value, InputControl control)
    {
         // Debug.Log($"{value + shift}:{value}");
        return value + shift;
    }
}