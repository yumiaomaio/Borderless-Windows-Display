using System.Runtime.InteropServices;
using BorderlessWindowApp.Interop;
using BorderlessWindowApp.Interop.Constants;
using BorderlessWindowApp.Interop.Enums.Display;
using BorderlessWindowApp.Interop.Structs.Display;
using BorderlessWindowApp.Services.Display.Models;
using Microsoft.Extensions.Logging;

namespace BorderlessWindowApp.Services.Display.implement
{
    /// <summary>
    /// 提供显示器配置的应用服务，例如分辨率、刷新率、色深与位置等。
    /// </summary>
    public class DisplayConfigService : IDisplayConfigService
    {
        private readonly ILogger<DisplayConfigService> _logger;

        public DisplayConfigService(ILogger<DisplayConfigService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 应用显示器的配置（分辨率、位置、刷新率等）。
        /// </summary>
        /// <param name="request">配置参数</param>
        /// <returns>操作是否成功</returns>
        public bool ApplyDisplayConfiguration(DisplayConfigRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.DeviceName))
            {
                _logger.LogError("ApplyDisplayConfiguration failed: Request or DeviceName is null/empty.");
                return false;
            }

            try
            {
                // Build the DEVMODE structure based on the request
                var devmode = BuildDevMode(request);

                // Apply the DEVMODE structure
                return ApplyDevMode(request.DeviceName, devmode, request.SetAsPrimary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApplyDisplayConfiguration failed for device: {Device}", request.DeviceName);
                return false;
            }
        }

        // --- MODIFIED BuildDevMode Method ---.

        private DEVMODE BuildDevMode(DisplayConfigRequest request)
        {
            var devmode = new DEVMODE
            {
                dmSize = (ushort)Marshal.SizeOf<DEVMODE>(),
                // Initialize necessary fields mask
                dmFields = (uint)(
                    DisplayDeviceModeFields.PelsWidth |
                    DisplayDeviceModeFields.PelsHeight |
                    DisplayDeviceModeFields.DisplayFrequency)
            };

            // --- Determine Target Orientation and Dimensions ---
            // Assume default orientation is Landscape if not specified in request
            DisplayOrientation targetOrientation = request.Orientation ?? DisplayOrientation.Landscape;

            // Determine the canonical landscape dimensions from the request
            // Regardless of input order, find the larger and smaller dimension
            uint landscapeWidth = (uint)Math.Max(request.Width, request.Height);
            uint landscapeHeight = (uint)Math.Min(request.Width, request.Height);
            uint finalWidth;
            uint finalHeight;

            // Set finalWidth and finalHeight based on the targetOrientation
            if (targetOrientation == DisplayOrientation.Portrait ||
                targetOrientation == DisplayOrientation.PortraitFlipped)
            {
                // For Portrait target, width should be the smaller dimension, height the larger
                finalWidth = landscapeHeight;
                finalHeight = landscapeWidth;
                _logger.LogDebug("Target orientation is Portrait. Setting DEVMODE Width={W}, Height={H}", finalWidth,
                    finalHeight);
            }
            else // Landscape or LandscapeFlipped
            {
                // For Landscape target, width should be the larger dimension, height the smaller
                finalWidth = landscapeWidth;
                finalHeight = landscapeHeight;
                _logger.LogDebug("Target orientation is Landscape. Setting DEVMODE Width={W}, Height={H}", finalWidth,
                    finalHeight);
            }
            // --- End Dimension Determination ---


            // Set DEVMODE resolution fields using the final calculated dimensions
            devmode.dmPelsWidth = finalWidth;
            devmode.dmPelsHeight = finalHeight;
            // Set other standard fields from request
            devmode.dmDisplayFrequency = (uint)request.RefreshRate;

            // Set Orientation field and flag if it was explicitly requested
            // (Even if it's Landscape, setting it explicitly might be necessary sometimes)
            if (request.Orientation.HasValue)
            {
                devmode.dmDisplayOrientation = (uint)request.Orientation.Value;
                devmode.dmFields |= (uint)DisplayDeviceModeFields.DisplayOrientation;
                _logger.LogDebug("Setting Orientation field to {Orientation}", request.Orientation.Value);
            }
            else // If not specified in request, ensure DEVMODE reflects the assumed target (Landscape)
            {
                devmode.dmDisplayOrientation = (uint)DisplayOrientation.Landscape; // Or DMDO_DEFAULT
                // Optionally add the flag even for default, depending on API strictness
                // devmode.dmFields |= (uint)DisplayDeviceModeFields.DisplayOrientation;
            }


            // Add Position Handling
            if (request.PositionX.HasValue && request.PositionY.HasValue)
            {
                devmode.dmPositionX = request.PositionX.Value;
                devmode.dmPositionY = request.PositionY.Value;
                devmode.dmFields |= (uint)DisplayDeviceModeFields.Position;
                _logger.LogDebug("Setting Position to X={PosX}, Y={PosY}", request.PositionX.Value,
                    request.PositionY.Value);
            }

            devmode.dmSize = (ushort)Marshal.SizeOf<DEVMODE>(); // Ensure size is correct

            _logger.LogDebug(
                "Built DEVMODE for {Device} with Fields: {Fields}, W={W}, H={H}, Freq={F}, Bits={B}, OrientVal={O}",
                request.DeviceName, (DisplayDeviceModeFields)devmode.dmFields,
                devmode.dmPelsWidth, devmode.dmPelsHeight, devmode.dmDisplayFrequency, devmode.dmBitsPerPel,
                devmode.dmDisplayOrientation); // Log the numeric orientation value set

            return devmode;
        }

