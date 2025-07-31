using UnityEngine;
using UnityEditor;

public class LightProbeGenerator : EditorWindow
{
    Vector3 center = Vector3.zero;
    Vector3 size = new Vector3(10, 5, 10);
    Vector3Int resolution = new Vector3Int(5, 3, 5);

    [MenuItem("Tools/Light Probe Generator")]
    public static void ShowWindow()
    {
        GetWindow<LightProbeGenerator>("Light Probe Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Light Probe Group Generator", EditorStyles.boldLabel);

        center = EditorGUILayout.Vector3Field("Center", center);
        size = EditorGUILayout.Vector3Field("Size", size);
        resolution = EditorGUILayout.Vector3IntField("Resolution (X,Y,Z)", resolution);

        if (GUILayout.Button("Generate Light Probe Group"))
        {
            GenerateLightProbes();
        }
    }

    void GenerateLightProbes()
    {
        GameObject groupObject = new GameObject("Generated Light Probe Group");
        LightProbeGroup probeGroup = groupObject.AddComponent<LightProbeGroup>();

        Vector3[] positions = new Vector3[
            resolution.x * resolution.y * resolution.z
        ];

        int index = 0;
        for (int x = 0; x < resolution.x; x++)
        {
            for (int y = 0; y < resolution.y; y++)
            {
                for (int z = 0; z < resolution.z; z++)
                {
                    float px = center.x - size.x / 2 + size.x * (x / (float)(resolution.x - 1));
                    float py = center.y - size.y / 2 + size.y * (y / (float)(resolution.y - 1));
                    float pz = center.z - size.z / 2 + size.z * (z / (float)(resolution.z - 1));
                    positions[index++] = new Vector3(px, py, pz);
                }
            }
        }

        probeGroup.probePositions = positions;

        Debug.Log($"Generated {positions.Length} light probes.");
    }
}
