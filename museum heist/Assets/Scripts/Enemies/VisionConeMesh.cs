using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisionConeMesh : MonoBehaviour
{
    public TopDownGuardPatrol guard;
    public int segments = 40;
    public float yOffset = 0.02f;

    private Mesh _mesh;

    void Awake()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
    }

    void LateUpdate()
    {
        if (guard == null) return;
        BuildMesh();
    }

    void BuildMesh()
    {
        _mesh.Clear();

        float range = guard.visionRange;
        float angle = guard.visionAngle;

        Vector3 origin = (guard.visionOrigin != null) ? guard.visionOrigin.position : (guard.transform.position + Vector3.up * 1f);

        Vector3[] verts = new Vector3[segments + 2];
        int[] tris = new int[segments * 3];

        // local-space center
        verts[0] = transform.InverseTransformPoint(origin + Vector3.up * yOffset);

        float half = angle * 0.5f;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float a = Mathf.Lerp(-half, half, t);
            Vector3 dirWorld = Quaternion.AngleAxis(a, Vector3.up) * guard.transform.forward;

            Vector3 point = origin + dirWorld * range;

            if (Physics.Raycast(origin, dirWorld, out RaycastHit hit, range, guard.visionMask))
                point = hit.point;

            point += Vector3.up * yOffset;

            verts[i + 1] = transform.InverseTransformPoint(point);
        }

        int tri = 0;
        for (int i = 0; i < segments; i++)
        {
            tris[tri++] = 0;
            tris[tri++] = i + 1;
            tris[tri++] = i + 2;
        }

        _mesh.vertices = verts;
        _mesh.triangles = tris;
        _mesh.RecalculateNormals();
    }
}
