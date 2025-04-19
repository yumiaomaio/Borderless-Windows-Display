namespace BorderlessWindowApp.ViewModels.Display;


    public interface IDisplayPresetService
    {
        /// <summary>
        /// Loads the list of display presets from persistence.
        /// </summary>
        /// <returns>A list of loaded presets.</returns>
        Task<List<DisplayPreset>> LoadPresetsAsync();

        /// <summary>
        /// Saves the provided list of display presets to persistence.
        /// </summary>
        /// <param name="presets">The list of presets to save.</param>
        /// <returns>Task representing the async save operation.</returns>
        Task SavePresetsAsync(IEnumerable<DisplayPreset> presets);
    }