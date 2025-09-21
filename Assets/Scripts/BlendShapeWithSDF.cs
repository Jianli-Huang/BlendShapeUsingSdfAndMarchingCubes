using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BlendShapeWithSDF : MonoBehaviour
{
    public float isoValue = 0;
    //private float[,,] sdfGrid;

    private int _cubeSize;
    private int _resolution;
    private List<float> _sdfValueList;
    private float _halfSize => (float)_cubeSize / 2;
    private float _stepSize => (float)_cubeSize / (_resolution - 1);
    
    private Dictionary<string, int> vertexCache;
    private List<Vector3> vertices;
    private List<int> triangles;

    public string sdfFileName = "CubeSDF";
    
    void Awake()
    {
        vertices = new List<Vector3>();
        triangles = new List<int>();
        vertexCache = new Dictionary<string, int>();
    }
    
    void Start()
    {
        LoadSdfData();
    }

    public void SetSdfFileName(string name)
    {
        sdfFileName = name;
    }
    
    void LoadSdfData()
    {
        string path = "Assets/Data/" + sdfFileName + ".asset";  // CubeSDF
        SDFData sdfDataAsset = AssetDatabase.LoadAssetAtPath<SDFData>(path);
        ConstructShape(sdfDataAsset.cubeSize, sdfDataAsset.resolution, sdfDataAsset.sdfValueList);
    }
    
    void ConstructShape(int cubeSize, int resolution, List<float> sdfValueList)
    {
        GameObject marchingCubes = new GameObject("Marching Cubes");
        marchingCubes.transform.SetParent(transform);
        var meshRenderer = marchingCubes.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var meshFilter = marchingCubes.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.name = "Marching Cubes Mesh";
        meshFilter.mesh = mesh;
        
        _cubeSize = cubeSize;
        _resolution = resolution;
        _sdfValueList = sdfValueList;
        
        for (int z = 0; z < resolution - 1; z++)
        {
            for (int y = 0; y < resolution - 1; y++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    float[] cubeValues = new float[8];
                    cubeValues[0] = GetGridSdfValue(x, y, z);                   
                    cubeValues[1] = GetGridSdfValue(x + 1, y, z);              
                    cubeValues[2] = GetGridSdfValue(x + 1, y, z + 1);        
                    cubeValues[3] = GetGridSdfValue(x, y, z + 1);              
                    cubeValues[4] = GetGridSdfValue(x, y + 1, z);              
                    cubeValues[5] = GetGridSdfValue(x + 1, y + 1, z);        
                    cubeValues[6] = GetGridSdfValue(x + 1, y + 1, z + 1);       
                    cubeValues[7] = GetGridSdfValue(x, y + 1, z + 1);          

                    int configIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (cubeValues[i] < isoValue) // cubeValues[i] < isoValue , 如果sdfGrid没有一个小于或等于isoValue那就无法构成
                            configIndex |= (1 << i);
                    }

                    int edgeMask = MarchingCubesData.edgeTable[configIndex];
                    if (edgeMask == 0) continue;

                    // 4. 为当前立方体存储12条边的顶点索引
                    int[] edgeVertexIndices = new int[12];
                    for (int edge = 0; edge < 12; edge++)
                    {
                        if ((edgeMask & (1 << edge)) == 0)
                        {
                            edgeVertexIndices[edge] = -1;
                            continue;
                        }
          
                        string cacheKey = GetCachedKey(x, y, z, edge);
                        if (vertexCache.TryGetValue(cacheKey, out int cachedIndex))
                        {
                            edgeVertexIndices[edge] = cachedIndex;
                        }
                        else
                        {
                            Vector3 vertexPos = CalculateEdgeVertexPos(x, y, z, edge, cubeValues);
                            
                            vertices.Add(vertexPos);
                            int newIndex = vertices.Count - 1;
                            edgeVertexIndices[edge] = newIndex;
                            vertexCache.Add(cacheKey, newIndex);
                        }
                    }

                    for (int i = 0; i < 16; i += 3)
                    {
                        if (i == 15) break;
                        
                        int edge0 = MarchingCubesData.triTable[configIndex, i];
                        int edge1 = MarchingCubesData.triTable[configIndex, i + 1];
                        int edge2 = MarchingCubesData.triTable[configIndex, i + 2];
                        
                        if (edge0 == -1 || edge1 == -1 || edge2 == -1)
                            break;
                        
                        triangles.Add(edgeVertexIndices[edge0]);
                        triangles.Add(edgeVertexIndices[edge1]);
                        triangles.Add(edgeVertexIndices[edge2]);
                    }
                }
            }
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        
    }

    float GetGridSdfValue(int x, int y, int z)
    {
        int gridIndex = GetGridIndex(x, y, z);
        return _sdfValueList[gridIndex];
    }
    
    int GetGridIndex(int x, int y, int z)
    {
        int index = x + y * _resolution + z * _resolution * _resolution;
        return index;
    }
    
    string GetCachedKey(int x, int y, int z, int edge)
    {
        int baseX = x, baseY = y, baseZ = z;
        char axis = 'X';

        switch (edge)
        {
            case 0: axis = 'X'; break;
            case 1: axis = 'Z'; baseX++; break;
            case 2: axis = 'X'; baseZ++; break;
            case 3: axis = 'Z'; break;
            case 4: axis = 'X'; baseY++; break;
            case 5: axis = 'Z'; baseX++; baseY++; break;
            case 6: axis = 'X'; baseY++; baseZ++; break;
            case 7: axis = 'Z'; baseY++; break;
            case 8: axis = 'Y'; break;
            case 9: axis = 'Y'; baseX++; break;
            case 10: axis = 'Y'; baseX++; baseZ++; break;
            case 11: axis = 'Y'; baseZ++; break;
        }

        return $"{baseX},{baseY},{baseZ},{axis}";
    }
    
    Vector3 CalculateEdgeVertexPos(int x, int y, int z, int edge, float[] cubeValues)
    {
        int v0 = MarchingCubesData.edgeConnections[edge, 0];
        int v1 = MarchingCubesData.edgeConnections[edge, 1];

        float sdf0 = GetCubeGridSdfValue(v0, cubeValues);
        float sdf1 = GetCubeGridSdfValue(v1, cubeValues);
        float t = Mathf.Clamp01((isoValue - sdf0) / (sdf1 - sdf0));
        
        Vector3 pos0 = GetCubeGridWorldPosWithLocalVertexIndex(x, y, z, v0);
        Vector3 pos1 = GetCubeGridWorldPosWithLocalVertexIndex(x, y, z, v1);
        
        Vector3 vertexPos = Vector3.Lerp(pos0, pos1, t);

        return vertexPos;
    }

    float GetCubeGridSdfValue(int vertexIndex, float[] cubeValues)
    {
        return cubeValues[vertexIndex];
    }
    
    Vector3 GetCubeGridWorldPosWithLocalVertexIndex(int x, int y, int z, int vertexIndex)
    {
        x += (vertexIndex == 1 || vertexIndex == 2 || vertexIndex == 5 || vertexIndex == 6) ? 1 : 0;
        y += vertexIndex > 3 ? 1 : 0;
        z += (vertexIndex == 2 || vertexIndex == 3 || vertexIndex == 6 || vertexIndex == 7) ? 1 : 0;
        return GetCubeGridWorldPos(x, y, z);
    }
    
    Vector3 GetCubeGridWorldPos(int x, int y, int z)
    {
        Vector3 pos = new Vector3(x, y, z) * _stepSize - new Vector3(_halfSize, _halfSize, _halfSize);
        return pos;
    }
    
}
