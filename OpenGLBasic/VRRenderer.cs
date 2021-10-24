using Common;
using OpenGLCommon;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Valve.VR;

namespace OpenGLBasic
{
    public abstract class VRRenderer : DebuggableRenderer
    {
        protected CVRSystem system;
        protected Eye[] Eyes = new Eye[2];
        protected Matrix4 HMDPose;
        private int QuadVAO;
        protected Shader Texture2D;

        public int EyeWidth, EyeHeight;

        public VRRenderer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) 
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            SetUpOpenVR();
            SetUpFrameBuffers();
            SetUpFullScreenQuad();
            SetUpFullScreenShader();
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

        protected virtual void SetUpFrameBuffers()
        {
            uint width = 0, height = 0;
            system.GetRecommendedRenderTargetSize(ref width, ref height);
            EyeWidth = (int)width;
            EyeHeight = (int)height;

            var leftFrameBuffer = new FrameBuffer(width, height, false, true, "leftEye");
            Eyes[(int)EVREye.Eye_Left].FrameBufferHandle = leftFrameBuffer.Handle;
            Eyes[(int)EVREye.Eye_Left].FrameBufferColorHandle = leftFrameBuffer.ColorHandle;

            var rightFrameBuffer = new FrameBuffer(width, height, false, true, "rightEye");
            Eyes[(int)EVREye.Eye_Right].FrameBufferHandle = rightFrameBuffer.Handle;
            Eyes[(int)EVREye.Eye_Right].FrameBufferColorHandle = rightFrameBuffer.ColorHandle;
        }

        private void SetUpFullScreenQuad()
        {
            QuadVAO = Geometry.CreateFullScreenQuad();
        }

        private void SetUpFullScreenShader()
        {
            Texture2D = new Shader(
                File.ReadAllText("resources/shaders/2D/VertexShader.vert"),
                File.ReadAllText("resources/shaders/2D/FragmentShader.frag")
            );
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            UpdatePoses();

            RenderScene();

            RenderWindow();

            SubmitEyes();

            GL.Finish();

            SwapBuffers();

            base.OnRenderFrame(args);
        }

        private void RenderWindow()
        {
            FrameBuffer.BindDefault();
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Texture2D.Bind();
            Vector2 scale = new Vector2(0.5f, 0.5f);
            Texture2D.SetVec2("scale", ref scale);
            Texture2D.SetInt("texture0", 0);
            Texture2D.SetFloat("opacity", 1);

            // Draw a quad on the left and right half of the screen - one for each eye
            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = new Vector2(1f * i - 0.5f, -0.5f);
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

        public abstract void RenderScene();
    }
}
