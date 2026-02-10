using EstateSurvey.Contracts.DTOs;

namespace EstateSurvey.Contracts.Mappers;

public static class ProtoMapper
{
    public static TaskDto ToDto(SurveyTask task)
    {
        return new TaskDto
        {
            TaskId = task.TaskId,
            ClientId = task.ClientId,
            Title = task.Title,
            Address = task.Address,
            Description = task.Description,
            Status = task.Status,
            CreatedAtUtc = task.CreatedAtUtc,
            UpdatedAtUtc = task.UpdatedAtUtc,
            AssignedAgentId = task.AssignedAgentId,
            Photos = task.Photos.Select(ToDto).ToList()
        };
    }

    public static SurveyTask ToProto(TaskDto dto)
    {
        var task = new SurveyTask
        {
            TaskId = dto.TaskId,
            ClientId = dto.ClientId,
            Title = dto.Title,
            Address = dto.Address,
            Description = dto.Description,
            Status = dto.Status,
            CreatedAtUtc = dto.CreatedAtUtc,
            UpdatedAtUtc = dto.UpdatedAtUtc,
            AssignedAgentId = dto.AssignedAgentId ?? string.Empty
        };

        task.Photos.AddRange(dto.Photos.Select(ToProto));
        return task;
    }

    private static PhotoDto ToDto(SurveyPhoto photo)
    {
        return new PhotoDto
        {
            PhotoId = photo.PhotoId,
            FileName = photo.FileName,
            Resolution = photo.Resolution,
            CapturedAtUtc = photo.CapturedAtUtc
        };
    }

    private static SurveyPhoto ToProto(PhotoDto dto)
    {
        return new SurveyPhoto
        {
            PhotoId = dto.PhotoId,
            FileName = dto.FileName,
            Resolution = dto.Resolution,
            CapturedAtUtc = dto.CapturedAtUtc
        };
    }
}
