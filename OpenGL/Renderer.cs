using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Valve.VR;
using Common;
using System.Diagnostics;
using OpenGLCommon;
using OpenGLBasic;

namespace OpenGLAdvanced
{
    class Renderer : VRRenderer
    {
        private Shader Texture3D;
        private int FrameBufferHandle;
        private int FloorVAO;
        private Texture DebugTexture;

        public Renderer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) 
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            SetUpShaders();
            LoadFloor();
            LoadHiddenAreaMeshes();
        }

        private void LoadFloor()
        {
            FloorVAO = Geometry.CreatePlane();
            LoadFloorTexture();
        }

        private void SetUpShaders()
        {
            Texture3D = new Shader(
                File.ReadAllText("resources/shaders/3D/multiview.vert"),
                File.ReadAllText("resources/shaders/3D/multiview.frag")
            );
        }

        protected override void SetUpFrameBuffers()
        {
            uint width = 0, height = 0;
            system.GetRecommendedRenderTargetSize(ref width, ref height);
            EyeWidth = (int)width;
            EyeHeight = (int)height;


            // for GL_MultiView, attachments must be arrays, one layer per "eye"
            FrameBufferHandle = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, FrameBufferHandle);

            var colorAttachment = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, colorAttachment);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureBorderColor, new[] { 1f, 0f, 1f });
            GL.TexStorage3D(TextureTarget3d.Texture2DArray, 1, SizedInternalFormat.Rgba8, EyeWidth, EyeHeight, 2);
            GL.Ovr.FramebufferTextureMultiview(FramebufferTarget.DrawFramebuffer, FramebufferAttachment.ColorAttachment0, colorAttachment, 0, 0, 2);

            var depthAttachment = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, depthAttachment);
            GL.TexStorage3D(TextureTarget3d.Texture2DArray, 1, (SizedInternalFormat)All.DepthComponent24, EyeWidth, EyeHeight, 2);
            GL.Ovr.FramebufferTextureMultiview(FramebufferTarget.DrawFramebuffer, FramebufferAttachment.DepthAttachment, depthAttachment, 0, 0, 2);
            var error = GL.CheckFramebufferStatus(FramebufferTarget.DrawFramebuffer);

            // Easiest way to render hidden area mesh to just one eye is to create a framebuffer for each eye
            // Dont want to copy the result after, luckly GL allows us to alias the texture arrays above
            // So, create two Framebuffers, one for each eye, re-using one layer of texture arrays used above each

            var leftEyeFBO = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, leftEyeFBO);
            var leftEye = GL.GenTexture();
            var leftEyeDepth = GL.GenTexture();
            GL.TextureView(leftEye, TextureTarget.Texture2D, colorAttachment, PixelInternalFormat.Rgba8, 0, 1, 0, 1);
            GL.TextureView(leftEyeDepth, TextureTarget.Texture2D, depthAttachment, PixelInternalFormat.DepthComponent24, 0, 1, 0, 1);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, leftEye, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, leftEyeDepth, 0);
            error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            Eyes[0].FrameBufferHandle = leftEyeFBO;
            Eyes[0].FrameBufferColorHandle = leftEye;

            var rightEyeFBO = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, rightEyeFBO);
            var rightEye = GL.GenTexture();
            var rightEyeDepth = GL.GenTexture();
            GL.TextureView(rightEye, TextureTarget.Texture2D, colorAttachment, PixelInternalFormat.Rgba8, 0, 1, 1, 1);
            GL.TextureView(rightEyeDepth, TextureTarget.Texture2D, depthAttachment, PixelInternalFormat.DepthComponent24, 0, 1, 1, 1);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, rightEye, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, rightEyeDepth, 0);
            error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            Eyes[1].FrameBufferHandle = rightEyeFBO;
            Eyes[1].FrameBufferColorHandle = rightEye;
        }

        private void LoadFloorTexture()
        {
            const int size = 8;
            var r = new Random();
            var pixels = Enumerable.Range(0, size * size * 3).Select(i => (float)r.NextDouble()).ToArray();
            DebugTexture = new Texture(size, size, PixelInternalFormat.Rgb, PixelFormat.Rgb, pixels, false);
        }

        private void LoadHiddenAreaMeshes()
        {
            for (int eye = 0; eye < 2; eye++)
            {
                var mesh = system.GetHiddenAreaMesh((EVREye)eye, EHiddenAreaMeshType.k_eHiddenAreaMesh_Standard);
                var numVertcies = (int)(mesh.unTriangleCount * 3);
                Eyes[eye].HiddenAreaMeshNumElements = numVertcies;
                var translatedVerts = new Vector2[numVertcies];
                var sizeofHmdVector2_t = sizeof(float) * 2;
                for (int i = 0; i < numVertcies; i++)
                {
                    var vert = (HmdVector2_t)Marshal.PtrToStructure(mesh.pVertexData + i * sizeofHmdVector2_t, typeof(HmdVector2_t));
                    translatedVerts[i] = new Vector2(vert.v0 * 2 - 1, vert.v1 * 2 - 1);
                }

                var vao = GL.GenVertexArray();
                GL.BindVertexArray(vao);

                var vbo = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
                GL.ObjectLabel(ObjectLabelIdentifier.Buffer, vao, 11, "HiddenMesh" + eye);
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    translatedVerts.Length * sizeofHmdVector2_t,
                    translatedVerts,
                    BufferUsageHint.StaticDraw
                );
                GL.VertexAttribPointer(
                    0,
                    2,
                    VertexAttribPointerType.Float,
                    false,
                    sizeofHmdVector2_t,
                    (IntPtr)0
                );
                GL.EnableVertexAttribArray(0);

                Eyes[eye].HiddenAreaMeshVAO = vao;
            }
        }

        public override void RenderScene()
        {
            GL.Enable(EnableCap.DepthTest);

            for (int eye = 0; eye < 2; eye++)
                RenderHiddenAreaMesh(Eyes[eye]);

            RenderEyes();
        }

        private void RenderHiddenAreaMesh(Eye eye)
        {
            FrameBuffer.Bind(eye.FrameBufferHandle);
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Texture2D.Bind();
            Vector2 scale = new Vector2(1f, 1f);
            Texture2D.SetVec2("scale", ref scale);
            Texture2D.SetInt("texture0", 0);
            Texture2D.SetFloat("opacity", 0); // Draw black
            Vector2 offset = new Vector2(0, 0);
            Texture2D.SetVec2("offset", ref offset);

            GL.Disable(EnableCap.CullFace);
            GL.BindVertexArray(eye.HiddenAreaMeshVAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, eye.HiddenAreaMeshNumElements);
            GL.Enable(EnableCap.CullFace);
        }

        private void RenderEyes()
        {
            FrameBuffer.Bind(FrameBufferHandle);
            GL.Viewport(0, 0, EyeWidth, EyeHeight);

            // Draw floor
            Texture3D.Bind();
            var leftViewMatrix = HMDPose * Eyes[0].ViewMatrix;
            var rightViewMatrix = HMDPose * Eyes[1].ViewMatrix;
            var modelMatrix = Matrix4.CreateScale(5);
            GL.ActiveTexture(TextureUnit.Texture0);
            DebugTexture.Bind();
            Texture3D.SetMat4("ModelMatrix", ref modelMatrix);
            Texture3D.SetMat4("ViewMatrix[0]", ref leftViewMatrix);
            Texture3D.SetMat4("ViewMatrix[1]", ref rightViewMatrix);
            Texture3D.SetMat4("ProjectionMatrix[0]", ref Eyes[0].ProjectionMatrix);
            Texture3D.SetMat4("ProjectionMatrix[1]", ref Eyes[1].ProjectionMatrix);
            Texture3D.SetInt("texture0", 0);
            GL.BindVertexArray(FloorVAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }
    }
}
