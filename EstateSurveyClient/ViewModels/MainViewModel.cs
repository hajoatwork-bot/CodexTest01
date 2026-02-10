using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EstateSurvey.Contracts;
using EstateSurvey.Contracts.DTOs;
using EstateSurvey.Contracts.Mappers;
using EstateSurveyClient.Services;

namespace EstateSurveyClient.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly TaskRepository _repository;
    private readonly RabbitMqService _rabbitMq;
    private readonly string _clientId;

    public MainViewModel()
    {
        _clientId = $"client-{Environment.MachineName.ToLowerInvariant()}";
        _repository = new TaskRepository(Path.Combine(AppContext.BaseDirectory, "client-tasks.json"));
        _rabbitMq = new RabbitMqService("localhost");
        Tasks = new ObservableCollection<TaskViewModel>();
        NewTitle = "Új felmérés";
        NewAddress = "";
        NewDescription = "";
        LoadAsync();
        StartListening();
    }

    public ObservableCollection<TaskViewModel> Tasks { get; }

    [ObservableProperty]
    private TaskViewModel? selectedTask;

    [ObservableProperty]
    private string newTitle;

    [ObservableProperty]
    private string newAddress;

    [ObservableProperty]
    private string newDescription;

    [RelayCommand]
    private async Task CreateTaskAsync()
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        var dto = new TaskDto
        {
            TaskId = Guid.NewGuid().ToString(),
            ClientId = _clientId,
            Title = NewTitle,
            Address = NewAddress,
            Description = NewDescription,
            Status = TaskStatus.ClientNew,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var viewModel = new TaskViewModel(dto);
        Tasks.Add(viewModel);
        await PersistAsync();
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitSelectedAsync()
    {
        if (SelectedTask is null)
        {
            return;
        }

        var dto = SelectedTask.Dto with
        {
            Status = TaskStatus.ClientSubmitted,
            UpdatedAtUtc = DateTimeOffset.UtcNow.ToString("O")
        };

        ReplaceTask(dto);
        _rabbitMq.PublishTask(ProtoMapper.ToProto(dto), "survey.task.submitted");
        await PersistAsync();
    }

    private bool CanSubmit() => SelectedTask is not null;

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
        var queueName = $"client.{_clientId}.queue";
        _rabbitMq.StartListening(queueName, "survey.task.*", task =>
        {
            if (task.ClientId != _clientId)
            {
                return;
            }

            var dto = ProtoMapper.ToDto(task);
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
