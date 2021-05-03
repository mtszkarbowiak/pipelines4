using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AuroraSeeker.Pipelines4
{
    public class MultiPipe : MonoBehaviour
    {
        public float3[] Nodes;
        
        [Tooltip("Distance between pipes")]
        public float Separation;
        
        [Range(0f,1f)][Tooltip("Multiplicative component for calculation of bend radius.")]
        public float RoundizatorMult = 1;
        
        [Tooltip("Linear component for calculation of bend radius.")]
        public float RoundizatorFlat = 1f;
        
        [Tooltip("")]
        public float CutMaxAngle = 0.15f;
        
        
        public float MinimalBendAngle = 0.05f;
        
        [Tooltip("True number of vertices per one cut of one pipe. Visible number is smaller by one.")]
        public int VerticesPerCut = 7;
        
        public float PipeRadius = 0.2f;
        
        private MeshFilter[] _pipeRenderers;
        private PipeJobsDispatcher[] _dispatchers;
        private float4[][] _nodesBuffers;
        
        
        private void Awake()
        {
            _pipeRenderers = GetComponentsInChildren<MeshFilter>().ToArray();

            _dispatchers = new PipeJobsDispatcher[_pipeRenderers.Length];

            for (var i = 0; i < _pipeRenderers.Length; i++)
                _dispatchers[i] = new PipeJobsDispatcher(Allocator.Persistent);

            _nodesBuffers = new float4[_pipeRenderers.Length][];

            for (var i = 0; i < _nodesBuffers.Length; i++)
                _nodesBuffers[i] = new float4[Nodes.Length];

            foreach (var t in _pipeRenderers)
            {
                t.mesh = new Mesh();
            }
        }

        private void Update()
        {
            for (var i = 1; i < Nodes.Length - 1; i++)
            {
                // Mapping onto horizontal plane.
                var in2d = (Nodes[i] - Nodes[i - 1]).xz;
                var out2d = (Nodes[i] - Nodes[i + 1]).xz;

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

                for (var pipeIndex = 0; pipeIndex < _dispatchers.Length; pipeIndex++)
                {
                    // Scalar for shift corresponding to specific pipe index.
                    var pipeShiftScalar = GetArmScalar(pipeIndex, _dispatchers.Length);

                    // Sub-node specific info.
                    var subNodePoint = Nodes[i] + shift * pipeShiftScalar;
                    var subNodeRadius = Separation + RoundizatorFlat + 
                        pipeShiftScalar * math.abs(crossingScalar) * Separation * RoundizatorMult;
                
                    _nodesBuffers[pipeIndex][i] = new float4(subNodePoint, subNodeRadius);
                }
            }

            for (var i = 0; i < _dispatchers.Length; i++)
            {
                var firstDelta = Nodes[1] - Nodes[0];
                SetupNodeDirectly(0, firstDelta, 1f);
                
                var lastDelta = Nodes[Nodes.Length-1] - Nodes[Nodes.Length-2];
                SetupNodeDirectly(Nodes.Length - 1, lastDelta, 1f);
                
                _dispatchers[i].SetNodes(_nodesBuffers[i]);
                
                _dispatchers[i].CutMaxAngle = CutMaxAngle;
                _dispatchers[i].MinimalBendAngle = MinimalBendAngle;
                _dispatchers[i].VerticesPerCut = VerticesPerCut;
                _dispatchers[i].PipeRadius = PipeRadius;
                
                _dispatchers[i].Dispatch();
            }
        }

        private void SetupNodeDirectly(int nodeIndex, float3 delta, float roundization)
        {
            var rightVector = math.cross(new float3(0f,1f,0f),delta);
            rightVector = math.normalize(rightVector);
            
            for (var pipeIndex = 0; pipeIndex < _dispatchers.Length; pipeIndex++)
            {
                var pipeShiftScalar = GetArmScalar(pipeIndex, _dispatchers.Length);
                var shift = rightVector * pipeShiftScalar * Separation;
                
                _nodesBuffers[pipeIndex][nodeIndex] = new float4(Nodes[nodeIndex] + shift, roundization);
            }
        }

        private void LateUpdate()
        {
            for (var i = 0; i < _dispatchers.Length; i++)
                _dispatchers[i].Complete(_pipeRenderers[i].sharedMesh);
        }
        
        private void OnDestroy()
        {
            foreach (var dispatcher in _dispatchers)
                dispatcher.Dispose();
        }

        private static float GetArmScalar(int index, int len){
            return -(len - 1)/2f + index;
        }

        private void OnDrawGizmos()
        {
            DrawOwnGizmos();

            if (_nodesBuffers == null) return; /* METHOD CUTTER */
            
            foreach (var dispatcher in _dispatchers)
                dispatcher.DrawGizmos();
        }

        private void DrawOwnGizmos()
        {
            Gizmos.color = Color.yellow;
            for (var i = 0; i < Nodes.Length - 1; i++)
                Gizmos.DrawLine(Nodes[i],Nodes[i+1]);
        
            for (var i = 1; i < Nodes.Length - 1; i++)
            {
                var in3 = Nodes[i] - Nodes[i - 1];
                var out3 = Nodes[i] - Nodes[i + 1];

                var in2 = in3.xz;
                var out2 = out3.xz;

                var in2n = math.normalize(in2);
                var out2n = math.normalize(out2);

                var inA = math.atan2(in2n.y, in2n.x);
                var outA = math.atan2(out2n.y, out2n.x);

                var midA = (inA + outA) / 2f;

                var mid = new float3(math.cos(midA), 0, math.sin(midA));
            
                var midSigned = mid;
                var midScalar = 1 / (math.sin(((inA - outA) / 2f)));
                midSigned *= midScalar;

                Gizmos.DrawLine(Nodes[i],Nodes[i]+midSigned/2); 
            }
        }
    }
}
