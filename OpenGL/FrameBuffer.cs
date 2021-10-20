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
                var renderBufferHandle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2DMultisample, renderBufferHandle);
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
                    renderBufferHandle,
                    0
                );
            }
            else
            {
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
                var renderBufferHandle = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBufferHandle);
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
                    renderBufferHandle
                );

                return renderBufferHandle;
            } 
            else
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
        }

        public void Bind() => GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);

        public static void BindDefault() => GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }
}
