using Data;

namespace Logic;

public abstract class LogicAbstract
{
    public abstract void CreateScene(int ballCount, double width, double height);
    public abstract void UpdateTick();
    public abstract List<IBall> GetBalls();

    public static LogicAbstract CreateAPI(DataAbstract data = null)
    {
        return new LogicLayerImplementation(data ?? DataAbstract.CreateAPI());
    }
}

internal class LogicLayerImplementation : LogicAbstract
{
    private readonly DataAbstract _data;
    private Board? _board; // Nasz nowy obiekt planszy

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

    public override void UpdateTick()
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