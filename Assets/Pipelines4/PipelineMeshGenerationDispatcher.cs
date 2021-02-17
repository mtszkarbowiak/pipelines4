using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Pipelines4
{
    [RequireComponent(typeof(MeshFilter))]
    public class PipelineMeshGenerationDispatcher : MonoBehaviour
    {
        [SerializeField] private float3[] Nodes;
        [SerializeField] private float CutMaxAngle = 0.1f;
        [SerializeField] private float MinimalBendAngle = 0.01f;
        [SerializeField] private float BendRadius = 0.1f;
        [SerializeField] private int VerticesPerCut = 7;
        [SerializeField] private float Radius = 0.1f;

        private MeshFilter _meshFilter;
        private Mesh _mesh;
        
        private NativeList<float3> _nodesBuffer;
        private NativeList<Cut> _cutsBuffer;
        private NativeArray<ushort> _meshTrIndicesBuffer;
        private NativeArray<UniversalVertex> _meshVerticesBuffer;
        private JobHandle _handle1, _handle2;


        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            Assert.IsNotNull(_meshFilter);
            
            _mesh = new Mesh();
            _meshFilter.sharedMesh = _mesh;
            
            
            _nodesBuffer = new NativeList<float3>(64,Allocator.Persistent);
            _cutsBuffer = new NativeList<Cut>(512,Allocator.Persistent);
            _meshTrIndicesBuffer = new NativeArray<ushort>(4096*2, Allocator.Persistent);
            _meshVerticesBuffer = new NativeArray<UniversalVertex>(4096, Allocator.Persistent);
        }

        private void OnDestroy()
        {
            _nodesBuffer.Dispose();
            _cutsBuffer.Dispose();

            _meshTrIndicesBuffer.Dispose();
            _meshVerticesBuffer.Dispose();
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(Vector3.zero, 0.02f);
            
            foreach (var t in Nodes)
                Gizmos.DrawSphere(t, 0.02f);

            if (_cutsBuffer.IsCreated == false) return;

            foreach (var t in _cutsBuffer)
                t.DrawGizmos(2f);
        }
        
        
        private void Update()
        {
            // Clear jobs input.
            _cutsBuffer.Clear();
            _nodesBuffer.Clear();
            _nodesBuffer.CopyFrom(Nodes);
            
            // Setup first job.
            var job = new CutsGenJob()
            {
                Cuts = _cutsBuffer,
                Nodes = _nodesBuffer,
                BendRadius = this.BendRadius,
                CutMaxAngle = this.CutMaxAngle,
                MinimalBendAngle = this.MinimalBendAngle,
                GlobalUp = new float3(0f, 1f, 0f),
            };

            // Validate input.
            if (job.ValidateBeforeExecution() == false)
                throw new UnityException($"Invalid {nameof(CutsGenJob)} input.");

            // Schedule jobs.
            var emptyHandle = new JobHandle();
            _handle1 = job.Schedule(_nodesBuffer.Length, emptyHandle);
            
        }

        private void LateUpdate()
        {
            // We can't use dependency, because we don't know how many Cuts gives us first job.
            // Therefore we can't estimate number of needed threads.
            _handle1.Complete();
            
            // Setup second job.
            var job2 = new UniversalMeshGenJob()
            {
                VertsPerCut = VerticesPerCut,
                Radius = Radius,
                Cuts = _cutsBuffer,
                TrIndexes = _meshTrIndicesBuffer,
                Vertices = _meshVerticesBuffer,
            };
            
            // Validate input.
            if (job2.ValidateBeforeExecution() == false)
                throw new UnityException($"Invalid {nameof(UniversalMeshGenJob)} input.");
            
            // Schedule it.
            _handle2 = job2.Schedule(_cutsBuffer.Length, 5, _handle1);
            
            // Cache result size.
            var _verticesCount = job2.GetVerticesBufferSize();
            var _trIndicesCount = job2.GetTrIndexesBufferSize();
            
            // Wait when all cut threads are ready.
            _handle2.Complete();
            
            // Use calculated data to build a result mesh.
            _mesh.SetVertexBufferParams(_verticesCount,UniversalVertex.Layout);
            _mesh.SetIndexBufferParams(_trIndicesCount,IndexFormat.UInt16);
                
            _mesh.SetVertexBufferData(_meshVerticesBuffer,0,0,_verticesCount);
            _mesh.SetIndexBufferData(_meshTrIndicesBuffer, 0, 0, _trIndicesCount);
                
            _mesh.SetSubMesh(0, new SubMeshDescriptor
            {
                indexCount = _trIndicesCount,
                vertexCount = _verticesCount,
            });
            _mesh.RecalculateBounds();
            //TODO Unsafe mesh update?
        }
    }
}
