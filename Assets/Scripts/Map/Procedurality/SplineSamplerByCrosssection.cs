using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public struct CrossSection3d
{
    public float3[] p;
}

[ExecuteInEditMode]

public class SplineSamplerByCrosssection : MonoBehaviour
{
    [SerializeField] private SpawnMeshByFloat3Array meshGenerator;

    public bool SplineIsDirty = false;
    public bool CrosssectionIsDirty = false;

    public Action OnSampleChanged;
    [SerializeField] private SplineContainer m_splineContainer;

    // Spline Variables
    [SerializeField]
    private int m_resolution = 1;
    public int Resolution { get { return m_resolution; } set { m_resolution = value; SplineIsDirty = true; } }

    [SerializeField]
    private float m_width = 1f, m_height = 1f, m_horizontalOffset = 0f, m_vertivalOffset = 0f;
    public float Width { get { return m_width; } set { m_width = value; SplineIsDirty = true; } }
    public float Height { get { return m_height; } set { m_height = value; SplineIsDirty = true; } }
    public float HorizontalOffset { get { return m_horizontalOffset; } set { m_horizontalOffset = value; SplineIsDirty = true; } }
    public float VerticalOffset { get { return m_vertivalOffset; } set { m_vertivalOffset = value; SplineIsDirty = true; } }

    private AnimationCurve m_previousWidthOverTime = new AnimationCurve(), m_previousHeightOverTime = new AnimationCurve();
    [SerializeField] AnimationCurve widthOverTime;
    [SerializeField] AnimationCurve heightOverTime;

    public int NumSplines => m_splineContainer.Splines.Count;

    // Crosssection Variables
    [SerializeField] private SplineContainer profileSplineContainer;
    private Spline profileSpline => profileSplineContainer.Splines[0];

    [SerializeField]
    private int profileResolution = 1;
    public int ProfileResolution { get { return profileResolution; } set { profileResolution = value; CrosssectionIsDirty = true; } }
    [SerializeField]
    private bool isTunnel = false, crosssectionIsClosed = true;
    public bool IsTunnel { get { return isTunnel; } set { isTunnel = value; CrosssectionIsDirty = true; } }
    public bool CrosssectionIsClosed { get { if (profileSpline != null) { crosssectionIsClosed = profileSpline.Closed; } return crosssectionIsClosed; } // synchronize with profileSpline.Closed
        set { crosssectionIsClosed = value; CrosssectionIsDirty = true; if (profileSpline!= null){ profileSpline.Closed = value; }} }

    private CrossSection2d latestCrosssection2d;

    float3 position;
    float3 forward;
    float3 upVector;

    void OnValidate()
    {
        Resolution = Mathf.Max(1, m_resolution);
        Width = m_width;
        Height = m_height;
        HorizontalOffset = m_horizontalOffset;
        VerticalOffset = m_vertivalOffset;

        ProfileResolution = Mathf.Max(1, profileResolution);
        IsTunnel = isTunnel;
        CrosssectionIsClosed = crosssectionIsClosed;
    }

    private void OnEnable()
    {
        Spline.Changed += OnSplineChanged;
    }
    private void OnDisable()
    {
        Spline.Changed -= OnSplineChanged;
    }

    private void OnSplineChanged(Spline changedSpline, int arg2, SplineModification arg3)
    {
        if (changedSpline == profileSpline)
        {
            CrosssectionIsDirty = true;
        }
        else if (m_splineContainer.Splines.Contains(changedSpline))
        {
            SplineIsDirty = true;
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlaying && !widthOverTime.keys.SequenceEqual(m_previousWidthOverTime.keys))
        {
            SplineIsDirty = true;
            m_previousWidthOverTime = new AnimationCurve(widthOverTime.keys);
        }
        if (!UnityEditor.EditorApplication.isPlaying && !heightOverTime.keys.SequenceEqual(m_previousHeightOverTime.keys))
        {
            SplineIsDirty = true;
            m_previousHeightOverTime = new AnimationCurve(heightOverTime.keys);
        }
#endif

        if (CrosssectionIsDirty) // Update Latest Crosssection and set SplineIsDirty to true
        {
            Sample2dOffsetAlongProfileSpline(profileSpline, profileResolution, isTunnel);
            SplineIsDirty = true;
            CrosssectionIsDirty = false;
        }

        if (SplineIsDirty) // Update Mesh
        {
            meshGenerator.BuildMesh(Sample2dOffsetArrayAlongSpline(latestCrosssection2d.p, m_splineContainer, Resolution, HorizontalOffset, VerticalOffset, Width, Height, widthOverTime, heightOverTime), Resolution, NumSplines, CrosssectionIsClosed);

            SplineIsDirty = false;
        }
    }

