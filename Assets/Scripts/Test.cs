using System.Collections.Generic;
using UnityEngine;
using EasyAssetBundle;

public class Test : MonoBehaviour
{
    [SerializeField] AssetBundleReference _blueCubeAbRef;
    [SerializeField] AssetBundleReference _blueSphereAbRef;
    [SerializeField] AssetReference _greenCubeRef;
    [SerializeField] AssetReference _greenSphereRef;

    List<IAssetBundle> _abs = new List<IAssetBundle>();

    void OnDestroy()
    {
        _abs.ForEach(ab => ab.Unload());
        _abs.Clear();
        _greenCubeRef.Unload();
        _greenSphereRef.Unload();
    }

    void OnGUI()
    {
        GUILayout.Label("存在Virtual和Real两种模式。Virtual模式不需要打AssetBundle，Real模式反之，并会通过打好的AssetBundle加载资源。");
        GUILayout.Space(10);
        
        GUILayout.Label("几种加载方式:");
        GUILayout.Space(10);
        
        GUILayout.Label("1. 直接通过字符串bundle名加载:");
        
        if (GUILayout.Button("同步加载"))
        {
            var ab = AssetBundleLoader.instance.Load("red_cube");
            _abs.Add(ab);
            Instantiate(ab.LoadAsset<GameObject>("RedCube"));
        }
        else if (GUILayout.Button("异步加载"))
        {
            LoadAsyncFromName();
        }
        
        GUILayout.Label("2. 通过AssetBundleReference加载:");

        if (GUILayout.Button("同步加载"))
        {
            var ab = _blueCubeAbRef.Load();
            _abs.Add(ab);
            Instantiate(ab.LoadAsset<GameObject>("BlueCube"));
        }
        else if (GUILayout.Button("异步加载"))
        {
            LoadAsyncFromAssetBundleReference();
        }
        
        GUILayout.Label("3. 通过AssetReference加载:");
        
        if (GUILayout.Button("同步加载"))
        {
            Instantiate(_greenCubeRef.Load<GameObject>());
        }
        else if (GUILayout.Button("异步加载"))
        {
            LoadAsyncFromAssetReference();
        }

        GUILayout.Label("Real模式下Unload(true)会发现材质丢失，prefab和材质都被从内存中卸载。");
        if (GUILayout.Button("Unload!"))
        {
            OnDestroy();
        }
    }

    async void LoadAsyncFromAssetReference()
    {
        Instantiate(await _greenSphereRef.LoadAsync<GameObject>());
    }

    async void LoadAsyncFromAssetBundleReference()
    {
        var ab = await _blueSphereAbRef.LoadAsync();
        _abs.Add(ab);
        Instantiate(await ab.LoadAssetAsync<GameObject>("BlueSphere"));
    }

    async void LoadAsyncFromName()
    {
        var ab = await AssetBundleLoader.instance.LoadAsync("red_sphere");
        _abs.Add(ab);
        Instantiate(await ab.LoadAssetAsync<GameObject>("RedSphere"));
    }
}
