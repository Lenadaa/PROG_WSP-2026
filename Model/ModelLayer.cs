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
    
    public override void UpdateTick() => _logic.UpdateTheState();
}