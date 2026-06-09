using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Timers;

namespace Data;

/// <summary>
/// @brief Interface for a ball.
/// </summary>
public interface IBall : INotifyPropertyChanged
{
    /** @brief Unique identifier for diagnostic logging */
    int Id { get; }

    /** @brief Current position of the ball in 2D */
    Vector Position { get; }

    /** @brief Current velocity vector of the ball */
    Vector Velocity { get; set; }

    /* @brief Current radius of the ball */
    double Radius { get; }

    /* @brief Mass of the ball */
    double Mass { get; }

    /* @brief Diameter of the ball */
    double Diameter { get; }

    int MoveCount { get; }
    bool IsDragging { get; set; }
    void Move();
    void Start();
    void Stop();
}

internal class Ball : IBall
{
    private static int _idCounter = 0;

    private readonly Random _random = new();
    private volatile bool _isMoving;
    private volatile bool _isDragging;

    /// <summary>
    /// Stopwatch used for real-time delta-time calculation.
    /// Movement distance = velocity × deltaTime × timeScale
    /// so physics are independent of the timer interval.
    /// </summary>
    private readonly Stopwatch _stopwatch = new();
    private double _lastTime;

    private readonly System.Timers.Timer _moveTimer;
    
    private Thread? _thread;
    
    private readonly AutoResetEvent _timerEvent = new(false);
    
    private int _moveCount;

    public int Id { get; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public Vector Position { get; set; }
    public Vector Velocity { get; set; }
    public double Radius { get; }
    public double Diameter => Radius * 2;
    public object SyncRoot { get; } = new object();
    public double Mass { get; set; }
    public int MoveCount => Volatile.Read(ref _moveCount);
    
    public bool IsDragging
    {
        get => _isDragging;
        set
        {
            if (_isDragging && !value)
                _lastTime = _stopwatch.Elapsed.TotalSeconds;

            _isDragging = value;
        }
    }

    public Ball(double maxX, double maxY)
    {
        Id = Interlocked.Increment(ref _idCounter);

        Mass   = GenerateRandom(10, 20);
        Radius = Mass;

        Position = new Vector(
            GenerateRandom(0, maxX - Diameter),
            GenerateRandom(0, maxY - Radius));

        double vx = GenerateNonZeroVelocity();
        double vy = GenerateNonZeroVelocity();
        Velocity = new Vector(vx, vy);

        Position.PropertyChanged += (s, e) => RaisePropertyChanged(nameof(Position));

        _moveTimer = new System.Timers.Timer(interval: 10);
        _moveTimer.Elapsed  += OnTimerElapsed;
        _moveTimer.AutoReset = true;
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_isMoving)
            _timerEvent.Set();
    }

    public void Start()
    {
        if (_isMoving) return;

        _isMoving = true;
        
        _stopwatch.Start();
        _lastTime = _stopwatch.Elapsed.TotalSeconds;
        
        _thread = new Thread(ThreadLoop)
        {
            IsBackground = true,
            Name = $"BallThread_{Id}" 
        };
        _thread.Start();

        _moveTimer.Start();
    }

    public void Stop()
    {
        _isMoving = false;
        _moveTimer.Stop();
        _moveTimer.Dispose();
        
        _timerEvent.Set(); 
    }

    private void ThreadLoop()
    {
        while (_isMoving)
        {
            _timerEvent.WaitOne();

            if (_isMoving && !_isDragging)
            {
                Move();
            }
        }
    }
    
    public void Move()
    {
        double currentTime = _stopwatch.Elapsed.TotalSeconds;
        double deltaTime   = currentTime - _lastTime;
        _lastTime = currentTime;

        const double timeScale = 60.0;

        double newX = Position.X + Velocity.X * deltaTime * timeScale;
        double newY = Position.Y + Velocity.Y * deltaTime * timeScale;

        Position.Update(newX, newY);
        Interlocked.Increment(ref _moveCount);
        
        Logger.Instance.LogMove(Id, newX, newY, Velocity.X, Velocity.Y);
    }

    private double GenerateRandom(double min, double max) =>
        _random.NextDouble() * (max - min) + min;

    private double GenerateNonZeroVelocity()
    {
        double v;
        do { v = GenerateRandom(-2, 2); } while (Math.Abs(v) < 0.1);
        return v;
    }

    protected virtual void RaisePropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override string ToString() =>
        $"[Ball#{Id}] Pos:{Position} Vel:{Velocity} Mass:{Mass:F1}";
}