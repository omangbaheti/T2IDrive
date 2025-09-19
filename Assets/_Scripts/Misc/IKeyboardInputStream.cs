using System;
using Experiment;
using ubco.ovilab.HPUI.Core;
using UnityEngine;

public interface IKeyboardInputStream
{
    public void OnCharacterInput(HPUICanvasEventArgs canvasArgs, InputStreamArgs inputArgs);
}

[Serializable]
public class KeyboardInputStream : MonoBehaviour,  IKeyboardInputStream
{
    public virtual void OnCharacterInput(HPUICanvasEventArgs canvasArgs, InputStreamArgs inputArgs)
    {
        throw new NotImplementedException();
    }
}