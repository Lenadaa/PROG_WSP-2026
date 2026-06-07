using Logic;
using Data;

namespace LogicAbstractTest;

[TestClass]
public class LogicLayerTests
{
    /// <summary>
    /// Stub dający pełną kontrolę nad parametrami kulki bez losowości warstwy Data.
    /// </summary>
    private class BallStub : IBall
    {
        private static int _idCounter = 0;
        public int Id { get; } = Interlocked.Increment(ref _idCounter);

        public Vector Position { get; set; } = new Vector(0, 0);
        public Vector Velocity { get; set; } = new Vector(0, 0);
        public double Mass     { get; set; } = 1.0;
        public double Radius   { get; set; } = 5.0;
        public double Diameter => Radius * 2;
        public object SyncRoot { get; } = new object();
        public int MoveCount   { get; set; } = 0;

        public void Move() { }
        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    // Collision velocity direction
    /// <summary>
    /// Sprawdza, czy algorytm zderzenia kulka-kulka odwraca wektory prędkości
    /// w logiczny sposób (dwie kule jadące na siebie zamieniają kierunki).
    /// </summary>
    [TestMethod]
    public void CheckBallCollisionVelocity()
    {
        var logic = (LogicLayerImplementation)LogicAbstract.CreateAPI();
        var ball1 = new BallStub { Position = new Vector(45, 50), Velocity = new Vector(2, 0), Mass = 10 };
        var ball2 = new BallStub { Position = new Vector(52, 50), Velocity = new Vector(-2, 0), Mass = 10 };

        double initialV1x = ball1.Velocity.X;
        double initialV2x = ball2.Velocity.X;

        var method = typeof(LogicLayerImplementation).GetMethod(
            "TryResolveBallCollision",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(logic, new object[] { ball1, ball2 });

        // Prędkości muszą się zmienić
        Assert.AreNotEqual(initialV1x, ball1.Velocity.X);
        Assert.AreNotEqual(initialV2x, ball2.Velocity.X);

        // ball1 jechał w prawo (+2) — po zderzeniu powinien jechać w lewo
        Assert.IsTrue(ball1.Velocity.X < 0, $"ball1.Vx powinno być ujemne, jest {ball1.Velocity.X}");
        // ball2 jechał w lewo (-2) — po zderzeniu powinien jechać w prawo
        Assert.IsTrue(ball2.Velocity.X > 0, $"ball2.Vx powinno być dodatnie, jest {ball2.Velocity.X}");
    }

    // Stability (async)
    /// <summary>
    /// Upewnia się, że CreateScene nie blokuje wywołującego wątku i
    /// po odczekaniu kulki wciąż istnieją.
    /// </summary>
    [TestMethod]
    public async Task StabilityTest()
    {
        var logic = LogicAbstract.CreateAPI();
        int sampleDurationMs = 200;

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            logic.CreateScene(2, 100, 100);
            sw.Stop();

            Assert.IsTrue(sw.ElapsedMilliseconds < 100,
                $"CreateScene zablokowała na {sw.ElapsedMilliseconds} ms");

            await Task.Delay(sampleDurationMs);

            var balls = logic.GetBalls();
            Assert.IsNotNull(balls);
            Assert.AreEqual(2, balls.Count);
        }
        finally
        {
            logic.Stop();
        }
    }

    // Wall collision
    /// <summary>
    /// Weryfikuje mechanizm odbijania od lewej ściany:
    /// x ujemne → odbicie z pozytywną prędkością X.
    /// </summary>
    [TestMethod]
    public void WallCollisionTest()
    {
        var logic = (LogicLayerImplementation)LogicAbstract.CreateAPI();
        logic.CreateScene(1, 100, 100);

        var ball = new BallStub
        {
            Position = new Vector(-5, 50),
            Velocity = new Vector(-3, 2),
            Radius   = 5
        };

        var method = typeof(LogicLayerImplementation).GetMethod(
            "CheckBoundaryCollision",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(logic, new object[] { ball });

        Assert.AreEqual(5,  ball.Position.X, delta: 0.001);
        Assert.IsTrue(ball.Velocity.X > 0, "Po odbiciu od lewej ściany VX powinno być dodatnie");
        Assert.AreEqual(50, ball.Position.Y, delta: 0.001);
        Assert.AreEqual(2,  ball.Velocity.Y, delta: 0.001);

        logic.Stop();
    }

    //  Elastic collision math
    /// <summary>
    /// Matematyczna weryfikacja zderzenia sprężystego dla równych mas:
    /// kule wymieniają prędkości.
    /// </summary>
    [TestMethod]
    public void ElasticCollisionTest()
    {
        var logic = (LogicLayerImplementation)LogicAbstract.CreateAPI();

        var ball1 = new BallStub { Position = new Vector(40, 50), Velocity = new Vector(2, 0), Mass = 10, Radius = 5 };
        var ball2 = new BallStub { Position = new Vector(48, 50), Velocity = new Vector(-2, 0), Mass = 10, Radius = 5 };

        var method = typeof(LogicLayerImplementation).GetMethod(
            "TryResolveBallCollision",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(logic, new object[] { ball1, ball2 });

        Assert.AreEqual(-2, ball1.Velocity.X, delta: 0.001);
        Assert.AreEqual(2,  ball2.Velocity.X, delta: 0.001);
        Assert.AreEqual(0,  ball1.Velocity.Y, delta: 0.001);
        Assert.AreEqual(0,  ball2.Velocity.Y, delta: 0.001);
    }

    // Fairness (100 balls)
    /// <summary>
    /// Weryfikuje, że przy 100 kulkach żadna nie jest głodzona —
    /// różnica w liczbie kroków między najszybszą a najwolniejszą kulką
    /// nie przekracza rozsądnego progu (20% wartości mediany).
    /// </summary>
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
            Assert.AreEqual(100, balls.Count);

            int max    = balls.Max(b => b.MoveCount);
            int min    = balls.Min(b => b.MoveCount);
            double avg = balls.Average(b => b.MoveCount);
            int diff   = max - min;

            Assert.IsTrue(min > 0, $"Kulka o najmniejszym MoveCount: {min} — powinna być > 0");

            double threshold = avg * 0.20;
            Assert.IsTrue(diff <= threshold,
                $"Rozrzut MoveCount ({diff}) przekracza 20% średniej ({threshold:F1}). Min={min}, Max={max}, Avg={avg:F1}");
        }
        finally
        {
            logic.Stop();
        }
    }
    
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
                        _ = ball.Position;
                    }
                }
            }
            catch (Exception ex) { caughtException = ex; }
        });

        Task writerTask = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                dataLayer.CreateBalls(5, 500, 500);
                Thread.Sleep(1);
            }
            keepRunning = false;
        });

        await Task.WhenAll(readerTask, writerTask);
        Assert.IsNull(caughtException, $"Wyjątek przy równoległym dostępie: {caughtException?.Message}");
    }
    [TestMethod]
    public async Task NoDeadlockTest()
    {
        var logic = LogicAbstract.CreateAPI();
        try
        {
            logic.CreateScene(20, 200, 200);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await Task.Delay(500, cts.Token).ContinueWith(_ => { });

            logic.Stop();

            Assert.IsFalse(cts.IsCancellationRequested, "Wykryto potencjalny deadlock — Stop() nie wrócił w czasie.");
        }
        catch (OperationCanceledException)
        {
            Assert.Fail("Deadlock — symulacja nie zatrzymała się w ciągu 2 sekund.");
        }
        finally
        {
            logic.Stop();
        }
    }
}