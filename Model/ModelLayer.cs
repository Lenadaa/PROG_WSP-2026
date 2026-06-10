using System.Collections.Generic;
using Data;
using Logic;

namespace Model;

internal class ModelLayer : ModelAbstract
{
    private readonly LogicAbstract _logic;

    public ModelLayer(LogicAbstract logic)
    {
        _logic = logic;
    }

    public override void Start(int ballCount, double width, double height)
    {
        _logic.CreateScene(ballCount, width, height);
    }

    public override List<IBall> GetBalls() => _logic.GetBalls();
    
    public override void UpdateTheState() => _logic.UpdateTheState();
    
    public override void Stop() => _logic.Stop();
    
    public override void StartDrag(IBall ball)
        => _logic.StartDrag(ball);

    public override void StopDrag(IBall ball, double velocityX, double velocityY)
        => _logic.StopDrag(ball, velocityX, velocityY);
    
    public override double BoardWidth  => _logic.BoardWidth;
    public override double BoardHeight => _logic.BoardHeight;
}