using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace Pipelines4
{
    [StructLayout(LayoutKind.Sequential)]
    public struct UniversalVertex
    {
        public float3 Position;
        public float3 Normals;
        public float4 Tangents;
        public float2 UVs;
        
        public readonly static VertexAttributeDescriptor[] Layout = {
            new VertexAttributeDescriptor(VertexAttribute.Position),
            new VertexAttributeDescriptor(VertexAttribute.Normal),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };
    }
}