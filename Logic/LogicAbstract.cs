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
            BoundaryCol(ball);
            ball.Move();
        }
    }

    private void BoundaryCol(IBall ball)
    {
        if (_board == null) return;

        double nextX = ball.Position.X + ball.Velocity.X;
        double nextY = ball.Position.Y + ball.Velocity.Y;

        if (nextX < 0 || nextX + ball.Radius > _board.Width)
        {
            ball.Velocity.X *= -1;
        }

        if (nextY < 0 || nextY + ball.Radius > _board.Height)
        {
            ball.Velocity.Y *= -1;
        }
    }
}