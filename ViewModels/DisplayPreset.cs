using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BorderlessWindowApp.ViewModels
{
    public class DisplayPreset : INotifyPropertyChanged
    {
        private string _name = "Unnamed Preset";
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        // Store only the core display settings
        public int Width { get; set; }
        public int Height { get; set; }
        public int RefreshRate { get; set; }
        public uint Dpi { get; set; }

        // Calculated property for display purposes
        public string Parameters => $"{Width}x{Height}, {RefreshRate}Hz, {Dpi}% DPI";

        // --- INotifyPropertyChanged Implementation ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public override string ToString() => Name;

        // Optional: Equals and GetHashCode for duplicate checking
        public override bool Equals(object? obj)
        {
            return obj is DisplayPreset preset &&
                   Width == preset.Width &&
                   Height == preset.Height &&
                   RefreshRate == preset.RefreshRate &&
                   Dpi == preset.Dpi;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height, RefreshRate, Dpi);
        }
    }
}