// File: ViewModels/PresetManagerViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input; // For ICommand (if adding commands here)
using BorderlessWindowApp.Models; // Assuming DisplayPreset is here


namespace BorderlessWindowApp.ViewModels
{
    public class PresetManagerViewModel : INotifyPropertyChanged
    {
        private readonly IDisplayPresetService _presetService;
        private bool _isLoaded = false;

        public ObservableCollection<DisplayPreset> Presets { get; } = new();

        private DisplayPreset? _selectedPreset;
        public DisplayPreset? SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                if (_selectedPreset != value)
                {
                    _selectedPreset = value;
                    OnPropertyChanged();
                    // Notify that CanExecute might have changed for commands using this
                    (DeletePresetCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    // Notify parent VM or handle ApplyPreset CanExecute if that command moves here
                }
            }
        }

        // Commands specific to preset management
        public ICommand DeletePresetCommand { get; }

        public PresetManagerViewModel(IDisplayPresetService presetService)
        {
            _presetService = presetService ?? throw new ArgumentNullException(nameof(presetService));
            DeletePresetCommand = new RelayCommand(async () => await DeletePresetAsync(), CanDeletePreset);
        }

        public async Task LoadPresetsAsync()
        {
            if (_isLoaded) return; // Prevent multiple loads if not necessary

            Presets.Clear();
            try
            {
                var loadedPresets = await _presetService.LoadPresetsAsync();
                foreach (var p in loadedPresets)
                {
                    Presets.Add(p);
                }
                _isLoaded = true; // Mark as loaded
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading presets: {ex.Message}");
                // TODO: Handle error reporting more robustly
                _isLoaded = false; // Allow retry?
            }
            SelectedPreset = null; // Ensure nothing selected after load
        }

        public bool PresetExists(DisplayPreset presetValues)
        {
            // Check if a preset with the same core values exists (uses Equals override)
            return Presets.Any(p => p.Equals(presetValues));
        }

        public async Task<bool> AddAndSavePresetAsync(DisplayPreset newPreset)
        {
            if (newPreset == null || PresetExists(newPreset)) // Double check existence before adding
            {
                Console.WriteLine($"Preset '{newPreset?.Name}' already exists or is null.");
                return false;
            }

            Presets.Add(newPreset); // Add to UI collection first

            try
            {
                await _presetService.SavePresetsAsync(Presets); // Save the whole list
                Console.WriteLine($"Preset saved: {newPreset.Name}");
                return true; // Indicate success
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving presets after adding {newPreset.Name}: {ex.Message}");
                Presets.Remove(newPreset); // Rollback UI change if save failed
                // TODO: Notify user of save failure
                return false; // Indicate failure
            }
        }


        private bool CanDeletePreset()
        {
            return SelectedPreset != null;
        }

        private async Task DeletePresetAsync()
        {
            if (SelectedPreset == null) return;

            // Optional: Confirmation dialog

            var presetToRemove = SelectedPreset;
            Presets.Remove(presetToRemove); // Remove from UI collection
            var previouslySelected = SelectedPreset; // Store ref before clearing
            SelectedPreset = null; // Clear selection

            try
            {
                await _presetService.SavePresetsAsync(Presets); // Save the updated list
                Console.WriteLine($"Preset deleted: {presetToRemove.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving presets after deletion: {ex.Message}");
                // Rollback UI change if save failed?
                Presets.Add(presetToRemove); // Add back
                SelectedPreset = previouslySelected; // Restore selection? Depends on desired UX.
                // TODO: Notify user of save failure
            }
        }


        // --- INotifyPropertyChanged Implementation ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}