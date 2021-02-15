using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Pipelines4
{
    public class MeshGenDispatcher : MonoBehaviour
    {
        [SerializeField] private float3[] Nodes;
        [SerializeField] private float CutMaxAngle = 0.1f;
        [SerializeField] private float MinimalBendAngle = 0.01f;
        [SerializeField] private float BendRadius = 0.1f;

        private NativeList<float3> _nodesBuffer;
        private NativeList<Cut> _cutsBuffer;
        private JobHandle _jobHandle;

        private void Awake()
        {
            _nodesBuffer = new NativeList<float3>(128,Allocator.Persistent);
            _cutsBuffer = new NativeList<Cut>(512,Allocator.Persistent);
        }

        private void OnDestroy()
        {
            _nodesBuffer.Dispose();
            _cutsBuffer.Dispose();
        }

        private void Update()
        {
            _cutsBuffer.Clear();
            _nodesBuffer.Clear();
            _nodesBuffer.CopyFrom(Nodes);

            var job = new GenPipeCutsJob()
            {
                Cuts = _cutsBuffer,
                Nodes = _nodesBuffer,
                BendRadius = this.BendRadius,
                CutMaxAngle = this.CutMaxAngle,
                MinimalBendAngle = this.MinimalBendAngle,
                GlobalUp = new float3(0f,1f,0f),
            };

            if (job.ValidateBeforeExecution()==false)
                throw new UnityException("Invalid Job input.");

            _jobHandle = job.Schedule(_nodesBuffer.Length, new JobHandle());
            _jobHandle.Complete();
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
