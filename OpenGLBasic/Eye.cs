using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGLBasic
{
    public struct Eye
    {
        public Matrix4 ProjectionMatrix;
        public Matrix4 ViewMatrix;
        public int HiddenAreaMeshVAO;
        public int HiddenAreaMeshNumElements;
        public int FrameBufferHandle;
        public int FrameBufferColorHandle;
    }
}
