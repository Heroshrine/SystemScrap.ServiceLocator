using NUnit.Framework;
using SystemScrap.ServiceLocator.Core;

namespace SystemScrap.ServiceLocator.Tests
{
    public class BootstrapperTests
    {
        [Test]
        public void LocatorIsNotNull()
        {
            Assert.IsNotNull(Services.GetLocator());
        }
    }
}