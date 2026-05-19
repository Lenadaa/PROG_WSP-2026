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
    //Test sprawdza poprawność implementacji wzorca Observer
        [TestMethod]
        public void BallTriggerEventChain()
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