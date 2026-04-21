using System.Collections.Generic;
using Data;
using Logic;

namespace Model;

public abstract class ModelAbstract
{
    public abstract void Start(int ballCount, double width, double height);
    public abstract List<IBall> GetBalls();
    public abstract void UpdateTheState(); 

    public static ModelAbstract Create(LogicAbstract? logic = null)
    {
        return new ModelLayer(logic ?? LogicAbstract.CreateAPI());
    }
}