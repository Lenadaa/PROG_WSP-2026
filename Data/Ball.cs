using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
    void Move();
    void Start();
    void Stop();
}

internal class Ball : IBall
{
    // ── Static ID counter ─────────────────────────────────────────────────
    private static int _idCounter = 0;

    // ── Fields ────────────────────────────────────────────────────────────
    private readonly Random _random = new();
    private volatile bool _isMoving;

    /// <summary>
    /// Stopwatch used for real-time delta-time calculation.
    /// Movement distance = velocity × deltaTime × timeScale
    /// so physics are independent of Thread.Sleep intervals.
    /// </summary>
    private readonly Stopwatch _stopwatch = new();
    private double _lastTime;

    private Thread? _thread;
    private int _moveCount;

    // ── Properties ────────────────────────────────────────────────────────
    public int Id { get; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public Vector Position { get; set; }
    public Vector Velocity { get; set; }
    public double Radius { get; }
    public double Diameter => Radius * 2;
    public object SyncRoot { get; } = new object();
    public double Mass { get; set; }
    public int MoveCount => Volatile.Read(ref _moveCount);

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

        _isMoving = true;

        _thread = new Thread(RunLoop)
        {
            IsBackground = true,
            Name = $"Ball#{Id}"
        };
    }


    public void Start() => _thread?.Start();

    public void Stop()
    {
        _isMoving = false;
        if (_thread != null && _thread.IsAlive)
            _thread.Interrupt();
    }

    /// <summary>
    /// Moves the ball by velocity × Δt × timeScale.
    /// Δt is measured with a Stopwatch so the simulation runs at the same
    /// physical speed regardless of how frequently Move() is called
    /// (real-time / wall-clock coupling).
    /// </summary>
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

        Logger.Instance.LogMove(
            Id, newX, newY, Velocity.X, Velocity.Y);
    }
    
    private void RunLoop()
    {
        try
        {
            _stopwatch.Start();
            _lastTime = _stopwatch.Elapsed.TotalSeconds;

            while (_isMoving)
            {
                Move();
                Thread.Sleep(10);  
            }
        }
        catch (ThreadInterruptedException)
        {
            Debug.WriteLine($"[Ball#{Id}] Thread interrupted – stopping.");
        }
    }

    private double GenerateRandom(double min, double max) =>
        _random.NextDouble() * (max - min) + min;

    /// <summary>Returns a velocity that is never exactly 0.</summary>
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