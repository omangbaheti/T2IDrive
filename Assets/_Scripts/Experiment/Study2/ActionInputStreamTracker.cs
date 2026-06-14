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
    [Serializable]
    public class ActionInputStreamTracker : Tracker
    {
        public override string MeasurementDescriptor => "KeyboardInputStreamTracker";
        public override IEnumerable<string> CustomHeader => new[]
        {
            "start_region","end_region", "target_action", "input_action"
        };

        private DiscreteActionInputArgs inputStream;
        private HPUICanvasEventArgs canvasEventArgs;
        private Study2ExperimentManager experimentManager;

        protected void Awake()
        {
            experimentManager = FindAnyObjectByType<Study2ExperimentManager>();
        }

        public void OnActionInput(HPUICanvasEventArgs canvasArgs, DiscreteActionInputArgs inputArgs)
        {
            // if (!Recording)
            // {
            //     return;
            // }
            Debug.Log("On Character Input");
            print($"Target Action: {inputArgs.targetAction} Input: {inputArgs.inputAction}");
            inputArgs.targetAction = experimentManager.TargetAction;
            experimentManager.HandleTrial(inputArgs.inputAction);
            RecordRow(canvasArgs, inputArgs);
        }
        public void RecordRow(HPUICanvasEventArgs canvasArgs, DiscreteActionInputArgs inputArgs)
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
                ("target_action", inputStream.targetAction),
                ("input_action", inputStream.inputAction)
            };
            return row;
        }

    }

}