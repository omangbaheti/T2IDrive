using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ubco.ovilab.HPUI.Interaction
{
    [Serializable]
    public class HPUIThumbCastDetection: IHPUIDetectionLogic
    {
        [SerializeField]
        [Tooltip("Interaction hover radius.")]
        private float interactionHoverRadius = 0.015f;

        /// <summary>
        /// Interaction hover radius.
        /// </summary>
        public float InteractionHoverRadius { get => interactionHoverRadius; set => interactionHoverRadius = value; }

        [SerializeField]
        [Tooltip("Physics layer mask used for limiting poke sphere overlap.")]
        private LayerMask physicsLayer = Physics.AllLayers;

        /// <summary>
        /// Physics layer mask used for limiting poke sphere overlap.
        /// </summary>
        public LayerMask PhysicsLayer { get => physicsLayer; set => physicsLayer = value; }

        // [SerializeField]
        // [Tooltip("Determines whether triggers should be collided with.")]
        // private QueryTriggerInteraction physicsTriggerInteraction = QueryTriggerInteraction.Ignore;

        [SerializeField]
        [Tooltip("Show sphere rays used for interaction selections.")]
        private bool showDebugRayVisual = true;


        [SerializeField] protected SkinnedMeshRenderer thumb;


        /// <summary>
        /// Show sphere rays used for interaction selections.
        /// </summary>
        public bool ShowDebugRayVisual { get => showDebugRayVisual; set => showDebugRayVisual = value; }

        // FIXME: debug code
        StringBuilder dataWriter = new(65000);
        public string DataWriter {
            get
            {
                string toReturn = dataWriter.ToString();
                dataWriter.Clear();
                return toReturn;
            }
            set
            {
                dataWriter.AppendFormat("::{0}", value);
            }
        }


        protected IHPUIInteractor interactor;
        protected Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets = new();
        // Used when computing the centroid
        protected Dictionary<IHPUIInteractable, List<RaycastInteractionInfo>> tempValidTargets = new();

        private RaycastHit[] rayCastHits = new RaycastHit[200];
        private Mesh bakedMesh;

        public void SetInteractor(IHPUIInteractor interactor)
        {
            this.interactor = interactor;
        }

        /// <inheritdoc />
        public void DetectedInteractables(IHPUIInteractor interactor, XRInteractionManager interactionManager, Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets, out Vector3 hoverEndPoint)
        {
            validTargets.Clear();
            Transform attachTransform = interactor.GetAttachTransform(null);
            Vector3 interactionPoint = attachTransform.position;
            hoverEndPoint = interactionPoint;
            if (bakedMesh == null)
            {
                bakedMesh = new Mesh();
            }
            thumb.BakeMesh(bakedMesh, true);
            Vector3[] vertices = bakedMesh.vertices;
            Vector3[] normals = bakedMesh.normals;
            int[] triangles = bakedMesh.triangles;
            attachTransform.TransformPoints(vertices);
            ShootRayCastFromSurface(vertices, normals, attachTransform, out List<RaycastHit> raycastHits);
            foreach (RaycastHit hit in raycastHits)
            {
                //bool validInteractable = false;
                if(interactionManager.TryGetInteractableForCollider(hit.collider, out var interactable) &&
                   interactable is IHPUIInteractable hpuiInteractable &&
                   hpuiInteractable.IsHoverableBy(interactor))
                {
                    if (!tempValidTargets.TryGetValue(hpuiInteractable, out List<RaycastInteractionInfo> infoList))
                    {
                        infoList = ListPool<RaycastInteractionInfo>.Get();
                        tempValidTargets.Add(hpuiInteractable, infoList);
                    }
                    infoList.Add(new RaycastInteractionInfo(hit.distance, hit.point, hit.collider));
                }
            }

            if (ComputeHeuristic(tempValidTargets, validTargets, out Vector3 newHoverEndPoint))
            {
                hoverEndPoint = newHoverEndPoint;
            }

            tempValidTargets.Clear();
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {}

        /// <inheritdoc />
        public virtual void Reset()
        {}

        /// </summary>
        protected bool ComputeHeuristic(Dictionary<IHPUIInteractable, List<RaycastInteractionInfo>> validRayCastTargets, Dictionary<IHPUIInteractable, HPUIInteractionInfo> validTargets, out Vector3 hoverEndPoint)
        {
            Vector3 centroid;
            float xEndPoint = 0, yEndPoint = 0, zEndPoint = 0;
            float count = validRayCastTargets.Sum(kvp => kvp.Value.Count);

            foreach (KeyValuePair<IHPUIInteractable, List<RaycastInteractionInfo>> kvp in tempValidTargets)
            {
                int localCount = kvp.Value.Count;
                float localXEndPoint = 0, localYEndPoint = 0, localZEndPoint = 0;

                foreach(RaycastInteractionInfo i in kvp.Value)
                {
                    xEndPoint += i.point.x;
                    yEndPoint += i.point.y;
                    zEndPoint += i.point.z;
                    localXEndPoint += i.point.x;
                    localYEndPoint += i.point.y;
                    localZEndPoint += i.point.z;
                }

                centroid = new Vector3(localXEndPoint, localYEndPoint, localZEndPoint) / count;
                RaycastInteractionInfo closestToCentroid = kvp.Value.OrderBy(el => (el.point - centroid).magnitude).First();
                // This distance is needed to compute the selection
                float shortestDistance = kvp.Value.Min(el => el.distanceValue);
                float heuristic = (((float)count / (float)localCount) + 1) ;
                float distance = shortestDistance;
                float extra = (float)localCount;
                HPUIInteractionInfo hpuiInteractionInfo = new(heuristic, true, closestToCentroid.point, closestToCentroid.collider, shortestDistance, null);

                validTargets.Add(kvp.Key, hpuiInteractionInfo);
                Debug.Log($"Valid Targets: {validTargets.Count}");
                ListPool<RaycastInteractionInfo>.Release(kvp.Value);
            }

            hoverEndPoint = new Vector3(xEndPoint, yEndPoint, zEndPoint) / count;

            return count > 0;
        }

        protected void ShootRayCastFromSurface(Vector3[] _vertices, Vector3[] _normals, Transform interactorTransform, out List<RaycastHit> raycastHits)
        {
            raycastHits = new List<RaycastHit>();
            for (int i = 0; i < _vertices.Length; i++)
            {
                Vector3 start = _vertices[i];
                Vector3 direction = interactorTransform.rotation  * _normals[i];
                Ray rayCast = new(start, direction.normalized);
                //Optimisation: Should be using RayCastNonAlloc but we have enough compute :)
                if (Physics.Raycast(rayCast, out RaycastHit hit, interactionHoverRadius))
                {
                    raycastHits.Add(hit);
                    if (ShowDebugRayVisual)
                    {
                        Debug.DrawLine(start, start + direction.normalized * interactionHoverRadius, Color.green);
                    }

                }
                else
                {
                    float colorval = (float)i / (float)_vertices.Length;
                    if (ShowDebugRayVisual)
                    {
                        Debug.DrawLine(start, start + direction.normalized * interactionHoverRadius, Color.red);
                    }

                }
            }
        }

        /// <summary>
        /// The raycast interactoin information used with the <see cref="HPUIRayCastDetectionBaseLogic.Process"/>
        /// </summary>
        protected struct RaycastInteractionInfo
        {
            public Vector3 point;
            public Collider collider;
            public float distanceValue;

            public RaycastInteractionInfo(float distanceValue, Vector3 point, Collider collider) : this()
            {
                this.point = point;
                this.collider = collider;
                this.distanceValue = distanceValue;
            }
        }
    }
}