using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ubco.ovilab.HPUI;
using ubco.ovilab.HPUI.Core;
using ubco.ovilab.HPUI.Interaction;
using ubco.ovilab.uxf.extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;
using UXF;
using static EulersCircuit;
using Random = UnityEngine.Random;


namespace Experiment
{
    public class FingerSwipeExperimentManager : ExperimentManager<SwipeGestureBlockData>
    {

        [SerializeField] private Transform handTransform;
        [SerializeField] private GestureLayoutSetup layoutSetup;
        [SerializeField] private AudioClip correct;
        [SerializeField] private AudioClip wrong;
        [SerializeField] private Unity.XR.CoreUtils.Collections.SerializableDictionary<string, Study1TrialManager> trialManager;
        [SerializeField] private List<TwoStepEulerConnection> EulerCircuit;
        private UnityAction<bool> ParticipantAction;
        private XRHandSubsystem handSubsystem;
        private XRHand activeHand;

        [SerializeField] private float indexLength = 0f;
        [SerializeField] private float middleLength = 0f;
        [SerializeField] private float ringLength = 0f;
        [SerializeField] private float littleLength = 0f;
        [SerializeField] private float thumbLength = 0f;
        [SerializeField] private float handLength = 0f;
        public Dictionary<string, float> FingerLengths;
        private System.Random rng;

        protected override void OnSessionBegin(Session session)
        {
            //Time
            session.settingsToLog.Add(StudyLogs.GestureStartTime);
            session.settingsToLog.Add(StudyLogs.GestureEndTime);

            //Trial Settings
            session.settingsToLog.Add(StudyLogs.FingerType);
            session.settingsToLog.Add(StudyLogs.StartRegion);
            session.settingsToLog.Add(StudyLogs.EndRegion);
            session.settingsToLog.Add(StudyLogs.Mobility);

            //HPUI Outputs
            session.settingsToLog.Add(StudyLogs.GestureStartRegion);
            session.settingsToLog.Add(StudyLogs.GestureEndRegion);
            session.settingsToLog.Add(StudyLogs.NetThumbDisplacement);
            session.settingsToLog.Add(StudyLogs.CumulativeDistance);
            session.settingsToLog.Add(StudyLogs.NetDisplacementNormalised);
            session.settingsToLog.Add(StudyLogs.CumulativeDistanceNormalised);
            session.settingsToLog.Add(StudyLogs.SuccessfulTrial);
            session.trackedObjects.AddRange(handTransform.GetComponentsInChildren<Tracker>());

            List<XRHandSubsystem> handSubsystems = new();
            SubsystemManager.GetSubsystems(handSubsystems);
            rng = new System.Random(int.Parse(Session.instance.ppid));

            foreach (XRHandSubsystem subSystem in handSubsystems)
            {
                if (!subSystem.running) continue;
                handSubsystem = subSystem;
                break;
            }
            if (handSubsystem != null)
            {
                activeHand = handSubsystem.rightHand;
            }
            else
            {
                Debug.LogError("Hand Subsystem is null");
            }

            AddCalibrationMethod(StudyLogs.CalibrationMethod, CalculateHandLength);
        }

        protected override void OnSessionEnd(Session session)
        {

        }

        #region Calibrate

        public void FinaliseCalibration()
        {
            Session.instance.settings.SetValue(StudyLogs.Index, indexLength);
            Session.instance.settings.SetValue(StudyLogs.Middle, middleLength);
            Session.instance.settings.SetValue(StudyLogs.Ring, ringLength);
            Session.instance.settings.SetValue(StudyLogs.Little, littleLength);
            Session.instance.settings.SetValue(StudyLogs.Thumb, thumbLength);
            Session.instance.settings.SetValue(StudyLogs.FullHand, handLength);
            Dictionary<string, object> calibrationParameters = new()
            {
                {StudyLogs.CalibrationMethod, new List<float>(){indexLength, middleLength, ringLength, littleLength, thumbLength, handLength}}
            };
            CalibrationComplete(calibrationParameters);
        }

        public void CalculateHandLength(SwipeGestureBlockData blockData)
        {
            activeHand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose tipPose);
            activeHand.GetJoint(XRHandJointID.IndexDistal).TryGetPose(out Pose distalPose);
            activeHand.GetJoint(XRHandJointID.IndexIntermediate).TryGetPose(out Pose intermediatePose);
            activeHand.GetJoint(XRHandJointID.IndexProximal).TryGetPose(out Pose proximalPose);
            indexLength = (tipPose.position - distalPose.position).magnitude +
                          (distalPose.position - intermediatePose.position).magnitude +
                          (intermediatePose.position - proximalPose.position).magnitude;

