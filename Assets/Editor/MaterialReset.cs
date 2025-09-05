using UnityEditor;
using UnityEngine;

public static class MaterialReset
{
    [MenuItem("Rubik/Reset Selected Materials to Shader Defaults")]
    static void ResetSelectedMaterials()
    {
        foreach (var obj in Selection.objects)
        {
            var mat = obj as Material;
            if (!mat || !mat.shader) continue;

            // シェーダのデフォルト値だけを持つ一時マテリアルを作成
            var fresh = new Material(mat.shader);

            Undo.RecordObject(mat, "Reset Material to Shader Defaults");
            mat.CopyPropertiesFromMaterial(fresh);       // 値を上書き
            mat.shaderKeywords = fresh.shaderKeywords;   // キーワードも同期
            mat.renderQueue = fresh.renderQueue;         // レンダーキューも同期

            EditorUtility.SetDirty(mat);
            Object.DestroyImmediate(fresh);
        }
        Debug.Log("[MaterialReset] Reset done for selected materials.");
    }
}
