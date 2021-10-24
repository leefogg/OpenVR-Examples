using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGLCommon
{
    public static class Geometry
    {
        public static int CreatePlane()
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

            var vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
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

            return vao;
        }

        public static int CreateFullScreenQuad()
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

            var vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
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

            return vao;
        }
    }
}
