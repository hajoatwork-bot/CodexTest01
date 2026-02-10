using CommunityToolkit.Mvvm.ComponentModel;
using EstateSurvey.Contracts.DTOs;

namespace EstateSurveyAgent.ViewModels;

public sealed partial class TaskViewModel : ObservableObject
{
    public TaskViewModel(TaskDto dto)
    {
        Dto = dto;
    }

    public TaskDto Dto { get; }

    public string TaskId => Dto.TaskId;
    public string Title => Dto.Title;
    public string Address => Dto.Address;
    public string Status => Dto.Status.ToString();
    public string UpdatedAtUtc => Dto.UpdatedAtUtc;
}
