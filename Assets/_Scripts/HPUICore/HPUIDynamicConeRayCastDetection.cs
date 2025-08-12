using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using Random = UnityEngine.Random;


namespace ubco.ovilab.HPUI.Interaction
{
    [Serializable]
    public class HPUIDynamicConeRayCastDetectionLogic : HPUIRayCastDetectionBaseLogic
    {
        [SerializeField]
        [Tooltip("The HPUIInteractorConeRayAngles asset to use when using cone")]
        private HPUIDynamicConeRayData coneRayData;
        private HPUIInteractorFullRangeAngles fullRangeRayAngles;
        private ClosestFingerData closestFingerData;

        [SerializeField] private XRHandTrackingEvents _xrHandTrackingEvents;
        [SerializeField] private Transform XROrigin;

        [SerializeField] float radiusA = 0.025f;
        [SerializeField] float radiusB = 0.015f;
        [SerializeField] float radiusC = 0.02f;
        [SerializeField] float scalingFactor = 1.2f;
        [SerializeField] private float rotationAngle = -20f;
        [SerializeField] private float tiltRotation = 0f;
        [SerializeField] private JointFollowerSkeletonDriver jointFollowerSkeletonDriver;
        public HPUIDynamicConeRayCastDetectionLogic()
        {

        }

        public float InteractionHoverRadius { get; set; }

        public override void DetectedInteractables(IHPUIInteractor interactor, XRInteractionManager interactionManager, Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets, out Vector3 hoverEndPoint)
        {
            bool failed = false;

            if (closestFingerData == null)
            {
                closestFingerData = new ClosestFingerData(_xrHandTrackingEvents, XROrigin, rotationAngle);
            }

            if (_xrHandTrackingEvents == null || XROrigin == null)
            {
                Debug.LogError("Hand tracking events or XROrigin not set!");
                hoverEndPoint = interactor.GetAttachTransform(null).position;
                return;
            }

            closestFingerData.Estimate( jointFollowerSkeletonDriver,rotationAngle,tiltRotation, out XRHandFingerID? closestFingerID, out XRHandJointID closestJoint, out Vector3 vectorToFingerTip, out Vector3 vectorToFingerProximal, out Vector3 targetDirection, out Vector3 thumbPos);
            float totalDistance = vectorToFingerTip.magnitude + vectorToFingerProximal.magnitude;
            // float tipWeight = vectorToFingerProximal.magnitude / totalDistance;
            float proximalWeight = vectorToFingerTip.magnitude / totalDistance;
            Debug.Log($"Proximal weight: {vectorToFingerTip.magnitude} / {totalDistance}");
            float scale = Mathf.Lerp(1, scalingFactor, proximalWeight);
            Debug.Log("Scaling Factor" + scale);
            Transform interactorObject = interactor.transform;
            List<HPUIInteractorRayAngle> angles = EllipsoidSampler(interactorObject, targetDirection, 5, radiusA*scale, radiusB*scale, radiusC*scale);
            Debug.DrawRay(interactor.transform.position, targetDirection, Color.magenta);
            Process(interactor, interactionManager, angles, validTargets, out hoverEndPoint);
        }

