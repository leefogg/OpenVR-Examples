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

namespace OpenGL
{
    class Renderer : GameWindow
    {
#if DEBUG
        private DebugProc _debugProcCallback = DebugCallback;
        private GCHandle _debugProcCallbackHandle;
        private CVRSystem system;
#endif
        struct Eye
        {
            public Matrix4 ProjectionMatrix;
            public Matrix4 ViewMatrix;
            public int HiddenAreaMeshVAO;
            public int HiddenAreaMeshNumElements;
            public int FrameBufferHandle;
            public int FrameBufferColorHandle;
        }

        private Texture DebugTexture;
        private Shader Texture2D, Texture3D;
        private int FloorVAO;
        private int QuadVAO;
        private Matrix4 HMDPose;
        private Eye[] Eyes = new Eye[2];
        private int FrameBufferHandle;

        public int EyeWidth, EyeHeight;

        public Renderer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) 
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            SetUpOpenGLErrorCallback();

            SetUpOpenVR();
            SetUpFrameBuffer();
            LoadHiddenAreaMeshes();
            SetUpFullScreenQuad();
            SetUpDebugTexture();
            SetUpShaders();
            SetUpFloor();
        }

        private void SetUpFloor()
        {
            var verts = new float[]
            {
                1, 0, 1, 1, 1, // Top right
                1, 0, -1, 1, 0, // Bottom right
                -1, 0,-1, 0, 0, // Bottom left

                -1, 0,-1, 0, 0, // Bottom left
                -1, 0, 1, 0, 1, // Top left
                1, 0, 1, 1, 1, // Top right
            };

            FloorVAO = GL.GenVertexArray();
            GL.BindVertexArray(FloorVAO);
            var vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                verts.Length * sizeof(float),
                verts,
                BufferUsageHint.StaticDraw
            );
            GL.VertexAttribPointer(
                0,
                3,
                VertexAttribPointerType.Float,
                false,
                5 * sizeof(float),
                (IntPtr)0
            );
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(
                1,
                2,
                VertexAttribPointerType.Float,
                false,
                5 * sizeof(float),
                (IntPtr)(3 * sizeof(float))
            );
            GL.EnableVertexAttribArray(1);
        }

        private void SetUpShaders()
        {
            Texture2D = new Shader(
                File.ReadAllText("resources/Texture/2D/VertexShader.vert"),
                File.ReadAllText("resources/Texture/2D/FragmentShader.frag")
            );
            Texture3D = new Shader(
                File.ReadAllText("resources/Texture/3D/VertexShader.vert"),
                File.ReadAllText("resources/Texture/3D/FragmentShader.frag")
            );
        }

        private void SetUpDebugTexture()
        {
            const int size = 8;
            var r = new Random();
            var pixels = Enumerable.Range(0, size * size * 3).Select(i => (float)r.NextDouble()).ToArray();
            DebugTexture = new Texture(size, size, PixelInternalFormat.Rgb, PixelFormat.Rgb, pixels, false);
        }

        private void SetUpFullScreenQuad()
        {
            var verts = new float[]
            {
                -1, -1, 0, 0, // Bottom left
                1, -1, 1, 0, // Bottom right
                1, 1, 1, 1, // Top right

                1, 1, 1, 1, // Top right
                -1, 1, 0, 1, // Top left
                -1, -1, 0, 0, // Bottom left
            };

            QuadVAO = GL.GenVertexArray();
            GL.BindVertexArray(QuadVAO);
            var vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                verts.Length * sizeof(float),
                verts,
                BufferUsageHint.StaticDraw
            );
            GL.VertexAttribPointer(
                0, 
                2,
                VertexAttribPointerType.Float, 
                false,
                4 * sizeof(float),
                (IntPtr)0
            );
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(
                1,
                2,
                VertexAttribPointerType.Float,
                false,
                4 * sizeof(float),
                (IntPtr)(2 * sizeof(float))
            );
            GL.EnableVertexAttribArray(1);
        }

        private void SetUpOpenGLErrorCallback()
        {
#if DEBUG
            _debugProcCallbackHandle = GCHandle.Alloc(_debugProcCallback);
            GL.DebugMessageCallback(_debugProcCallback, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
#endif
        }

        private static void DebugCallback(DebugSource source,
                                 DebugType type,
                                 int id,
                                 DebugSeverity severity,
                                 int messageLength,
                                 IntPtr message,
                                 IntPtr userParam)
        {
            if (type == DebugType.DebugTypeOther)
                return;

            string messageString = Marshal.PtrToStringAnsi(message, messageLength);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{severity} {type} | {messageString}");
            Console.ForegroundColor = ConsoleColor.White;

            if (type == DebugType.DebugTypeError)
                throw new Exception(messageString);
        }

        private void SetUpOpenVR()
        {
            // Set up OpenVR
            var error = EVRInitError.None;
            system = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
            if (error != EVRInitError.None)
            {
                Console.WriteLine("Failed to initilize OpenVR");
                return;
            }

            GetHMDProjectionMatrixForEye(EVREye.Eye_Left, ref Eyes[(int)EVREye.Eye_Left].ProjectionMatrix);
            GetHMDProjectionMatrixForEye(EVREye.Eye_Right, ref Eyes[(int)EVREye.Eye_Right].ProjectionMatrix);
            GetHMDPoseMatrixForEye(EVREye.Eye_Left, ref Eyes[(int)EVREye.Eye_Left].ViewMatrix);
            GetHMDPoseMatrixForEye(EVREye.Eye_Right, ref Eyes[(int)EVREye.Eye_Right].ViewMatrix);
        }

        private void GetHMDPoseMatrixForEye(EVREye eye, ref Matrix4 output)
        {
            var mat = system.GetEyeToHeadTransform(eye);
            mat.ToOpenTK(ref output);
            output.Invert();
        }

        private void GetHMDProjectionMatrixForEye(EVREye eye, ref Matrix4 output)
        {
            const float nearZ = 0.05f;
            const float farZ = 100f;
            var m = system.GetProjectionMatrix(eye, nearZ, farZ);
            m.ToOpenTK(ref output);
        }

        private void SetUpFrameBuffer()
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

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);

            for (int eye = 0; eye < 2; eye++)
            {
                RenderHiddenAreaMesh(Eyes[eye]);
            }

            RenderScene();

            RenderWindow();
            SubmitEyes();

            GL.Finish();

            UpdatePoses();
            SwapBuffers();

            base.OnRenderFrame(args);
        }

        private void RenderHiddenAreaMesh(Eye eye)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, eye.FrameBufferHandle);
            GL.ClearColor(0.1f, 0.1f, 0.1f, 0);
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

        private void UpdatePoses()
        {
            var renderPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            var gamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            OpenVR.Compositor.WaitGetPoses(renderPoses, gamePoses);

            if (renderPoses[OpenVR.k_unTrackedDeviceIndex_Hmd].bPoseIsValid)
            {
                gamePoses[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking.ToOpenTK(ref HMDPose);
                HMDPose.Invert();
            }
        }

        private void RenderWindow()
        {
            FrameBuffer.BindDefault();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Texture2D.Bind();
            Vector2 scale = new Vector2(0.5f, 0.5f);
            Texture2D.SetVec2("scale", ref scale);
            Texture2D.SetInt("texture0", 0);
            Texture2D.SetFloat("opacity", 1);

            // Draw a quad on the left and right half of the screen - one for each eye
            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = new Vector2(1f * i - 0.5f,  -0.5f);
                Texture2D.SetVec2("offset", ref offset);

                GL.ActiveTexture(TextureUnit.Texture0);
                Texture.Bind(Eyes[i].FrameBufferColorHandle);

                GL.BindVertexArray(QuadVAO);
                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            }
        }

        private void SubmitEyes()
        {
            VRTextureBounds_t bounds;
            bounds.uMin = bounds.vMin = 0;
            bounds.uMax = bounds.vMax = 1;

            Texture_t eyeTexture;
            eyeTexture.eType = ETextureType.OpenGL;
            eyeTexture.eColorSpace = EColorSpace.Gamma;

            EVRCompositorError error;
            for (int i = 0; i < 2; i++)
            {
                eyeTexture.handle = new IntPtr(Eyes[i].FrameBufferColorHandle);
                error = OpenVR.Compositor.Submit(
                    (EVREye)i,
                    ref eyeTexture, 
                    ref bounds,
                    EVRSubmitFlags.Submit_Default
                );
                //Debug.Assert(error == EVRCompositorError.None);
            }
        }

        private void RenderScene()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBufferHandle);
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
