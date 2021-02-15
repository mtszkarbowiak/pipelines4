using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Pipelines4
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Cut
    {
        public float3x3 Matrix;
        public float3 Origin;
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