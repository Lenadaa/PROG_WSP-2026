using Data;

namespace BallsTests
{
    [TestClass]
    public class BallDataTests
    {
        private DataAbstract _dataApi;

        [TestInitialize]
        public void Setup()
        {
            _dataApi = DataAbstract.CreateAPI();
        }
        
        [TestMethod]
        public void BallVelocitydNotBeZeroInitially()
        {
            _dataApi.CreateBalls(10, 100, 100);
            var balls = _dataApi.GetBalls();

            foreach (var ball in balls)
            {
                bool isMoving = Math.Abs(ball.Velocity.X) > 0 || Math.Abs(ball.Velocity.Y) > 0;
                Assert.IsTrue(isMoving);
            }
        }
        [TestMethod]
        public void IndependentInstances()
        {
            _dataApi.CreateBalls(2, 100, 100);
            var balls = _dataApi.GetBalls();
            IBall ball1 = balls[0];
            IBall ball2 = balls[1];

            ball1.Velocity = new Vector(99, 99);

            Assert.AreNotEqual(ball1.Velocity.X, ball2.Velocity.X);
            Assert.AreEqual(99, ball1.Velocity.X);
        }
        
        [TestMethod]
        public void ShouldReturnCorrectSum()
        {
            Vector v1 = new Vector(10, 20);
            Vector v2 = new Vector(5, -5);

            Vector result = new Vector(v1.X + v2.X, v1.Y + v2.Y);

            Assert.AreEqual(15, result.X);
            Assert.AreEqual(15, result.Y);
        }

        [TestMethod]
        public void ShouldPreserveValues()
        {
            
            var dataApi = DataAbstract.CreateAPI();
            dataApi.CreateBalls(1, 100, 100);
            var ball = dataApi.GetBalls()[0];

            Assert.IsNotNull(ball.Position);
            Assert.IsNotNull(ball.Velocity);
            Assert.IsGreaterThan(0, ball.Radius);
            Assert.IsGreaterThan(0, ball.Mass);
        }
    }
}