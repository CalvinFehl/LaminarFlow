using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class SpawnMeshByFloat3Array : MonoBehaviour
{
    [SerializeField] private MeshFilter m_meshFilter;
    [SerializeField] private MeshCollider m_meshCollider;

    //[SerializeField] public SplineSamplerByCrosssection splineSampler3d;

    //private void OnEnable()
    //{
    //    splineSampler3d.OnSampleChanged += BuildMesh;
    //}

    //private void OnDisable()
    //{
    //    splineSampler3d.OnSampleChanged -= BuildMesh;
    //}

    public void BuildMesh(List<CrossSection3d> crossSections, int resolution = 1, int numSplines = 1, bool crosssectionIsClosed = true)
    {
        //var crossSections = splineSampler3d.Get3dVerts(crossSection.p);

        if (m_meshFilter == null) m_meshFilter = GetComponent<MeshFilter>();
        if (m_meshFilter.sharedMesh != null) DestroyImmediate(m_meshFilter.sharedMesh); // Destroy old mesh

        Mesh m = new Mesh();

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        int offset = 0;

        int crosssectionVertices = crossSections[0].p.Length;
        int iterations = crosssectionIsClosed ? crosssectionVertices : crosssectionVertices - 1;
        float crosssectionRelativeCircumference = 0f;
        float[] distanceAtVert = new float[iterations];

        for (int i = 0; i < iterations; i++)
        {
            distanceAtVert[i] = crosssectionRelativeCircumference;
            crosssectionRelativeCircumference += Vector3.Distance(crossSections[0].p[i], crossSections[0].p[(i + 1) % crosssectionVertices]);
        }

        for (int currentSplineIndex = 0; currentSplineIndex < numSplines; currentSplineIndex++) // Iterate splines along SplineContainer
        {
            int splineOffset = currentSplineIndex * resolution;
            splineOffset += currentSplineIndex;

            // Iterate verts and build a face
            for (int currentSplinePoint = 0; currentSplinePoint <= resolution; currentSplinePoint++) // Iterate crosssection-points along Spline
            {
                int vertOffset = currentSplinePoint + splineOffset; // index of current point in crossSections

                for (int i = 0; i < iterations; i++) // Iterate iterations-Times
                {
                    Vector3 p1 = crossSections[vertOffset].p[i];
                    verts.Add(p1);
                    uvs.Add(new Vector2((float)currentSplinePoint / (float)resolution, distanceAtVert[i] / crosssectionRelativeCircumference));

                    if (currentSplinePoint < resolution)
                    {

                        int t1 = offset;
                        int t2 = offset + crosssectionVertices;
                        int t3 = offset + crosssectionVertices + 1;
                        int t4 = offset + crosssectionVertices + 1; // second triangle
                        int t5 = offset + 1;
                        int t6 = offset;

                        if (i == crosssectionVertices - 1)
                        { t3 = offset + 1; t4 = offset + 1; t5 = offset + 1 - crosssectionVertices; }   // correct the last two triangles

                        tris.AddRange(new List<int> { t1, t2, t3, t4, t5, t6 });
                    }

                    offset++;
                }
            }
        }

        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.SetUVs(0, uvs);
        m.RecalculateNormals();

        m_meshFilter.sharedMesh = m;
        if (m_meshCollider != null) m_meshCollider.sharedMesh = m;
    }
}