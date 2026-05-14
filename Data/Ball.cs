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

    /* @brief Updates the position of the ball based on velocity */
    void Move();
}

internal class Ball : IBall
    {
        private readonly Random _random = new();
        private volatile bool _isMoving;

        public event PropertyChangedEventHandler? PropertyChanged;
        public Vector Position { get; }
        public Vector Velocity { get; set; }
        public double Radius { get; } = 10;
        public double Diameter => Radius * 2;
        public double Mass { get; }

        public Ball(double maxX, double maxY)
        {
            Mass = GenerateRandom(10, 20);
            Radius = Mass;
            Position = new Vector(GenerateRandom(0, maxX - Diameter), GenerateRandom(0, maxY - Radius));
            Velocity = new Vector(GenerateRandom(-2,2),GenerateRandom(-2,2));
            Position.PropertyChanged += (s, e) => RaisePropertyChanged(nameof(Position));
        }

        public void Move()
        { 
            double newX = Position.X + Velocity.X;
            double newY = Position.Y + Velocity.Y;
            Position.Update(newX, newY);
        }

        private double GenerateRandom(double min, double max)
        {
            return _random.NextDouble() * (max - min) + min;
        }

        protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
}