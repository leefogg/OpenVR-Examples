using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;
using Valve.VR;

namespace Common
{
    public static class OpenVRExtensions
    {
        public static void ToOpenTK(this HmdMatrix34_t m, ref Matrix4 output)
        {
            output.Row0 = new Vector4(m.m0, m.m4, m.m8, 0);
            output.Row1 = new Vector4(m.m1, m.m5, m.m9, 0);
            output.Row2 = new Vector4(m.m2, m.m6, m.m10, 0);
            output.Row3 = new Vector4(m.m3, m.m7, m.m11, 1);
    }

        public static void ToOpenTK(this HmdMatrix44_t m, ref Matrix4 output)
        {
            output.Row0 = new Vector4(m.m0, m.m4, m.m8, m.m12);
            output.Row1 = new Vector4(m.m1, m.m5, m.m9, m.m13);
            output.Row2 = new Vector4(m.m2, m.m6, m.m10, m.m14);
            output.Row3 = new Vector4(m.m3, m.m7, m.m11, m.m15);
        }
    }
}
