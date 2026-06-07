using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Data;

/// <summary>
/// @brief Interface for a ball.
/// </summary>
public interface IBall : INotifyPropertyChanged
{
    /** @brief Current postion of the ball in 2D*/
    Vector Position { get; }

    /** @brief Current velocity vector of the ball*/
    Vector Velocity { get; set; }

    /* @brief Current radius of the ball*/
    double Radius { get; }

    /* @brief Mass of the ball*/
    double Mass { get; }

    /* @brief Diameter of the ball */
    double Diameter { get; }

    int MoveCount { get; }
    void Move();
    void Start();
    void Stop();
}

internal class Ball : IBall
{
    private readonly Random _random = new();
    private volatile bool _isMoving;
    private readonly Stopwatch _stopwatch = new(); // Dodany stoper
    private double _lastTime; // Śledzenie czasu

    public event PropertyChangedEventHandler? PropertyChanged;
    public Vector Position { get; set; }
    public Vector Velocity { get; set; }
    public double Radius { get; } = 10;
    public double Diameter => Radius * 2;
    public object SyncRoot { get; } = new object();
    public double Mass { get; set; }
    
    private Thread? _thread;
    private int _moveCount;
    public int MoveCount => Volatile.Read(ref _moveCount);
    
    public Ball(double maxX, double maxY)
    {
        Mass = GenerateRandom(10, 20);
        Radius = Mass;
        Position = new Vector(GenerateRandom(0, maxX - Diameter), GenerateRandom(0, maxY - Radius));
        Velocity = new Vector(GenerateRandom(-2,2), GenerateRandom(-2,2));
        Position.PropertyChanged += (s, e) => RaisePropertyChanged(nameof(Position));
        _isMoving = true;
        
        _thread = new Thread(() =>
        {
            try
            {
                _stopwatch.Start();
                _lastTime = _stopwatch.Elapsed.TotalSeconds;

                while (_isMoving)
                {
                    Move();
                    Thread.Sleep(10); // Wciąż odciążamy procesor, ale fizyka jest niezależna od tego
                }
            }
            catch (ThreadInterruptedException)
            {
                Debug.WriteLine("Thread killed");
            }
        });
        _thread.IsBackground = true;
    }

    public void Start() => _thread?.Start();

    public void Stop()
    {
        _isMoving = false;
        if (_thread != null && _thread.IsAlive)
            _thread.Interrupt(); 
    }

    public void Move()
    {
        double currentTime = _stopwatch.Elapsed.TotalSeconds;
        double deltaTime = currentTime - _lastTime;
        _lastTime = currentTime;

        double timeScale = 60.0; 

        double newX = Position.X + Velocity.X * deltaTime * timeScale;
        double newY = Position.Y + Velocity.Y * deltaTime * timeScale;
        
        Position.Update(newX, newY);
        Interlocked.Increment(ref _moveCount);
    }

    private double GenerateRandom(double min, double max) => _random.NextDouble() * (max - min) + min;

    protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override string ToString() => 
        $"[Ball] Position: {Position}, Velocity: {Velocity}, Mass: {Mass}";
}