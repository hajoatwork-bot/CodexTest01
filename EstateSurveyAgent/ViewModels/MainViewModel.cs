using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EstateSurvey.Contracts;
using EstateSurvey.Contracts.DTOs;
using EstateSurvey.Contracts.Mappers;
using EstateSurveyAgent.Services;

namespace EstateSurveyAgent.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly TaskRepository _repository;
    private readonly RabbitMqService _rabbitMq;
    private readonly string _agentId;

    public MainViewModel()
    {
        _agentId = $"agent-{Environment.MachineName.ToLowerInvariant()}";
        _repository = new TaskRepository(Path.Combine(AppContext.BaseDirectory, "agent-tasks.json"));
        _rabbitMq = new RabbitMqService("localhost");
        Tasks = new ObservableCollection<TaskViewModel>();
        LoadAsync();
        StartListening();
    }

    public ObservableCollection<TaskViewModel> Tasks { get; }

    [ObservableProperty]
    private TaskViewModel? selectedTask;

    [RelayCommand(CanExecute = nameof(CanUpdate))]
    private async Task MarkInProgressAsync()
    {
        await UpdateStatusAsync(TaskStatus.AgentInProgress, "survey.task.agent.inprogress");
    }

    [RelayCommand(CanExecute = nameof(CanUpdate))]
    private async Task MarkCompletedAsync()
    {
        await UpdateStatusAsync(TaskStatus.AgentCompleted, "survey.task.agent.completed");
    }

    private bool CanUpdate() => SelectedTask is not null;

    private async Task UpdateStatusAsync(TaskStatus status, string routingKey)
    {
        if (SelectedTask is null)
        {
            return;
        }

        var dto = SelectedTask.Dto with
        {
            Status = status,
            AssignedAgentId = _agentId,
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
        var queueName = $"agent.{_agentId}.queue";
        _rabbitMq.StartListening(queueName, "survey.task.approved", task =>
        {
            var dto = ProtoMapper.ToDto(task) with
            {
                Status = TaskStatus.CentreApproved,
                AssignedAgentId = _agentId
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
