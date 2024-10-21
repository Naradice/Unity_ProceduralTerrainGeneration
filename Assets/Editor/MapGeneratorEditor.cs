using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapManager))]
public class MapGeneratorEditor : Editor
{


    public override void OnInspectorGUI()
    {
        MapManager mapGen = (MapManager)target;

        if(DrawDefaultInspector()){
            if(mapGen.autoUpdate){
                mapGen.DrawMap();
            }
        }

        if (GUILayout.Button("Generate"))
        {
            mapGen.DrawMap();
        }
    }
}
