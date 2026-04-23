using System.Collections.Generic;
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
    }

    public override List<IBall> GetBalls() => _board?.Balls ?? new List<IBall>();

    public override void UpdateTheState()
    {
        if (_board == null) return;

        foreach (var ball in _board.Balls)
        {
            ball.Move();
            BoundaryCol(ball);
        }
    }

    private void BoundaryCol(IBall ball)
    {
        if (_board == null) return;

        double currentX = ball.Position.X;
        double currentY = ball.Position.Y;
        double vx = ball.Velocity.X;
        double vy = ball.Velocity.Y;
        double maxX = _board.Width - ball.Radius;
        double maxY = _board.Height - ball.Radius;

        while (currentX < 0 || currentX > maxX)
        {
            if (currentX < 0)
            {
                currentX = -currentX;
                vx = Math.Abs(vx);
            }

            if (currentX > maxX)
            {
                currentX = 2 * maxX - currentX;
                vx = -Math.Abs(vx);
            }
        }

        while (currentY < 0 || currentY > maxY)
        {
            if (currentY < 0)
            {
                currentY = -currentY;
                vy = Math.Abs(vy);
            }

            if (currentY > maxY)
            {
                currentY = 2 * maxY - currentY;
                vy = -Math.Abs(vy);
            }
        }

        ball.Velocity.X = vx;
        ball.Velocity.Y = vy;
        ball.Position.Update(currentX, currentY);
    }
}