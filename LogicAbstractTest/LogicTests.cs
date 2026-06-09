using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Logic;
using Data;

namespace LogicTest
{
    public class MockDataAPI : DataAbstract
    {
        private List<IBall> _balls = new List<IBall>();

        public void InjectBalls(List<IBall> balls)
        {
            _balls = balls;
        }

        public override void CreateBalls(int count, double maxX, double maxY)
        {
        }

        public override List<IBall> GetBalls() => _balls;
    }

    [TestClass]
    public class LogicConcurrencyTests
    {
        [TestMethod]
        public void ConcurrencyAndDeadlock_StressTest()
        {
            var logic = LogicAbstract.CreateAPI();
            
            logic.CreateScene(ballCount: 100, width: 50, height: 50);

            Thread.Sleep(500);
            
            logic.Stop();
            
            var balls = logic.GetBalls();
            Assert.IsNotNull(balls);
            Assert.AreEqual(100, balls.Count);
            
            Assert.IsTrue(balls.Any(b => b.MoveCount > 0));
        }

        [TestMethod]
        public void CollisionTest_BallsBounceOffEachOther()
        {
            var mockData = new MockDataAPI();
            var logic = LogicAbstract.CreateAPI(mockData);

            var ball1 = new Ball(1000, 1000);
            ball1.Position.X = 100; ball1.Position.Y = 100;
            ball1.Velocity.X = 10; ball1.Velocity.Y = 0; 
            ball1.Mass = 10;

            var ball2 = new Ball(1000, 1000);
            ball2.Position.X = 100 + ball1.Diameter;
            ball2.Position.Y = 100;
            ball2.Velocity.X = -10; ball2.Velocity.Y = 0; 
            ball2.Mass = 10;

            mockData.InjectBalls(new List<IBall> { ball1, ball2 });
            logic.CreateScene(2, 500, 500);

            Thread.Sleep(200);
            logic.Stop();

            Assert.IsTrue(ball1.Velocity.X < 0);
            Assert.IsTrue(ball2.Velocity.X > 0);
        }
    }
}