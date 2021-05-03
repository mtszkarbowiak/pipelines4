using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pipelines4
{
    public class PipeJobsDispatcher : IDisposable
    {
        public float CutMaxAngle = 0.1f;
        public float MinimalBendAngle = 0.01f;
        public int VerticesPerCut = 7;
        public float PipeRadius = 0.1f;

        private NativeList<float4> _nodesBuffer;
        private NativeList<Cut> _cutsBuffer;
        private NativeArray<ushort> _meshTrIndicesBuffer;
        private NativeArray<Vertex> _meshVerticesBuffer;
        private JobHandle _handle1, _handle2;
        
        public State CurrentState { get; private set; }
        

        public PipeJobsDispatcher(
            Allocator allocator, 
            int nodesBufferSize = 64,
            int cutsBufferSize = 512,
            int trianglesBufferSize = 8192, 
            int verticesBufferSize = 4096)
        {
            _nodesBuffer = new NativeList<float4>(nodesBufferSize,allocator);
            _cutsBuffer = new NativeList<Cut>(cutsBufferSize,allocator);
            _meshTrIndicesBuffer = new NativeArray<ushort>(trianglesBufferSize, allocator);
            _meshVerticesBuffer = new NativeArray<Vertex>(verticesBufferSize, allocator);

            CurrentState = State.Idle;
        }
        
        
        public void SetNodes(float4[] nodes)
        {
            ClearBuffers();

            _nodesBuffer.CopyFrom(nodes);
        }

        public void SetNodes(in NativeArray<float4> nodes)
        {
            ClearBuffers();
            
            _nodesBuffer.CopyFrom(nodes);
        }

        private void ClearBuffers()
        {
            _cutsBuffer.Clear();
            _nodesBuffer.Clear();
        }

        public void Dispatch()
        {
            // Setup first job.
            var job = new PipeCutsJob()
            {
                Cuts = _cutsBuffer,
                Nodes = _nodesBuffer,
                CutMaxAngle = CutMaxAngle,
                MinimalBendAngle = MinimalBendAngle,
                GlobalUp = new float3(0f, 1f, 0f),
            };

            // Validate input.
            if (job.ValidateBeforeExecution() == false)
                throw new UnityException($"Invalid {nameof(PipeCutsJob)} input.");

            // Schedule jobs.
            var emptyHandle = new JobHandle();
            _handle1 = job.Schedule(_nodesBuffer.Length, emptyHandle);
            
            
            CurrentState = State.Dispatched;
        }

        public void Complete( Mesh targetMesh )
        {
            // We can't use dependency, because we don't know how many Cuts gives us first job.
            // Therefore we can't estimate number of needed threads.
            _handle1.Complete();
            
            // Setup second job.
            var job2 = new PipeMeshJob()
            {
                VertsPerCut = VerticesPerCut,
                PipeRadius = PipeRadius,
                Cuts = _cutsBuffer,
                TrIndexes = _meshTrIndicesBuffer,
                Vertices = _meshVerticesBuffer,
            };
            
            // Validate input.
            if (job2.ValidateBeforeExecution() == false)
                throw new UnityException($"Invalid {nameof(PipeMeshJob)} input.");
            
            // Schedule it.
            _handle2 = job2.Schedule(_cutsBuffer.Length, 5, _handle1);
            
            // Cache result size.
            var _verticesCount = job2.GetVerticesBufferSize();
            var _trIndicesCount = job2.GetTrIndexesBufferSize();
            
            // Wait when all cut threads are ready.
            _handle2.Complete();
            
            // Use calculated data to build a result mesh.
            targetMesh.SetVertexBufferParams(_verticesCount,Vertex.Layout);
            targetMesh.SetIndexBufferParams(_trIndicesCount,IndexFormat.UInt16);
                
            targetMesh.SetVertexBufferData(_meshVerticesBuffer,0,0,_verticesCount);
            targetMesh.SetIndexBufferData(_meshTrIndicesBuffer, 0, 0, _trIndicesCount);
                
            targetMesh.SetSubMesh(0, new SubMeshDescriptor
            {
                indexCount = _trIndicesCount,
                vertexCount = _verticesCount,
            });
            targetMesh.RecalculateBounds();
            //TODO Unsafe mesh update?
            
            
            CurrentState = State.Idle;
        }

        public void Dispose()
        {
            ClearBuffers();
            
            _nodesBuffer.Dispose();
            _cutsBuffer.Dispose();
            _meshTrIndicesBuffer.Dispose();
            _meshVerticesBuffer.Dispose();

            CurrentState = State.Disposed;
        }


        public void DrawGizmos()
        {
            Gizmos.color = Color.white;
            foreach (var t in _nodesBuffer)
                Gizmos.DrawSphere(t.xyz, 0.1f);
            
            Gizmos.color = Color.grey;
            for (var i = 0; i < _nodesBuffer.Length - 1; i++)
                Gizmos.DrawLine(_nodesBuffer[i].xyz, _nodesBuffer[i+1].xyz);
            
            if (_cutsBuffer.IsCreated == false) return;

            foreach (var t in _cutsBuffer)
                t.DrawGizmos(2f);
        }

        public enum State
        {
            Idle,
            Dispatched,
            Disposed
        }
    }
}