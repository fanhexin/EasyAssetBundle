using System.Collections.Generic;
using System.IO;
using System.Linq;
using EncryptionProcessor.Editor;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(TestDataSource), menuName = nameof(TestDataSource))]
public class TestDataSource : DataSource
{
    [SerializeField] Object _path;
    
    public override IEnumerator<string> GetEnumerator()
    {
        if (_path == null)
        {
            return Enumerable.Empty<string>().GetEnumerator();
        }
        
        string path = AssetDatabase.GetAssetPath(_path);
        return Directory.GetFiles(path, "*.json").AsEnumerable().GetEnumerator();
    }
}