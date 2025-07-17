using System.Collections.Generic;
using Unity.XR.CoreUtils;
#if BURST_PRESENT
using Unity.Burst;
#endif

namespace UnityEngine.XR.Hands
{

    /// <summary>
    /// Controls a hierarchy of Transforms driven by joints in an <see cref="XRHand"/>.
    /// This component subscribes to events from an <see cref="XRHandTrackingEvents"/> component to move and rotate the joints when the hand is updated.
    /// </summary>
    // [HelpURL(XRHelpURLConstants.k_XRHandSkeletonDriver)]
#if BURST_PRESENT
    [BurstCompile]
#endif
    public class XRHandViconSkeletonDriver : XRHandSkeletonDriver
    {
        [SerializeField] private Quaternion rotationOffset;
        protected float viconUnitsToUnityUnits = 0.001f;  // This into vicon units = unity units


        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected override void Reset()
        {
            base.Reset();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// MonoBehaviour OnEnable method that subscribes to hand tracking events and allocates the joint local poses array.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// MonoBehaviour OnDisable method that unsubscribes from hand tracking events and disposes the joint local poses array.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();
        }


        protected override void OnRootPoseUpdated(Pose rootPose)
        {
            if (!m_HasRootTransform)
                return;
            if (hasRootOffset)
                rootTransform.localPosition = rootPose.position + rootOffset;
            else
                rootTransform.localPosition = rootPose.position;

            rootTransform.localRotation = rootPose.rotation;
        }

        /// <summary>
        /// Updates all the joints of the hand. This method calls <see cref="UpdateJointLocalPoses"/> to
        /// calculate the local poses of the joints and then immediately calls <see cref="ApplyUpdatedTransformPoses"/>
        /// to apply the changes to the joint Transforms.
        /// </summary>
        /// <param name="args">The event arguments for the XRHand joints updated.</param>
        /// <remarks>
        /// Override this method to change either how or when the <see cref="m_JointLocalPoses"/> array is updated and
        /// applied to the transforms.
        /// </remarks>
        protected override void OnJointsUpdated(XRHandJointsUpdatedEventArgs args)
        {
            // Debug.Log("Trying to Update joints on hand");
            UpdateJointLocalPoses(args);
            ApplyUpdatedTransformPoses();
        }

        /// <summary>
        /// Applies the values in the <see cref="m_JointLocalPoses"/> array to the <see cref="m_JointTransforms"/> array.
        /// </summary>
        /// <remarks>
        /// Override this method to change how the local hand joint poses affect the transforms, such as ignoring position,
        /// or converting to a different coordinate space.
        /// </remarks>
        protected override void ApplyUpdatedTransformPoses()
        {
            // Apply the local poses to the joint transforms
            for (var i = 0; i < m_JointTransforms.Length; i++)
            {
                if (m_HasJointTransformMask[i])
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (m_JointTransforms[i] == null)
                    {
                        Debug.LogError("XR Hand Skeleton has detected that a joint transform has been destroyed after it was initialized." +
                            " After removing or modifying transform joint references at runtime it is required to call InitializeFromSerializedReferences to update the joint transform references.", this);

                        continue;
                    }
#endif
                    SetLocalPose(m_JointTransforms[i], m_JointLocalPoses[i]);
                }
            }
        }

        public void SetLocalPose(Transform transform, Pose pose)
        {
            if (transform.parent != null)
            {
                pose.rotation = Quaternion.LookRotation(-pose.up, pose.forward);
                transform.localPosition = pose.position;
                transform.localRotation = pose.rotation;

            }
            else
            {
                transform.localPosition = pose.position;
                transform.localRotation = pose.rotation;

            }

            // transform.SetLocalPositionAndRotation(pose.position, pose.rotation);
        }

        // protected virtual void FindAndTransform(Transform iTransform, string BoneName)
        // {
        //     int ChildCount = iTransform.childCount;
        //     for (int i = 0; i < ChildCount; ++i)
        //     {
        //         Transform Child = iTransform.GetChild(i);
        //         if (Child.name == BoneName)
        //         {
        //             ApplyBoneTransform(Child);
        //             TransformChildren(Child);
        //             break;
        //         }
        //         // if not finding root in this layer, try the children
        //         FindAndTransform(Child, BoneName);
        //     }
        // }
        //
        // /// <summary>
        // /// Recursively assign the pose of the children starting from the transform passed in.
        // /// </summary>
        // protected void TransformChildren(Transform iTransform)
        // {
        //     int childCount = iTransform.childCount;
        //     for (int i = 0; i < childCount; ++i)
        //     {
        //         Transform Child = iTransform.GetChild(i);
        //         this.ApplyBoneTransform(Child);
        //         TransformChildren(Child);
        //     }
        // }
        //
        // protected virtual void ApplyBoneTransform(XRHandJointID jointID)
        // {
        //     string BoneName = Bone.gameObject.name;
        //     if (segments.TryGetValue(BoneName, out Vector3 segment))
        //     {
        //         Bone.position = segment * viconUnitsToUnityUnits;
        //         Bone.rotation = segmentsRotation[BoneName];
        //     }
        // }


        /// <summary>
        /// Finds the joint transform references from the root.
        /// </summary>
        /// <remarks>
        /// Override this method to change how the joint transform references are found from the root and setup in the
        /// <see cref="m_JointTransformReferences"/>. This method is called from the default inspector editor UI when
        /// the Find Joints button is clicked.
        /// </remarks>
        /// <param name="missingJointNames">A list of strings to list the joints that were not found.</param>
        public override void FindJointsFromRoot(List<string> missingJointNames)
        {
            XRHandSkeletonDriverUtility.FindJointsFromRoot(this, missingJointNames);
        }
    }
}
