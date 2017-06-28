using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Gateway.Handlers;
using NUnit.Framework;

namespace Gateway.Tests.Handlers
{
    [TestFixture]
    public class TelemetryTests
    {
        [Test]
        public async Task when_a_proxying_error_is_thrown_it_is_logged()
        {
            var logger = new FakeTelemetryLogger();
            var sut = new TelemetryHandler(() => logger);
            sut.InnerHandler = new FakeDelegatingHandler(new ProxyToServiceInvokeException(new Exception("Test"), new Uri("http://attempted.com")));
            var invoker = new HttpMessageInvoker(sut);

            Assert.Throws<ProxyToServiceInvokeException>(async () => await invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None));

            Assert.That(logger.ProxyToServiceErrorOccurredCalled, Is.True);
            Assert.That(logger.RequestCompletedCalled, Is.True);
        }

        [Test]
        public async Task when_an_error_is_thrown_it_is_logged()
        {
            var logger = new FakeTelemetryLogger();
            var sut = new TelemetryHandler(() => logger);
            sut.InnerHandler = new FakeDelegatingHandler(new Exception("Test"));
            var invoker = new HttpMessageInvoker(sut);

            Assert.Throws<Exception>(async () => await invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None));

            Assert.That(logger.ErrorOccurredCalled, Is.True);
            Assert.That(logger.RequestCompletedCalled, Is.True);
        }

        [Test]
        public async Task when_any_successful_response_is_returned_it_is_logged()
        {
            var logger = new FakeTelemetryLogger();
            var sut = new TelemetryHandler(() => logger);
            sut.InnerHandler = new FakeDelegatingHandler();
            var invoker = new HttpMessageInvoker(sut);

            await invoker.SendAsync(new HttpRequestMessage(), CancellationToken.None);

            Assert.That(logger.ProxyToServiceErrorOccurredCalled, Is.False);
            Assert.That(logger.ErrorOccurredCalled, Is.False);
            Assert.That(logger.RequestCompletedCalled);
        }

        internal class FakeTelemetryLogger : ITelemetryLogger
        {
            internal bool RequestCompletedCalled { get; set; }

            internal bool ErrorOccurredCalled { get; set; }

            internal bool ProxyToServiceErrorOccurredCalled { get; set; }

            public void RequestCompleted(HttpRequestMessage request, DateTimeOffset startDate, HttpStatusCode responseCode, TimeSpan duration)
            {
                RequestCompletedCalled = true;
            }

            public void ErrorOccurred(Exception exception)
            {
                ErrorOccurredCalled = true;
            }

            public void ErrorOccurred(ProxyToServiceInvokeException exception)
            {
                ProxyToServiceErrorOccurredCalled = true;
            }
        }

        private class FakeDelegatingHandler : DelegatingHandler
        {
            private readonly Exception ex;

            public FakeDelegatingHandler()
            {
            }

            public FakeDelegatingHandler(Exception ex)
            {
                this.ex = ex;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (this.ex != null)
                {
                    throw ex;
                }

                return Task.FromResult(new HttpResponseMessage {StatusCode = HttpStatusCode.OK});
            }
        }
    }
}
