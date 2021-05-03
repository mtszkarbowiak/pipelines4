using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace AuroraSeeker.Pipelines4
{
    [Unity.Burst.BurstCompile]
    public struct PipeMeshJob : IJobParallelFor
    {
        private const float MinRadius = 0.1f;
        private const int MinVertsPerCut = 4, MaxVertsPerCut = 32;
        
        
        [ReadOnly] public int VertsPerCut;
        [ReadOnly] public float PipeRadius;
        [ReadOnly] public NativeList<Cut> Cuts;
        [WriteOnly][NativeDisableParallelForRestriction] public NativeArray<ushort> TrIndexes;  
        [WriteOnly][NativeDisableParallelForRestriction] public NativeArray<Vertex> Vertices;


        public bool ValidateBeforeExecution()
        {
            if (PipeRadius < MinRadius) return false;

            if (VertsPerCut < MinVertsPerCut) return false;

            if (VertsPerCut > MaxVertsPerCut) return false;
            
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

        
        
        public void Execute(int index)
        {
            if(index >= Cuts.Length) return;

            AddVerts(index);
            
            if(index < Cuts.Length - 1 )
                AddTrIndexes(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddVerts(int cut)
        {
            // Shift of all indices of vertices of this cut.
            var shift = cut * VertsPerCut;
            
            // Radians angle of one cut 'slice' (per vertex, except of last one).
            var angleMult = math.PI * 2 / (VertsPerCut - 1);

            // Anti number to be multiplied by spline lenght to get UV coordinate.
            var lenghtAntiMult = PipeRadius * math.PI * 2; 
            
            for (var vtx = 0; vtx < VertsPerCut; vtx++)
            {
                // Angle of vertex.
                var angle = vtx * angleMult;

                // Local position.
                var x = math.cos(angle);
                var y = math.sin(angle);
                
                // Transform normal vector.
                var normal = math.mul( Cuts[cut].Matrix, new float3(x,  y, 0) );
                var position = normal * PipeRadius + Cuts[cut].Origin;

                // Calculate UV mapping.
                var uvX = Cuts[cut].Lenght / lenghtAntiMult;
                var uvY = vtx / (VertsPerCut - 1.0f);
                
                Vertices[shift + vtx] = new Vertex
                {
                    Position = position,
                    Normals = normal,
                    UVs = new float2(uvX, uvY),
                    Tangents = new float4(uvY, uvX, 0f, 1f)
                };
            }
        }

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