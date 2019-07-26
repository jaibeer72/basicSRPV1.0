using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;



//Required Compnetes fot this to work
/// <summary>
///  UniversalAdditionalCameraData,
///  LWRPAdditionalCameraData,
///  ScriptableRenderer,
///  ProfilingSample,
///  SupportedRenderingFeatures,
/// </summary>


public class MyPieline : RenderPipeline
{
 
    CullingResults cull;
    Material errorMaterial;

    //Object Rendering Essentials
    FilteringSettings m_FilteringSettings;
    RenderStateBlock m_RenderStateBlock;
    List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
    string m_ProfilerTag;
    bool m_IsOpaque;


    // Command Buffer
    // The Idea of command buffer is that it will manage graphis memory but at the same time control what can be done in that memory
    // For example the CommandBuffer will allow you to render things faster but at the same time it can control wheather you drawMesh or
    // This will also be added to the camera
    // Read MOAR at https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.html
    // TODO : Make a seprate function for this so you can may be have more control over this. 
    CommandBuffer cameraBuffer = new CommandBuffer
    {
        name = "Render Camera", //Gives the command the cameras name in the frame debugger to work with can be customised how you want it.
    };
 
    /// <summary>
    /// Render Pipeline Loop basically everthing that can be rendered renders here and we define it here.
    /// </summary>
    /// <param name="context">Context is like HTML5 Canvas context basically in 3D rather than 2D</param> (if you want a condeing example)
    /// <param name="cameras">All the UnityEngine.Camera components.</param>
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(context, cameras);

        GraphicsSettings.lightsUseLinearIntensity = (QualitySettings.activeColorSpace == ColorSpace.Linear);
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
        //SetupPerFrameShaderConstants();

        foreach (var Cam in cameras)
        {
            //New Builtin Passes for camera? maybe
            BeginCameraRendering(context, Cam);

            RenderEachCam(context, Cam);
            EndCameraRendering(context, Cam);
        }

