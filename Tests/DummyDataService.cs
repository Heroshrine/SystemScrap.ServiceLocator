
namespace SystemScrap.ServiceLocator.Tests
{
    public class DummyDataService
    {
        public string Guid { get; } = System.Guid.NewGuid().ToString();
        public object Obj { get; set; }

        public override string ToString() => $"<DummyDataService>{Guid}";
    }
}