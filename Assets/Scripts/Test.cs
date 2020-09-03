using System;
using UnityEngine;
using EasyAssetBundle;
using UnityEngine.SceneManagement;

public class Test : MonoBehaviour, IProgress<float>
{
    [SerializeField] AssetReference[] _assetReferences;
    [SerializeField] SceneReference _testScene;

    Matrix4x4 _guiScaleMatrix;

    void Start()
    {
        _guiScaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(2, 2, 1f));
    }

    void OnDestroy()
    {
        foreach (AssetReference ar in _assetReferences)
        {
            ar.Unload();    
        }
        _testScene.Unload();
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

        if (GUILayout.Button("load test scene", GUILayout.ExpandWidth(false)))
        {
            LoadSceneAsync(_testScene);
        }

        GUILayout.Label("Real模式下Unload(true)会发现材质丢失，prefab和材质都被从内存中卸载。");
        if (GUILayout.Button("Unload!", GUILayout.ExpandWidth(false)))
        {
            OnDestroy();
        }
    }

    async void LoadAsync(AssetReference ar)
    {
        try
        {
            Instantiate(await ar.LoadAsync<GameObject>(this));
        }
        catch (Exception e)
        {
            Debug.Log($"Exception: {e}");
        }
    }

    async void LoadSceneAsync(SceneReference sceneReference)
    {
        try
        {
            Scene newScene = await sceneReference.LoadAsync(progress: this);
            Debug.Log($"load new scene: {newScene.name}!");
        }
        catch (Exception e)
        {
            Debug.Log($"{nameof(LoadSceneAsync)} exception: {e}");
        }
    }

    public void Report(float value)
    {
        Debug.Log($"load progress: {value}");
    }
}
