using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MyPieline : RenderPipeline
{
    /// <summary>
    ///Render Pipeline Loop basically everthing that can be rendered renders here and we define it here.
    /// </summary>
    /// <param name="context">Context is like HTML5 Canvas context basically in 3D rather than 2D</param> (if you want a condeing example)
    /// <param name="cameras">All the UnityEngine.Camera components.</param>
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var Cam in cameras)
        {
            Render(context, Cam);
        }
    }

    /// <summary>
    /// Single Camera Rendering rather than all at once it will still do the same thing as the Render from the RenderPipeline abstract class.
    /// we will proabable be using this overload as the main render loop though. 
    /// </summary>
    /// <param name="context">the main screen space context</param>
    /// <param name="camera">the UnityEngine.Camera components</param>
    void Render(ScriptableRenderContext context,Camera camera)
    {
        // Command Buffer
        // The Idea of command buffer is that it will manage graphis memory but at the same time control what can be done in that memory
        // For example the CommandBuffer will allow you to render things faster but at the same time it can control wheather you drawMesh or
        // This will also be added to the camera
        // Read MOAR at https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.html
        // And yes i 
        var buffer = new CommandBuffer();
        context.ExecuteCommandBuffer(buffer); 

        // Applies MatrixViewProjection from unity to the camera read more on https://learnopengl.com/Getting-started/Coordinate-Systems
        context.SetupCameraProperties(camera);
        context.DrawSkybox(camera);
        // And whenver you allow something on the paper you need to make sure to submit it to the teacher which is unity here :P 
        context.Submit(); 
    }
}
