using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Things this class does
/// <list type="bullet">
/// <item>1.Creats New Pipeling</item>
/// </list>
/// </summary>
//Adding the new instance of a pipeline in the menu. 
[CreateAssetMenu(menuName = "Rendering/My Pipeline")]
public class MyPipelineAsset : RenderPipelineAsset
{
    protected override RenderPipeline CreatePipeline()
    {
        return new MyPieline(); 
    }
}
