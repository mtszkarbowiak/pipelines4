using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Pipelines4
{
    public struct HorizontalLayoutGenJob// : IJobParallelFor
    {
        [ReadOnly] public float Separation;
        [ReadOnly] public float Roundization;
        [ReadOnly] public float MaxSlope;
        [ReadOnly] public int PipesCount;
        [ReadOnly] public NativeArray<float3> Nodes;
        
        
        [NativeDisableParallelForRestriction] 
        public NativeArray<float4> SubNodes;

        
        public bool ValidateBeforeExecution()
        {
            return SubNodes.Length == Nodes.Length * PipesCount;
        }
        
        private static float GetScalar(int index, int len)
        {
            return -(len - 1)/2f + index;
        }
    }
}