    // Turns a Spline into a CrossSection2d
    public CrossSection2d Sample2dOffsetAlongProfileSpline(Spline profile, int profileResolution = 0, bool isTunnel = false)
    {
        var crossSection = new CrossSection2d();
        int splineKnotCount = profile.Knots.Count();

        profileResolution = profileResolution < 3? 3 : profileResolution;
        var crossSectionElements = new List<float3>();

        // Get the amount of curves in the spline
        var curveCount = profile.GetCurveCount();

        // Iterate over each knot
        for (int i = 0; i < curveCount; i++)
        {
            var curve = profile.GetCurve(i);

            // sample the curve with the given resolution
            for (int resSetp = 0; resSetp < profileResolution; resSetp++)
            {
                // calculate the current t value for the curve
                var currentCurveT = (float)resSetp / (float)profileResolution;

                // get the spline t value for the current curve
                // the id of the curve is i and the t value is currentCurveT
                // the integer part of the t value is the id of the curve
                // and the decimal part is the t value for the curve
                var splineT = profile.CurveToSplineT(i+currentCurveT);

                // evaluate the position of the spline at the given spline t value
                var pos = profile.EvaluatePosition(splineT);
                crossSectionElements.Add(pos);
            }
        }

        crossSection.p = crossSectionElements.Select(x => new float2(x.x, x.z)).ToArray();
        if (!isTunnel) { crossSection.p = crossSection.p.Reverse().ToArray(); }

        latestCrosssection2d = crossSection;
        return crossSection;
    }

    // turns a CrossSection2d into a List of CrossSection3ds by adding (eventually Dimension-stretched) CrossSection3ds (calculated by Sample2dOffsetAlongSpline) along the spline
    public List<CrossSection3d> Sample2dOffsetArrayAlongSpline(float2[] _vertOffsets, SplineContainer _splineContainer, int _resolution = 1, float _horizontalOffset = 0f, float _verticalOffset = 0f, float _width = 1f, float _height = 1f, AnimationCurve _widthOverTime = null, AnimationCurve _heightOverTime = null)
    {
        var locationOfVertices = new List<CrossSection3d>();

        float step = 1f / (float)_resolution;

        for (int j = 0; j < _splineContainer.Splines.Count; j++) // j = spline in SplineContainer
        {
            for (int i = 0; i < (float)_resolution; i++) // t = position along spline
            {
                float t = i * step;
                locationOfVertices.Add(Sample2dOffsetAlongSpline(_vertOffsets, _splineContainer, t, j, _horizontalOffset, _verticalOffset, DimensionsByAnimationCurve(t, _widthOverTime, _heightOverTime, _width, _height))); // calculate crosssection at t
            }
            locationOfVertices.Add(Sample2dOffsetAlongSpline(_vertOffsets, _splineContainer, 1, j, _horizontalOffset, _verticalOffset, DimensionsByAnimationCurve(1, _widthOverTime, _heightOverTime, _width, _height))); // calculate crosssection at 1
        }
        return locationOfVertices;
    }

    // turn an float2 Array into a Crosssection3d by Sampling it along the Spline
    private CrossSection3d Sample2dOffsetAlongSpline(float2[] _vertOffsets, SplineContainer _splineContainer, float t,  int _splineNumber = 0, float _horizontalOffset = 0f, float _verticalOffset = 0f, Vector2 _dimensions = default)
    {
        CrossSection3d _crossSection3d = new()
        {
            p = new float3[_vertOffsets.Length]
        };

        _dimensions = _dimensions == default ? new Vector2(Width, Height) : _dimensions;
        _splineContainer[_splineNumber].Evaluate(t, out position, out forward, out upVector);

        if (forward.Equals(Vector3.zero) || upVector.Equals(Vector3.zero))
        {
            float tShift = t == 1 ? -0.0001f : 0.0001f;
            _splineContainer[_splineNumber].Evaluate(t + tShift, out position, out forward, out upVector);
        }
        float3 right = Vector3.Cross(forward, upVector).normalized;

        for (int k = 0; k < _vertOffsets.Length; k++) // k = vertex along crosssection/vertOffsets
        {
            Vector2 offset = _vertOffsets[k];
            offset *= _dimensions;
            _crossSection3d.p[k] = position + right * (offset.x + _horizontalOffset) + upVector * (offset.y + _verticalOffset);
        }
        return _crossSection3d;
    }

    // Stretch dimensions by current t of AnimationCurve
    private Vector2 DimensionsByAnimationCurve(float t, AnimationCurve _widthOverTime = null, AnimationCurve _heightOverTime = null, float _width = 1f, float _height = 1f)
    {
        Vector2 dimensions = new Vector2(_width, _height);

        if (_widthOverTime != null)
        { dimensions.x = _width * _widthOverTime.Evaluate(t); }
        if (_heightOverTime != null)
        { dimensions.y = _height * _heightOverTime.Evaluate(t); }

        return dimensions;
    }
}
