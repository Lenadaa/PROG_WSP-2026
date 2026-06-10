using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using Data;

namespace Logic;

/// <summary>
/// @brief Abstract class for the logic layer.
/// </summary>
public abstract class LogicAbstract
{
    public abstract void CreateScene(int ballCount, double width, double height);
    public abstract void UpdateTheState();
    public abstract List<IBall> GetBalls();
    public abstract void Stop();
    
    public abstract void StartDrag(IBall ball);
    public abstract void StopDrag(IBall ball, double velocityX, double velocityY);
    public abstract double BoardWidth  { get; }
    public abstract double BoardHeight { get; }
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

    private readonly System.Timers.Timer _collisionTimer;

    public LogicLayerImplementation(DataAbstract data)
    {
        _data = data;

        _collisionTimer = new System.Timers.Timer(interval: 5);
        _collisionTimer.Elapsed  += OnCollisionTimerElapsed;
        _collisionTimer.AutoReset = true;
    }

    public override double BoardWidth  => _board?.Width  ?? 0;
    public override double BoardHeight => _board?.Height ?? 0;
    
    private void OnCollisionTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_isRunning)
            CheckCollisions();
    }

    public override void CreateScene(int ballCount, double width, double height)
    {
        _board = new Board(width, height);
        _data.CreateBalls(ballCount, width, height);
        _board.AddBalls(_data.GetBalls());
        _isRunning = true;

        foreach (var ball in _board.Balls)
            ball.Start();

        _collisionTimer.Start();
    }

    public override List<IBall> GetBalls() => _board?.Balls ?? new List<IBall>();

    public override void UpdateTheState() { }

    public override void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;

        _collisionTimer.Stop();
        _collisionTimer.Dispose();

        if (_board != null)
            foreach (var ball in _board.Balls)
                ball.Stop();

        Logger.Instance.Dispose();
    }
    
    public override void StartDrag(IBall ball)
    {
        ball.IsDragging = true;
    }
    
    public override void StopDrag(IBall ball, double velocityX, double velocityY)
    {
        ball.Velocity = new Vector(velocityX, velocityY);
        ball.IsDragging = false;
    }

    private void CheckCollisions()
    {
        var balls = GetBalls();
        for (int i = 0; i < balls.Count; i++)
        {
            IBall ball1 = balls[i];
            CheckBoundaryCollision(ball1);

            for (int j = i + 1; j < balls.Count; j++)
            {
                IBall ball2 = balls[j];

                double dx = (ball1.Position.X + ball1.Radius) -
                            (ball2.Position.X + ball2.Radius);
                double dy = (ball1.Position.Y + ball1.Radius) -
                            (ball2.Position.Y + ball2.Radius);
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance <= ball1.Radius + ball2.Radius)
                    CheckBallCollision(ball1, ball2);
            }
        }
    }

    private void CheckBoundaryCollision(IBall ball)
    {
        if (_board == null) return;
        
        if (ball.IsDragging) return;

        lock (ball)
        {
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
                    vx = Math.Abs(vx);
                }
                else if (x > maxX)
                {
                    x = 2 * maxX - x;
                    vx = -Math.Abs(vx);
                }
                collided = true;
            }

            while (y < 0 || y > maxY)
            {
                if (y < 0)
                {
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

                Logger.Instance.LogWallCollision(
                    ball.Id, x, y, vx, vy);
            }
        }
    }

    private void CheckBallCollision(IBall ball, IBall otherBall)
    {
        if (ball.IsDragging && otherBall.IsDragging) return;

        IBall firstLock  = ball.Id < otherBall.Id ? ball      : otherBall;
        IBall secondLock = ball.Id < otherBall.Id ? otherBall : ball;

        lock (firstLock)
        lock (secondLock)
        {
            double dx = (ball.Position.X + ball.Radius) -
                        (otherBall.Position.X + otherBall.Radius);
            double dy = (ball.Position.Y + ball.Radius) -
                        (otherBall.Position.Y + otherBall.Radius);
            double distance    = Math.Sqrt(dx * dx + dy * dy);
            double minDistance = ball.Radius + otherBall.Radius;

            if (distance > minDistance || distance <= 0) return;

            double overlap = minDistance - distance;
            double nx = dx / distance;
            double ny = dy / distance;

            double dvx = ball.Velocity.X - otherBall.Velocity.X;
            double dvy = ball.Velocity.Y - otherBall.Velocity.Y;
            double speedNormal = dvx * nx + dvy * ny;
            
            if (ball.IsDragging)
            {
                otherBall.Position.X -= nx * overlap;
                otherBall.Position.Y -= ny * overlap;
                if (speedNormal >= 0) return;
                otherBall.Velocity.X += 2 * speedNormal * nx;
                otherBall.Velocity.Y += 2 * speedNormal * ny;
                Logger.Instance.LogBallCollision(
                    otherBall.Id, ball.Id,
                    otherBall.Position.X, otherBall.Position.Y,
                    otherBall.Velocity.X, otherBall.Velocity.Y);
                return;
            }

            if (otherBall.IsDragging)
            {
                ball.Position.X += nx * overlap;
                ball.Position.Y += ny * overlap;
                if (speedNormal >= 0) return;
                ball.Velocity.X -= 2 * speedNormal * nx;
                ball.Velocity.Y -= 2 * speedNormal * ny;
                Logger.Instance.LogBallCollision(
                    ball.Id, otherBall.Id,
                    ball.Position.X, ball.Position.Y,
                    ball.Velocity.X, ball.Velocity.Y);
                return;
            }
            
            double totalMass = ball.Mass + otherBall.Mass;
            double ratio1 = otherBall.Mass / totalMass;
            double ratio2 = ball.Mass      / totalMass;

            ball.Position.X      += nx * overlap * ratio1;
            ball.Position.Y      += ny * overlap * ratio1;
            otherBall.Position.X -= nx * overlap * ratio2;
            otherBall.Position.Y -= ny * overlap * ratio2;

            if (speedNormal > 0) return;

            double impulse = -2.0 * speedNormal / (1.0 / ball.Mass + 1.0 / otherBall.Mass);

            ball.Velocity.X      += (impulse * nx) / ball.Mass;
            ball.Velocity.Y      += (impulse * ny) / ball.Mass;
            otherBall.Velocity.X -= (impulse * nx) / otherBall.Mass;
            otherBall.Velocity.Y -= (impulse * ny) / otherBall.Mass;

            Logger.Instance.LogBallCollision(
                ball.Id, otherBall.Id,
                ball.Position.X, ball.Position.Y,
                ball.Velocity.X, ball.Velocity.Y);
        }
    }
}