using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

//#define AGGRESSIVE_COMPILATION

#if AGGRESSIVE_COMPILATION
using System.Runtime.CompilerServices;
using Unity.Burst;
#endif

namespace Pipelines4
{
    public struct UniversalMeshGenJob : IJobParallelFor
    {
        private const float MIN_RADIUS = 0.1f;
        private const int MIN_VERTS_PER_CUT = 4, MAX_VERTS_PER_CUT = 32;
        
        
        [ReadOnly] public int VertsPerCut;
        [ReadOnly] public float Radius;
        [ReadOnly] public NativeList<Cut> Cuts;
        [WriteOnly] public NativeArray<ushort> TrIndexes;  
        [WriteOnly] public NativeArray<UniversalVertex> Vertices;


        public bool ValidateBeforeExecution()
        {
            if (Radius < MIN_RADIUS) return false;

            if (VertsPerCut < MIN_VERTS_PER_CUT) return false;

            if (VertsPerCut > MAX_VERTS_PER_CUT) return false;

            if (TrIndexes.Length != 0) return false;

            if (Vertices.Length != 0) return false;
            
            return true;
        }

        public int GetTrIndexesBufferSize()
        {
            return (Cuts.Length - 1) * (VertsPerCut - 1) * 3 * 2;
        }

        public int GetVerticesBufferSize()
        {
            return Cuts.Length * VertsPerCut;
        }
        
        
        #if AGGRESSIVE_COMPILATION
        [BurstCompile]
        #endif
        public void Execute(int index)
        {
            if(index >= Cuts.Length) return;

            AddVerts(index);
            
            if(index < Cuts.Length - 1 )
                AddTrIndexes(index);
        }

        #if AGGRESSIVE_COMPILATION
        [BurstCompile][MethodImpl(MethodImplOptions.AggressiveInlining)]
        #endif
        private void AddVerts(int cut)
        {
            // Shift of all indices of vertices of this cut.
            var shift = cut * VertsPerCut;
            
            // Radians angle of one cut 'slice' (per vertex, except of last one).
            var angleMult = math.PI * 2 / (VertsPerCut - 1);
            
            for (var vtx = 0; vtx < VertsPerCut; vtx++)
            {
                // Angle of vertex.
                var angle = vtx * angleMult;

                // Local position.
                var x = math.cos(angle) * Radius;
                var y = math.sin(angle) * Radius;
                var localPos = new float3(x, 0.0f, y);
                
                // Transformed position.
                var position = math.mul( Cuts[cut].Matrix, localPos );

                var vertex = new UniversalVertex
                {
                    Position = position,
                    
                    //TODO
                };

                Vertices[shift + vtx] = vertex;
            }
        }

        #if AGGRESSIVE_COMPILATION
        [BurstCompile][MethodImpl(MethodImplOptions.AggressiveInlining)]
        #endif
        private void AddTrIndexes(int index)
        {
            var cutTriShift = index * (VertsPerCut - 1) * 3 * 2;
            var cutVtxShift = index * VertsPerCut;
            
            for (var i = 0; i < VertsPerCut - 1; i++)
            {
                var sliceTriShift = cutTriShift + i * 6;
                var sliceVtxShift = cutVtxShift + i;
                
                TrIndexes[sliceTriShift + 0] = (ushort)(sliceVtxShift + 0);
                TrIndexes[sliceTriShift + 0] = (ushort)(sliceVtxShift + 1);
                TrIndexes[sliceTriShift + 0] = (ushort)(sliceVtxShift + 2);
                TrIndexes[sliceTriShift + 0] = (ushort)(sliceVtxShift + 3);
                TrIndexes[sliceTriShift + 0] = (ushort)(sliceVtxShift + 2);
                TrIndexes[sliceTriShift + 0] = (ushort)(sliceVtxShift + 1);
            }
        }
    }
}