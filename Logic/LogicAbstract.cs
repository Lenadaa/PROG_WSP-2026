using System.Collections.Generic;
using System.Diagnostics;
using Data;

namespace Logic;

public abstract class LogicAbstract
{
    public abstract void CreateScene(int ballCount, double width, double height);
    public abstract void UpdateTheState();
    public abstract List<IBall> GetBalls();
    public abstract void Stop();

    public static LogicAbstract CreateAPI(DataAbstract? data = null)
    {
        return new LogicLayerImplementation(data ?? DataAbstract.CreateAPI());
    }
}

internal class LogicLayerImplementation : LogicAbstract
{
    private readonly DataAbstract _data;
    private Board? _board;
    private volatile bool _isRunning = false;

    private readonly List<Thread> _ballThreads = new();

    public LogicLayerImplementation(DataAbstract data)
    {
        _data = data;
    }

    public override void CreateScene(int ballCount, double width, double height)
    {
        Stop(); 

        _board = new Board(width, height);
        _data.CreateBalls(ballCount, width, height);
        _board.AddBalls(_data.GetBalls());
        _isRunning = true;

        foreach (var ball in _board.Balls)
        {
            var b = ball; 
            var t = new Thread(() => BallLoop(b))
            {
                IsBackground = true,
                Name = $"Ball-{b.Id}"
            };
            _ballThreads.Add(t);
        }

        foreach (var t in _ballThreads)
            t.Start();
    }
    
    private void BallLoop(IBall ball)
    {
        try
        {
            while (_isRunning)
            {
                lock (ball.SyncRoot)
                {
                    ball.Move();
                    CheckBoundaryCollision(ball);
                }

                if (_board != null)
                {
                    foreach (var other in _board.Balls)
                    {
                        if (ReferenceEquals(ball, other)) continue;
                        TryResolveBallCollision(ball, other);
                    }
                }

                Thread.Sleep(10);
            }
        }
        catch (ThreadInterruptedException)
        {
            Debug.WriteLine($"Thread {Thread.CurrentThread.Name} interrupted.");
        }
    }
    
    private void TryResolveBallCollision(IBall a, IBall b)
    {
        var first  = a.Id < b.Id ? a : b;
        var second = first.Id == a.Id ? b : a;

        lock (first.SyncRoot)
        lock (second.SyncRoot)
        {
            double dx = (a.Position.X + a.Radius) - (b.Position.X + b.Radius);
            double dy = (a.Position.Y + a.Radius) - (b.Position.Y + b.Radius);
            double distance = Math.Sqrt(dx * dx + dy * dy);
            double minDist  = a.Radius + b.Radius;

            if (distance >= minDist || distance <= 0) return;

            double overlap = minDist - distance;
            double nx = dx / distance;
            double ny = dy / distance;

            double totalMass = a.Mass + b.Mass;
            double ra = b.Mass / totalMass;   
            double rb = a.Mass / totalMass;   

            a.Position.X += nx * overlap * ra;
            a.Position.Y += ny * overlap * ra;
            b.Position.X -= nx * overlap * rb;
            b.Position.Y -= ny * overlap * rb;

            double dvx = a.Velocity.X - b.Velocity.X;
            double dvy = a.Velocity.Y - b.Velocity.Y;
            double speedAlongNormal = dvx * nx + dvy * ny;

            if (speedAlongNormal > 0) return;

            double impulse = -2.0 * speedAlongNormal / (1.0 / a.Mass + 1.0 / b.Mass);

            a.Velocity.X += (impulse * nx) / a.Mass;
            a.Velocity.Y += (impulse * ny) / a.Mass;
            b.Velocity.X -= (impulse * nx) / b.Mass;
            b.Velocity.Y -= (impulse * ny) / b.Mass;

            Logger.Instance.Log(new LoggerData(
                DateTime.UtcNow, "BallCollision", a.Id,
                a.Position.X, a.Position.Y,
                a.Velocity.X, a.Velocity.Y));
        }
    }
    
    private void CheckBoundaryCollision(IBall ball)
    {
        if (_board == null) return;

        double maxX = _board.Width  - ball.Diameter;
        double maxY = _board.Height - ball.Diameter;

        double x  = ball.Position.X;
        double y  = ball.Position.Y;
        double vx = ball.Velocity.X;
        double vy = ball.Velocity.Y;
        bool collided = false;

        while (x < 0 || x > maxX)
        {
            if (x < 0)
            {
                x = -x;         
                vx =  Math.Abs(vx);
            }
            else if (x > maxX) { x = 2 * maxX - x; vx = -Math.Abs(vx); }
            collided = true;
        }

        while (y < 0 || y > maxY)
        {
            if (y < 0){
                y = -y;
                vy = Math.Abs(vy);
            }
            else if (y > maxY)
            {
                y = 2 * maxY - y;
                vy = -Math.Abs(vy);
            }
            collided = true;
        }

        if (collided)
        {
            ball.Position.X = x;
            ball.Position.Y = y;
            ball.Velocity.X = vx;
            ball.Velocity.Y = vy;

            Logger.Instance.Log(new LoggerData(
                DateTime.UtcNow, "WallCollision", ball.Id,
                x, y, vx, vy));
        }
    }

    public override List<IBall> GetBalls() => _board?.Balls ?? new List<IBall>();

    public override void UpdateTheState() { /* driven by per-ball threads */ }

    public override void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;

        foreach (var t in _ballThreads)
        {
            if (t.IsAlive) t.Interrupt();
        }

        foreach (var t in _ballThreads)
            t.Join(timeout:new TimeSpan(200));

        _ballThreads.Clear();
    }
}