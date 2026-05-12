using Data;

namespace DataApiTest;

[TestClass]
public sealed class Test1
{
    private DataAbstract _dataApi;

    [TestInitialize]
    public void Setup()
    {
        _dataApi = DataAbstract.CreateAPI();
    }

    [TestMethod]
    public void ShouldGenerateCorrectAmount()
    {
        var dataApi = DataAbstract.CreateAPI();
        int count = 10;

        dataApi.CreateBalls(count, 800, 500);
        var balls = dataApi.GetBalls();

        Assert.HasCount(count, balls);
    }

    [TestMethod]
    public void ShouldBeWithinBounds()
    {
        var dataApi = DataAbstract.CreateAPI();
        double maxX = 100;
        double maxY = 100;

        dataApi.CreateBalls(50, maxX, maxY);
        var balls = dataApi.GetBalls();

        foreach (var ball in balls)
        {
            Assert.IsTrue(ball.Position.X >= 0 && ball.Position.X <= maxX, "Ball X out of bounds");
            Assert.IsTrue(ball.Position.Y >= 0 && ball.Position.Y <= maxY, "Ball Y out of bounds");
        }
    }
    [TestMethod]
    public void ShouldCalculateCorrectDifference()
    {
        Vector v1 = new Vector(10.5, 20.0);
        Vector v2 = new Vector(5.5, 10.0);

        Vector result = new Vector(v1.X - v2.X, v1.Y - v2.Y);

        Assert.AreEqual(5.0, result.X);
        Assert.AreEqual(10.0, result.Y);
    }
    [TestMethod]
    public void ShouldBeConsistent()
    {
        _dataApi.CreateBalls(1, 100, 100);
        IBall ball = _dataApi.GetBalls()[0];
        
        Assert.IsGreaterThan(0, ball.Radius);
        Assert.AreEqual(ball.Radius * 2, ball.Diameter);
        Assert.IsGreaterThan(0, ball.Mass);
    }
    
    [TestMethod]
    public void WithZeroCountEmptyList()
    {
        _dataApi.CreateBalls(0, 100, 100);
        
        Assert.IsEmpty(_dataApi.GetBalls());
    }
    [TestMethod]
    public void DataApi_GetBalls_ShouldReturnSnapshot()
    {
        // Arrange
        _dataApi.CreateBalls(2, 100, 100);

        var firstCall = _dataApi.GetBalls();
        var secondCall = _dataApi.GetBalls();
        
        Assert.AreNotSame(firstCall, secondCall);
        Assert.HasCount(firstCall.Count, secondCall);
    }
}