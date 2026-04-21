using Logic;

namespace LogicAbstractTest;
using Data;
internal class DataStub : DataAbstract
{
    public List<IBall> TestBalls { get; set; } = new();
    
    public override void CreateBalls(int count, double maxX, double maxY) 
    {
        for (int i = 0; i < count; i++)
        {
            TestBalls.Add(new Ball(maxX, maxY));
        }
    }
    
    public override List<IBall> GetBalls() => TestBalls;
}
[TestClass]
public sealed class LogicTests
{
    [TestMethod]
    public void CheckIfBounceOfLeftWall()
    {
        var stub = new DataStub();
        var ball = new Ball(100, 100); 
        ball.Position.X = 1; 
        ball.Velocity.X = -2; 
        stub.TestBalls.Add(ball);

        var logic = LogicAbstract.CreateAPI(stub);
        logic.CreateScene(1, 100, 100); 

        logic.UpdateTheState();

        Assert.IsTrue(ball.Velocity.X > 0);
    }

    [TestMethod]
    public void CheckIfBounceOffRightWall()
    {
        var stub = new DataStub();
        var ball = new Ball(100, 100); 
        ball.Position.X = 90; 
        ball.Velocity.X = 2; 
        stub.TestBalls.Add(ball);

        var logic = LogicAbstract.CreateAPI(stub);
        logic.CreateScene(1, 100, 100); 

        logic.UpdateTheState();

        Assert.IsTrue(ball.Velocity.X < 0);
    }
    
    [TestMethod]
    public void CorrectSceneInit()
    {
        var stub = new DataStub();
        var logic = LogicAbstract.CreateAPI(stub);
    
        logic.CreateScene(5, 500, 500);

        Assert.AreEqual(5, logic.GetBalls().Count);
    }
    [TestMethod]
    public void CheckIfBounceOffTopWall()
    {
        var stub = new DataStub();
        var ball = new Ball(100, 100); 
        ball.Position.Y = 1; 
        ball.Velocity.Y = -2; // Leci w górę
        stub.TestBalls.Add(ball);

        var logic = LogicAbstract.CreateAPI(stub);
        logic.CreateScene(1, 100, 100); 

        logic.UpdateTheState();

        Assert.IsTrue(ball.Velocity.Y > 0);
    }

    [TestMethod]
    public void CheckIfBounceOffBottomWall()
    {
        var stub = new DataStub();
        var ball = new Ball(100, 100); 
        // Pozycja Y + Radius (10) + Prędkość (2) > Height (100)
        ball.Position.Y = 89; 
        ball.Velocity.Y = 2; // Leci w dół
        stub.TestBalls.Add(ball);

        var logic = LogicAbstract.CreateAPI(stub);
        logic.CreateScene(1, 100, 100); 

        logic.UpdateTheState();

        Assert.IsTrue(ball.Velocity.Y < 0);
    }
    
    [TestMethod]
    public void CheckIfBounceOffCorner()
    {
        var stub = new DataStub();
        var ball = new Ball(100, 100); 
        ball.Position.X = 1;
        ball.Position.Y = 1;
        ball.Velocity.X = -2;
        ball.Velocity.Y = -2;
        stub.TestBalls.Add(ball);

        var logic = LogicAbstract.CreateAPI(stub);
        logic.CreateScene(1, 100, 100); 

        logic.UpdateTheState();

        Assert.IsTrue(ball.Velocity.X > 0 && ball.Velocity.Y > 0);
    }

    [TestMethod]
    public void VelocityDoesNotChangeAfterBounce()
    {
        var stub = new DataStub();
        var ball = new Ball(100, 100);
        ball.Position.X = 1;
        ball.Position.Y = 1;
        ball.Velocity.X = -2;
        ball.Velocity.Y = -2;
        stub.TestBalls.Add(ball);
        
        double speedBefore = Math.Sqrt(ball.Velocity.X * ball.Velocity.X + ball.Velocity.Y * ball.Velocity.Y);
        
        var logic = LogicAbstract.CreateAPI(stub);
        logic.CreateScene(1, 100, 100); 
        logic.UpdateTheState();
        
        double speedAfter = Math.Sqrt(ball.Velocity.X * ball.Velocity.X + ball.Velocity.Y * ball.Velocity.Y);
        
        Assert.AreEqual(speedBefore, speedAfter);
    }
}