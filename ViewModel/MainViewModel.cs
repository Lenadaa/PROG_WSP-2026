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
    
    private IBall?   _draggedBall;
    private double   _prevMouseX, _prevMouseY;
    private double   _currMouseX, _currMouseY;
    private DateTime _prevTime,   _currTime;

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
    
    public void StartDrag(IBall ball)
    {
        _draggedBall = ball;
        _currTime    = DateTime.Now;
        _model.StartDrag(ball);
    }
    
    public void MoveDrag(double x, double y)
    {
        if (_draggedBall == null) return;
        
        double maxX = _model.BoardWidth  - _draggedBall.Diameter;
        double maxY = _model.BoardHeight - _draggedBall.Diameter;
        if (maxX < 0) maxX = 0;
        if (maxY < 0) maxY = 0;
        if (x < 0)    x = 0;
        if (y < 0)    y = 0;
        if (x > maxX) x = maxX;
        if (y > maxY) y = maxY;

        _prevMouseX = _currMouseX;
        _prevMouseY = _currMouseY;
        _prevTime   = _currTime;

        _currMouseX = x;
        _currMouseY = y;
        _currTime   = DateTime.Now;

        double dt = (_currTime - _prevTime).TotalSeconds;
        if (dt > 0.001)
        {
            const double timeScale = 60.0;
            _draggedBall.Velocity.X = (_currMouseX - _prevMouseX) / (dt * timeScale);
            _draggedBall.Velocity.Y = (_currMouseY - _prevMouseY) / (dt * timeScale);
        }

        lock (_draggedBall)
        {
            _draggedBall.Position.Update(x, y);
        }
    }

    public void EndDrag()
    {
        if (_draggedBall == null) return;

        double dt = (_currTime - _prevTime).TotalSeconds;
        double vx = 0, vy = 0;
        if (dt > 0.001)
        {
            const double timeScale = 60.0;
            vx = (_currMouseX - _prevMouseX) / (dt * timeScale);
            vy = (_currMouseY - _prevMouseY) / (dt * timeScale);
        }

        _model.StopDrag(_draggedBall, vx, vy);
        _draggedBall = null;
    }
}