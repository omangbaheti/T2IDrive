using System;
using System.Collections.Generic;
using ubco.ovilab.HPUI.Interaction;
using ubco.ovilab.HPUI.Tracking;
using UnityEngine;
using UnityEngine.XR.Hands;

[Serializable]
public class JointIDDataDictionary : SerializableDictionary<XRHandJointID, JointFollowerDatum>
{

}

[Serializable]
public class JointIDTransformDictionary : SerializableDictionary<string, Transform>
{

}

[Serializable]
public class StringPrefabDictionary : SerializableDictionary<string, GameObject>
{

}