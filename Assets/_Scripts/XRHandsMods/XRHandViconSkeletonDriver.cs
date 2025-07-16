using System;
using System.Collections.Generic;
using Unity.Collections;
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

        protected override void Reset()
        {
            base.Reset();
        }

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

            // if (!m_HasRootTransform)
            //     return;
            //
            // if (hasRootOffset)
            //     m_RootTransform.localPosition = rootPose.position + rootOffset;
            // else
            //     m_RootTransform.localPosition = rootPose.position;
            //
            // m_RootTransform.localRotation = rootPose.rotation;
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
                    m_JointTransforms[i].SetLocalPose(m_JointLocalPoses[i]);
                }
            }
        }

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
            // XRHandSkeletonDriverUtility.FindJointsFromRoot(this, missingJointNames);
        }
    }
}
