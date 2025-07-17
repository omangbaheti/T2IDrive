using System;
using System.Collections.Generic;
using ArtificeToolkit.Runtime.SerializedDictionary;
using ubco.ovilab.HPUI.Tracking;
using UnityEngine;
using UnityEngine.XR.Hands;

public class JointFollowerManager : HandSubsystemSubscriber
{
    public Dictionary<XRHandJointID, JointFollowerDatumProperty> JointFollowerData { get => jointFollowerData; set => jointFollowerData = value; }

    public override Handedness Handedness { get => handedness; set => handedness = value; }

    [SerializeField] private SerializedDictionary<XRHandJointID, Transform> handJoints;

    [SerializeField]
    [Tooltip("Joint follower data to use for this Joint.")]
    private Dictionary<XRHandJointID, JointFollowerDatumProperty> jointFollowerData = new();

    [SerializeField]
    private Handedness handedness = Handedness.Right;

    private void Start()
    {
        foreach ((XRHandJointID jointID, Transform jointTransform) in handJoints)
        {
            JointFollowerData jointData = new JointFollowerData
            {
                handedness = handedness,
                jointID = jointID,
                useSecondJointID = false,
                defaultJointRadius = 0.01f,
                offsetAngle = 0f,
                offsetAsRatioToRadius = 1f,
                longitudinalOffset = 0f
            };
            JointFollowerDatumProperty jointDatumProperty = new (jointData);
            jointFollowerData.Add(jointID, jointDatumProperty);
        }

    }

    protected override void ProcessJointData(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags)
    {
        XRHand hand;
        if (handedness == Handedness.Invalid)
        {
            Debug.LogWarning($"Handedness value in JointFollowerData not valid on {transform.name}, Disabling JointFollowerManager");
            enabled = false;
            return;
        }
        hand = Handedness.Right == handedness ? subsystem.rightHand : subsystem.leftHand;

        foreach ((XRHandJointID jointID, JointFollowerDatumProperty jointDatum) in JointFollowerData)
        {
            JointFollowerData jointFollowerDataValue = jointDatum.Value;
            XRHandJoint currentJoint = hand.GetJoint(jointFollowerDataValue.jointID);
            bool jointPoseExists = currentJoint.TryGetPose(out Pose mainJointPose);
            bool jointRadiusExists = currentJoint.TryGetRadius(out float mainRadius);

            if (jointPoseExists)
            {

            }
        }
    }
}
