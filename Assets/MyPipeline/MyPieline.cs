using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MyPieline : RenderPipeline
{

    CullingResults cull;

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
        foreach (var Cam in cameras)
        {
            //New Builtin Passes for camera? maybe
            BeginCameraRendering(context, Cam); 
            Render(context, Cam);
            EndCameraRendering(context, Cam);
        }
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
    void Render(ScriptableRenderContext context, Camera camera)
    {
        

        ScriptableCullingParameters cullingParameters;
        // In the Catlike Codeing they use an outdated metho and class but not you can get the paramters via the camera
        // using the cullingParamters
        // This Function allows you keep track of the cameras settings and take care of the culling rules in the provided object of type cullingParameters
        if (!camera.TryGetCullingParameters(out cullingParameters)) { return; }

#if UNITY_EDITOR
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
#endif
        // CullingResults was previously called CullResult and not that is depricated i have this here for now if i use it in future.
        cull = context.Cull(ref cullingParameters);

        // Applies MatrixViewProjection from unity to the camera read more on https://learnopengl.com/Getting-started/Coordinate-Systems
        context.SetupCameraProperties(camera);

        // Acts like GL clear Color or clearning the canvas.
        // Only difference it uses the Flags to see what features are avalable.
        // CameraClearFlags :  is an enumeration that can be used as a set of bit flags. Each bit of the value is used to indicate whether a certain feature is enabled or not.
        // TODO : Figure out what all can be done with flags
        
        CameraClearFlags clearFlags = camera.clearFlags;
        cameraBuffer.ClearRenderTarget(
            (clearFlags & CameraClearFlags.Depth) != 0,
            (clearFlags & CameraClearFlags.Color) != 0,
            camera.backgroundColor
        );
        context.ExecuteCommandBuffer(cameraBuffer);
        cameraBuffer.Clear();
        // And whenver you allow something on the paper you need to make sure to submit it to the teacher which is unity here :P 
        context.Submit();
    }
}
