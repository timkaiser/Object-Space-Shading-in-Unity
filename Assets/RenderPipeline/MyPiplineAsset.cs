//Tutorial Source: https://catlikecoding.com/unity/tutorials/scriptable-render-pipeline/custom-pipeline/

using UnityEngine;
using UnityEngine.Experimental.Rendering;

[CreateAssetMenu(menuName = "Rendering/My Pipeline")]
public class MyPiplineAsset : RenderPipelineAsset {

    protected override IRenderPipeline InternalCreatePipeline() {
        return new MyPipline();
    }

}

