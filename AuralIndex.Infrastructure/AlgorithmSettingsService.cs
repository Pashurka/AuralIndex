using System.Text.Json;
using AuralIndex.Application;

namespace AuralIndex.Infrastructure;

public class AlgorithmSettingsService : IAlgorithmSettingsService
{
    private readonly string _settingsPath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AlgorithmSettingsService()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AuralIndex");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "algorithm-settings.json");
    }

    public async Task<AlgorithmWeightsDto> GetAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_settingsPath))
        {
            return new AlgorithmWeightsDto();
        }

        await using var stream = File.OpenRead(_settingsPath);
        var settings = await JsonSerializer.DeserializeAsync<AlgorithmWeightsDto>(stream, JsonOptions, ct);
        return settings ?? new AlgorithmWeightsDto();
    }

    public async Task SaveAsync(AlgorithmWeightsDto settings, CancellationToken ct = default)
    {
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, ct);
    }

    public Task ResetAsync(CancellationToken ct = default)
    {
        if (File.Exists(_settingsPath))
        {
            File.Delete(_settingsPath);
        }

        return Task.CompletedTask;
    }
}
