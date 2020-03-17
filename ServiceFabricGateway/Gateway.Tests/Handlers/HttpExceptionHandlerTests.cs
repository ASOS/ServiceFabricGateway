using System;
using System.Net;
using System.Net.Sockets;
using Gateway.Handlers;
using Microsoft.ServiceFabric.Services.Communication.Client;
using NUnit.Framework;
using Moq;

namespace Gateway.Tests.Handlers
{
    [TestFixture]
    public class HttpExceptionHandlerTests
    {
        private HttpExceptionHandler httpExceptionHandler;
        private OperationRetrySettings operationRetrySettings;

        [SetUp]
        public void Initialise()
        {
            this.httpExceptionHandler = new HttpExceptionHandler();
            this.operationRetrySettings = new OperationRetrySettings();
        }

        [Test]
        public void when_handling_a_timeoutexception_then_a_transient_retry_result_that_resolves_the_address_is_returned()
        {
            // Arrange
            ExceptionHandlingResult exceptionHandlingResult = null;
            var exceptionInformation = new ExceptionInformation(new TimeoutException());

            // Act
            var isKnownException = this.httpExceptionHandler.TryHandleException(exceptionInformation, operationRetrySettings, out exceptionHandlingResult);

            // Assert
            AssertIsTransientRetryResultThatResolvesTheAddress(exceptionHandlingResult, isKnownException);
        }

        [Test]
        public void when_handling_a_socketexception_then_a_transient_retry_result_that_resolves_the_address_is_returned()
        {
            // Arrange
            ExceptionHandlingResult exceptionHandlingResult = null;
            var exceptionInformation = new ExceptionInformation(new SocketException());

            // Act
            var isKnownException = this.httpExceptionHandler.TryHandleException(exceptionInformation, operationRetrySettings, out exceptionHandlingResult);

            // Assert
            AssertIsTransientRetryResultThatResolvesTheAddress(exceptionHandlingResult, isKnownException);
        }

        [TestCase(WebExceptionStatus.Timeout)]
        [TestCase(WebExceptionStatus.RequestCanceled)]
        [TestCase(WebExceptionStatus.ConnectionClosed)]
        [TestCase(WebExceptionStatus.ConnectFailure)]
        public void when_handling_a_webexception_with_retryable_status_then_a_transient_retry_result_that_resolves_the_address_is_returned(WebExceptionStatus webExceptionStatus)
        {
            // Arrange
            ExceptionHandlingResult exceptionHandlingResult = null;
            var exceptionInformation = new ExceptionInformation(new WebException(string.Empty, webExceptionStatus));

            // Act
            var isKnownException = this.httpExceptionHandler.TryHandleException(exceptionInformation, operationRetrySettings, out exceptionHandlingResult);

            // Assert
            AssertIsTransientRetryResultThatResolvesTheAddress(exceptionHandlingResult, isKnownException);
        }

        [TestCase(WebExceptionStatus.ReceiveFailure)]
        [TestCase(WebExceptionStatus.SendFailure)]
        [TestCase(WebExceptionStatus.PipelineFailure)]
        public void when_handling_a_webexception_with_retryable_status_then_a_non_transient_retry_result_that_does_not_resolve_the_address_is_returned(WebExceptionStatus webExceptionStatus)
        {
            // Arrange
            ExceptionHandlingResult exceptionHandlingResult = null;
            var exceptionInformation = new ExceptionInformation(new WebException(string.Empty, webExceptionStatus));

            // Act
            var isKnownException = this.httpExceptionHandler.TryHandleException(exceptionInformation, operationRetrySettings, out exceptionHandlingResult);

            // Assert
            AssertIsTransientRetryResultThatDoesNotResolveTheAddress(exceptionHandlingResult, isKnownException);
        }

        [Test]
        public void when_handling_a_webexception_with_a_badgateway_http_status_code_then_a_transient_retry_result_that_resolves_the_address_is_returned()
        {
            // Arrange
            ExceptionHandlingResult exceptionHandlingResult = null;
            var exceptionInformation = GetExceptionInformationForWebExceptionWithStatusCode(HttpStatusCode.BadGateway);

            // Act
            var isKnownException = this.httpExceptionHandler.TryHandleException(exceptionInformation, operationRetrySettings, out exceptionHandlingResult);

            // Assert
            AssertIsTransientRetryResultThatResolvesTheAddress(exceptionHandlingResult, isKnownException);
        }

