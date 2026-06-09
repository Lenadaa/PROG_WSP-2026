using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using Data;

namespace DataTest
{
    [TestClass]
    public class RealTimeMovementTests
    {
        [TestMethod]
        public void RealTimeMovement_DeltaTimeChangesPosition()
        {
            var ball = new Ball(500, 500);
            
            double startX = ball.Position.X;
            double startY = ball.Position.Y;
            int initialMoveCount = ball.MoveCount;

            ball.Start();
            Thread.Sleep(200); 
            ball.Stop();

            Assert.IsTrue(ball.MoveCount > initialMoveCount);
            
            Assert.AreNotEqual(startX, ball.Position.X);
            Assert.AreNotEqual(startY, ball.Position.Y);
        }

        [TestMethod]
        public void Stop_ShouldTerminateThreadCorrectly()
        {
            var ball = new Ball(500, 500);
            ball.Start();
            Thread.Sleep(50); 

            ball.Stop();
            int moveCountAfterStop = ball.MoveCount;

            Thread.Sleep(100);


            Assert.AreEqual(moveCountAfterStop, ball.MoveCount);
        }
    }
}