        private bool ApplyDevMode(string deviceName, DEVMODE devmode, bool setAsPrimary)
        {
            // Flags for applying the change
            // Consider adding CDS_NORESET if you don't want immediate screen flicker/reset prompt
            int flags = ChangeDisplayConstants.CDS_UPDATEREGISTRY | ChangeDisplayConstants.CDS_GLOBAL /*| CDS_NORESET*/;
            if (setAsPrimary)
            {
                flags |= ChangeDisplayConstants.CDS_SET_PRIMARY;
            }

            _logger.LogDebug("Calling ChangeDisplaySettingsEx for {Device} with Flags: {Flags}", deviceName, flags);

            // Assuming NativeDisplayApi class provides the P/Invoke declaration
            int result = NativeDisplayApi.ChangeDisplaySettingsEx(
                deviceName,
                ref devmode, // Pass DEVMODE by reference
                IntPtr.Zero, // Must be zero
                flags,
                IntPtr.Zero); // Must be zero (lpParam)

            // Check result code
            if (result == ChangeDisplayConstants.DISP_CHANGE_SUCCESSFUL)
            {
                _logger.LogInformation("Display configuration applied successfully to device: {Device}", deviceName);
                // Optional: You might need to send a broadcast message WM_SETTINGCHANGE
                // NativeMethods.SendMessageTimeout(NativeMethods.HWND_BROADCAST, NativeMethods.WM_SETTINGCHANGE, ...)
                return true;
            }
            else
            {
                // Log detailed error based on the result code
                string errorMsg = result switch
                {
                    ChangeDisplayConstants.DISP_CHANGE_BADDUALVIEW =>
                        "The settings change was unsuccessful because the system is DualView capable.",
                    ChangeDisplayConstants.DISP_CHANGE_BADFLAGS => "An invalid set of flags was passed in.",
                    ChangeDisplayConstants.DISP_CHANGE_BADMODE => "The graphics mode is not supported.",
                    ChangeDisplayConstants.DISP_CHANGE_BADPARAM =>
                        "An invalid parameter was passed in. This can include an invalid flag or combination of flags.",
                    ChangeDisplayConstants.DISP_CHANGE_FAILED =>
                        "The display driver failed the specified graphics mode.",
                    ChangeDisplayConstants.DISP_CHANGE_NOTUPDATED => "Unable to write settings to the registry.",
                    ChangeDisplayConstants.DISP_CHANGE_RESTART =>
                        "The computer must be restarted for the graphics mode to work.", // Success, but needs restart
                    _ => $"Unknown error code: {result}"
                };
                _logger.LogWarning("ChangeDisplaySettingsEx failed with code {Code} ({ErrorMsg}) for device {Device}",
                    result, errorMsg, deviceName);

                // Consider DISP_CHANGE_RESTART as success if applicable to your app's logic
                // if (result == ChangeDisplayConstants.DISP_CHANGE_RESTART) return true;

                return false;
            }
        }
    }
}