            activeHand.GetJoint(XRHandJointID.MiddleTip).TryGetPose(out tipPose);
            activeHand.GetJoint(XRHandJointID.MiddleDistal).TryGetPose(out distalPose);
            activeHand.GetJoint(XRHandJointID.MiddleIntermediate).TryGetPose(out intermediatePose);
            activeHand.GetJoint(XRHandJointID.MiddleProximal).TryGetPose(out proximalPose);
            middleLength = (tipPose.position - distalPose.position).magnitude +
                           (distalPose.position - intermediatePose.position).magnitude +
                           (intermediatePose.position - proximalPose.position).magnitude;

            activeHand.GetJoint(XRHandJointID.RingTip).TryGetPose(out tipPose);
            activeHand.GetJoint(XRHandJointID.RingDistal).TryGetPose(out distalPose);
            activeHand.GetJoint(XRHandJointID.RingIntermediate).TryGetPose(out intermediatePose);
            activeHand.GetJoint(XRHandJointID.RingProximal).TryGetPose(out proximalPose);
            ringLength = (tipPose.position - distalPose.position).magnitude +
                         (distalPose.position - intermediatePose.position).magnitude +
                         (intermediatePose.position - proximalPose.position).magnitude;

            activeHand.GetJoint(XRHandJointID.LittleTip).TryGetPose(out tipPose);
            activeHand.GetJoint(XRHandJointID.LittleDistal).TryGetPose(out distalPose);
            activeHand.GetJoint(XRHandJointID.LittleIntermediate).TryGetPose(out intermediatePose);
            activeHand.GetJoint(XRHandJointID.LittleProximal).TryGetPose(out proximalPose);
            littleLength = (tipPose.position - distalPose.position).magnitude +
                           (distalPose.position - intermediatePose.position).magnitude +
                           (intermediatePose.position - proximalPose.position).magnitude;

            activeHand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out tipPose);
            activeHand.GetJoint(XRHandJointID.ThumbDistal).TryGetPose(out distalPose);
            activeHand.GetJoint(XRHandJointID.ThumbProximal).TryGetPose(out intermediatePose);
            activeHand.GetJoint(XRHandJointID.ThumbMetacarpal).TryGetPose(out proximalPose);
            thumbLength = (tipPose.position - distalPose.position).magnitude +
                          (distalPose.position - intermediatePose.position).magnitude +
                          (intermediatePose.position - proximalPose.position).magnitude;

            activeHand.GetJoint(XRHandJointID.BeginMarker).TryGetPose(out Pose WristPose);
            activeHand.GetJoint(XRHandJointID.MiddleTip).TryGetPose(out tipPose);
            handLength = (WristPose.position - tipPose.position).magnitude;

