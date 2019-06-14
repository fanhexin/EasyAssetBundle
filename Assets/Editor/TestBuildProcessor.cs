using EasyAssetBundle.Editor;
using UnityEngine;

[CreateAssetMenu(fileName = "TestBuildProcessor", menuName = "BuildProcessors/TestBuildProcessor")]
public class TestBuildProcessor : AbstractBuildProcessor
{
    public override void BeforeBuild()
    {
        Debug.Log("Before build!");
    }

    public override void AfterBuild()
    {
        Debug.Log("After build!");
    }
}
