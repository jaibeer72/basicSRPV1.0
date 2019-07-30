using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;


public class MyPieline : RenderPipeline
{
    //TODO : See the best way to add drawing settings and Shdaer ID list 

    /// <summary>
    /// Class Properties. 
    /// </summary>
    CullingResults cull;
    Material errorMaterial;

    //Object Rendering Essentials
    FilteringSettings m_FilteringSettings;
    RenderStateBlock m_RenderStateBlock;
    List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
    string m_ProfilerTag;


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
    /// Constructor that will make sure to add the most basic things in the start at the instantiation opoint
    /// </summary>
    public MyPieline()
    {
        m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));

        m_FilteringSettings = new FilteringSettings(RenderQueueRange.all, -1);
        m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Everything);
        m_ProfilerTag = "Lord Save Our Souls";

        
    }


    //So destroy things here that need to be done with from the menu 
    protected override void Dispose(bool disposing)
    {
         
    }

    /// <summary>
    /// Render Pipeline Loop basically everthing that can be rendered renders here and we define it here.
    /// </summary>
    /// <param name="context">Context is like HTML5 Canvas context basically in 3D rather than 2D</param> (if you want a condeing example)
    /// <param name="cameras">All the UnityEngine.Camera components.</param>
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        BeginFrameRendering(context, cameras);
        //GraphicsSettings.lightsUseLinearIntensity = (QualitySettings.activeColorSpace == ColorSpace.Linear);
        //GraphicsSettings.useScriptableRenderPipelineBatching = true;
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
        
        cull = context.Cull(ref cullingParameters);
        // Applies MatrixViewProjection from unity to the camera read more on https://learnopengl.com/Getting-started/Coordinate-Systems
        context.SetupCameraProperties(camera);
        //Draw the skybox  
        context.DrawSkybox(camera);
        //Clear screen 
        ClearScreen(camera);

#if UNITY_EDITOR
        if (camera.cameraType == CameraType.SceneView)
        {
            //Renders The stuff in Scean View Gizoms anf all go here. 
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
#endif

        cameraBuffer.BeginSample("CoolTag");
        CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
        /// This is when you profile each frame.
        using (new ProfilingSample(cmd, m_ProfilerTag))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            //Rendering Transparent 
            var drawSettings = CreateDrawingSettings(SortingCriteria.CommonTransparent, camera);
            m_FilteringSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(cull, ref drawSettings, ref m_FilteringSettings, ref m_RenderStateBlock);

            //Rendering Opeque 
            drawSettings = CreateDrawingSettings(SortingCriteria.CommonOpaque, camera);
            m_FilteringSettings.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(cull, ref drawSettings, ref m_FilteringSettings, ref m_RenderStateBlock);

            DrawDefaultPipeline(context, camera);

        }
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);

        cameraBuffer.EndSample("CoolTag");
        context.ExecuteCommandBuffer(cameraBuffer);
        cameraBuffer.Clear();
        context.Submit();
    }

    /// <summary>
    /// Does the basic Rendering settings based on the info provided. 
    /// </summary>
    /// <param name="sortFlags">The Type of Sorting {Check SortingCriteria}</param>
    /// <param name="camera">Cam in Render loop</param>
    /// <returns></returns>
    private DrawingSettings CreateDrawingSettings(SortingCriteria sortFlags, Camera camera)
    {

        SortingSettings sortingSettings = new SortingSettings(camera) { criteria = sortFlags };
        var settings = new DrawingSettings(m_ShaderTagIdList[0], sortingSettings)
        {
            //perObjectData = PerObjectData.None,
            enableInstancing = true,
            //mainLightIndex = renderingData.lightData.mainLightIndex,
            enableDynamicBatching = true,
        };
        if (m_ShaderTagIdList.Count > 1)
        {
            for (int i = 1; i < m_ShaderTagIdList.Count-1; ++i)
                settings.SetShaderPassName(i, m_ShaderTagIdList[i]);
        }
        //Sets up all th shaders to in the list and sends it to the settings.
        return settings;
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
        SortingSettings sortingSettings = new SortingSettings(camera);
        
        //drawSettings.overrideMaterial(errorMaterial, 0);

        var settings = new DrawingSettings(new ShaderTagId("ForwardRendering"), new SortingSettings(camera));
        settings.SetShaderPassName(1, new ShaderTagId("PrepassBase"));
        settings.SetShaderPassName(2,new ShaderTagId("Always"));
        settings.SetShaderPassName(3,new ShaderTagId("Vertex"));
        settings.SetShaderPassName(4, new ShaderTagId("VertexLMRGBM"));
        settings.SetShaderPassName(5,new ShaderTagId("VertexLM"));
        settings.overrideMaterial= errorMaterial;
        settings.sortingSettings = new SortingSettings(camera);
        var filterSettings = new FilteringSettings(RenderQueueRange.all, -1) {excludeMotionVectorObjects = true,
         sortingLayerRange = SortingLayerRange.all};
        context.DrawRenderers(
            cull, ref settings, ref filterSettings
        );
    }

}


///Shadow Rendeings Code here
/////Shadow rendering basics ? 
//ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(cull, default);
//if (cull.visibleLights != null)
//{
//    context.DrawShadows(ref shadowDrawingSettings);
//}
//drawSettings.sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonTransparent };
