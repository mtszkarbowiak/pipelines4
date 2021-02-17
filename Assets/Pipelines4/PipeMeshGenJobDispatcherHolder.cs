using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Pipelines4
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class PipeMeshGenJobDispatcherHolder : MonoBehaviour
    {
        [SerializeField] public float4[] Nodes;
        [SerializeField] private float CutMaxAngle = 0.1f;
        [SerializeField] private float MinimalBendAngle = 0.01f;
        [SerializeField] private int VerticesPerCut = 7;
        [SerializeField] private float PipeRadius = 0.1f;
        
        private PipeMeshGenJobDispatcher _dispatcher;
        private MeshFilter _meshFilter;
        private Mesh _mesh;
        
        private void Awake()
        {
            _dispatcher = new PipeMeshGenJobDispatcher(Allocator.Persistent);
            _meshFilter = GetComponent<MeshFilter>();
            _mesh = new Mesh();
            
            _meshFilter.sharedMesh = _mesh;
        }

        private void OnDestroy()
        {
            _dispatcher.Dispose();
        }

        private void Update()
        {
            _dispatcher.CutMaxAngle = CutMaxAngle;
            _dispatcher.MinimalBendAngle = MinimalBendAngle;
            _dispatcher.VerticesPerCut = VerticesPerCut;
            _dispatcher.PipeRadius = PipeRadius;
            
            _dispatcher.SetNodes(Nodes);
            
            _dispatcher.Dispatch();
        }

        private void LateUpdate()
        {
            _dispatcher.Complete( _mesh );
        }

        private void OnDrawGizmos()
        {
            _dispatcher?.DrawGizmos();
        }
    }
}