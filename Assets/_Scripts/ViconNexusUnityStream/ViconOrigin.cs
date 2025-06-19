using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ubco.ovilab.ViconUnityStream
{
    public class ViconOrigin : CustomSubjectScript
    {
        [Serializable]
        struct SegmentMarkerPattern
        {
            public List<string> markerNames;
            public Axis axis;
        }

        public enum Axis
        {
            None = 0,
            PositiveForwardAxis = 1,
            NegativeForwardAxis = 2,
            PositiveRightAxis = 3,
            NegativeRightAxis = 4,
            PositiveUpAxis = 5,
            NegativeUpAxis = 6,
        }

        [FormerlySerializedAs("patterns")]
        [Tooltip("The patterns used. The markers names should match the names in Nexus. The segment name should match the values set in the forward/right segment and also the objects of the seleton")]
        [SerializeField] private List<SegmentMarkerPattern> subjectMarkerPattern;


        private (string, string) forwardSegments;
        private (string, string) rightSegments;
        private (string, string) upSegments;
        private bool ifRightAxis;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        protected override void Start()
        {
            PopulateAxes();
        }

        protected void OnValidate()
        {
            if (Application.isPlaying)
            {
                PopulateAxes();
                if (segmentMarkers == null)
                {
                    segmentMarkers = new Dictionary<string, List<string>>();
                }
                segmentMarkers.Clear();

                //What the heck
                foreach (SegmentMarkerPattern pattern in subjectMarkerPattern)
                {
                    foreach (string markerName in pattern.markerNames)
                    {
                        if (!segmentMarkers.ContainsKey(markerName))
                        {
                            segmentMarkers.Add(markerName, new List<string>());
                        }
                        segmentMarkers[markerName].Add(markerName);
                    }
                }
            }
        }

        private void PopulateAxes()
        {
            ifRightAxis = false;
            foreach (SegmentMarkerPattern segment in subjectMarkerPattern)
            {
                switch (segment.axis)
                {
                    case Axis.PositiveForwardAxis:
                        forwardSegments = (segment.markerNames[0], segment.markerNames[1]);
                        Debug.Log($"{forwardSegments.Item1}, {forwardSegments.Item2}");
                        break;
                    case Axis.NegativeForwardAxis:
                        forwardSegments = (segment.markerNames[1], segment.markerNames[0]);
                        Debug.Log($"{forwardSegments.Item1}, {forwardSegments.Item2}");
                        break;
                    case Axis.PositiveRightAxis:
                        rightSegments = (segment.markerNames[0], segment.markerNames[1]);
                        ifRightAxis = true;
                        break;
                    case Axis.NegativeRightAxis:
                        rightSegments = (segment.markerNames[1], segment.markerNames[0]);
                        ifRightAxis = true;
                        break;
                    case Axis.PositiveUpAxis:
                        upSegments = (segment.markerNames[0], segment.markerNames[1]);
                        break;
                    case Axis.NegativeUpAxis:
                        upSegments = (segment.markerNames[1], segment.markerNames[0]);
                        break;
                    case Axis.None:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        protected override Dictionary<string, Vector3> ProcessSegments(Dictionary<string, Vector3> segments, ViconStreamData data)
        {
            Vector3 forward;
            Vector3 right;
            Vector3 up;
            Debug.Log(forwardSegments.Item1);

            if (segments.TryGetValue(forwardSegments.Item1, out Vector3 forward1) && segments.TryGetValue(forwardSegments.Item2, out Vector3 forward2))
            {
                forward = forward2 - forward1;
            }
            else
            {
                Debug.LogError($"Missing segment. Make sure `forwardSegment1` and `forwardSegment2` are also in `pattern`");
                return segments;
            }

            if (ifRightAxis)
            {
                if (segments.TryGetValue(rightSegments.Item1, out Vector3 right1) && segments.TryGetValue(rightSegments.Item2, out Vector3 right2))
                {
                    right = right2 - right1;
                }
                else
                {
                    Debug.LogError($"Missing segment. Make sure `rightSegment1` and `rightSegment2` are also in `pattern`");
                    return segments;
                }

                up = Vector3.Cross(right, forward);
            }
            else
            {
                if (segments.TryGetValue(upSegments.Item1, out Vector3 up1) && segments.TryGetValue(upSegments.Item2, out Vector3 up2))
                {
                    up = up2 - up1;
                }
                else
                {
                    Debug.LogError($"Missing segment. Make sure `upSegment1` and `upSegment2` are also in `pattern`");
                    return segments;
                }
            }

            Quaternion rot = Quaternion.LookRotation(forward, up);
            foreach(string segmentName in segments.Keys)
            {
                segmentsRotation[segmentName] = rot;
            }

            return segments;

        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
