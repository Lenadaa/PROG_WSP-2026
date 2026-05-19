using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using Data;

namespace Logic;

/// <summary>
/// @brief Abstract class for the logic layer.
/// </summary>
public abstract class LogicAbstract
{
    // @brief Creates the scene with the given parameters
    // @param ballCount the number of balls on the board
    // @param width the width of the board
    public abstract void CreateScene(int ballCount, double width, double height);
    
    // @brief Updates the state of the scene
    public abstract void UpdateTheState();
    public abstract List<IBall> GetBalls();
    public abstract void Stop();

    // @brief Creates new instance of LogicLayerImplementation
    public static LogicAbstract CreateAPI(DataAbstract data = null)
    {
        return new LogicLayerImplementation(data ?? DataAbstract.CreateAPI());
    }
}

internal class LogicLayerImplementation : LogicAbstract
{
private readonly DataAbstract _data;
    private Board? _board;
    
    private volatile bool _isRunning = false; 
    private Thread? _collisionThread;
    private Thread? _mainThread;
    private readonly Stopwatch stopWatch = new();
    
    public LogicLayerImplementation(DataAbstract data)
    {
        _data = data;
    }
    public override void CreateScene(int ballCount, double width, double height)
    {
        _board = new Board(width, height);
        _data.CreateBalls(ballCount, width, height);
        _board.AddBalls(_data.GetBalls());
        _isRunning = true;
        foreach (var ball in _board.Balls)
        {
            ball.Start();
        }
        _mainThread = new Thread(() =>
        {
            try
            {
                while (_isRunning)
                {
                    CheckCollisions();
                    int timeToWait = 10 - (int)stopWatch.ElapsedMilliseconds;
                    if (timeToWait > 0)
                    {
                        Thread.Sleep(timeToWait);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        });
        _mainThread.IsBackground = true;
        _mainThread.Start();
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
            
                double dx = (ball1.Position.X + ball1.Radius) - (ball2.Position.X + ball2.Radius);
                double dy = (ball1.Position.Y + ball1.Radius) - (ball2.Position.Y + ball2.Radius);
                double distance = Math.Sqrt(dx * dx + dy * dy);
                var first = ball1.GetHashCode() < ball2.GetHashCode() ? ball1 : ball2;
                var second = first == ball1 ? ball2 : ball1;
                if (distance <= ball1.Radius + ball2.Radius)
                { 
                    CheckBallCollision(ball1, ball2);
                }
            }
        }
    }

    public override List<IBall> GetBalls() => _board?.Balls ?? new List<IBall>();
    
    public override void UpdateTheState()
    {
        
    }
    
    public override void Stop()         // <-- TU
    {
        _isRunning = false;
        if (_mainThread != null && _mainThread.IsAlive)
        {
            _mainThread.Join();
        }
        
        if (_board != null)
        {
            foreach (var ball in _board.Balls)
            {
                ball.Stop();
            }
        }
    }
    
    private void CheckBoundaryCollision(IBall ball)
    {
        if (_board == null) return;

        lock (ball.SyncRoot)
        {
            double maxX = _board.Width - ball.Diameter;
            double maxY = _board.Height - ball.Diameter;

            double x = ball.Position.X;
            double y = ball.Position.Y;
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
            }
        }
    }

private void CheckBallCollision(IBall ball, IBall otherBall)
{
    var first = ball.GetHashCode() <= otherBall.GetHashCode() ? ball : otherBall;
    var second = ReferenceEquals(first, ball) ? otherBall : ball;
    
    lock (first.SyncRoot)
    lock (second.SyncRoot)
    {
        double dx = (ball.Position.X + ball.Radius) - (otherBall.Position.X + otherBall.Radius);
        double dy = (ball.Position.Y + ball.Radius) - (otherBall.Position.Y + otherBall.Radius);
        double distance = Math.Sqrt(dx * dx + dy * dy);
        double minDistance = ball.Radius + otherBall.Radius;

        if (distance <= minDistance && distance > 0)
        {
            double initialSpeed1 = Math.Sqrt(ball.Velocity.X * ball.Velocity.X + ball.Velocity.Y * ball.Velocity.Y);
            double initialSpeed2 = Math.Sqrt(otherBall.Velocity.X * otherBall.Velocity.X + otherBall.Velocity.Y * otherBall.Velocity.Y);

            double overlap = minDistance - distance;
            double nx = dx / distance;
            double ny = dy / distance;

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

            double impulse = -2 * speedNormal / (1 / ball.Mass + 1 / otherBall.Mass);

            double newVx1 = ball.Velocity.X + (impulse * nx) / ball.Mass;
            double newVy1 = ball.Velocity.Y + (impulse * ny) / ball.Mass;
            double newVx2 = otherBall.Velocity.X - (impulse * nx) / otherBall.Mass;
            double newVy2 = otherBall.Velocity.Y - (impulse * ny) / otherBall.Mass;

            ball.Velocity.X = newVx1;
            ball.Velocity.Y = newVy1;
        
            otherBall.Velocity.X = newVx2;
            otherBall.Velocity.Y = newVy2;
        }
    }
}
}