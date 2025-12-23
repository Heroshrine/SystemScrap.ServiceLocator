using System;
using NUnit.Framework;
using SystemScrap.ServiceLocator.Framework;

namespace SystemScrap.ServiceLocator.Tests
{
    public class RegistrationHandleTests
    {
        [Test]
        public void ConstructorRequiresDisposeAction()
        {
            Assert.Throws<ArgumentNullException>(() => new RegistrationHandle(null));
        }

        [Test]
        public void Dispose_InvokesActionOnce_AndMarksDisposed()
        {
            var calls = 0;
            var handle = new RegistrationHandle(() => calls++);

            handle.Dispose();
            handle.Dispose();

            Assert.AreEqual(1, calls);
            Assert.IsTrue(handle.IsDisposed);
        }

        [Test]
        public void RegisterTokenCallback_InvokesOnDispose()
        {
            var calls = 0;
            var handle = new RegistrationHandle(() => { });

            handle.RegisterTokenCallback(() => calls++);
            handle.Dispose();

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void RegisterTokenCallback_InvokesImmediatelyIfDisposed()
        {
            var calls = 0;
            var handle = new RegistrationHandle(() => { });
            handle.Dispose();

            handle.RegisterTokenCallback(() => calls++);

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Combine_DisposesChildren()
        {
            var first = new RegistrationHandle(() => { });
            var second = new RegistrationHandle(() => { });
            var combined = RegistrationHandle.Combine(first, second);
            var tokenCalls = 0;
            var token = combined.GetToken();

            token.OnDisposed += () => tokenCalls++;

            first.Dispose();

            Assert.IsFalse(combined.IsDisposed);
            Assert.AreEqual(0, tokenCalls);

            combined.Dispose();

            Assert.IsTrue(combined.IsDisposed);
            Assert.IsTrue(second.IsDisposed);
            Assert.AreEqual(1, tokenCalls);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Combine_DisposesImmediately_WhenInputAlreadyDisposed(bool disposeFirst)
        {
            var first = new RegistrationHandle(() => { });
            var second = new RegistrationHandle(() => { });

            if (disposeFirst)
                first.Dispose();
            else
                second.Dispose();

            var combined = RegistrationHandle.Combine(first, second);

            Assert.IsTrue(combined.IsDisposed);
        }
    }
}