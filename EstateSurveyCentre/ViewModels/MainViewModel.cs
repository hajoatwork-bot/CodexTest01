using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EstateSurvey.Contracts;
using EstateSurvey.Contracts.DTOs;
using EstateSurvey.Contracts.Mappers;
using EstateSurveyCentre.Services;

namespace EstateSurveyCentre.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly TaskRepository _repository;
    private readonly RabbitMqService _rabbitMq;

    public MainViewModel()
    {
        _repository = new TaskRepository(Path.Combine(AppContext.BaseDirectory, "centre-tasks.json"));
        _rabbitMq = new RabbitMqService("localhost");
        Tasks = new ObservableCollection<TaskViewModel>();
        LoadAsync();
        StartListening();
    }

    public ObservableCollection<TaskViewModel> Tasks { get; }

    [ObservableProperty]
    private TaskViewModel? selectedTask;

    [RelayCommand(CanExecute = nameof(CanApproveOrReject))]
    private async Task ApproveSelectedAsync()
    {
        await UpdateStatusAsync(TaskStatus.CentreApproved, "survey.task.approved");
    }

    [RelayCommand(CanExecute = nameof(CanApproveOrReject))]
    private async Task RejectSelectedAsync()
    {
        await UpdateStatusAsync(TaskStatus.CentreRejected, "survey.task.rejected");
    }

    private bool CanApproveOrReject() => SelectedTask is not null;

    private async Task UpdateStatusAsync(TaskStatus status, string routingKey)
    {
        if (SelectedTask is null)
        {
            return;
        }

        var dto = SelectedTask.Dto with
        {
            Status = status,
            UpdatedAtUtc = DateTimeOffset.UtcNow.ToString("O")
        };

        ReplaceTask(dto);
        _rabbitMq.PublishTask(ProtoMapper.ToProto(dto), routingKey);
        await PersistAsync();
    }

    private async void LoadAsync()
    {
        var tasks = await _repository.LoadAsync();
        foreach (var task in tasks)
        {
            Tasks.Add(new TaskViewModel(task));
        }
    }

    private void StartListening()
    {
        _rabbitMq.StartListening("centre.main.queue", "survey.task.submitted", task =>
        {
            var dto = ProtoMapper.ToDto(task) with
            {
                Status = TaskStatus.ClientSubmitted
            };

            App.Current.Dispatcher.Invoke(async () =>
            {
                ReplaceTask(dto);
                await PersistAsync();
            });
        });
    }

    private void ReplaceTask(TaskDto dto)
    {
        var existing = Tasks.FirstOrDefault(item => item.TaskId == dto.TaskId);
        if (existing is not null)
        {
            var index = Tasks.IndexOf(existing);
            Tasks[index] = new TaskViewModel(dto);
        }
        else
        {
            Tasks.Add(new TaskViewModel(dto));
        }
    }

    private Task PersistAsync() => _repository.SaveAsync(Tasks.Select(t => t.Dto));
}
