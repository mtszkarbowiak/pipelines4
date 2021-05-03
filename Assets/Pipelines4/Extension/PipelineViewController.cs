using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace Pipelines4
{
    internal class PipelineViewController
    {
        public ViewState CurrentState => _state;
        private volatile ViewState _state;

        private float3[] _nodes;
        private int _nodesCount = -1;
        private float4[][] _nodesBuffers;
        
        public float Separation = 0.1f;
        public float CutMaxAngle = 0.1f;
        public float MinimalBendAngle = 0.01f;
        public int VerticesPerCut = 7;
        public float PipeRadius = 0.1f;
        public float RoundizatorMult = 0.5f;
        public float RoundizatorFlat = 1.0f;
        
        private List<PipeJobsDispatcher> _dispatchers;
        private List<MeshRenderer> _renderers;

        private IPipelineModel _model;
        
            
        public void PassCreateViewRequest(IPipelineModel model) // Invoked from Task thread
        {
            var _nodes = model.NodePositions;
            _nodesCount = _nodes.Count;
            
            for (var i = 1; i < _nodes.Count - 1; i++)
            {
                // Mapping onto horizontal plane.
                var in2d = ((float3)(_nodes[i] - _nodes[i - 1])).xz;
                var out2d = ((float3)(_nodes[i] - _nodes[i + 1])).xz;

                // Heading of the vectors.
                var in2n = math.normalize(in2d);
                var out2n = math.normalize(out2d);

                // Mean heading of vectors.
                var inHdg = math.atan2(in2n.y, in2n.x);
                var outHdg = math.atan2(out2n.y, out2n.x);
                var meanHdg = (inHdg + outHdg) / 2f;

                // 3D mean vector between arms.
                var meanVec3d = new float3(math.cos(meanHdg), 0, math.sin(meanHdg));
            
                // Scalar making mean vector point onto crossing of corresponding line mappings.
                var crossingScalar = 1 / (math.sin(((inHdg - outHdg) / 2f)));
            
                // Total unitary shift from original node.
                var shift = meanVec3d * crossingScalar * Separation;

                for (var pipeIndex = 0; pipeIndex < _dispatchers.Count; pipeIndex++)
                {
                    // Scalar for shift corresponding to specific pipe index.
                    var pipeShiftScalar = GetScalar(pipeIndex, _dispatchers.Count);

                    // Sub-node specific info.
                    var subNodePoint = new float3(_nodes[i]) + shift * pipeShiftScalar;
                    var subNodeRadius = Separation + RoundizatorFlat + 
                        pipeShiftScalar * math.abs(crossingScalar) * Separation * RoundizatorMult;
                
                    _nodesBuffers[pipeIndex][i] = new float4(subNodePoint, subNodeRadius);
                }
            }

            _model = model;
            _state = ViewState.WaitingForDispatching;
        }
        
        private void SetupNodeDirectly(int nodeIndex, float3 delta, float3 nodePos, float roundization)
        {
            var rightVector = math.cross(new float3(0f,1f,0f),delta);
            rightVector = math.normalize(rightVector);
            
            for (var pipeIndex = 0; pipeIndex < _dispatchers.Count; pipeIndex++)
            {
                var pipeShiftScalar = GetScalar(pipeIndex, _dispatchers.Count);
                var shift = rightVector * pipeShiftScalar * Separation;
                
                _nodesBuffers[pipeIndex][nodeIndex] = new float4(nodePos + shift, roundization);
            }
        }

        public void PassRecycleRequest() // Invoked from Task thread
        {
            _state = ViewState.WaitingForRecycling;
        }

        public void Update(ConcurrentBag<PipeJobsDispatcher> dispatchers) // Invoked from Main thread
        {
            _state = ViewState.Dispatched;

            var Nodes = _model.NodePositions;
            
            for (var i = 0; i < _dispatchers.Count; i++)
            {
                var firstDelta = Nodes[1] - Nodes[0];
                SetupNodeDirectly(0, firstDelta, Nodes[i],1f);
                
                var lastDelta = Nodes[Nodes.Count-1] - Nodes[Nodes.Count-2];
                SetupNodeDirectly(Nodes.Count - 1, lastDelta, Nodes[i],1f);
                
                _dispatchers[i].SetNodes(_nodesBuffers[i]);
                
                _dispatchers[i].CutMaxAngle = CutMaxAngle;
                _dispatchers[i].MinimalBendAngle = MinimalBendAngle;
                _dispatchers[i].VerticesPerCut = VerticesPerCut;
                _dispatchers[i].PipeRadius = PipeRadius;
                
                
                _dispatchers[i].Dispatch();
            }
        }

        public void LateUpdate() // Invoked from Main thread
        {
            _state = ViewState.Active;
        }

        public bool HandleRecycling(ConcurrentBag<PipeJobsDispatcher> dispatchersPool) // Invoked from Main thread
        {
            if (_state != ViewState.WaitingForRecycling) 
                return false;

            foreach (var dispatcher in _dispatchers)
                dispatchersPool.Add(dispatcher);
                
            _dispatchers.Clear();
            _state = ViewState.Idle;
            
            return true;
        }
            
            
        public enum ViewState
        {
            WaitingForDispatching,
            Dispatched,
            Active,
            WaitingForRecycling,
            Idle,
        }
        
        
        private static float GetScalar(int index, int len){
            return -(len - 1)/2f + index;
        }
    }
}