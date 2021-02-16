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

        private MeshFilter _meshFilter;
        private Mesh _mesh;
        
        private NativeList<float3> _nodesBuffer;
        private NativeList<Cut> _cutsBuffer;
        private JobHandle _cutsGenJobHandle;
        private NativeArray<ushort> _meshTrindicesBuffer;
        private NativeArray<UniversalVertex> _meshVerticesBuffer;
        private JobHandle _meshGenJobHandle;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            Assert.IsNotNull(_meshFilter);
            
            _mesh = new Mesh();
            _meshFilter.sharedMesh = _mesh;
            
            
            _nodesBuffer = new NativeList<float3>(64,Allocator.Persistent);
            _cutsBuffer = new NativeList<Cut>(512,Allocator.Persistent);
            _meshTrindicesBuffer = new NativeArray<ushort>(4096, Allocator.Persistent);
            _meshVerticesBuffer = new NativeArray<UniversalVertex>(1024, Allocator.Persistent);
        }

        private void OnDestroy()
        {
            _nodesBuffer.Dispose();
            _cutsBuffer.Dispose();

            _meshTrindicesBuffer.Dispose();
            _meshVerticesBuffer.Dispose();
        }

        private void Update()
        {
            _cutsBuffer.Clear();
            _nodesBuffer.Clear();
            _nodesBuffer.CopyFrom(Nodes);
            
            // Cuts job scheduling.
            {
                var job = new CutsGenJob()
                {
                    Cuts = _cutsBuffer,
                    Nodes = _nodesBuffer,
                    BendRadius = this.BendRadius,
                    CutMaxAngle = this.CutMaxAngle,
                    MinimalBendAngle = this.MinimalBendAngle,
                    GlobalUp = new float3(0f, 1f, 0f),
                };

                if (job.ValidateBeforeExecution() == false)
                    throw new UnityException($"Invalid {nameof(CutsGenJob)} input.");

                _cutsGenJobHandle = job.Schedule(_nodesBuffer.Length, new JobHandle());
                _cutsGenJobHandle.Complete();
            }
            
            // Mesh job scheduling.
            {
                var job2 = new UniversalMeshGenJob()
                {
                    VertsPerCut = 6,
                    Radius = 0.3f,
                    Cuts = _cutsBuffer,
                    TrIndexes = _meshTrindicesBuffer,
                    Vertices = _meshVerticesBuffer,
                };

                if (job2.ValidateBeforeExecution() == false)
                    throw new UnityException($"Invalid {nameof(UniversalMeshGenJob)} input.");

                _meshGenJobHandle = job2.Schedule(_cutsBuffer.Length, 1);
                _meshGenJobHandle.Complete();
                
                var verticesCount = job2.GetVerticesBufferSize();
                var trIndicesCount = job2.GetTrIndexesBufferSize();
                
                //TODO Unsafe mesh update?
                _mesh.SetVertexBufferParams(verticesCount,UniversalVertex.Layout);
                _mesh.SetIndexBufferParams(trIndicesCount,IndexFormat.UInt16);
                
                _mesh.SetVertexBufferData(_meshVerticesBuffer,0,0,verticesCount);
                _mesh.SetIndexBufferData(_meshTrindicesBuffer, 0, 0, trIndicesCount);
                
                _mesh.SetSubMesh(0, new SubMeshDescriptor
                {
                    indexCount = trIndicesCount,
                    vertexCount = verticesCount,
                });
                _mesh.RecalculateBounds();
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(Vector3.zero, 0.02f);
            
            foreach (var t in Nodes)
                Gizmos.DrawSphere(t, 0.02f);

            if (_cutsBuffer.IsCreated == false) return;

            foreach (var t in _cutsBuffer)
                t.DrawGizmos();
        }
    }
}
