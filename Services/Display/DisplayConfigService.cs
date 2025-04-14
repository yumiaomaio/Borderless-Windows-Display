using System;
using System.Runtime.InteropServices;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Structs;
using BorderlessWindowApp.Models;
using BorderlessWindowApp.Services.Display.Models;

namespace BorderlessWindowApp.Services.Display
{
    public class DisplayConfigService : IDisplayConfigService
    {
        public bool ApplyDisplayConfiguration(DisplayConfigRequest request)
        {
            var devmode = new DEVMODE
            {
                dmSize = (ushort)Marshal.SizeOf<DEVMODE>(),
                dmPelsWidth = (uint)request.Width,
                dmPelsHeight = (uint)request.Height,
                dmDisplayFrequency = (uint)request.RefreshRate,
                dmBitsPerPel = (uint)request.BitDepth,
                dmFields =
                    (uint)(
                        DisplayDeviceModeFields.PelsWidth |
                        DisplayDeviceModeFields.PelsHeight |
                        DisplayDeviceModeFields.DisplayFrequency |
                        DisplayDeviceModeFields.BitsPerPel)
            };

            // 设置位置（如果有）
            if (request.PositionX.HasValue && request.PositionY.HasValue)
            {
                devmode.dmPositionX = request.PositionX.Value;
                devmode.dmPositionY = request.PositionY.Value;
                devmode.dmFields |= (uint)DisplayDeviceModeFields.Position;
            }

            const int CDS_UPDATEREGISTRY = 0x01;
            const int CDS_SET_PRIMARY = 0x10;
            const int CDS_GLOBAL = 0x08;

            int flags = CDS_UPDATEREGISTRY | CDS_GLOBAL;
            if (request.SetAsPrimary)
                flags |= CDS_SET_PRIMARY;

            int result = NativeDisplayApi.ChangeDisplaySettingsEx(
                request.DeviceName,
                ref devmode,
                IntPtr.Zero,
                flags,
                IntPtr.Zero);

            return result == DISP_CHANGE.Successful;
        }
    }
}