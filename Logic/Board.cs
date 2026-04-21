using Data;

namespace Logic;
/// <summary>
/// @brief Class for a board.
/// </summary>
internal class Board
{
    // @brief Width and height of the board
    public double Width { get; }
    public double Height { get; }
    // @brief The list of the balls that are on the board
    public List<IBall> Balls { get; }

    // @brief Constructor
    public Board(double width, double height)
    {
        Width = width;
        Height = height;
        Balls = new List<IBall>();
    }

    // @brief Adds a ball to the board
    public void AddBalls(List<IBall> balls)
    {
        Balls.AddRange(balls);
    }
    
}