        EndFrameRendering(context, cameras); 
    }

    static void SetupPerFrameShaderConstants()
    {
        // When glossy reflections are OFF in the shader we set a constant color to use as indirect specular
        SphericalHarmonicsL2 ambientSH = RenderSettings.ambientProbe;
        Color linearGlossyEnvColor = new Color(ambientSH[0, 0], ambientSH[1, 0], ambientSH[2, 0]) * RenderSettings.reflectionIntensity;
        Color glossyEnvColor = CoreUtils.ConvertLinearToActiveColorSpace(linearGlossyEnvColor);
        //Shader.SetGlobalVector(PerFrameBuffer._GlossyEnvironmentColor, glossyEnvColor);

        // Used when subtractive mode is selected
        //Shader.SetGlobalVector(PerFrameBuffer._SubtractiveShadowColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.subtractiveShadowColor));
    }

    protected override void Dispose(bool disposing)
    {
        
    }

    /// <summary>
    /// Single Camera Rendering rather than all at once it will still do the same thing as the Render from the RenderPipeline abstract class.
    /// we will proabable be using this overload as the main render loop though. 
    /// </summary>
    /// <param name="context">the main screen space context</param>
    /// <param name="camera">the UnityEngine.Camera components</param>
    void RenderEachCam(ScriptableRenderContext context, Camera camera)
    {
        // In the Catlike Codeing they use an outdated metho and class but not you can get the paramters via the camera
        // using the cullingParamters
        // This Function allows you keep track of the cameras settings and take care of the culling rules in the provided object of type cullingParameters
        if (!camera.TryGetCullingParameters(out var cullingParameters)) { return; }
        //Renders The stuff in Scean View Gizoms anf all go here. 
        
        

        //

        cull = context.Cull(ref cullingParameters);
        // Applies MatrixViewProjection from unity to the camera read more on https://learnopengl.com/Getting-started/Coordinate-Systems
        context.SetupCameraProperties(camera);

        ClearScreen(camera);

        
        //cameraBuffer.BeginSample("Render Camera");
        CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
        using (new ProfilingSample(cmd, m_ProfilerTag))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            //var sortFlags = (true) ? renderingData.cameraData.defaultOpaqueSortFlags : SortingCriteria.CommonTransparent;
            var drawSettings = CreateDrawingSettings(SortingCriteria.CommonOpaque, camera);
            context.DrawRenderers(cull, ref drawSettings, ref m_FilteringSettings, ref m_RenderStateBlock);
            context.DrawSkybox(camera);

            // Render objects that did not match any shader pass with error shader
            //RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, camera, m_FilteringSettings, SortingCriteria.None);
        }
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
#if UNITY_EDITOR
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
#endif
        //Shadow rendering basics ? 
        //ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(cull, default);
        //if (cull.visibleLights != null)
        //{
        //    context.DrawShadows(ref shadowDrawingSettings);
        //}
        //drawSettings.sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonTransparent };



        //cameraBuffer.EndSample("Render Camera");
        context.ExecuteCommandBuffer(cameraBuffer);
        cameraBuffer.Clear();
        context.Submit(); 
        //IgnoreForNow(context, camera);
    }

    private DrawingSettings CreateDrawingSettings(SortingCriteria sortFlags , Camera camera)
    {

        SortingSettings sortingSettings = new SortingSettings(camera) { criteria = sortFlags };
        var settings = new DrawingSettings(m_ShaderTagIdList[0], sortingSettings)
        {
            //perObjectData = renderingData.perObjectData,
            enableInstancing = true,
            //mainLightIndex = renderingData.lightData.mainLightIndex,
            enableDynamicBatching = true,
        };
        return settings;
    }

    private void IgnoreForNow(ScriptableRenderContext context, Camera camera)
    {

        // CullingResults was previously called CullResult and not that is depricated i have this here for now if i use it in future.

        cameraBuffer.Clear();
        ///
        var sortSettings = new SortingSettings(camera);
        sortSettings.criteria = SortingCriteria.CommonOpaque;
        var drawSettings = new DrawingSettings(new ShaderTagId("SRPDefault"), sortSettings);
        drawSettings.sortingSettings = sortSettings;

        var filterSettings = new FilteringSettings()
        {
            renderQueueRange = RenderQueueRange.opaque
        };

        context.DrawRenderers(cull, ref drawSettings, ref filterSettings);

        context.ExecuteCommandBuffer(cameraBuffer);
        context.Submit();


        sortSettings.criteria = SortingCriteria.CommonTransparent;
        drawSettings.sortingSettings = sortSettings;
        filterSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(
            cull, ref drawSettings, ref filterSettings
        );
        context.ExecuteCommandBuffer(cameraBuffer);


        cameraBuffer.Clear();
        // And whenver you allow something on the paper you need to make sure to submit it to the teacher which is unity here :P 
        context.Submit();
    }

    // Acts like GL clear Color or clearning the canvas.
    // Only difference it uses the Flags to see what features are avalable.
    // CameraClearFlags :  is an enumeration that can be used as a set of bit flags. Each bit of the value is used to indicate whether a certain feature is enabled or not.
    // TODO : Figure out what all can be done with flags
    private void ClearScreen(Camera camera)
    {
        CameraClearFlags clearFlags = camera.clearFlags;
        cameraBuffer.ClearRenderTarget(
            (clearFlags & CameraClearFlags.Depth) != 0,
            (clearFlags & CameraClearFlags.Color) != 0,
            camera.backgroundColor
        );
        
    }

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    void DrawDefaultPipeline(ScriptableRenderContext context, Camera camera)
    {
        if (errorMaterial == null)
        {
            Shader errorShader = Shader.Find("Hidden/InternalErrorShader");
            errorMaterial = new Material(errorShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        var drawSettings = new DrawingSettings( new ShaderTagId("ForwardBase") , new SortingSettings(camera));
        drawSettings.SetShaderPassName(1, new ShaderTagId("PrepassBase"));
        drawSettings.SetShaderPassName(2, new ShaderTagId("Always"));
        drawSettings.SetShaderPassName(3, new ShaderTagId("Vertex"));
        drawSettings.SetShaderPassName(4, new ShaderTagId("VertexLMRGBM"));
        drawSettings.SetShaderPassName(5, new ShaderTagId("VertexLM"));
        drawSettings.overrideMaterial = errorMaterial;

        var filterSettings = new FilteringSettings();

        context.DrawRenderers(
            cull, ref drawSettings, ref filterSettings
        );
    }

    public MyPieline()
    {
        m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
        m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
        m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Everything);
        m_IsOpaque = true;
        m_ProfilerTag = "Lord Save Our Souls"; 
    }
}

public enum RenderPassEvent
{
    BeforeRendering = 0,
    BeforeRenderingShadows = 50,
    AfterRenderingShadows = 100,
    BeforeRenderingPrepasses = 150,
    AfterRenderingPrePasses = 200,
    BeforeRenderingOpaques = 250,
    AfterRenderingOpaques = 300,
    BeforeRenderingSkybox = 350,
    AfterRenderingSkybox = 400,
    BeforeRenderingTransparents = 450,
    AfterRenderingTransparents = 500,
    BeforeRenderingPostProcessing = 550,
    AfterRenderingPostProcessing = 600,
    AfterRendering = 1000,
}
