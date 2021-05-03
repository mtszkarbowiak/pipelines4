using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace AuroraSeeker.Pipelines4
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class MonoPipe : MonoBehaviour
    {
        [SerializeField] public float4[] Nodes;
        [SerializeField] private float CutMaxAngle = 0.1f;
        [SerializeField] private float MinimalBendAngle = 0.01f;
        [SerializeField] private int VerticesPerCut = 7;
        [SerializeField] private float PipeRadius = 0.1f;
        
        private PipeJobsDispatcher _dispatcher;
        private MeshFilter _meshFilter;
        private Mesh _mesh;
        
        private void Awake()
        {
            _dispatcher = new PipeJobsDispatcher(Allocator.Persistent);
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
            for (var i = 0; i < Nodes.Length - 1; i++)
                Gizmos.DrawLine(Nodes[i].xyz,Nodes[i+1].xyz);
            
            _dispatcher?.DrawGizmos();
        }
    }
}