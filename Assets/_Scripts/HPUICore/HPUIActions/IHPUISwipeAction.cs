using System;
using System.Collections.Generic;
using Experiment;
// using Experiment;
using ubco.ovilab.HPUI.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UXF;

namespace ubco.ovilab.HPUI
{
    public interface IHPUISwipeAction
    {
        public UnityEvent<HPUICanvasEventArgs> OnSwipeStarted { get;}
        public UnityEvent<HPUICanvasEventArgs> OnSwipeCompleted { get;}
        public void GestureStarted(HPUICanvasEventArgs canvasArgs);
        public void GestureCompleted(HPUICanvasEventArgs canvasArgs);
    }

    [Serializable]
    public class CharacterOutput : IHPUISwipeAction
    {
        [SerializeField] public string  outputKey;
        public UnityEvent<HPUICanvasEventArgs> OnSwipeStarted => onSwipeStarted;
        public UnityEvent<HPUICanvasEventArgs> OnSwipeCompleted => onSwipeCompleted;
    
        [SerializeField] public UnityEvent<HPUICanvasEventArgs> onSwipeStarted = new();
        [SerializeField] public UnityEvent<HPUICanvasEventArgs> onSwipeCompleted = new();
        [NonSerialized] public List<KeyboardInputStream> inputStreamTrackers;
        public void GestureStarted(HPUICanvasEventArgs canvasArgs)
        {
            OnSwipeStarted?.Invoke(canvasArgs);
        }
    
        public void GestureCompleted(HPUICanvasEventArgs canvasArgs)
        {
            InputStreamArgs inputStreamArgs = new InputStreamArgs
            {
                SwipeStartRegion = canvasArgs.SwipeStartRegion,
                SwipeEndRegion = canvasArgs.SwipeEndRegion,
                inputAction = outputKey.ToLower()
            };
            Debug.LogWarning($"Gesture Completed: === From {inputStreamArgs.SwipeStartRegion} to {inputStreamArgs.SwipeEndRegion} producing {inputStreamArgs.inputAction}");
            Debug.Log("gesture completed in interface");
            foreach (IKeyboardInputStream keyboardInputStream in inputStreamTrackers)
            {
                keyboardInputStream.OnCharacterInput(canvasArgs, inputStreamArgs);
            }
            OnSwipeCompleted?.Invoke(canvasArgs);
        }
    }

    [Serializable]
    public class IconAction : IHPUISwipeAction
    {
        [SerializeField] public string actionLabel;
        [SerializeField] public Sprite displayImage;
        [HideInInspector] public UnityEvent<HPUICanvasEventArgs> OnSwipeStarted => onSwipeStarted;
        [HideInInspector] public UnityEvent<HPUICanvasEventArgs> OnSwipeCompleted => onSwipeCompleted; 
        
        [SerializeField] public UnityEvent<HPUICanvasEventArgs> onSwipeStarted = new();
        [SerializeField] public UnityEvent<HPUICanvasEventArgs> onSwipeCompleted = new();
        public void GestureStarted(HPUICanvasEventArgs canvasArgs)
        {
            OnSwipeStarted?.Invoke(canvasArgs);
        }

        public void GestureCompleted(HPUICanvasEventArgs canvasArgs)
        {
            OnSwipeCompleted?.Invoke(canvasArgs);
        }
    }
    
    [Serializable]
    public class ExperimentHandler : IHPUISwipeAction
    {
        public UnityEvent<HPUICanvasEventArgs> OnSwipeStarted => onSwipeStarted;
        public UnityEvent<HPUICanvasEventArgs> OnSwipeCompleted => onSwipeCompleted;
        [SerializeField] public UnityEvent<HPUICanvasEventArgs> onSwipeStarted = new();
        [SerializeField] public UnityEvent<HPUICanvasEventArgs> onSwipeCompleted = new();
        [SerializeField, HideInInspector] public Vector2Int startRegion;
        [SerializeField, HideInInspector] public Vector2Int endRegion;
        bool isSwipeStarted = false;
        public void GestureStarted(HPUICanvasEventArgs canvasArgs)
        {
            if(isSwipeStarted) return;
            OnSwipeStarted?.Invoke(canvasArgs);
            isSwipeStarted = true;
        }

        public void GestureCompleted(HPUICanvasEventArgs canvasArgs)
        {
            OnSwipeCompleted?.Invoke(canvasArgs);
            isSwipeStarted = false;
        }
    }
}