using EasyAssetBundle.Editor;
using UnityEngine;

[CreateAssetMenu(fileName = "TestBuildProcessor", menuName = "BuildProcessors/TestBuildProcessor")]
public class TestBuildProcessor : AbstractBuildProcessor
{
    public override void OnBeforeBuild()
    {
        Debug.Log("Before build!");
    }

    public override void OnAfterBuild()
    {
        Debug.Log("After build!");
    }

    public override void OnCancelBuild()
    {
        Debug.Log("cancel build!");
    }
}
