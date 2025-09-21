using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class TransformedSDF
{
    public SDFShapeType shapeType;
    public int cubeSize;
    public int resolution;
    public List<float> sdfValueList;
    public Vector3 boundMin;
    public Vector3 boundMax;
    public Vector3 boundSize => boundMax - boundMin;

    private float _halfSize;
    private float _stepSize;

    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public Matrix4x4 LocalToWorldMatrix => Matrix4x4.TRS(position, rotation, scale);
    public Matrix4x4 WorldToLocalMatrix => LocalToWorldMatrix.inverse;

    public void Init(SDFShapeType shapeType, int cubeSize, int resolution, List<float> sdfValueList)
    {
        this.shapeType = shapeType;
        this.cubeSize = cubeSize;
        this.resolution = resolution;
        _halfSize = (float)cubeSize / 2;
        _stepSize = (float)cubeSize / (resolution - 1);
        this.sdfValueList = sdfValueList;
        boundMin = new Vector3(-_halfSize, -_halfSize, -_halfSize);     //GetGridPos(0, 0, 0);
        boundMax = new Vector3(_halfSize, _halfSize, _halfSize);        //GetGridPos(resolution - 1, resolution - 1, resolution - 1);
        
        position = Vector3.zero;
        rotation = quaternion.identity;
        scale = Vector3.one;
    }

    public void SetPosition(Vector3 pos)
    {
        position = pos;
    }

    public void SetScale(Vector3 scale)
    {
        this.scale = scale;
    }

    public void SetRotation(Quaternion rot)
    {
        rotation = rot;
    }

    public float GetSampleGridValue(Vector3 worldPos)
    {
        Vector3 localPos = WorldToLocalMatrix.MultiplyPoint3x4(worldPos);
        
        if (!IsInBounds(localPos))
        {
            // 对于边界外的点，返回一个大的正值（表示远离表面）
            return float.MaxValue;
        }
        
        Vector3 normalized = new Vector3(
            (localPos.x - boundMin.x) / boundSize.x,
            (localPos.y - boundMin.y) / boundSize.y,
            (localPos.z - boundMin.z) / boundSize.z
        );
        normalized.x = Mathf.Clamp01(normalized.x);
        normalized.y = Mathf.Clamp01(normalized.y);
        normalized.z = Mathf.Clamp01(normalized.z);
        
        // 计算数据索引
        float xFloat = normalized.x * (resolution - 1);
        float yFloat = normalized.y * (resolution - 1);
        float zFloat = normalized.z * (resolution - 1);
        
        int x = Mathf.FloorToInt(xFloat);
        int y = Mathf.FloorToInt(yFloat);
        int z = Mathf.FloorToInt(zFloat);

        Vector3 delta = new Vector3(xFloat - x, yFloat - y, zFloat - z);

        float value = 0;

        int nextX = x + 1 <= resolution - 1 ? x + 1 : x;
        int nextY = y + 1 <= resolution - 1 ? y + 1 : y;
        int nextZ = z + 1 <= resolution - 1 ? z + 1 : z;
        float v0 = GetGridValue(x, y, z);            
        float v1 = GetGridValue(nextX, y, z);        
        float v2 = GetGridValue(nextX, y, nextZ);    
        float v3 = GetGridValue(x, y, nextZ);        
        float v4 = GetGridValue(x, nextY, z);        
        float v5 = GetGridValue(nextX, nextY, z);    
        float v6 = GetGridValue(nextX, nextY, nextZ);
        float v7 = GetGridValue(x, nextY, nextZ);

        float lerp1 = Mathf.Lerp(v0, v1, delta.x);
        float lerp2 = Mathf.Lerp(v4, v5, delta.x);
        float lerp12 = Mathf.Lerp(lerp1, lerp2, delta.y);
        
        float lerp3 = Mathf.Lerp(v3, v2, delta.x);
        float lerp4 = Mathf.Lerp(v7, v6, delta.x);
        float lerp34 = Mathf.Lerp(lerp3, lerp4, delta.y);

        value = Mathf.Lerp(lerp12, lerp34, delta.z);
        
        return value;
    }
    
    private bool IsInBounds(Vector3 localPos)
    {
        return localPos.x >= boundMin.x && localPos.x <= boundMax.x &&
               localPos.y >= boundMin.y && localPos.y <= boundMax.y &&
               localPos.z >= boundMin.z && localPos.z <= boundMax.z;
    }

    Vector3 GetGridPos(int x, int y, int z)
    {
        Vector3 pos = new Vector3(x, y, z) * _stepSize - new Vector3(_halfSize, _halfSize, _halfSize);
        return pos;
    }
    
    int GetGridIndex(int x, int y, int z)
    {
        int index = x + y * resolution + z * resolution * resolution;
        return index;
    }

    float GetGridValue(int x, int y, int z)
    {
        int index = GetGridIndex(x, y, z);
        return sdfValueList[index];
    }
    
}