        public List<HPUIInteractorRayAngle> EllipsoidSampler(Transform interactorObject, Vector3 targetDirection, int angleStep, float a, float b, float c)
        {
            List<HPUIInteractorRayAngle> allAngles = new();

            float numberOfSamples = Mathf.Pow(360f / angleStep, 2);
            float phi = Mathf.PI * (Mathf.Sqrt(5f) - 1f);

            // Your cone limit in degrees
            float maxAngleDeg = 30f; // example
            float cosMaxAngle = Mathf.Cos(maxAngleDeg * Mathf.Deg2Rad);

            // Direction from ellipsoid center you want rays near
            Vector3 targetDir = targetDirection.normalized;
            Vector3 localTargetDir = interactorObject.InverseTransformDirection(targetDir).normalized;
            Quaternion rotationToTarget = Quaternion.FromToRotation(Vector3.forward, localTargetDir);
            for (int i = 0; i < numberOfSamples; i++)
            {
                float y = 1f - (i / (numberOfSamples - 1f)) * 2f;
                float radius = Mathf.Sqrt(1f - y * y);

                float theta = phi * i;

                float x = Mathf.Cos(theta) * radius;
                float z = Mathf.Sin(theta) * radius;
                Vector3 spherePoint = new Vector3(x, y, z);



                // Rotate point so cone is aligned to targetDir
                Vector3 rotatedDir = rotationToTarget * spherePoint;

                // Stretch to ellipsoid dimensions
                Vector3 ellipsoidPoint = new Vector3(rotatedDir.x * a, rotatedDir.y * b, rotatedDir.z * c);

                // Debug.DrawLine(
                //     interactorObject.position,
                //     interactorObject.position + interactorObject.TransformDirection(ellipsoidPoint),
                //     Color.yellow
                // );

                if (Vector3.Dot(Vector3.forward, spherePoint.normalized) < cosMaxAngle)
                    continue;

                // Angles + distance
                float xAngle = Vector3.Angle(Vector3.up, new Vector3(0f, ellipsoidPoint.y, ellipsoidPoint.z)) * (ellipsoidPoint.z < 0f ? -1f : 1f);
                float zAngle = Vector3.Angle(Vector3.up, new Vector3(ellipsoidPoint.x, ellipsoidPoint.y, 0f)) * (ellipsoidPoint.x < 0f ? -1f : 1f);
                float distance = ellipsoidPoint.magnitude;

                allAngles.Add(new HPUIInteractorRayAngle(xAngle, zAngle, distance));

            }

            return allAngles;
        }

        public void Reset(){ }

        public void Dispose(){ }

    }

    [Serializable]
    public class ClosestFingerData : IDisposable
    {
        public XRHandTrackingEvents XRHandTrackingEvents
        {
            get => xrHandTrackingEvents;
            set
            {
                if (value != xrHandTrackingEvents)
                {
                    xrHandTrackingEvents?.jointsUpdated.RemoveListener(UpdateJointsData);
                }

                xrHandTrackingEvents = value;
                xrHandTrackingEvents?.jointsUpdated.AddListener(UpdateJointsData);
            }
        }

        public Transform XROriginTransform { get => xrOriginTransform; set => xrOriginTransform = value; }
        // public XRHandFingerID ClosestFingerID => closestFingerID;


        [SerializeField]
        [Tooltip("(optional) XR Origin transform. If not set, will attempt to find XROrigin and use its transform.")]
        private Transform xrOriginTransform;
        [SerializeField]
        [Tooltip("The XR Hand Tracking Events component used to track the state of the segments.")]
        private XRHandTrackingEvents xrHandTrackingEvents;

        private Dictionary<XRHandJointID, Pose> jointLocations = new();

        private List<XRHandJointID> trackedJoints = new List<XRHandJointID>()
        {
            XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip,
            XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip,
            XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip,
            XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip,
            XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip
        };


        private List<XRHandJointID> impJoints = new List<XRHandJointID>()
        {
            XRHandJointID.IndexProximal, XRHandJointID.IndexDistal,
            XRHandJointID.MiddleProximal,  XRHandJointID.MiddleDistal,
            XRHandJointID.RingProximal,  XRHandJointID.RingDistal,
            XRHandJointID.LittleProximal,  XRHandJointID.LittleDistal,
            XRHandJointID.ThumbTip
        };

