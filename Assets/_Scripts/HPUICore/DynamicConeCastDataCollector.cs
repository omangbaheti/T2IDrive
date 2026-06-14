using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using ubco.ovilab.HPUI;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using UnityEngine.XR.Hands;

public class DynamicConeCastDataCollector : RaycastDataCollectorBase
{
    [SerializeField, Tooltip("The list of interactables whose gesture events this collector listens to.")]
    private List<HPUIBaseInteractable> interactables = new();
    public List<HPUIBaseInteractable> Interactables { get => interactables; set => interactables = value; }
    
    private XRHandJointID closestJoint;
    private FingerSide closestSide;
    
    [Button]
    public override bool StartDataCollection()
    {
        bool retval = base.StartDataCollection();

        foreach (IHPUIInteractable interactable in Interactables)
        {
            interactable.GestureEvent.AddListener(OnGestureCallback);
        }
        return retval;
    }

    protected void OnGestureCallback(HPUIGestureEventArgs args)
    {
        if (args.State == HPUIGestureState.Stopped)
        {
            foreach(IGrouping<(XRHandJointID, FingerSide), RaycastDataRecordsContainer> records in currentInteractionData.GroupBy(data => (data.handJointID, data.fingerSide)))
            {
                HPUIInteractorConeRayAngleSegment segment = records.Key switch
                {
                    (XRHandJointID.IndexDistal,        FingerSide.volar)  => HPUIInteractorConeRayAngleSegment.IndexDistalVolarSegment,
                    (XRHandJointID.IndexIntermediate,  FingerSide.volar)  => HPUIInteractorConeRayAngleSegment.IndexIntermediateVolarSegment,
                    (XRHandJointID.IndexProximal,      FingerSide.volar)  => HPUIInteractorConeRayAngleSegment.IndexProximalVolarSegment,

                    (XRHandJointID.MiddleDistal,       FingerSide.volar)  => HPUIInteractorConeRayAngleSegment.MiddleDistalVolarSegment,
                    (XRHandJointID.MiddleIntermediate, FingerSide.volar)  => HPUIInteractorConeRayAngleSegment.MiddleIntermediateVolarSegment,
                    (XRHandJointID.MiddleProximal,     FingerSide.volar)  => HPUIInteractorConeRayAngleSegment.MiddleProximalVolarSegment,

                    (XRHandJointID.RingDistal,         FingerSide.volar)  => HPUIInteractorConeRayAngleSegment.RingDistalVolarSegment,
                    (XRHandJointID.RingIntermediate,   FingerSide.volar)  => HPUIInteractorConeRayAngleSegment.RingIntermediateVolarSegment,
                    (XRHandJointID.RingProximal,       FingerSide.volar)  => HPUIInteractorConeRayAngleSegment.RingProximalVolarSegment,

                    (XRHandJointID.LittleDistal,       FingerSide.volar)  => HPUIInteractorConeRayAngleSegment.LittleDistalVolarSegment,
                    (XRHandJointID.LittleIntermediate, FingerSide.volar)  => HPUIInteractorConeRayAngleSegment.LittleIntermediateVolarSegment,
                    (XRHandJointID.LittleProximal,     FingerSide.volar)  => HPUIInteractorConeRayAngleSegment.LittleProximalVolarSegment,

                    (XRHandJointID.IndexDistal,        FingerSide.radial) => HPUIInteractorConeRayAngleSegment.IndexDistalRadialSegment,
                    (XRHandJointID.IndexIntermediate,  FingerSide.radial) => HPUIInteractorConeRayAngleSegment.IndexIntermediateRadialSegment,
                    (XRHandJointID.IndexProximal,      FingerSide.radial) => HPUIInteractorConeRayAngleSegment.IndexProximalRadialSegment,

                    (XRHandJointID.MiddleDistal,       FingerSide.radial) => HPUIInteractorConeRayAngleSegment.MiddleDistalRadialSegment,
                    (XRHandJointID.MiddleIntermediate, FingerSide.radial) => HPUIInteractorConeRayAngleSegment.MiddleIntermediateRadialSegment,
                    (XRHandJointID.MiddleProximal,     FingerSide.radial) => HPUIInteractorConeRayAngleSegment.MiddleProximalRadialSegment,

                    (XRHandJointID.RingDistal,         FingerSide.radial) => HPUIInteractorConeRayAngleSegment.RingDistalRadialSegment,
                    (XRHandJointID.RingIntermediate,   FingerSide.radial) => HPUIInteractorConeRayAngleSegment.RingIntermediateRadialSegment,
                    (XRHandJointID.RingProximal,       FingerSide.radial) => HPUIInteractorConeRayAngleSegment.RingProximalRadialSegment,

                    (XRHandJointID.LittleDistal,       FingerSide.radial) => HPUIInteractorConeRayAngleSegment.LittleDistalRadialSegment,
                    (XRHandJointID.LittleIntermediate, FingerSide.radial) => HPUIInteractorConeRayAngleSegment.LittleIntermediateRadialSegment,
                    (XRHandJointID.LittleProximal,     FingerSide.radial) => HPUIInteractorConeRayAngleSegment.LittleProximalRadialSegment,
                    _ => throw new ArgumentException($"Unexpected cone ray angle segment: {records.Key}")
                };

                DataRecords.Add(new (currentInteractionData, segment));
            }
            currentInteractionData = new();
        }
    }
    
    [Button]
    public override bool StopDataCollection()
    {
        foreach (IHPUIInteractable interactable in Interactables)
        {
            interactable.GestureEvent.RemoveListener(OnGestureCallback);
        }
        return base.StopDataCollection();
    }

    
}