            FingerLengths = new()
            {
                {StudyLogs.Index, indexLength},
                {StudyLogs.Middle, middleLength},
                {StudyLogs.Ring, ringLength},
                {StudyLogs.Little, littleLength},
                {StudyLogs.Thumb, thumbLength},
                {StudyLogs.FullHand, handLength}
            };
        }


        #endregion

        #region Block

        protected override void ConfigureBlock(SwipeGestureBlockData el, Block block, bool lastBlockCancelled)
        {
            block.settings.SetValue(StudyLogs.BlockName, el.name);
            block.settings.SetValue(StudyLogs.FingerType, el.FingerType);
            block.settings.SetValue(StudyLogs.Mobility, el.Mobility);

            layoutSetup.microGestureActions = ShuffleList(layoutSetup.microGestureActions);

            Dictionary<int, Vector2Int> regions = new();
            int counter = 0;
            for (int i = 0; i < layoutSetup.regions.x; i++)
            {
                for (int j = 0; j < layoutSetup.regions.y; j++)
                {
                    regions.Add(counter, (new Vector2Int(i,j)));
                    counter++;
                }
            }

            EulerCircuit = TwoStepEulersCircuit(regions.Count, true, rng);

            for (int i = 0; i < EulerCircuit.Count; i++)
            {
                if (EulerCircuit[i].connectionType == TwoStepEulerConnectionType.Gesture)
                {
                    Trial trial = block.CreateTrial();
                    trial.settings.SetValue(StudyLogs.StartRegion, StudyLogs.VectorToRegionDict[regions[EulerCircuit[i].node]]);
                    trial.settings.SetValue(StudyLogs.EndRegion, StudyLogs.VectorToRegionDict[regions[EulerCircuit[i+1].node]]);
                    Debug.Log($"Creating trial{trial.number} with Start Region {regions[EulerCircuit[i].node]} and End Region {regions[EulerCircuit[i+1].node]}");
                }
            }

            foreach (MicrogestureAction action in layoutSetup.microGestureActions)
            {
                foreach (IHPUISwipeAction swipe in action.SwipeActions.ToList().Where(swipe => swipe.GetType() == typeof(ExperimentHandler)))
                {
                    action.SwipeActions.Remove(swipe);
                }

                ExperimentHandler handler = new()
                {
                    startRegion = action.startRegion,
                    endRegion = action.endRegion
                };
                handler.OnSwipeStarted.AddListener(GestureStarted);
                handler.OnSwipeCompleted.AddListener(HandleTrial);
                action.SwipeActions.Add(handler);
            }
        }

        protected override void OnBlockBegin(Block block)
        {
            if (layoutSetup.microGestureActions.Count < 36)
            {
                Debug.LogWarning("Make sure All Microgestures are setup correctly");
            }

            foreach ((string _, Study1TrialManager _trialManager) in trialManager)
            {
                _trialManager.GetComponent<HPUIMultiFingerCanvas>().enabled = false;
                HPUIBaseInteractable[] interactables = _trialManager.GetComponentsInChildren<HPUIBaseInteractable>();
                foreach (HPUIBaseInteractable interactable in interactables)
                {
                    interactable.enabled = false;
                }
            }

            Study1TrialManager currentStudy1TrialManager = trialManager[block.settings.GetString(StudyLogs.FingerType)];
            currentStudy1TrialManager.GetComponent<HPUIMultiFingerCanvas>().enabled = true;
            HPUIBaseInteractable[] currentActiveInteractables = currentStudy1TrialManager.GetComponentsInChildren<HPUIBaseInteractable>();
            foreach (HPUIBaseInteractable interactable in currentActiveInteractables)
            {
                interactable.enabled = true;
            }
        }

        protected override void OnBlockEnd(Block block)
        {
            foreach (MicrogestureAction action in layoutSetup.microGestureActions)
            {
                foreach (IHPUISwipeAction swipeAction in action.SwipeActions)
                {
                    if (swipeAction.GetType() != typeof(ExperimentHandler)) continue;
                    ExperimentHandler handler = (ExperimentHandler)swipeAction;
                    handler.OnSwipeStarted.RemoveListener(GestureStarted);
                    handler.OnSwipeCompleted.RemoveListener(HandleTrial);
                }
                trialManager["Index"].SetUIActive(false);
            }

        }

        #endregion

        #region Trial

        protected override void OnTrialBegin(Trial trial)
        {
            Debug.Log($"Trial Started {trial.number} {trial.settings.GetString(StudyLogs.StartRegion)}:{trial.settings.GetString(StudyLogs.EndRegion)}");
            string currentFinger = Session.instance.CurrentBlock.settings.GetString(StudyLogs.FingerType);
            trialManager[currentFinger].SetCurrentTrialActive(trial);

        }

        private void GestureStarted(HPUICanvasEventArgs args)
        {
            if (!Session.instance.InTrial)
            {
                Debug.LogWarning("Participant Touched the finger surface when not in trial");
                return;
            }
            Trial CurrentTrial = Session.instance.CurrentTrial;
            var TrialStarRegionName = (FingerRegions)CurrentTrial.settings.GetObject(StudyLogs.StartRegion);
            Vector2Int trialStartRegion = StudyLogs.RegionToVectorDict[TrialStarRegionName];
            if (args.SwipeStartRegion != trialStartRegion)
            {
                Debug.LogWarning("Participant did not start the gesture correctly");
                return;
            }
            CurrentTrial.settings.SetValue(StudyLogs.GestureStartTime, Time.time);
        }

        protected override void OnTrialEnd(Trial trial)
        {
            Debug.Log($"Trial Ended {trial.number}");
        }

        private void HandleTrial(HPUICanvasEventArgs args)
        {
            Settings currentTrial = Session.instance.CurrentTrial.settings;

            if (args.State == HPUICanvasState.Cancelled)
            {
                Session.instance.CurrentTrial.settings.SetValue(StudyLogs.SuccessfulTrial, "FalseTouch");
                CancelTrial();
            }
            else
            {
                bool result = args.SwipeStartRegion != null && args.SwipeEndRegion != null
                                                            && StudyLogs.VectorToRegionDict[args.SwipeStartRegion.Value] == (FingerRegions) currentTrial.GetObject(StudyLogs.StartRegion)
                                                            && StudyLogs.VectorToRegionDict[args.SwipeEndRegion.Value] == (FingerRegions) currentTrial.GetObject(StudyLogs.EndRegion);
                string Result = result ? "Successful" : "Failed";
                AudioClip clip = result ? correct : wrong;
                SoundManager.Instance.PlaySound(clip);
                Session.instance.CurrentTrial.settings.SetValue(StudyLogs.SuccessfulTrial, Result);
                currentTrial.SetValue(StudyLogs.GestureEndTime, Time.time);
                if (result && args.State == HPUICanvasState.Completed) NextTrial();
                else CancelTrial();
            }

        }

        private void NextTrial()
        {
            try
            {
                StartCoroutine(TransitionTrial());
            }
            catch(NoSuchTrialException)
            {
                Debug.Log($"Block ended (i think)");
            }
        }

        private IEnumerator TransitionTrial()
        {
            Session.instance.EndCurrentTrial();
            yield return new WaitForSeconds(0.5f);
            Session.instance.BeginNextTrial();
        }

        private void CancelTrial()
        {
            Trial newTrial = Session.instance.CurrentBlock.CreateTrial();
            List<Trial> trials = Session.instance.CurrentBlock.trials;
            int currTrialIdx = trials.IndexOf(Session.instance.CurrentTrial);
            Settings currentTrialSettings = trials[currTrialIdx].settings;
            newTrial.settings.SetValue(StudyLogs.StartRegion, currentTrialSettings.GetObject(StudyLogs.StartRegion));
            newTrial.settings.SetValue(StudyLogs.EndRegion, currentTrialSettings.GetObject(StudyLogs.EndRegion));
            trials.Remove(newTrial);
            trials.Insert(currTrialIdx+1, newTrial);
            Debug.Log("Cancelling trial");
            NextTrial();
        }


        #endregion


        private static List <T> ShuffleList<T>(List <T> list) 
        {
            System.Random random = new System.Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(0, i+1);
                (list[index: i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

    }

    public class SwipeGestureBlockData : BlockData
    {
        public string FingerType;
        public string Mobility;
    }
}

public enum FingerRegions
{
    Invalid = -1,
    VolarProximal = 0,
    VolarIntermediate = 1,
    VolarDistal = 2,
    RadialProximal = 3,
    RadialIntermediate = 4,
    RadialDistal = 5,
}

public static class StudyLogs
{
    public const string FingerType = "fingerType";
    public const string Mobility = "mobility";
    public const string StartRegion = "start_region";
    public const string EndRegion = "end_region";
    public const string GestureStartRegion = "gesture_start_region";
    public const string GestureEndRegion = "gesture_end_region";
    public const string GestureStartTime = "gesture_start_time";
    public const string GestureEndTime = "gesture_end_time";
    public const string NetThumbDisplacement = "net_thumb_displacement";
    public const string CumulativeDistance = "cumulative_thumb_distance";
    public const string NetDisplacementNormalised = "net_displacement_normalised";
    public const string CumulativeDistanceNormalised = "cumulative_thumb_distance_normalised";
    public const string SuccessfulTrial = "successful_trial";
    public const string BlockName = "BlockName";
    public const string CalibrationMethod = "CalculateHandLength";
    public const string TargetAction = "target_action";
    public const string UIType= "ui_type";
    public const string InputAction = "input_action";

    public const string Index = "Index";
    public const string Middle = "Middle";
    public const string Ring = "Ring";
    public const string Little = "Little";
    public const string Thumb = "Thumb";
    public const string FullHand = "FullHand";
    public static readonly Dictionary<Vector2Int, FingerRegions> VectorToRegionDict = new()
    {
        { new Vector2Int(0, 0), FingerRegions.VolarProximal},
        { new Vector2Int(0, 1), FingerRegions.VolarIntermediate},
        { new Vector2Int(0, 2), FingerRegions.VolarDistal},
        { new Vector2Int(1, 0), FingerRegions.RadialProximal},
        { new Vector2Int(1, 1), FingerRegions.RadialIntermediate},
        { new Vector2Int(1, 2), FingerRegions.RadialDistal}
    };

    public static readonly Dictionary<FingerRegions,Vector2Int> RegionToVectorDict = new()
    {
        { FingerRegions.VolarProximal, new Vector2Int(0, 0)},
        { FingerRegions.VolarIntermediate, new Vector2Int(0, 1)},
        { FingerRegions.VolarDistal, new Vector2Int(0, 2)},
        { FingerRegions.RadialProximal, new Vector2Int(1, 0)},
        { FingerRegions.RadialIntermediate, new Vector2Int(1, 1)},
        {  FingerRegions.RadialDistal,new Vector2Int(1, 2)}
    };
}