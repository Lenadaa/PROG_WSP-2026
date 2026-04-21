namespace Data;
public abstract class DataAbstract
{
    public abstract void CreateBalls(int count, double maxX, double maxY);
    public abstract List<IBall> GetBalls();
    
    public static DataAbstract CreateAPI()
    {
        return new DataLayerImplementation();
    }
}
internal class DataLayerImplementation : DataAbstract
{
    private readonly List<IBall> _balls = new();

    public override void CreateBalls(int count, double maxX, double maxY)
    {
        _balls.Clear();
        for (int i = 0; i < count; i++)
        {
            _balls.Add(new Ball(maxX, maxY));
        }
    }

    public override List<IBall> GetBalls()
    {
        return new List<IBall>(_balls);
    }
}