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
    
    object SyncRoot { get; }

    int MoveCount { get; }
    /* @brief Updates the position of the ball based on velocity */
    void Move();
    
    void Start(Barrier barrier);

    void Stop();
}

internal class Ball : IBall
    {
        private readonly Random _random = new();
        private volatile bool _isMoving;
        private Barrier? _barrier;

        public event PropertyChangedEventHandler? PropertyChanged;
        public Vector Position { get; set; }
        public Vector Velocity { get; set; }
        public double Radius { get; } = 10;
        public double Diameter => Radius * 2;
        public double Mass { get; set; }
        private Thread? _thread;
        public object SyncRoot { get;  } = new object();
        private int _moveCount;
        public int MoveCount => Volatile.Read(ref _moveCount);
        
        public Ball(double maxX, double maxY)
        {
            Mass = GenerateRandom(10, 20);
            Radius = Mass;
            Position = new Vector(GenerateRandom(0, maxX - Diameter), GenerateRandom(0, maxY - Radius));
            Velocity = new Vector(GenerateRandom(-2,2),GenerateRandom(-2,2));
            Position.PropertyChanged += (s, e) => RaisePropertyChanged(nameof(Position));
            _isMoving = true;
            _thread = new Thread(() =>
            {
                try
                {
                    while (_isMoving)
                    {
                        Move();
                        try
                        {
                            _barrier?.SignalAndWait();
                        }
                        catch (ObjectDisposedException) { break; }
                        catch (BarrierPostPhaseException) { break; }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            });
            _thread.IsBackground = true;
        }

        public void Start(Barrier barrier)
        {
            _barrier = barrier;
            _thread?.Start();
        }

        public void Stop()
        {
            _isMoving = false;
            if (_thread != null && _thread.IsAlive)
            {
                _thread.Join();
            }
        }
        public void Move()
        {
            lock (SyncRoot)
            {
                double newX = Position.X + Velocity.X;
                double newY = Position.Y + Velocity.Y;
                Position.Update(newX, newY);
                Interlocked.Increment(ref _moveCount);
            }
        }

        private double GenerateRandom(double min, double max)
        {
            return _random.NextDouble() * (max - min) + min;
        }

        protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChangedEventHandler? handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public override string ToString()
        {
            return "[Ball]- Postion" + Position.ToString() + "Velocity" + Velocity.ToString() + "Mass: " + Mass;
        }
    }