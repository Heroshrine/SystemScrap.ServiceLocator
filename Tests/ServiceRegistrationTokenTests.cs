using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SystemScrap.ServiceLocator.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SystemScrap.ServiceLocator.Tests
{
    public class ServiceRegistrationTokenTests
    {
        [Test]
        public void TokenReflectsHandleDisposedState()
        {
            var handle = new RegistrationHandle(() => { });
            var token = handle.GetToken();

            Assert.IsTrue(token.IsValid);
            Assert.IsFalse(token.IsDisposed);

            handle.Dispose();

            Assert.IsTrue(token.IsDisposed);
        }

        [Test]
        public void OnDisposedInvokedWhenHandleDisposed()
        {
            var handle = new RegistrationHandle(() => { });
            var token = handle.GetToken();
            var calls = 0;

            token.OnDisposed += () => calls++;

            handle.Dispose();

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void OnDisposedExceptionsAreLogged()
        {
            var handle = new RegistrationHandle(() => { });
            var token = handle.GetToken();

            token.OnDisposed += () => throw new InvalidOperationException("boom");

            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            handle.Dispose();
        }

        [Test]
        public void OnDisposedInvokedImmediatelyWhenSubscribingAfterDisposed()
        {
            var handle = new RegistrationHandle(() => { });
            var token = handle.GetToken();
            var calls = 0;

            handle.Dispose();
            token.OnDisposed += () => calls++;

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void OnDisposedExceptionsDoNotStopOtherCallbacks()
        {
            var handle = new RegistrationHandle(() => { });
            var token = handle.GetToken();
            var calls = 0;

            token.OnDisposed += () => throw new InvalidOperationException("boom");
            token.OnDisposed += () => calls++;

            LogAssert.Expect(LogType.Exception, new Regex("boom"));

            handle.Dispose();

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void RemovingWorks()
        {
            var handle = new RegistrationHandle(() => { });
            var token = handle.GetToken();
            var calls = 0;

            token.OnDisposed += Callback;
            token.OnDisposed -= Callback;

            handle.Dispose();

            Assert.AreEqual(0, calls);

            return;

            void Callback() => calls++;
        }

        [Test]
        public void MultipleSubscribersWork()
        {
            var handle = new RegistrationHandle(() => { });
            var token = handle.GetToken();
            var calls = 0;

            token.OnDisposed += Callback;
            token.OnDisposed += Callback;

            handle.Dispose();
            
            Assert.AreEqual(2, calls);
            
            handle = new RegistrationHandle(() => { });
            token = handle.GetToken();
            
            token.OnDisposed += Callback;
            token.OnDisposed += Callback;
            token.OnDisposed -= Callback;
            
            handle.Dispose();
            
            Assert.AreEqual(3, calls);

            return;

            void Callback() => calls++;
        }
    }
}