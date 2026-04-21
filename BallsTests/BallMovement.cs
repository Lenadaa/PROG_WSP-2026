using Data;

namespace BallsTests;

[TestClass]
public sealed class BallMovement
{
    [TestMethod]
    public void CheckIfBallMoveNotifes()
    {
        var ball = new Ball(100, 100);
        bool receivedNotification = false;

        ball.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(ball.Position))
                receivedNotification = true;
        };

        ball.Move(); 

        Assert.IsTrue(receivedNotification, "Kulka powinna wysłać powiadomienie o zmianie Position!");
    }
    [TestMethod]
    public void CheckIfBallMove()
    {
        var ball = new Ball(100, 100);
    
        double initialX = ball.Position.X;
        double initialY = ball.Position.Y;
        
        double vx = ball.Velocity.X;
        double vy = ball.Velocity.Y;

        ball.Move();

        Assert.AreEqual(initialX + vx, ball.Position.X);
        Assert.AreEqual(initialY + vy, ball.Position.Y);
    }
}