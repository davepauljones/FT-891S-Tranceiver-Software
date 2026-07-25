using FT891S_CatControl;
using System;
using System.IO;
using System.Text.Json;

public static class RadioStateManager
{
    private static readonly string FolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FT891Controller"
    );

    private static readonly string FilePath = Path.Combine(FolderPath, "radiostate.json");

    /// <summary>
    /// Saves the current RadioState instance to a JSON file in AppData.
    /// </summary>
    public static void SaveState(RadioState state)
    {
        try
        {
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(state, options);
            File.WriteAllText(FilePath, jsonString);
        }
        catch (Exception ex)
        {
            // Handle or log exception (e.g., file permissions error)
            Console.WriteLine($"Failed to save state: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the RadioState from AppData. Returns a new default instance if the file doesn't exist.
    /// </summary>
    public static RadioState LoadState()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string jsonString = File.ReadAllText(FilePath);
                var state = JsonSerializer.Deserialize<RadioState>(jsonString);
                if (state != null)
                {
                    return state;
                }
            }
        }
        catch (Exception ex)
        {
            // Handle or log exception (e.g., corrupted JSON file)
            Console.WriteLine($"Failed to load state: {ex.Message}");
        }

        // Return a fresh state with default values if loading fails or file is missing
        return new RadioState();
    }
}