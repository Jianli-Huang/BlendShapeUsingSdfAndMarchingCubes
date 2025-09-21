using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SDFData", menuName = "Data/SDF Data", order = 1)]
public class SDFData : ScriptableObject
{
    public int cubeSize;
    public int resolution;
    public List<float> sdfValueList;

    public void Initialize(int cubeSize, int resolution)
    {
        this.cubeSize = cubeSize;
        this.resolution = resolution;
        this.sdfValueList = new List<float>();
    }
}
