using System.Collections.Generic;
using UXF;
using System.Linq;
using ubco.ovilab.HPUI.Core;
using ubco.ovilab.HPUI.Tracking;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;

namespace ubco.ovilab.hpuiModel
{
    /// <summary>
    /// Extending the PositionRotationTracker to also include data from an HPUIInteratable.
    /// </summary>
    public class HPUIInteractableCanvasTracker : LocalPositionTracker
    {
        public override string MeasurementDescriptor => "CanvasInteractable";
        public string ParentName { get => parentName; set => parentName = value; }

        public override IEnumerable<string> CustomHeader => base.CustomHeader.Union(new string[]
        {
                "parentName", "interactableName",
                "hpui_state", "canvas_state",
                "x_size", "y_size",
                "contactPoint_x", "contactPoint_y",
                "current_canvas_region"
        }).ToArray();

        private bool contactPointSet = false;
        private string parentName, interactableName;
        private Vector2 contactPoint;
        private JointFollower jointFollower;
        private HPUIMultiFingerCanvas multiFinger;
        private HPUIGestureEventArgs eventArgs;
        private HPUICanvasEventArgs canvasEventArgs;
        void Start()
        {
            jointFollower = GetComponent<JointFollower>();
            multiFinger = GetComponent<HPUIMultiFingerCanvas>();
            interactableName = multiFinger.transform.name;
            updateType = TrackerUpdateType.Manual;
        }


        public void RecordRow(HPUIGestureEventArgs args, HPUICanvasEventArgs canvasArgs)
        {
            eventArgs = args;
            canvasEventArgs = canvasArgs;
            base.RecordRow();
        }

        /// <summary>
        /// Returns the data collected from the contactZone object
        /// </summary>
        /// <returns></returns>
        protected override UXFDataRow GetCurrentValues()
        {
            UXFDataRow data = base.GetCurrentValues();

            contactPointSet = false;
            UXFDataRow newData =  new()
            {
                ("parentName", parentName),
                ("interactableName", interactableName),
                ("hpui_state", eventArgs.State),
                ("canvas_state", canvasEventArgs.State),
                ("x_size", multiFinger.X_size),
                ("y_size", multiFinger.Y_size),
                ("current_canvas_region", canvasEventArgs.CurrentSwipeRegion),
                ("contactPoint_x", canvasEventArgs.GesturePositions[^1].x),
                ("contactPoint_y", canvasEventArgs.GesturePositions[^1].y)
            };
            data.AddRange(newData);
            return data;
        }


    }
}
