using System;
using System.Threading.Tasks;
using UnityEngine;
using EasyAssetBundle;

public class Test : MonoBehaviour
{
    [SerializeField] private AssetReference[] _assetReferences;
    
    private Matrix4x4 _guiScaleMatrix;

    private void Start()
    {
        _guiScaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 360f, Screen.height / 640f, 1f));
    }

    void OnDestroy()
    {
        foreach (AssetReference ar in _assetReferences)
        {
            ar.Unload();    
        }
    }

    void OnGUI()
    {
        GUI.matrix = _guiScaleMatrix;
        GUILayout.Label("存在Virtual和Real两种模式。Virtual模式不需要打AssetBundle，\nReal模式反之，并会通过打好的AssetBundle加载资源。");
        GUILayout.Space(10);
        
        foreach (AssetReference ar in _assetReferences)
        {
            if (GUILayout.Button($"load {ar.assetName}", GUILayout.ExpandWidth(false)))
            {
                LoadAsync(ar);
            }
        }

        GUILayout.Label("Real模式下Unload(true)会发现材质丢失，prefab和材质都被从内存中卸载。");
        if (GUILayout.Button("Unload!", GUILayout.ExpandWidth(false)))
        {
            OnDestroy();
        }
    }

    private static async Task LoadAsync(AssetReference ar)
    {
        try
        {
            Instantiate(await ar.LoadAsync<GameObject>());
        }
        catch (Exception e)
        {
            Debug.Log($"Exception: {e}");
        }
    }
}
