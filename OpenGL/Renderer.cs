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
            public FrameBuffer FrameBuffer;
        }

        private Texture DebugTexture;
        private Shader Texture2D, Texture3D;
        private int FloorVAO;
        private int QuadVAO;
        private Matrix4 HMDPose;
        private Eye[] Eyes = new Eye[2];

        public Renderer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) 
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            SetUpOpenGLErrorCallback();

            SetUpOpenVR();
            SetUpStereoRenderTargets();
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
                1, 1, 1, 1, // Top right
                1, -1, 1, 0, // Bottom right
                -1, -1, 0, 0, // Bottom left

                -1, -1, 0, 0, // Bottom left
                -1, 1, 0, 1, // Top left
                1, 1, 1, 1, // Top right
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

        private void SetUpStereoRenderTargets()
        {
            uint width = 0, height = 0;
            system.GetRecommendedRenderTargetSize(ref width, ref height);
            Eyes[(int)EVREye.Eye_Left].FrameBuffer  = new FrameBuffer(width, height, true, "leftEye");
            Eyes[(int)EVREye.Eye_Right].FrameBuffer = new FrameBuffer(width, height, true, "rightEye");
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);

            for (int i = 0; i < 2; i++)
                RenderScene(Eyes[i]);

            RenderWindow();
            SubmitEyes();

            GL.Finish();

            UpdatePoses();
            SwapBuffers();

            base.OnRenderFrame(args);
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
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Texture2D.Bind();
            Vector2 scale = new Vector2(0.5f, 1f);
            Texture2D.SetVec2("scale", ref scale);
            Texture2D.SetInt("texture0", 0);

            // Draw a quad on the left and right half of the screen - one for each eye
            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = new Vector2(1f * i - 0.5f, 0);
                Texture2D.SetVec2("offset", ref offset);

                GL.ActiveTexture(TextureUnit.Texture0);
                Texture.Bind(Eyes[i].FrameBuffer.Handle);

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
                eyeTexture.handle = new IntPtr(Eyes[i].FrameBuffer.Handle);
                error = OpenVR.Compositor.Submit(
                    (EVREye)i,
                    ref eyeTexture, 
                    ref bounds,
                    EVRSubmitFlags.Submit_Default
                );
                //Debug.Assert(error == EVRCompositorError.None);
            }
        }

        private void RenderScene(Eye eye)
        {
            eye.FrameBuffer.Bind();
            GL.Viewport(0, 0, eye.FrameBuffer.Width, eye.FrameBuffer.Height);
            GL.ClearColor(.1f, .1f, .5f, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            var viewMatrix = HMDPose * eye.ViewMatrix;

            var modelMatrix = Matrix4.CreateScale(5);
            Texture3D.Bind();
            GL.ActiveTexture(TextureUnit.Texture0);
            DebugTexture.Bind();
            Texture3D.SetMat4("ModelMatrix", ref modelMatrix);
            Texture3D.SetMat4("ViewMatrix", ref viewMatrix);
            Texture3D.SetMat4("ProjectionMatrix", ref eye.ProjectionMatrix);
            Texture3D.SetInt("texture0", 0);

            GL.BindVertexArray(FloorVAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }
    }
}
