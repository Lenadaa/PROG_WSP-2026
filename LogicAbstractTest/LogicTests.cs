using Logic;
using Data;
namespace LogicAbstractTest;

[TestClass]
public class LogicLayerTests
{
    /// <summary>
    /// Pozwala na pełną kontrolę parametrów bez zależności od losowości w warstwie Data.
    /// </summary>
    private class BallStub : IBall
    {
        public Vector Position { get; set; } = new Vector(0, 0);
        public Vector Velocity { get; set; } = new Vector(0, 0);
        public double Mass { get; set; } = 1.0;
        public double Radius { get; set; } = 5.0;
        public double Diameter => Radius * 2;
        public object SyncRoot { get; } = new object();
        public void Move() { } 
        public void Start() { }
        public void Stop() { }
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
    
    
    [TestMethod]
    public void CheckBallCollisiohnVelocity()
    {
        var logic = (LogicLayerImplementation)LogicAbstract.CreateAPI();
        var ball1 = new BallStub { Position = new Vector(45, 50), Velocity = new Vector(2, 0), Mass = 10 };
        var ball2 = new BallStub { Position = new Vector(52, 50), Velocity = new Vector(-2, 0), Mass = 10 };

        double initialV1 = ball1.Velocity.X;
        double initialV2 = ball2.Velocity.X;

        var method = typeof(LogicLayerImplementation).GetMethod("CheckBallCollision", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(logic, new object[] { ball1, ball2 });

        Assert.AreNotEqual(initialV1, ball1.Velocity.X);
        Assert.AreNotEqual(initialV2, ball2.Velocity.X);
        Assert.IsLessThan(0, ball1.Velocity.X);
        Assert.IsGreaterThan(0, ball2.Velocity.X);
    }
    
    [TestMethod]
    public async Task StabilityTest()
    {
        var logic = LogicAbstract.CreateAPI();
        int sampleDurationMs = 200;

        logic.CreateScene(2, 100, 100);

        var startTime = DateTime.Now;
        await Task.Delay(sampleDurationMs);
        var endTime = DateTime.Now;


        var balls = logic.GetBalls();
        Assert.IsNotNull(balls);
        Assert.HasCount(2, balls);

        double timePassed = (endTime - startTime).TotalMilliseconds;
        Assert.IsGreaterThanOrEqualTo(sampleDurationMs * 0.8, timePassed);
    }

    [TestMethod]
    public void WallCollisionTest()
    {
        var logic = (LogicLayerImplementation)LogicAbstract.CreateAPI();
        logic.CreateScene(1, 100, 100);
        
        var ball = new BallStub
        {
            Position = new Vector(-5, 50),
            Velocity = new Vector(-3, 2),
            Radius = 5
        };
        
        var method = typeof(LogicLayerImplementation).GetMethod("CheckBoundaryCollision", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        method?.Invoke(logic, new object[] { ball });
        
        Assert.AreEqual(5, ball.Position.X);
        Assert.AreEqual(3, ball.Velocity.X);
        Assert.AreEqual(50, ball.Position.Y);
        Assert.AreEqual(2, ball.Velocity.Y);
    }

    [TestMethod]
    public void ElasticCollisionTest()
    {
        var logic = (LogicLayerImplementation)LogicAbstract.CreateAPI();

        var ball1 = new BallStub();
        {
            ball1.Position = new Vector(40, 50);
            ball1.Velocity = new Vector(2, 0);
            ball1.Mass = 10;
            ball1.Radius = 5;
        }
        
        var ball2 = new BallStub();
        {
            ball2.Position = new Vector(48, 50);
            ball2.Velocity = new Vector(-2, 0);
            ball2.Mass = 10;
            ball2.Radius = 5;
        }
        
        var method = typeof(LogicLayerImplementation).GetMethod("CheckBallCollision", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        method?.Invoke(logic, new object[] { ball1, ball2 });
        
        Assert.AreEqual(39, ball1.Position.X);
        Assert.AreEqual(49, ball2.Position.X);
        
        Assert.AreEqual(-2, ball1.Velocity.X);
        Assert.AreEqual(2, ball2.Velocity.X);
        
        Assert.AreEqual(0, ball1.Velocity.Y);
        Assert.AreEqual(0, ball2.Velocity.Y);
    }
}