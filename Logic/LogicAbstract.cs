using System.Collections.Generic;
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
    private bool _isSimulationRunning = false;
    private readonly object _collisionLock = new object();

    public LogicLayerImplementation(DataAbstract data)
    {
        _data = data;
    }

    public override void CreateScene(int ballCount, double width, double height)
    {
        _board = new Board(width, height);
    
        _data.CreateBalls(ballCount, width, height);
        var generatedBalls = _data.GetBalls();
        _board.AddBalls(generatedBalls);

        foreach (var ball in _board.Balls)
        {
            ball.Start();
        }
        
        _isSimulationRunning = true;
        Task.Run(CollisionMonitor);
    }

    public override List<IBall> GetBalls() => _board?.Balls ?? new List<IBall>();

    public override void UpdateTheState()
    {
        
    }

    private async Task CollisionMonitor()
    {
        while (_isSimulationRunning)
        {
            if (_board != null)
            {
                var balls = _board.Balls;

                lock (_collisionLock)
                {
                    for (int i = 0; i < balls.Count; i++)
                    {
                        var ball = balls[i];
                        for (int j = i + 1; j < balls.Count; j++)
                        {
                            var otherBall = balls[j];
                            CheckBallCollision(ball, otherBall);
                        }
                    }

                    foreach (var ball in balls)
                    {
                        CheckBoundaryCollision(ball);
                    }
                }
            }
            await Task.Delay(10);
        }
    }

    private void CheckBoundaryCollision(IBall ball)
    {
        if (_board == null) return;
        
        if (ball.Position.X <= 0)
        {
            ball.Position.X = 0;
            ball.Velocity.X = Math.Abs(ball.Velocity.X);
        }
        else if (ball.Position.X + ball.Diameter >= _board.Width)
        {
            ball.Position.X = _board.Width - ball.Diameter;
            ball.Velocity.X = -Math.Abs(ball.Velocity.X);
        }
        if (ball.Position.Y <= 0)
        {
            ball.Position.Y = 0;
            ball.Velocity.Y = Math.Abs(ball.Velocity.Y);
        }
        else if (ball.Position.Y + ball.Diameter >= _board.Height)
        {
            ball.Position.Y = _board.Height - ball.Diameter;
            ball.Velocity.Y = -Math.Abs(ball.Velocity.Y);
        }
    }

    private void CheckBallCollision(IBall ball, IBall otherBall)
    {
        double dx = ball.Position.X - otherBall.Position.X;
        double dy = ball.Position.Y - otherBall.Position.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        
        if (distance <= ball.Radius + otherBall.Radius)
        {
            double nx = dx / distance;
            double ny = dy / distance;
            
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