using System;
using System.Collections.Generic;
using UXF;
using System.Linq;
using TMPro;
using ubco.ovilab.HPUI.Core;
using ubco.ovilab.HPUI.Tracking;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Experiment
{
    
    public class KeyboardInputStreamTracker : Tracker, IKeyboardInputStream
    {
        public override string MeasurementDescriptor => "KeyboardInputStreamTracker";
        public override IEnumerable<string> CustomHeader => new[]
        {
            "start_region","end_region", "target_character", "input_action"
        };

        [SerializeField] private TextMeshPro targetPhrase;
        [SerializeField] private TextMeshPro outputPhrase;

        private InputStreamArgs inputStream;
        private HPUICanvasEventArgs canvasEventArgs;
        // private TypingExperimentManager typingExperimentManager;

        protected void Awake()
        {
            // typingExperimentManager = GetComponent<TypingExperimentManager>();
        }

        public void OnCharacterInput(HPUICanvasEventArgs canvasArgs, InputStreamArgs inputArgs)
        {
            if (!Recording)
            {
                return;
            }

            Debug.Log("On Character Input");
            int targetCharacterIndex = outputPhrase.text.Length;
            targetCharacterIndex = Math.Clamp(targetCharacterIndex, 0, targetPhrase.text.Length - 1);
            inputArgs.targetCharacter = targetPhrase.text[targetCharacterIndex].ToString();
            string input = inputArgs.inputAction.Trim();
            print($"Input Character: {input} Character: {targetCharacterIndex}");
            Debug.LogWarning($"Trial Number : {Session.instance.CurrentTrial.number}");
            RecordRow(canvasArgs, inputArgs);

            switch (input)
            {
                // Left Arrow (←)
                case "\\u2190":
                {
                    Debug.Log("Detected Backspace");
                    if (outputPhrase.text.Length > 0)
                    {
                        outputPhrase.text = outputPhrase.text.Substring(0, outputPhrase.text.Length - 1);
                    }
                    break;
                }
                // Right Arrow (→)
                // case "\\u2192":
                    // typingExperimentManager.HandleTrial();
                    // break;
                default:
                    outputPhrase.text += inputArgs.inputAction;
                    if (inputArgs.targetCharacter != inputArgs.inputAction)
                    {
                        // typingExperimentManager.CancelTrial();
                    }
                    else if (outputPhrase.text == targetPhrase.text)
                    {
                        // typingExperimentManager.HandleTrial();
                    }
                    break;
            }
        }

        public void RecordRow(HPUICanvasEventArgs canvasArgs, InputStreamArgs inputArgs)
        {
            inputStream = inputArgs;
            canvasEventArgs = canvasArgs;
            base.RecordRow();
        }
        protected override UXFDataRow GetCurrentValues()
        {
            UXFDataRow row = new()
            {
                ("start_region", canvasEventArgs.SwipeStartRegion),
                ("end_region", canvasEventArgs.SwipeEndRegion),
                ("target_character", inputStream.targetCharacter),
                ("input_action", inputStream.inputAction)
            };
            return row;
        }
    }

    public class InputStreamArgs
    {
        public Vector2Int? SwipeStartRegion;
        public Vector2Int? SwipeEndRegion;
        public string targetCharacter;
        public string inputAction;
    }
}