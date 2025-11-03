using System;
using Experiment;
using ubco.ovilab.HPUI.Core;
using UnityEngine;

public interface IActionInputStream<T> where T : InputStreamArgs
{
    public void OnActionInput(HPUICanvasEventArgs canvasArgs, T inputArgs);
}

[Serializable]
public abstract class InputStreamArgs
{
    public Vector2Int? SwipeStartRegion;
    public Vector2Int? SwipeEndRegion;
}

public class CharacterInputStreamArgs : InputStreamArgs
{
    public string targetCharacter;
    public string inputAction;
}

public class DiscreteActionInputArgs : InputStreamArgs
{
    public string targetAction;
    public string inputAction;
}
