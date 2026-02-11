using System.Text.Json;
using EstateSurvey.Contracts.DTOs;

namespace EstateSurveyAgent.Services;

public sealed class TaskRepository
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public TaskRepository(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<IReadOnlyList<TaskDto>> LoadAsync()
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<TaskDto>();
        }

        await using var stream = File.OpenRead(_filePath);
        var tasks = await JsonSerializer.DeserializeAsync<List<TaskDto>>(stream, Options);
        return tasks ?? Array.Empty<TaskDto>();
    }

    public async Task SaveAsync(IEnumerable<TaskDto> tasks)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, tasks, Options);
    }
}
