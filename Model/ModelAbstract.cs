using Data;
using Logic;

namespace Model;

public abstract class ModelAbstract
{
    public abstract void Start(int ballCount, double width, double height);
    public abstract List<IBall> GetBalls();
    public abstract void UpdateTheState(); 
    public abstract void Stop();
    
    public abstract void StartDrag(IBall ball);
    public abstract void StopDrag(IBall ball, double velocityX, double velocityY);

    public abstract double BoardWidth  { get; }
    public abstract double BoardHeight { get; }
    
    public static ModelAbstract Create(LogicAbstract? logic = null)
    {
        return new ModelLayer(logic ?? LogicAbstract.CreateAPI());
    }
}