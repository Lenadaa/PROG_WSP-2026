using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Data;

/// <summary>
/// @brief Interface for a ball.
/// </summary>
public interface IBall : INotifyPropertyChanged
{
    /** @brief Unique identifier of the ball */
    int Id { get; }

    /** @brief Current position of the ball in 2D */
    Vector Position { get; }

    /** @brief Current velocity vector of the ball */
    Vector Velocity { get; set; }

    /** @brief Current radius of the ball */
    double Radius { get; }

    /** @brief Mass of the ball */
    double Mass { get; }

    /** @brief Diameter of the ball */
    double Diameter { get; }

    /** @brief Lock object for thread-safe access to this ball's data */
    object SyncRoot { get; }

    /** @brief Number of times Move() was called */
    int MoveCount { get; }

    /**
     * @brief Moves the ball by one step based on current velocity.
     *        Should be called while holding SyncRoot lock.
     */
    void Move();
}

internal class Ball : IBall
{
    private static int _idCounter = 0;

    private readonly Random _random = new();
    private int _moveCount;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Id { get; } = Interlocked.Increment(ref _idCounter);
    public Vector Position { get; }
    public Vector Velocity { get; set; }
    public double Radius { get; }
    public double Diameter => Radius * 2;
    public double Mass { get; }
    public object SyncRoot { get; } = new object();
    public int MoveCount => Volatile.Read(ref _moveCount);

    public Ball(double maxX, double maxY)
    {
        Mass   = GenerateRandom(10, 20);
        Radius = Mass;
        Position = new Vector(
            GenerateRandom(Radius, maxX - Diameter),
            GenerateRandom(Radius, maxY - Diameter));
        Velocity = new Vector(GenerateRandom(-3, 3), GenerateRandom(-3, 3));

        Position.PropertyChanged += (s, e) => RaisePropertyChanged(nameof(Position));
    }
    public void Move()
    {
        Position.Update(
            Position.X + Velocity.X,
            Position.Y + Velocity.Y);
        Interlocked.Increment(ref _moveCount);

        Logger.Instance.Log(new LoggerData(
            DateTime.UtcNow, "Move", Id,
            Position.X, Position.Y,
            Velocity.X, Velocity.Y));
    }

    private double GenerateRandom(double min, double max) =>
        _random.NextDouble() * (max - min) + min;

    protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public override string ToString() =>
        $"[Ball #{Id}] Pos:{Position} Vel:{Velocity} Mass:{Mass:F1}";
}