        private Dictionary<XRHandJointID, XRHandJointID> trackedJointsToSegment = new ()
        {
            {XRHandJointID.IndexProximal,      XRHandJointID.IndexIntermediate},
            {XRHandJointID.IndexIntermediate,  XRHandJointID.IndexDistal},
            {XRHandJointID.IndexDistal,        XRHandJointID.IndexTip},
            {XRHandJointID.MiddleProximal,     XRHandJointID.MiddleIntermediate},
            {XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal},
            {XRHandJointID.MiddleDistal,       XRHandJointID.MiddleTip},
            {XRHandJointID.RingProximal,       XRHandJointID.RingIntermediate},
            {XRHandJointID.RingIntermediate,   XRHandJointID.RingDistal},
            {XRHandJointID.RingDistal,         XRHandJointID.RingTip},
            {XRHandJointID.LittleProximal,     XRHandJointID.LittleIntermediate},
            {XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal},
            {XRHandJointID.LittleDistal,       XRHandJointID.LittleTip},
        };

        private Dictionary<XRHandFingerID?, (XRHandJointID start, XRHandJointID end)> fingerToJointsExtremities = new()
        {
            { XRHandFingerID.Index,  (XRHandJointID.IndexDistal,    XRHandJointID.IndexProximal) },
            { XRHandFingerID.Middle, (XRHandJointID.MiddleDistal,   XRHandJointID.MiddleProximal) },
            { XRHandFingerID.Ring,   (XRHandJointID.RingDistal,     XRHandJointID.RingProximal) },
            { XRHandFingerID.Little, (XRHandJointID.LittleDistal,   XRHandJointID.LittleProximal) }
        };


        private Dictionary<XRHandJointID, XRHandFingerID> jointToFinger = new()
        {
            // Index Finger
            { XRHandJointID.IndexProximal,      XRHandFingerID.Index },
            { XRHandJointID.IndexIntermediate,  XRHandFingerID.Index },
            { XRHandJointID.IndexDistal,        XRHandFingerID.Index },
            { XRHandJointID.IndexTip,           XRHandFingerID.Index },

            // Middle Finger
            { XRHandJointID.MiddleProximal,     XRHandFingerID.Middle },
            { XRHandJointID.MiddleIntermediate, XRHandFingerID.Middle },
            { XRHandJointID.MiddleDistal,       XRHandFingerID.Middle },
            { XRHandJointID.MiddleTip,          XRHandFingerID.Middle },

            // Ring Finger
            { XRHandJointID.RingProximal,       XRHandFingerID.Ring },
            { XRHandJointID.RingIntermediate,   XRHandFingerID.Ring },
            { XRHandJointID.RingDistal,         XRHandFingerID.Ring },
            { XRHandJointID.RingTip,            XRHandFingerID.Ring },

            // Little Finger
            { XRHandJointID.LittleProximal,     XRHandFingerID.Little },
            { XRHandJointID.LittleIntermediate, XRHandFingerID.Little },
            { XRHandJointID.LittleDistal,       XRHandFingerID.Little },
            { XRHandJointID.LittleTip,          XRHandFingerID.Little }
        };

        private bool receivedNewJointData;
        private GameObject tipViz;
        private GameObject proximalViz;
        public ClosestFingerData(XRHandTrackingEvents handTrackingEvents, Transform _xrOriginTransform, float _rotationAngle)
        {
            this.XRHandTrackingEvents = handTrackingEvents;
            xrOriginTransform = _xrOriginTransform;
            tipViz = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proximalViz = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Reset();
        }

        public void Reset()
        {
            foreach(XRHandJointID id in trackedJoints)
            {
                if (!jointLocations.ContainsKey(id))
                {
                    jointLocations.Add(id, Pose.identity);
                }
            }
            if (XRHandTrackingEvents != null)
            {
                XRHandTrackingEvents.jointsUpdated.AddListener(UpdateJointsData);
            }
        }

        private void UpdateJointsData(XRHandJointsUpdatedEventArgs args)
        {
            foreach(XRHandJointID id in trackedJoints)
            {
                if ( args.hand.GetJoint(id).TryGetPose(out Pose pose) )
                {
                    jointLocations[id] = pose.GetTransformedBy(XROriginTransform);
                    receivedNewJointData = true;
                }
            }
        }

