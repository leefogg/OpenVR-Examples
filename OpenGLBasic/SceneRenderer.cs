using OpenGLCommon;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenGLBasic
{
    class SceneRenderer : VRRenderer
    {
        private int FloorVAO;
        private Shader Texture3D;
        private Texture DebugTexture;

        public SceneRenderer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) 
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            LoadFloor();
            LoadShader();
        }

        private void LoadFloor()
        {
            FloorVAO = Geometry.CreatePlane();
            LoadFloorTexture();
        }

        private void LoadFloorTexture()
        {
            const int size = 8;
            var r = new Random();
            var pixels = Enumerable.Range(0, size * size * 3).Select(i => (float)r.NextDouble()).ToArray();
            DebugTexture = new Texture(size, size, PixelInternalFormat.Rgb, PixelFormat.Rgb, pixels, false);
        }

        private void LoadShader()
        {
            Texture3D = new Shader(
                 File.ReadAllText("resources/shaders/3D/VertexShader.vert"),
                 File.ReadAllText("resources/shaders/3D/FragmentShader.frag")
             );
        }

        public override void RenderScene()
        {
            for (int i = 0; i < 2; i++)
            {
                var eye = Eyes[i];
                FrameBuffer.Bind(eye.FrameBufferHandle);
                GL.Viewport(0, 0, EyeWidth, EyeHeight);
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
}
