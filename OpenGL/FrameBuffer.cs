using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGL
{
    public class FrameBuffer
    {
        public readonly int Width, Height;
        public int Handle;

        public FrameBuffer(uint width, uint height, bool withDepth, string name = "")
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

            var attachmentHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, attachmentHandle);
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
                    attachmentHandle,
                    0
                );

            if (withDepth)
                AttachDepth();

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                Console.WriteLine("Incomplete Framebuffer");
            }
        }

        private int AttachDepth()
        {
            var RBOHandle = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, RBOHandle);
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
                RBOHandle
            );
            return RBOHandle;
        }

        public void Bind() => GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);

        public static void BindDefault() => GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }
}