        public void Estimate(JointFollowerSkeletonDriver skeletonDriver, float flatRotation, float tiltRotation,  out XRHandFingerID? _closestFinger, out XRHandJointID _closestJoint, out Vector3 vectorToFingerTip, out Vector3 vectorToFingerProximal, out Vector3 targetDirection, out Vector3 thumbMidPoint)
        {
            _closestFinger = null;
            _closestJoint = XRHandJointID.BeginMarker;
            float angleToFingerTip = float.MinValue;
            float angleToFingerProximal = float.MinValue;
            vectorToFingerTip = Vector3.negativeInfinity;
            vectorToFingerProximal = Vector3.negativeInfinity;
            thumbMidPoint = Vector3.negativeInfinity;
            targetDirection = Vector3.negativeInfinity;
            if (receivedNewJointData)
            {
                receivedNewJointData = false;
                Vector3 thumbTipPos = skeletonDriver.HandJoints[XRHandJointID.ThumbTip].position;
                Vector3 toClosestPoint = skeletonDriver.HandJoints[XRHandJointID.ThumbTip].forward;
                float shortestDistance = float.MaxValue;
                foreach (KeyValuePair<XRHandJointID, XRHandJointID> kvp in trackedJointsToSegment)
                {
                    Vector3 baseVector = jointLocations[kvp.Key].position;
                    Vector3 segmentVector = jointLocations[kvp.Value].position - baseVector;
                    Vector3 toTipVector = thumbTipPos - segmentVector;
                    float distanceOnSegmentVector = Mathf.Clamp(Vector3.Dot(toTipVector, segmentVector.normalized), 0, segmentVector.magnitude);
                    Vector3 closestPoint = distanceOnSegmentVector * segmentVector.normalized + baseVector;
                    Vector3 currentToClosestPoint = (closestPoint - thumbTipPos);
                    float distance = currentToClosestPoint.sqrMagnitude;
                    if (distance < shortestDistance)
                    {
                        toClosestPoint = currentToClosestPoint;
                        shortestDistance = distance;
                        _closestJoint = kvp.Key;
                    }
                }

                _closestFinger = jointToFinger[_closestJoint];
                (XRHandJointID start, XRHandJointID end) fingerExtremities = fingerToJointsExtremities[_closestFinger];
                // Debug.Log($"=== Closest Finger{_closestFinger.Value.ToString()} ===");

                Vector3 thumbToTipVector = Vector3.zero;
                Vector3 thumbToProximalVector = Vector3.zero;
                vectorToFingerTip = thumbToTipVector;
                vectorToFingerProximal = thumbToProximalVector;
                XRHandJointID thumbTip = XRHandJointID.ThumbTip;
                XRHandJointID thumbDistal = XRHandJointID.ThumbDistal;
                thumbMidPoint = thumbTipPos;

                {
                    XRHandJointID fingerTipID = trackedJointsToSegment[fingerExtremities.start];
                    Vector3 fingerTipPos =   skeletonDriver.HandJoints[fingerTipID].position;
                    Vector3 distalPos = skeletonDriver.HandJoints[fingerExtremities.start].position;
                    // Offset the fingertip position along the segment vector
                    Vector3 segmentVector = distalPos - fingerTipPos;
                    thumbToTipVector = fingerTipPos  - thumbMidPoint;
                    vectorToFingerTip = thumbToTipVector;
                    float distanceOnSegmentVector = Mathf.Clamp(Vector3.Dot(thumbToTipVector, segmentVector.normalized), 0, segmentVector.magnitude);
                    Vector3 closestPoint = distanceOnSegmentVector * segmentVector.normalized + fingerTipPos;
                    Vector3 currentToClosestPoint = (closestPoint - thumbMidPoint);
                    angleToFingerTip = Vector3.Dot(-jointLocations[fingerExtremities.start].right.normalized, thumbToTipVector.normalized);
                    Debug.DrawLine(fingerTipPos, distalPos, Color.red);
                    Debug.DrawRay(thumbMidPoint, thumbToTipVector, Color.blue);
                    // Debug.DrawLine(closestPoint, thumbTipPos, Color.green);
                    tipViz.transform.position = fingerTipPos;
                    tipViz.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                }

                {
                    XRHandJointID intermediateID = trackedJointsToSegment[fingerExtremities.end];
                    Vector3 fingerProximalPos = skeletonDriver.HandJoints[fingerExtremities.end].position;
                    Vector3 intermediatePos = skeletonDriver.HandJoints[intermediateID].position;
                    Vector3 segmentVector = intermediatePos - fingerProximalPos;
                    thumbToProximalVector = fingerProximalPos - thumbMidPoint;
                    vectorToFingerProximal = thumbToProximalVector;
                    float distanceOnSegmentVector = Mathf.Clamp(Vector3.Dot(thumbToProximalVector, segmentVector.normalized), 0, segmentVector.magnitude);
                    Vector3 closestPoint = distanceOnSegmentVector * segmentVector.normalized + fingerProximalPos;
                    Vector3 currentToClosestPoint = (closestPoint - thumbMidPoint);
                    angleToFingerProximal = Vector3.Dot(-jointLocations[fingerExtremities.end].right.normalized, thumbToProximalVector.normalized);
                    Debug.DrawLine(fingerProximalPos, intermediatePos, Color.red);
                    Debug.DrawRay(thumbMidPoint, thumbToProximalVector, Color.blue);
                    // Debug.DrawLine(closestPoint, thumbTipPos, Color.green);
                    proximalViz.transform.position = fingerProximalPos;
                    proximalViz.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                }
                Debug.Log($"angleToFingerTip: {Mathf.Acos(angleToFingerTip) *  Mathf.Rad2Deg} -> angleToFingerProximal {Mathf.Acos(angleToFingerProximal) *  Mathf.Rad2Deg }");
                Debug.Log($"DotToFingerTip: {angleToFingerTip} -> DotToFingerProximal {angleToFingerProximal}");
                float totalDistance = vectorToFingerTip.magnitude + vectorToFingerProximal.magnitude;
                float tipWeight = vectorToFingerProximal.magnitude * 1.5f / totalDistance;
                float proximalWeight = vectorToFingerTip.magnitude * 1.5f/ totalDistance;
                Debug.Log($"total{totalDistance}  tip{tipWeight}  proximal{proximalWeight}----");
                targetDirection = (tipWeight * vectorToFingerTip + proximalWeight * vectorToFingerProximal);
                // Calculate plane normal

                Vector3 planeNormal = Vector3.Cross(vectorToFingerProximal, vectorToFingerTip).normalized;

                // 1️⃣ Rotate ALONG the plane (spin flat)
                targetDirection -= Vector3.Dot(targetDirection, planeNormal) * planeNormal; // project into plane
                Quaternion alongPlaneRot = Quaternion.AngleAxis(-flatRotation, planeNormal);
                targetDirection = alongPlaneRot * targetDirection;

                // 2️⃣ Rotate PERPENDICULAR to the plane (tilt out)
                Vector3 perpendicularAxis = Vector3.Cross(planeNormal, targetDirection).normalized;
                Quaternion perpendicularRot = Quaternion.AngleAxis(tiltRotation, perpendicularAxis);
                targetDirection = perpendicularRot * targetDirection;



                // Rotate around plane normal (negative angle = clockwise)
                // Debug.Log($"RotationAngle {rotationAngle}");
                Debug.Log($"Target: {targetDirection}");
                Debug.DrawRay(thumbMidPoint, targetDirection, Color.magenta);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            XRHandTrackingEvents.jointsUpdated.RemoveListener(UpdateJointsData);
        }

    }
}