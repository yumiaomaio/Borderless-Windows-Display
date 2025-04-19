using System;
using System.Drawing;
using BorderlessWindowApp.Interop.Enums;
using BorderlessWindowApp.Interop.Enums.Window;

namespace BorderlessWindowApp.Models
{
    public class WindowStateSnapshot
    {
        public IntPtr Handle { get; set; }
        public Rectangle Bounds { get; set; }
        public WindowStyles Style { get; set; }
        public WindowExStyles ExStyle { get; set; }
        public bool IsVisible { get; set; }
    }
    
}