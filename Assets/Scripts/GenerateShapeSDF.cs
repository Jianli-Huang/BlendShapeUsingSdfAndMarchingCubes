using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class GenerateShapeSDF : MonoBehaviour
{
    [Range(1, 5)]public int cubeSize = 3;
    [Range(2, 100)] public int resolution = 20;     // cubeSize边长的点数
    
    public Transform targetTrans;
    private MeshFilter _targetMeshFilter;
    
    public ComputeShader computeShader;
    private ComputeBuffer _bvhBuffer;
    private ComputeBuffer _triangleBuffer;
    private ComputeBuffer _sdfBuffer;
    private List<BVHNode> _bvhNodes;
    private List<Triangle> _triangles;
    private float[] _sdfData;

    public string sdfAssetName;
    
    //test
    private float[,,] sdfGrid; // SDF数据网格
    private BlendShapeWithSDF _blendShape;
    private BlendSceneShapeWithSDF _blendSceneShape;
    
    struct Triangle
    {
        public Vector3 a, b, c;
    }
    
    struct BVHNode
    {
        public Vector3 min;
        public Vector3 max;
        public int leftChild;
        public int rightChild;
        public int triangleStart;
        public int triangleCount;
    }

    private void Awake()
    {
        _targetMeshFilter = targetTrans.GetComponent<MeshFilter>();
    }

    void Start()
    {
        GenerateSDFWithBVH();
    }
    
    void GenerateSDFWithBVH()
    {
        Mesh mesh = _targetMeshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        _triangles = new List<Triangle>();

        for (int i = 0; i < triangles.Length; i+= 3)
        {
            Triangle tri = new Triangle()
            {
                a = vertices[triangles[i]],
                b = vertices[triangles[i + 1]],
                c = vertices[triangles[i + 2]],
            };
            _triangles.Add(tri);
        }
        
        //构建BVH
        _bvhNodes = new List<BVHNode>();
        _bvhNodes.Add(new BVHNode());
        RecursiveBuildBVH(0, 0, _triangles.Count, 0);
        
        // BVH节点缓冲区
        _bvhBuffer = new ComputeBuffer(_bvhNodes.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(BVHNode)));
        _bvhBuffer.SetData(_bvhNodes);

        // 三角形缓冲区
        _triangleBuffer = new ComputeBuffer(_triangles.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangle)));
        _triangleBuffer.SetData(_triangles);

        // SDF结果缓冲区
        int cnt = resolution  * resolution  * resolution;
        _sdfBuffer = new ComputeBuffer(cnt, sizeof(float));
        _sdfData = new float[cnt];
        _sdfBuffer.SetData(_sdfData);
        
        int kernel = computeShader.FindKernel("CSMain");
        computeShader.SetBuffer(kernel, "_BVHNodes", _bvhBuffer);
        computeShader.SetBuffer(kernel, "_Triangles", _triangleBuffer);
        computeShader.SetBuffer(kernel, "_SDFBuffer", _sdfBuffer);
        computeShader.SetInt("_CubeSize", cubeSize);
        computeShader.SetInt("_Resolution", resolution);
        
        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(kernel, groups, groups, groups);
        
        // 从GPU读取数据回CPU
        _sdfBuffer.GetData(_sdfData);

        // save to scriptable object
        SDFData sdfDataAsset = ScriptableObject.CreateInstance<SDFData>();
        sdfDataAsset.Initialize(cubeSize, resolution);
        
        for (int z = 0; z < resolution; z++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int index = x + y * resolution + z * resolution * resolution;
                    sdfDataAsset.sdfValueList.Add(_sdfData[index]);
                }
            }
        }
        
        string path = "Assets/Data/" + sdfAssetName + ".asset";
        Directory.CreateDirectory(Application.dataPath + "/Data");
        AssetDatabase.CreateAsset(sdfDataAsset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _blendShape = transform.AddComponent<BlendShapeWithSDF>();
        _blendShape.SetSdfFileName(sdfAssetName);
        //_blendSceneShape = transform.AddComponent<BlendSceneShapeWithSDF>();
    }

    void RecursiveBuildBVH(int nodeIndex, int start, int end, int depth)
    {
        Vector3 min = Vector3.one * float.MaxValue;
        Vector3 max = Vector3.one * float.MinValue;
        for (int i = start; i < end; i++)
        {
            Triangle tri = _triangles[i];
            ExpandBoundingBox(tri.a, ref min, ref max);
            ExpandBoundingBox(tri.b, ref min, ref max);
            ExpandBoundingBox(tri.c, ref min, ref max);
        }
        
        BVHNode node = new BVHNode();
        node.min = min;
        node.max = max;
        node.triangleStart = start;
        node.triangleCount = end - start;

        if (end - start <= 5 || depth > 10)
        {
            node.leftChild = node.rightChild = -1;
            _bvhNodes[nodeIndex] = node;
            return;
        }

        Vector3 size = max - min;
        int splitAxis = 0;
        if (size.y > size.x && size.y > size.z) splitAxis = 1;
        else if (size.z > size.x && size.z > size.y) splitAxis = 2;
        
        _triangles.Sort(start, end - start, Comparer<Triangle>.Create((a, b) => 
        {
            Vector3 centerA = (a.a + a.b + a.c) / 3f;
            Vector3 centerB = (b.a + b.b + b.c) / 3f;
            return centerA[splitAxis].CompareTo(centerB[splitAxis]);
        }));

        int mid = start + (end - start) / 2;
        
        node.leftChild = _bvhNodes.Count;
        _bvhNodes.Add(new BVHNode());
        node.rightChild = _bvhNodes.Count;
        _bvhNodes.Add(new BVHNode());

        RecursiveBuildBVH(node.leftChild, start, mid, depth + 1);
        RecursiveBuildBVH(node.rightChild, mid, end, depth + 1);

        _bvhNodes[nodeIndex] = node;
    }

    void ExpandBoundingBox(Vector3 point, ref Vector3 min, ref Vector3 max)
    {
        min = Vector3.Min(min, point);
        max = Vector3.Max(max, point);
    }

    private void OnDestroy()
    {
        _bvhBuffer.Release();
        _bvhBuffer = null;
        _triangleBuffer.Release();
        _triangleBuffer = null;
        _sdfBuffer.Release();
        _sdfBuffer = null;
        _bvhNodes.Clear();
        _triangles.Clear();
    }
}
