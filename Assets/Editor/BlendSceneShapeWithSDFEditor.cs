using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BlendSceneShapeWithSDF))]
public class BlendSceneShapeWithSDFEditor : Editor
{
    private SDFShapeType shapeType = SDFShapeType.Cube;
    private Vector3 shapePos = Vector3.zero;
    private Quaternion shapeRot = Quaternion.identity;
    private Vector3 shapeScale = Vector3.one;
    private int removeIndex = 0;
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        BlendSceneShapeWithSDF blendSceneShape = target as BlendSceneShapeWithSDF;

        if (Application.isPlaying)
        {
            shapeType = (SDFShapeType)EditorGUILayout.EnumPopup("type", shapeType);
            shapePos = EditorGUILayout.Vector3Field("position", shapePos);
            shapeRot = Quaternion.Euler(EditorGUILayout.Vector3Field("rotation", shapeRot.eulerAngles));
            shapeScale = EditorGUILayout.Vector3Field("scale", shapeScale);
            
            if (GUILayout.Button("Add Shape"))
            {
                blendSceneShape.AddShape(shapeType, shapePos, shapeRot, shapeScale);
                blendSceneShape.SetDirty();
            }
            
            removeIndex = EditorGUILayout.IntField("index", removeIndex);
            if (GUILayout.Button("Remove Shape"))
            {
                blendSceneShape.RemoveShape(removeIndex);
                blendSceneShape.SetDirty();
            }

            EditorGUI.BeginChangeCheck();

            List<TransformedSDF> trasnformedSDFList = blendSceneShape.GetTrasnformedSDFList();
            int cnt = trasnformedSDFList.Count;
            for (int i = 0; i < cnt; i++)
            {
                TransformedSDF transformedSDF = trasnformedSDFList[i];
                EditorGUILayout.LabelField("index", i.ToString());
                EditorGUILayout.LabelField("shapeType", transformedSDF.shapeType.ToString());
                transformedSDF.position = EditorGUILayout.Vector3Field("position", transformedSDF.position);
                EditorGUILayout.Vector3Field("rotation", transformedSDF.rotation.eulerAngles);
                EditorGUILayout.Vector3Field("scale", transformedSDF.scale);
            }


            if (EditorGUI.EndChangeCheck())
            {
                blendSceneShape.SetDirty();
            }
            
        }
    }
}
