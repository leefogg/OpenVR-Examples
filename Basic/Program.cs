using System;
using System.Threading;
using Valve.VR;

namespace Tracking
{
    class Program
    {
        static void Main(string[] _)
        {
            var error = EVRInitError.None;
            var system = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
            if (error != EVRInitError.None)
            {
                Console.WriteLine("Failed to initilize OpenVR");
                return;
            };

            while (true)
            {
                var devicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
                for (uint unDevice = 0; unDevice < OpenVR.k_unMaxTrackedDeviceCount; unDevice++)
                {
                    system.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, devicePoses);
                    var trackedDeviceClass = system.GetTrackedDeviceClass(unDevice);
                    if (trackedDeviceClass != ETrackedDeviceClass.Controller)
                        continue;
                    var trackedControllerRole = system.GetControllerRoleForTrackedDeviceIndex(unDevice);
                    if (trackedControllerRole != ETrackedControllerRole.RightHand)
                        continue;

                    var transationMatrix = devicePoses[unDevice].mDeviceToAbsoluteTracking;
                    Console.WriteLine($"x:{transationMatrix.m3:00.0000}, y:{transationMatrix.m7:00.0000}, z:{transationMatrix.m11:00.0000}");
                    break;
                }

                Thread.Sleep(50);
                Console.CursorLeft = 0;
                Console.CursorTop = 0;
            }
        }
    }
}
