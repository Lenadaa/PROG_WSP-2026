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
    private readonly object _collisionLock = new object();
    private const int TargetUpdatesPerSecond = 100;
    private const int TargetMsPerFrame = 1000 / TargetUpdatesPerSecond;
    
    public LogicLayerImplementation(DataAbstract data)
    {
        _data = data;
    }

    public override void CreateScene(int ballCount, double width, double height)
    {
        _board = new Board(width, height);
        _data.CreateBalls(ballCount, width, height);
        _board.AddBalls(_data.GetBalls());

        foreach (var ball in _board.Balls)
        {
            ball.Start();
        }

        _isRunning = true;
        _collisionThread = new Thread(CollisionLoop);
        _collisionThread.IsBackground = true; 
        _collisionThread.Start();
    }

    private void CollisionLoop()
    {
        Stopwatch stopwatch = new Stopwatch();
        try
        {
            while (_isRunning)
            {
                stopwatch.Restart(); 
                if (_board != null)
                {
                    lock (_collisionLock)
                    {
                        PerformPhysicsCycle();
                    }
                }

                stopwatch.Stop(); 

                int executionTime = (int)stopwatch.ElapsedMilliseconds;
                int sleepTime = TargetMsPerFrame - executionTime;

                if (sleepTime > 0)
                {
                    Thread.Sleep(sleepTime);
                }
            }
        }
        catch (ThreadInterruptedException)
        {
            throw new Exception("Collision loop interrupted");
        }
    }
    private void PerformPhysicsCycle()
    {
        var balls = _board!.Balls;

        foreach (var ball in balls)
        {
            ball.Move();
        }

        for (int i = 0; i < balls.Count; i++)
        {
            CheckBoundaryCollision(balls[i]);

            for (int j = i + 1; j < balls.Count; j++)
            {
                CheckBallCollision(balls[i], balls[j]);
            }
        }
    }

    public override List<IBall> GetBalls() => _board?.Balls ?? new List<IBall>();

    public override void UpdateTheState()
    {
        
    }
    private void CheckBoundaryCollision(IBall ball)
    {
        if (_board == null) return;

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

    private void CheckBallCollision(IBall ball, IBall otherBall)
    {
        double dx = ball.Position.X - otherBall.Position.X;
        double dy = ball.Position.Y - otherBall.Position.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        double minDistance = ball.Radius + otherBall.Radius;

        if (distance <= minDistance)
        {
            if (distance > 0)
            {
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

                dx = ball.Position.X - otherBall.Position.X;
                dy = ball.Position.Y - otherBall.Position.Y;
                distance = Math.Sqrt(dx * dx + dy * dy);
                nx = dx / distance;
                ny = dy / distance;

                double dvx = ball.Velocity.X - otherBall.Velocity.X;
                double dvy = ball.Velocity.Y - otherBall.Velocity.Y;

                double speed = dvx * nx + dvy * ny;

                if (speed > 0) return;

                double impulse = -2 * speed;
                impulse /= (1 / ball.Mass + 1 / otherBall.Mass);

                double impulseX = impulse * nx;
                double impulseY = impulse * ny;

                ball.Velocity.X += impulseX / ball.Mass;
                ball.Velocity.Y += impulseY / ball.Mass;

                otherBall.Velocity.X -= impulseX / otherBall.Mass;
                otherBall.Velocity.Y -= impulseY / otherBall.Mass;
            }
        }
    }
}