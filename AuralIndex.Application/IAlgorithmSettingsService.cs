namespace AuralIndex.Application;

public interface IAlgorithmSettingsService
{
    Task<AlgorithmWeightsDto> GetAsync(CancellationToken ct = default);
    Task SaveAsync(AlgorithmWeightsDto settings, CancellationToken ct = default);
    Task ResetAsync(CancellationToken ct = default);
}
