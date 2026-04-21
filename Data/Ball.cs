using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Data;

public interface IBall : INotifyPropertyChanged
{
    Vector Position { get; }
    Vector Velocity { get; }
    double Radius { get; }
    void Move();
}
internal class Ball : IBall
{
    private static readonly Random _random = new();
    public event PropertyChangedEventHandler? PropertyChanged;

    public Vector Position { get; }
    public Vector Velocity { get; }
    public double Radius { get; } = 10.0;

    public Ball(double maxX, double maxY)
    {
        Position = new Vector(GenerateRandom(0, maxX - Radius), GenerateRandom(0, maxY - Radius));
        Velocity = new Vector(GenerateRandom(-2, 2), GenerateRandom(-2, 2));
        
        Position.PropertyChanged += (s, e) => RaisePropertyChanged(nameof(Position));
    }

    private double GenerateRandom(double min, double max)
    {
        return _random.NextDouble() * (max - min) + min;
    }
    public Vector GetPosition() => Position;
    public Vector GetVelocity() => Velocity;

    public void Move()
    {
        double newX = Position.X + Velocity.X;
        double newY = Position.Y + Velocity.Y;
        Position.Update(newX, newY);
    }

    protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}