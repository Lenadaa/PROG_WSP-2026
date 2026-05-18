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
        public async Task ShouldAllSucceedAndBeCounted()
        {
            int numberOfThreads = 50; 
            // Każdy ruch wywołuje zdarzenie 2 razy (dla X i dla Y), stąd mnożenie przez 2
            int expectedNotifications = numberOfThreads * 2;
            int actualNotificationsCount = 0;
            
            IBall ball = new Ball(100, 100);

            object counterLock = new object();

            ball.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "Position")
                {
                    lock (counterLock)
                    {
                        actualNotificationsCount++;
                    }
                }
            };
            Task[] tasks = new Task[numberOfThreads];
            
            for (int i = 0; i < numberOfThreads; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    ball.Move();
                });
            }

            await Task.WhenAll(tasks);

            await Task.Delay(100);
            
            Assert.AreEqual(expectedNotifications, actualNotificationsCount, 
                $"Oczekiwano {expectedNotifications} powiadomień, ale odebrano {actualNotificationsCount}. " +
                "Semafor lub mechanizm synchronizacji zdarzeń gubi wątki!");
        }

        [TestMethod]
        public void Ball_Move_ShouldTriggerEventChain()
        {
            IBall ball = new Ball(200, 200);
            bool eventFired = false;
            ball.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Position")
                {
                    eventFired = true;
                }
            };

            ball.Move();
            Assert.IsTrue(eventFired);
        }
}