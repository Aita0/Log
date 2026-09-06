using System.Text.Json;

namespace LogicTrainer.Services;

public sealed class JsonStorage
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public string DirectoryPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LogicTrainer");
    private string SettingsPath => Path.Combine(DirectoryPath, "settings.json");
    private string ResultsPath => Path.Combine(DirectoryPath, "results.json");
    public AppSettings LoadSettings() => Load(SettingsPath, new AppSettings());
    public List<SessionResult> LoadResults() => Load(ResultsPath, new List<SessionResult>());
    public bool SaveSettings(AppSettings value, out string? error) => Save(SettingsPath, value, out error);
    public bool SaveResults(List<SessionResult> value, out string? error) => Save(ResultsPath, value, out error);
    private static T Load<T>(string path, T fallback)
    {
        try { return File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options) ?? fallback : fallback; }
        catch (JsonException) { return fallback; } catch (IOException) { return fallback; } catch (UnauthorizedAccessException) { return fallback; }
    }
    private bool Save<T>(string path, T value, out string? error)
    {
        try { Directory.CreateDirectory(DirectoryPath); File.WriteAllText(path, JsonSerializer.Serialize(value, Options)); error = null; return true; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { error = ex.Message; return false; }
    }
}
