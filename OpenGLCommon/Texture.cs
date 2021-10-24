using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGLCommon
{
    public class Texture
    {
        private readonly int Width;
        private readonly int Height;
        private readonly int Handle;

        public Texture(int width, int height, PixelInternalFormat internalFormat, PixelFormat pixelFormat, float[] data, bool generateMips)
        {
            Width = width;
            Height = height;
            Handle = GL.GenTexture();
            Bind();

            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                internalFormat,
                Width,
                Height,
                0,
                pixelFormat,
                PixelType.Float,
                data
            );
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new[] { 1f, 0f, 1f });

            if (generateMips)
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        public void Bind() => Bind(Handle);

        public static void Bind(int handle) => GL.BindTexture(TextureTarget.Texture2D, handle);
    }
}
