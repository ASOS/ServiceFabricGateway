using System;
using Gateway.Handlers;
using NUnit.Framework;

namespace Gateway.Tests.Handlers
{
    [TestFixture]
    public class GatewayHandlerTests
    {
        [Test]
        public void given_a_null_client_proxy_an_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => new GatewayHandler(null));
        }
    }
}
