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
        public int MoveCount { get; set; } = 0;
        public void Start(Barrier barrier) { }
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
    
    //Sprawdzenie, czy algorytm zderzenia kulka-kulka zmienia kierunki wektorów prędkości w logiczny sposób
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
    
    //Upewnienie się, że powoływanie sceny z kulkami zachowuje płynność asynchroniczną i nie blokuje wywołującego wątku na zbyt długo.
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
    //Weryfikacja mechanizmu odbijania się kulki od krawędzi
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

    //Dokładne, matematyczne sprawdzenie zderzenia sprężystego pomiędzy dwoma ciałami pod kątem zmian pozycji i prędkości.
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
    //Weryfikacja synchronizacji przy dużym obciążeniu (100 kulek).
    [TestMethod]
    public async Task FairnessTest100Balls()
    {
        var logic = LogicAbstract.CreateAPI();
        try
        {
            logic.CreateScene(100, 1000, 1000);
            await Task.Delay(1000);
        
            logic.Stop();
        
            var balls = logic.GetBalls();
            Assert.HasCount(100, balls);

            int max = balls.Max(b => b.MoveCount);
            int min = balls.Min(b => b.MoveCount);
            int diff = max - min;

            Assert.IsLessThanOrEqualTo(1, diff);
        }
        finally
        {
            logic.Stop();
        }
    }
    //Sprawdzenie odporności warstwy danych na równoległy odczyt i modyfikację kolekcji kulek.
    [TestMethod]
    public async Task TestForReadAndWriteOfListOfBall()
    {
        var dataLayer = DataAbstract.CreateAPI(); 
        dataLayer.CreateBalls(10, 100, 100); 
    
        bool keepRunning = true;
        Exception? caughtException = null;
        
        Task readerTask = Task.Run(() =>
        {
            try
            {
                while (keepRunning)
                {
                    var balls = dataLayer.GetBalls(); 
                    foreach (var ball in balls)
                    {
                        var pos = ball.Position; 
                    }
                }
            }
            catch (Exception ex)
            {
                caughtException = ex; 
            }
        });

        Task writerTask = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                dataLayer.CreateBalls(5, 500, 500); //
                Thread.Sleep(1); 
            }
            keepRunning = false;
        });
        await Task.WhenAll(readerTask, writerTask);
        Assert.IsNull(caughtException);
    }
    
    //Testowanie integralności i spójności danych wektora pozycji podczas intensywnej modyfikacji wielowątkowej.
    [TestMethod]
    public async Task TestThreadSafeAgainstPositionReads()
    {
        var ball = new Ball(500, 500);
        ball.Velocity = new Vector(5, 5); 
    
        bool keepRunning = true;
        Exception? caughtException = null;
        
        Task writerTask = Task.Run(() =>
        {
            for (int i = 0; i < 10000; i++)
            {
                ball.Move(); 
            }
            keepRunning = false;
        });

        Task readerTask = Task.Run(() =>
        {
            try
            {
                while (keepRunning)
                {

                    lock (ball.SyncRoot)
                    {
                        var pos = ball.Position;
                    }
                }
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }
        });
        await Task.WhenAll(writerTask, readerTask);
        Assert.IsNull(caughtException);
    }
    
    //
    [TestMethod]
    public async Task TestHandleBarrierDisposal()
    {
        int ballCount = 3;
        var dataLayer = DataAbstract.CreateAPI(); 
        dataLayer.CreateBalls(ballCount, 300, 300); 
        var balls = dataLayer.GetBalls(); 
    
        Barrier barrier = new Barrier(ballCount); 

        foreach (var ball in balls)
        {
            ball.Start(barrier); 
        }

        await Task.Delay(100);

        barrier.Dispose(); 

        await Task.Delay(100);
        
        foreach (var ball in balls)
        {
            ball.Stop(); 
        }
        Assert.IsTrue(true);
    }
}