using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Valve.VR;

namespace OpenGLBasic
{
    public abstract class DebuggableRenderer : GameWindow
    {
#if DEBUG
        private DebugProc _debugProcCallback = DebugCallback;
        private GCHandle _debugProcCallbackHandle;
#endif

        public DebuggableRenderer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
           : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            SetUpOpenGLErrorCallback();
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
    }
}
