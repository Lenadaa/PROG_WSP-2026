using Data;

namespace Logic;

internal class Board
{
    public double Width { get; }
    public double Height { get; }
    public List<IBall> Balls { get; }

    public Board(double width, double height)
    {
        Width = width;
        Height = height;
        Balls = new List<IBall>();
    }

    public void AddBalls(List<IBall> balls)
    {
        Balls.AddRange(balls);
    }
    
}