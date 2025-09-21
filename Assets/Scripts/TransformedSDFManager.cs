using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum SDFShapeType
{
    Sphere,
    Cube,
}

public class TransformedSDFManager
{
    private static TransformedSDFManager _instance;
    
    public static TransformedSDFManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new TransformedSDFManager();
            }
            return _instance;
        }
    }

    private List<TransformedSDF> _transformedSDFList = new List<TransformedSDF>();
    public List<TransformedSDF> trasnformedSDFList => _transformedSDFList;

    public void AddTransformSDF(SDFShapeType shapeType)
    {
        AddTransformSDF(shapeType, new Vector3(0, 0, 0), Quaternion.Euler(0f, 0f, 0f), new Vector3(1, 1, 1));
    }
    
    public void AddTransformSDF(SDFShapeType shapeType, Vector3 pos)
    {
        AddTransformSDF(shapeType, pos, Quaternion.Euler(0f, 0f, 0f), new Vector3(1, 1, 1));
    }
    
    public void AddTransformSDF(SDFShapeType shapeType, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        string path = null;
        if (shapeType == SDFShapeType.Cube)
        {
            path = "Assets/Data/" + "CubeSDF" + ".asset";
        }
        else if (shapeType == SDFShapeType.Sphere)
        {
            path = "Assets/Data/" + "SphereSDF" + ".asset";
        }
        
        if (String.IsNullOrEmpty(path)) return;
        
        SDFData cubeSdfDataAsset = AssetDatabase.LoadAssetAtPath<SDFData>(path);
        TransformedSDF transformedSDF = new TransformedSDF();
        transformedSDF.Init(shapeType, cubeSdfDataAsset.cubeSize, cubeSdfDataAsset.resolution, cubeSdfDataAsset.sdfValueList);
        transformedSDF.SetPosition(pos);
        transformedSDF.SetRotation(rot);
        transformedSDF.SetScale(scale);
        
        _transformedSDFList.Add(transformedSDF);
    }

    public void RemoveTransformSDF(int removeIndex)
    {
        if (_transformedSDFList == null || _transformedSDFList.Count == 0) return;
        if (removeIndex > _transformedSDFList.Count - 1) return;
        _transformedSDFList.RemoveAt(removeIndex);
    }

    public float GetSampleGridSdfValue(Vector3 worldPos)
    {
        float value = float.MaxValue;
        foreach (var transformedSDF in _transformedSDFList)
        {
            value = Mathf.Min(value, transformedSDF.GetSampleGridValue(worldPos));
        }
        return value;
    }

    public float[] GetSceneCubeGridSdfValues(Vector3[] worldPosList)
    {
        float[] gridValues = new float[8];
        for (int i = 0; i < 8; i++)
        {
            gridValues[i] = GetSampleGridSdfValue(worldPosList[i]);
        }
        return gridValues;
    }
}
