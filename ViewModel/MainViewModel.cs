using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Data;
using Model;
using Presentation.ViewModel;

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
    private int _ballCount;
    private double _width;
    private double _height;
    
    

    public double Width
    {
        get => _width;
        set { _width = value; OnPropertyChanged(); }
    }

    public double Height
    {
        get => _height;
        set { _height = value; OnPropertyChanged(); }
    }

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
        Balls.Clear();

        _model.Start(BallCount, Width, Height); 
        
        foreach (var ball in _model.GetBalls()) 
        {
            Balls.Add(ball);
        }
    }

    private void StopSimulation()
    {
        _model.Stop();
        Balls.Clear();
    }
}