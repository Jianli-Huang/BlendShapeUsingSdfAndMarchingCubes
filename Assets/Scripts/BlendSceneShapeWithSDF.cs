using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// public struct SceneShapeInfo
// {
//     public SDFShapeType shapeType;
//     public 
// }

public class BlendSceneShapeWithSDF : MonoBehaviour
{
    public float isoValue = 0;

    public int sceneCubeSize = 3;
    public int sceneResolution = 21;
    private float _sceneHalfSize => (float)sceneCubeSize / 2;
    private float _sceneStepSize => (float)sceneCubeSize / (sceneResolution - 1);
    
    private Dictionary<string, int> vertexCache;
    private List<Vector3> vertices;
    private List<int> triangles;

    private MeshFilter _meshFilter;
    private bool _dirty;

    //private TransformedSDF _TransformedSDF;
    //private TransformedSDF _TransformedCubeSDF;
    
    void Awake()
    {
        vertices = new List<Vector3>();
        triangles = new List<int>();
        vertexCache = new Dictionary<string, int>();
    }
    
    void Start()
    {
        LoadSDFData();
        ConstructSceneShape();
        //DrawSeneWireframe();
    }

    void Update()
    {
        if (_dirty)
        {
            _dirty = false;
            ConstructSceneMesh();
        }
    }

    public void SetDirty()
    {
        _dirty = true;
    }

    public void AddShape(SDFShapeType shapeType, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        TransformedSDFManager.instance.AddTransformSDF(shapeType, pos, rot, scale);
    }

    public void RemoveShape(int removeIndex)
    {
        TransformedSDFManager.instance.RemoveTransformSDF(removeIndex);
    }

    public List<TransformedSDF> GetTrasnformedSDFList()
    {
        return TransformedSDFManager.instance.trasnformedSDFList;
    }
    
    void LoadSDFData()
    {
        // TransformedSDFManager.instance.AddTransformSDF(SDFShapeType.Cube);
        // TransformedSDFManager.instance.AddTransformSDF(SDFShapeType.Sphere, new Vector3(0.5f, 0, 0.5f));
        // TransformedSDFManager.instance.AddTransformSDF(SDFShapeType.Sphere, new Vector3(-0.5f, 0, 0.5f));
        // TransformedSDFManager.instance.AddTransformSDF(SDFShapeType.Sphere, new Vector3(0.5f, 0, -0.5f));
        // TransformedSDFManager.instance.AddTransformSDF(SDFShapeType.Sphere, new Vector3(-0.5f, 0, -0.5f));
    }

    void DrawSeneWireframe()
    {
        GameObject wireframe = new GameObject("Wireframe");
        wireframe.transform.SetParent(transform);
        var meshRenderer = wireframe.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        meshRenderer.material.color = Color.red;
        var meshFilter = wireframe.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.name = "Wireframe Mesh";
        meshFilter.mesh = mesh;
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();
        
        for (int j = 0; j < sceneResolution; j++)                                                                              
        {                                                                                                                  
            for (int i = 0; i < sceneResolution; i++)                                                                          
            {                                                                                                              
                Vector3 startPos = new Vector3(-_sceneHalfSize + _sceneStepSize * i, -_sceneHalfSize + _sceneStepSize * j, -_sceneHalfSize);           
                Vector3 endPos = new Vector3(-_sceneHalfSize + _sceneStepSize * i, -_sceneHalfSize + _sceneStepSize * j, _sceneHalfSize);              
                vertices.Add(startPos);
                vertices.Add(endPos);
                indices.Add(indices.Count);
                indices.Add(indices.Count);                               
            }                                                                                                              
        }                                                                                                                  
                                                                                                                           
        for (int k = 0; k < sceneResolution; k++)                                                                              
        {                                                                                                                  
            for (int i = 0; i < sceneResolution; i++)                                                                          
            {                                                                                                              
                Vector3 startPos = new Vector3(-_sceneHalfSize + _sceneStepSize * i, -_sceneHalfSize, -_sceneHalfSize + _sceneStepSize * k);           
                Vector3 endPos = new Vector3(-_sceneHalfSize + _sceneStepSize * i, _sceneHalfSize, -_sceneHalfSize + _sceneStepSize * k);              
                vertices.Add(startPos);
                vertices.Add(endPos);
                indices.Add(indices.Count);
                indices.Add(indices.Count);                               
            }                                                                                                              
        }                                                                                                                  
                                                                                                                           
        for (int k = 0; k <= sceneResolution; k++)                                                                              
        {                                                                                                                  
            for (int j = 0; j <= sceneResolution; j++)                                                                          
            {                                                                                                              
                Vector3 startPos = new Vector3(-_sceneHalfSize,-_sceneHalfSize + _sceneStepSize * j,  -_sceneHalfSize + _sceneStepSize * k);           
                Vector3 endPos = new Vector3(_sceneHalfSize, -_sceneHalfSize + _sceneStepSize * j, -_sceneHalfSize + _sceneStepSize * k);              
                vertices.Add(startPos);
                vertices.Add(endPos);
                indices.Add(indices.Count);
                indices.Add(indices.Count);                                     
            }                                                                                                              
        }                                                                                                                  
        
        mesh.SetVertices(vertices);
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();
    }

