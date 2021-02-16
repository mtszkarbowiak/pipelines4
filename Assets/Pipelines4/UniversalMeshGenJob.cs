using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.UIElements;

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
        [WriteOnly][NativeDisableParallelForRestriction] public NativeArray<ushort> TrIndexes;  
        [WriteOnly][NativeDisableParallelForRestriction] public NativeArray<UniversalVertex> Vertices;


        public bool ValidateBeforeExecution()
        {
            if (Radius < MIN_RADIUS) return false;

            if (VertsPerCut < MIN_VERTS_PER_CUT) return false;

            if (VertsPerCut > MAX_VERTS_PER_CUT) return false;
            
            return true;
        }

        public int GetVerticesBufferSize()
        {
            return Cuts.Length * VertsPerCut;
        }

        public int GetTrIndexesBufferSize()
        {
            return (Cuts.Length - 1) * (VertsPerCut - 1) * 3 * 2;
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

            // Anti number to be multiplied by spline lenght to get UV coordinate.
            var lenghtAntiMult = Radius * math.PI * 2; 
            
            for (var vtx = 0; vtx < VertsPerCut; vtx++)
            {
                // Angle of vertex.
                var angle = vtx * angleMult;

                // Local position.
                var x = math.cos(angle);
                var y = math.sin(angle);
                
                // Transform normal vector.
                var normal = math.mul( Cuts[cut].Matrix, new float3(x,  y, 0) );
                var position = normal * Radius + Cuts[cut].Origin;

                // Calculate UV mapping.
                var uvX = Cuts[cut].Lenght / lenghtAntiMult;
                var uvY = vtx / (VertsPerCut - 1.0f);
                
                Vertices[shift + vtx] = new UniversalVertex
                {
                    Position = position,
                    Normals = normal,
                    UVs = new float2(uvX, uvY),
                };
            }
        }

        #if AGGRESSIVE_COMPILATION
        [BurstCompile][MethodImpl(MethodImplOptions.AggressiveInlining)]
        #endif
        private void AddTrIndexes(int cut)
        {
            // Get shifts for entire cut.
            var cutTrIndexShift = (VertsPerCut - 1) * cut * 6;
            var cutVertexShift = VertsPerCut * cut;

            for (var s = 0; s < VertsPerCut - 1; s++)
            {
                // Get shifts for a slice
                var sliceIndexShift = cutTrIndexShift + s * 6;
                var sliceVertexShift = cutVertexShift + s;
                
                // Calculate indices of vertices
                var ll = sliceVertexShift;
                var lh = sliceVertexShift + 1;
                var hl = sliceVertexShift + VertsPerCut;
                var hh = sliceVertexShift + VertsPerCut + 1;

                // Assign them.
                TrIndexes[sliceIndexShift + 0] = (ushort) hh;
                TrIndexes[sliceIndexShift + 1] = (ushort) lh;
                TrIndexes[sliceIndexShift + 2] = (ushort) ll;
                TrIndexes[sliceIndexShift + 3] = (ushort) ll;
                TrIndexes[sliceIndexShift + 4] = (ushort) hl;
                TrIndexes[sliceIndexShift + 5] = (ushort) hh;
            }
        }
    }
}