        [Test]
        public void when_handling_a_webexception_with_a_gatewaytimeout_http_status_code_then_a_transient_retry_result_that_resolves_the_address_is_returned()
        {
            // Arrange
            ExceptionHandlingResult exceptionHandlingResult = null;
            var exceptionInformation = GetExceptionInformationForWebExceptionWithStatusCode(HttpStatusCode.GatewayTimeout);

            // Act
            var isKnownException = this.httpExceptionHandler.TryHandleException(exceptionInformation, operationRetrySettings, out exceptionHandlingResult);

            // Assert
            AssertIsTransientRetryResultThatResolvesTheAddress(exceptionHandlingResult, isKnownException);
        }

        [Test]
        public void when_handling_a_webexception_with_a_serviceunavailable_http_status_code_then_a_transient_retry_result_that_does_not_resolve_the_address_is_returned()
        {
            // Arrange
            ExceptionHandlingResult exceptionHandlingResult = null;
            var exceptionInformation = GetExceptionInformationForWebExceptionWithStatusCode(HttpStatusCode.ServiceUnavailable);

            // Act
            var isKnownException = this.httpExceptionHandler.TryHandleException(exceptionInformation, operationRetrySettings, out exceptionHandlingResult);

            // Assert
            AssertIsTransientRetryResultThatDoesNotResolveTheAddress(exceptionHandlingResult, isKnownException);
        }

        [TestCase(HttpStatusCode.HttpVersionNotSupported)]
        [TestCase(HttpStatusCode.InternalServerError)]
        [TestCase(HttpStatusCode.NotImplemented)]
        public void when_handling_a_webexception_with_a_5xx_non_transient_and_non_retryable_http_status_code_then_no_retry_result_is_returned(HttpStatusCode httpStatusCode)
        {
            // Arrange
            ExceptionHandlingResult exceptionHandlingResult = null;
            var exceptionInformation = GetExceptionInformationForWebExceptionWithStatusCode(httpStatusCode);

            // Act
            var isKnownException = this.httpExceptionHandler.TryHandleException(exceptionInformation, operationRetrySettings, out exceptionHandlingResult);

            // Assert
            Assert.That(isKnownException, Is.False);
            Assert.That(exceptionHandlingResult, Is.Null);
        }

        [Test]
        public void when_handling_a_non_retryable_exception_then_no_retry_result_is_returned()
        {
            // Arrange
            ExceptionHandlingResult exceptionHandlingResult = null;
            var exceptionInformation = new ExceptionInformation(new InvalidOperationException());

            // Act
            var isKnownException = this.httpExceptionHandler.TryHandleException(exceptionInformation, operationRetrySettings, out exceptionHandlingResult);

            // Assert
            Assert.That(isKnownException, Is.False);
            Assert.That(exceptionHandlingResult, Is.Null);
        }

        private static ExceptionInformation GetExceptionInformationForWebExceptionWithStatusCode(HttpStatusCode httpStatusCode)
        {
            var httpWebResponse = new Mock<HttpWebResponse>();
            httpWebResponse.SetupGet(p => p.StatusCode).Returns(httpStatusCode);

            return new ExceptionInformation(
                new WebException(
                    string.Empty,
                    null,
                    WebExceptionStatus.ProtocolError,
                    httpWebResponse.Object));
        }

        private static void AssertIsTransientRetryResultThatResolvesTheAddress(ExceptionHandlingResult exceptionHandlingResult, bool isKnownException)
        {
            AssertIsTransientRetryResult(exceptionHandlingResult, isKnownException, true);
        }

        private static void AssertIsTransientRetryResultThatDoesNotResolveTheAddress(ExceptionHandlingResult exceptionHandlingResult, bool isKnownException)
        {
            AssertIsTransientRetryResult(exceptionHandlingResult, isKnownException, false);
        }

        private static void AssertIsTransientRetryResult(ExceptionHandlingResult exceptionHandlingResult, bool isKnownException, bool resolvesAddress)
        {
            var exceptionHandlingRetryResult = exceptionHandlingResult as ExceptionHandlingRetryResult;
            Assert.That(isKnownException, Is.True);
            Assert.That(exceptionHandlingRetryResult, Is.Not.Null);
            Assert.That(exceptionHandlingRetryResult.IsTransient, Is.EqualTo(!resolvesAddress)); // This is the property that tells the ServicePartitionClient to re-resolve the address and try again
        }
    }
}
