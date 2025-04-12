using System;

namespace BorderlessWindowApp.Interop.Enums
{
    /// <summary>
    /// Flags used with SetDisplayConfig (user32.dll)
    /// Reference: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setdisplayconfig
    /// </summary>
    [Flags]
    public enum SetDisplayConfigFlags : uint
    {
        /// <summary>
        /// Applies the new display settings.
        /// </summary>
        SDC_APPLY = 0x00000080,

        /// <summary>
        /// Indicates the caller is providing a display configuration that should be used.
        /// </summary>
        SDC_USE_SUPPLIED_DISPLAY_CONFIG = 0x00000020,

        /// <summary>
        /// Updates the database with the settings without applying them to the system.
        /// </summary>
        SDC_SAVE_TO_DATABASE = 0x00000200,

        /// <summary>
        /// Validates the supplied configuration without applying it.
        /// </summary>
        SDC_VALIDATE = 0x00000040,

        /// <summary>
        /// Allows path changes (such as re-targeting to different monitors).
        /// </summary>
        SDC_ALLOW_CHANGES = 0x00000400,

        /// <summary>
        /// Prevents SetDisplayConfig from modifying the topology (e.g., keeps current mode groupings).
        /// </summary>
        SDC_NO_OPTIMIZATION = 0x00000100,

        /// <summary>
        /// Applies settings but does not persist changes to database.
        /// </summary>
        SDC_DO_NOT_SAVE_TO_DATABASE = 0x00000800,

        /// <summary>
        /// Uses the current topology from the system.
        /// </summary>
        SDC_TOPOLOGY_INTERNAL = 0x00000001,

        SDC_TOPOLOGY_CLONE = 0x00000002,
        SDC_TOPOLOGY_EXTEND = 0x00000004,
        SDC_TOPOLOGY_EXTERNAL = 0x00000008,

        /// <summary>
        /// Combinations of common flags
        /// </summary>
        SDC_TOPOLOGY_SUPPLIED = SDC_TOPOLOGY_INTERNAL | SDC_TOPOLOGY_CLONE | SDC_TOPOLOGY_EXTEND | SDC_TOPOLOGY_EXTERNAL
    }
}