    void ConstructSceneShape()
    {
        GameObject marchingCubes = new GameObject("Marching Cubes");
        marchingCubes.transform.SetParent(transform);
        var meshRenderer = marchingCubes.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        _meshFilter = marchingCubes.AddComponent<MeshFilter>();
        ConstructSceneMesh();
    }

    void ConstructSceneMesh()
    {
        if (_meshFilter.mesh == null)
        {
            _meshFilter.mesh = new Mesh();
            _meshFilter.mesh.name = "Marching Cubes Mesh";
           
        }
        _meshFilter.mesh.Clear();
        var mesh = _meshFilter.mesh;
        vertices.Clear();
        triangles.Clear();
        vertexCache.Clear();
        
         for (int z = 0; z < sceneResolution - 1; z++)
        {
            for (int y = 0; y < sceneResolution - 1; y++)
            {
                for (int x = 0; x < sceneResolution - 1; x++)
                {
                    Vector3[] worldPosList = GetSceneCubeGridWorldPosList(x, y, z);
                    float[] cubeValues = TransformedSDFManager.instance.GetSceneCubeGridSdfValues(worldPosList);
                    
                    int configIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (cubeValues[i] < isoValue)  // cubeValues[i] < isoValue , 如果sdfGrid没有一个小于或等于isoValue那就无法构成
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
                            Vector3 vertexPos = GetSceneCubeEdgeVertexPos(x, y, z, edge, cubeValues);
                            
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
    
    Vector3 GetSceneCubeEdgeVertexPos(int x, int y, int z, int edge, float[] cubeValues)
    {
        int v0 = MarchingCubesData.edgeConnections[edge, 0];
        int v1 = MarchingCubesData.edgeConnections[edge, 1];
        
        float sdf0 = GetSceneCubeGridSdfValue(v0, cubeValues);
        float sdf1 = GetSceneCubeGridSdfValue(v1, cubeValues);
        float t = Mathf.Clamp01((isoValue - sdf0) / (sdf1 - sdf0));
        
        Vector3 worldPos0 = GetSceneCubeGridWolrdPosWithLocalVertexIndex(x, y, z, v0);
        Vector3 worldPos1 = GetSceneCubeGridWolrdPosWithLocalVertexIndex(x, y, z, v1);

        Vector3 vertexPos = Vector3.Lerp(worldPos0, worldPos1, t);

        return vertexPos;
    }
    
    float GetSceneCubeGridSdfValue(int vertexIndex, float[] cubeValues)
    {
        return cubeValues[vertexIndex];
    }
    
    Vector3 GetSceneCubeGridWolrdPosWithLocalVertexIndex(int x, int y, int z, int vertexIndex)
    {
        x += (vertexIndex == 1 || vertexIndex == 2 || vertexIndex == 5 || vertexIndex == 6) ? 1 : 0;
        y += vertexIndex > 3 ? 1 : 0;
        z += (vertexIndex == 2 || vertexIndex == 3 || vertexIndex == 6 || vertexIndex == 7) ? 1 : 0;
        return GetSceneCubeGridWolrdPos(x, y, z);
    }
    
    Vector3 GetSceneCubeGridWolrdPos(int x, int y, int z)
    {
        Vector3 pos = new Vector3(x, y, z) * _sceneStepSize - new Vector3(_sceneHalfSize, _sceneHalfSize, _sceneHalfSize);
        return pos;
    }

    Vector3[] GetSceneCubeGridWorldPosList(int x, int y, int z)
    {
        Vector3[] worldPosList = new Vector3[8];
        worldPosList[0] = GetSceneCubeGridWolrdPos(x, y, z);
        worldPosList[1] = GetSceneCubeGridWolrdPos(x + 1, y, z);
        worldPosList[2] = GetSceneCubeGridWolrdPos(x + 1, y, z + 1);
        worldPosList[3] = GetSceneCubeGridWolrdPos(x, y, z + 1);
        worldPosList[4] = GetSceneCubeGridWolrdPos(x, y + 1, z);
        worldPosList[5] = GetSceneCubeGridWolrdPos(x + 1, y + 1, z);
        worldPosList[6] = GetSceneCubeGridWolrdPos(x + 1, y + 1, z + 1);
        worldPosList[7] = GetSceneCubeGridWolrdPos(x, y + 1, z + 1);
        return worldPosList;
    }
}
