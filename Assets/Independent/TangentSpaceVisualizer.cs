using UnityEngine;

namespace Independent
{
    public class TangentSpaceVisualizer : MonoBehaviour
    {
        [Range(0, 0.1f)] public float offset = 0.01f;
        [Range(0, 5f)] public float scale = 0.1f;

        private MeshFilter _filter;

        private void Awake()
        {
            _filter = GetComponent<MeshFilter>();
        }

        private void OnDrawGizmos()
        {
            if (!_filter) return;

            var mesh = _filter.sharedMesh;

            if (!mesh) return;

            ShowTangentSpace(mesh);
        }

        private void ShowTangentSpace(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var tangents = mesh.tangents;
            for (var i = 0; i < vertices.Length; i++)
            {
                ShowTangentSpace(
                    transform.TransformPoint(vertices[i]),
                    transform.TransformDirection(normals[i]),
                    transform.TransformDirection(tangents[i]),
                    tangents[i].w
                );
            }
        }

        private void ShowTangentSpace(Vector3 vertex, Vector3 normal, Vector3 tangent, float binormalSign)
        {
            vertex += normal * offset;

            var binormal = Vector3.Cross(normal, tangent) * binormalSign;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(vertex, vertex + normal * scale);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(vertex, vertex + tangent * scale);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(vertex, vertex + binormal * scale);
        }
    }
}