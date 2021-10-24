using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGLCommon
{
    public class FrameBuffer
    {
        public readonly int Width, Height;
        public int Handle { get; private set; }
        public int ColorHandle { get; private set; }
        public int DepthHandle { get; private set; }

        public FrameBuffer(uint width, uint height, bool multisample, bool withDepth, string name = "")
        {
            Width = (int)width;
            Height = (int)height;

            Handle = GL.GenFramebuffer();
            Bind();

            if (!string.IsNullOrEmpty(name))
            {
                name = name[..Math.Min(name.Length, 64)];
                GL.ObjectLabel(ObjectLabelIdentifier.Framebuffer, Handle, name.Length, name);
            }

            if (multisample)
            {
                ColorHandle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2DMultisample, ColorHandle);
                GL.TexImage2DMultisample(
                    TextureTargetMultisample.Texture2DMultisample,
                    1, 
                    PixelInternalFormat.Rgb8,
                    Width, Height,
                    true
                );
                GL.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer, 
                    FramebufferAttachment.ColorAttachment0, 
                    TextureTarget.Texture2DMultisample,
                    ColorHandle,
                    0
                );
            }
            else
            {
                ColorHandle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, ColorHandle);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
                GL.TexImage2D(
                       TextureTarget.Texture2D,
                       0,
                       PixelInternalFormat.Rgba8,
                       Width,
                       Height,
                       0,
                       PixelFormat.Rgba,
                       PixelType.UnsignedByte,
                       new IntPtr()
                   );
                GL.FramebufferTexture2D(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.ColorAttachment0,
                        TextureTarget.Texture2D,
                        ColorHandle,
                        0
                    );
            }

            if (withDepth)
                AttachDepth(multisample);

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("Incomplete Framebuffer");
            }
        }

        private int AttachDepth(bool multisample)
        {
            if (multisample)
            {
                DepthHandle = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, DepthHandle);
                GL.RenderbufferStorageMultisample(
                    RenderbufferTarget.Renderbuffer,
                    8,
                    RenderbufferStorage.DepthComponent,
                    Width, Height
                );
                GL.FramebufferRenderbuffer(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment,
                    RenderbufferTarget.Renderbuffer, 
                    DepthHandle
                );

                return DepthHandle;
            } 
            else
            {
                DepthHandle = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, DepthHandle);
                GL.RenderbufferStorage(
                    RenderbufferTarget.Renderbuffer,
                    RenderbufferStorage.DepthComponent,
                    Width,
                    Height
                );
                GL.FramebufferRenderbuffer(
                    FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment,
                    RenderbufferTarget.Renderbuffer,
                    DepthHandle
                );

                return DepthHandle;
            }
        }

        public void Bind() => Bind(Handle);

        public static void BindDefault() => Bind(0);

        public static void Bind(int handle) => GL.BindFramebuffer(FramebufferTarget.Framebuffer, handle);
    }
}
