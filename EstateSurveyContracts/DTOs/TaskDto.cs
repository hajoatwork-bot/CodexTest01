using System.Text.Json.Serialization;
using EstateSurvey.Contracts;

namespace EstateSurvey.Contracts.DTOs;

public sealed record class TaskDto
{
    public string TaskId { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TaskStatus Status { get; init; }
    public string CreatedAtUtc { get; init; } = string.Empty;
    public string UpdatedAtUtc { get; init; } = string.Empty;
    public string? AssignedAgentId { get; init; }
    public List<PhotoDto> Photos { get; init; } = new();
}

public sealed record class PhotoDto
{
    public string PhotoId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ImageResolution Resolution { get; init; }
    public string CapturedAtUtc { get; init; } = string.Empty;
}
