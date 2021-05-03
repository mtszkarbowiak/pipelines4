using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace AuroraSeeker.Pipelines4
{
    /// <summary>Defines local space in the vicinity of a point on spline.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Cut
    {
        /// <summary>Local space matrix.</summary>
        public float3x3 Matrix;
        
        /// <summary>Point located on a spline translating all cut's points.</summary>
        public float3 Origin;
        
        /// <summary>Total traversed distance by a spline.</summary>
        public float Lenght;

        public void DrawGizmos( float size = 0.1f )
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(Origin, 0.1f * size );
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(Origin,Origin + math.mul( Matrix,new float3(1,0,0)) * size);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(Origin,Origin + math.mul( Matrix,new float3(0,1,0)) * size);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(Origin,Origin + math.mul( Matrix,new float3(0,0,1)) * size);
        }
    }
}