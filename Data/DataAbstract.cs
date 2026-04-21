namespace Data;

/// <summary>
/// @brief Data layer.
/// </summary>
public abstract class DataAbstract
{
    /*
     * @brief Creates balls and initialize them.
     * @param count - number of balls to create
     * @param maxX - max x coordinate of the board
     * @param maxY - max y coordinate of the board
     * 
     */
    public abstract void CreateBalls(int count, double maxX, double maxY);
    
    /*
     * @brief Returns list of existing balls
     * @return List of objects of type IBall
     */
    public abstract List<IBall> GetBalls();
    
    /*
     * @brief Creates new instance of DataLayerImplementation
     * @return DataLayerImplementation
     * 
     */
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