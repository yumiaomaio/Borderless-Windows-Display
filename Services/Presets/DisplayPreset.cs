using System.ComponentModel;
using System.Runtime.CompilerServices;
using BorderlessWindowApp.Interop.Enums.Display;

namespace BorderlessWindowApp.Services.Presets
{
    public class DisplayPreset : INotifyPropertyChanged
    {
        private string _name = "Unnamed Preset";
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        // Store core display settings including Orientation
        public int Width { get; set; }
        public int Height { get; set; }
        public int RefreshRate { get; set; }
        public uint Dpi { get; set; }
        public DisplayOrientation Orientation { get; set; } = DisplayOrientation.Landscape; // Add Orientation

        // Updated Parameters property to include orientation abbreviation
        public string Parameters => $"{Width}x{Height}, {RefreshRate}Hz, {Dpi}% DPI, {FormatOrientation(Orientation)}";

        // Helper for parameter string
        private static string FormatOrientation(DisplayOrientation orientation) => orientation switch
        {
            DisplayOrientation.Portrait => "Portrait",
            DisplayOrientation.LandscapeFlipped => "Land (Flip)",
            DisplayOrientation.PortraitFlipped => "Port (Flip)",
            _ => "Landscape",
        };


        // --- INotifyPropertyChanged Implementation ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public override string ToString() => Name;

        // Updated Equals and GetHashCode to include Orientation
        public override bool Equals(object? obj)
        {
            return obj is DisplayPreset preset &&
                   Width == preset.Width &&
                   Height == preset.Height &&
                   RefreshRate == preset.RefreshRate &&
                   Dpi == preset.Dpi &&
                   Orientation == preset.Orientation; // Compare Orientation
        }

        public override int GetHashCode()
        {
            // Include Orientation in hash code
            return HashCode.Combine(Width, Height, RefreshRate, Dpi, Orientation);
        }
    }
}