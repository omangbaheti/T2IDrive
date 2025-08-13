using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ubco.ovilab.HPUI.Interaction;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace ubco.ovilab.HPUI.Interaction
{

    [Serializable]
    public class HPUICalibratedConeSubSampler : IHPUIRaySubSampler
    {
        [SerializeField] private HPUIInteractorConeRayAngles coneRayData;
        [SerializeField] private float coneAngle = 45f;
        [SerializeField] private float lowerPercentile = 0f;
        [SerializeField] private float upperPercentile = 90f;
        [SerializeField] private float IQRThreshold = 0.4f;


        private XRHandFingerID previousFingerID = XRHandFingerID.Thumb;
        private List<HPUIInteractorRayAngle> fingerRelevantRays = new();
        private List<Vector3> rayDirections = new();
        private float Q1;
        private float Q3;
        private float IQR;
        private float lowerBound;
        private float upperBound;
        private Dictionary<XRHandFingerID, List<XRHandJointID>> fingerToJoints = new()
        {
            { XRHandFingerID.Index, new() { XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip } },
            { XRHandFingerID.Middle, new() { XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip } },
            { XRHandFingerID.Ring, new() { XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip } },
            { XRHandFingerID.Little, new() { XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip } }
        };
        public List<HPUIInteractorRayAngle> SampleRays(Transform interactorObject, HandJointEstimatedData estimatedData)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Sampling Rays");
            Vector3 targetDir = estimatedData.TargetDirection.normalized;
            Vector3 localTargetDir = interactorObject.InverseTransformDirection(targetDir).normalized;
            Debug.DrawRay(interactorObject.position, targetDir, Color.green);
            List<HPUIInteractorRayAngle> filteredRays = new();
            if (estimatedData._closestFinger == null)
            {
                Debug.LogError("No finger joints found.");
                return fingerRelevantRays;
            }

            if (previousFingerID != estimatedData._closestFinger.Value)
            {
                fingerRelevantRays = CacheRayAngles(estimatedData);
            }

            float targetThreshold = Mathf.Lerp(Q1, Q3, estimatedData.GetProximalWeight());
            float thresholdRange = IQR * IQRThreshold;
            float lowerbound = targetThreshold - thresholdRange;
            float upperbound = lowerbound + thresholdRange;
            float cosMaxAngle = Mathf.Cos(coneAngle * Mathf.Deg2Rad);

            for (int i = 0; i < fingerRelevantRays.Count; i++)
            {
                HPUIInteractorRayAngle ray = fingerRelevantRays[i];
                Vector3 direction = rayDirections[i];
                if (Vector3.Dot(direction.normalized, localTargetDir.normalized) < cosMaxAngle)
                    continue;
                if (ray.RaySelectionThreshold < lowerbound || ray.RaySelectionThreshold > upperbound)
                    continue;
                filteredRays.Add(ray);
            }

            UnityEngine.Profiling.Profiler.EndSample();
            previousFingerID = estimatedData._closestFinger.Value;
            return filteredRays;
        }

        private List<HPUIInteractorRayAngle> CacheRayAngles(HandJointEstimatedData estimatedData)
        {
            rayDirections.Clear();
            List<HPUIInteractorRayAngle> rays = new List<HPUIInteractorRayAngle>();
            switch (estimatedData._closestFinger.Value)
            {
                case XRHandFingerID.Index:
                {
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.IndexDistalAngles)
                        rays.AddRange(side.rayAngles);
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.IndexIntermediateAngles)
                        rays.AddRange(side.rayAngles);
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.IndexProximalAngles)
                        rays.AddRange(side.rayAngles);
                    break;
                }

                case XRHandFingerID.Middle:
                {
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.MiddleDistalAngles)
                        rays.AddRange(side.rayAngles);
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.MiddleIntermediateAngles)
                        rays.AddRange(side.rayAngles);
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.MiddleProximalAngles)
                        rays.AddRange(side.rayAngles);
                    break;
                }

                case XRHandFingerID.Ring:
                {
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.RingDistalAngles)
                        rays.AddRange(side.rayAngles);
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.RingIntermediateAngles)
                        rays.AddRange(side.rayAngles);
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.RingProximalAngles)
                        rays.AddRange(side.rayAngles);
                    break;
                }

                case XRHandFingerID.Little:
                {
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.LittleDistalAngles)
                        rays.AddRange(side.rayAngles);
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.LittleIntermediateAngles)
                        rays.AddRange(side.rayAngles);
                    foreach (HPUIInteractorConeRayAngleSides side in coneRayData.LittleProximalAngles)
                        rays.AddRange(side.rayAngles);
                    break;
                }

                case XRHandFingerID.Thumb:
                {
                    Debug.LogError("Thumb should never be the closest finger?");
                    break;
                }
            }
            List<float> values = fingerRelevantRays.Select(x => x.RaySelectionThreshold).OrderBy(x => x).ToList();
            Q1 = GetPercentile(values, lowerPercentile);
            Q3 = GetPercentile(values, upperPercentile);
            IQR = Q3 - Q1;
            foreach (HPUIInteractorRayAngle ray in fingerRelevantRays)
            {
                Vector3 direction = Vector3.forward;
                direction = Quaternion.Euler(ray.X, 0f, ray.Z) * direction;
                direction *= ray.RaySelectionThreshold;
                rayDirections.Add(direction);
            }

            return rays;
        }

        private float GetPercentile(List<float> sortedValues, float percentile)
        {
            if (sortedValues.Count == 0) return 0;

            float position = (sortedValues.Count + 1) * percentile / 100f;
            int index = Mathf.FloorToInt(position);

            if (index < 1) return sortedValues[0];
            if (index >= sortedValues.Count) return sortedValues[sortedValues.Count - 1];

            float fraction = position - index;
            return sortedValues[index - 1] + fraction * (sortedValues[index] - sortedValues[index - 1]);
        }
        public void Dispose()
        {

        }

    }
}