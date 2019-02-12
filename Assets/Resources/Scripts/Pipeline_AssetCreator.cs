//Tutorial Source: https://catlikecoding.com/unity/tutorials/scriptable-render-pipeline/custom-pipeline/
//This script creates a menue entry over which a pipeline asset can be created
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[CreateAssetMenu(menuName = "Rendering/My Pipeline")]
public class Pipeline_AssetCreator : RenderPipelineAsset {

    protected override IRenderPipeline InternalCreatePipeline() {
        return new Pipeline_OSS();
    }

}

