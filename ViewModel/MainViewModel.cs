using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using Data;
using Model;
using Presentation.ViewModel;
using Timer = System.Threading.Timer;

namespace ViewModel;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
public class MainViewModel : ViewModelBase
{
    private readonly ModelAbstract _model;
    private bool _isMoving; 
    private int _ballCount;

    public ObservableCollection<IBall> Balls { get; } = new();

    public int BallCount
    {
        get => _ballCount;
        set { _ballCount = value; OnPropertyChanged(); }
    }

    public ICommand StartSimulationCommand { get; }
    public ICommand StopSimulationCommand { get; }

    public MainViewModel()
    {
        _model = ModelAbstract.Create(); //
        StartSimulationCommand = new RelayCommand(StartSimulation);
        StopSimulationCommand = new RelayCommand(StopSimulation);
    }

    private void StartSimulation()
    {
        if (_isMoving) return; 

        _model.Start(BallCount, 700, 300); 
        
        Balls.Clear();
        foreach (var ball in _model.GetBalls()) 
        {
            Balls.Add(ball);
        }

        _isMoving = true;
        Task.Run(SimulationLoop);
    }

    private void StopSimulation()
    {
        _isMoving = false;
    }

    private async Task SimulationLoop()
    {
        while (_isMoving)
        {
            _model.UpdateTheState(); 
            await Task.Delay(16);
        }
    }
}