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
        if (ball.IsDragging || otherBall.IsDragging) return;
        
        IBall firstLock = ball.Id < otherBall.Id ? ball : otherBall;
        IBall secondLock = ball.Id < otherBall.Id ? otherBall : ball;

        lock (firstLock)
        {
            lock (secondLock)
            {
                double dx = (ball.Position.X + ball.Radius) -
                            (otherBall.Position.X + otherBall.Radius);
                double dy = (ball.Position.Y + ball.Radius) -
                            (otherBall.Position.Y + otherBall.Radius);
                double distance = Math.Sqrt(dx * dx + dy * dy);
                double minDistance = ball.Radius + otherBall.Radius;

                if (distance > minDistance || distance <= 0) return;

                double overlap = minDistance - distance;
                double nx = dx / distance;
                double ny = dy / distance;

                if (ball.IsDragging)
                {
                    otherBall.Position.X -= nx * overlap;
                    otherBall.Position.Y -= ny * overlap;
                    
                    double dvx1 = ball.Velocity.X - otherBall.Velocity.X;
                    double dvy1 = ball.Velocity.Y - otherBall.Velocity.Y;
                    double speedNormal1 = dvx1 * nx + dvy1 * ny;
                    if (speedNormal1 >= 0) return;
                    otherBall.Velocity.X += 2 * speedNormal1 * nx;
                    otherBall.Velocity.Y += 2 * speedNormal1 * ny;
                    return;
                }

                if (otherBall.IsDragging)
                {
                    ball.Position.X += nx * overlap;
                    ball.Position.Y += ny * overlap;

                    double dvx2 = ball.Velocity.X - otherBall.Velocity.X;
                    double dvy2 = ball.Velocity.Y - otherBall.Velocity.Y;
                    double speedNormal2 = dvx2 * nx + dvy2 * ny;
                    if (speedNormal2 >= 0) return;
                    ball.Velocity.X -= 2 * speedNormal2 * nx;
                    ball.Velocity.Y -= 2 * speedNormal2 * ny;
                    return;
                }
                
                double totalMass = ball.Mass + otherBall.Mass;
                double ratio1 = otherBall.Mass / totalMass;
                double ratio2 = ball.Mass / totalMass;

                ball.Position.X += nx * overlap * ratio1;
                ball.Position.Y += ny * overlap * ratio1;
                otherBall.Position.X -= nx * overlap * ratio2;
                otherBall.Position.Y -= ny * overlap * ratio2;

                double dvx = ball.Velocity.X - otherBall.Velocity.X;
                double dvy = ball.Velocity.Y - otherBall.Velocity.Y;
                double speedNormal = dvx * nx + dvy * ny;

                if (speedNormal > 0) return;

                double impulse = -2.0 * speedNormal / (1.0 / ball.Mass + 1.0 / otherBall.Mass);

                double newVx1 = ball.Velocity.X + (impulse * nx) / ball.Mass;
                double newVy1 = ball.Velocity.Y + (impulse * ny) / ball.Mass;
                double newVx2 = otherBall.Velocity.X - (impulse * nx) / otherBall.Mass;
                double newVy2 = otherBall.Velocity.Y - (impulse * ny) / otherBall.Mass;

                ball.Velocity.X = newVx1;
                ball.Velocity.Y = newVy1;
                otherBall.Velocity.X = newVx2;
                otherBall.Velocity.Y = newVy2;

                Logger.Instance.LogBallCollision(
                    ball.Id, otherBall.Id,
                    ball.Position.X, ball.Position.Y,
                    newVx1, newVy1);
            }
        }
    }
}