namespace Gateway.Handlers
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using Microsoft.ServiceFabric.Services.Communication.Client;

    public class HttpExceptionHandler : IExceptionHandler
    {
        private const bool TransientException = true;
        private const bool NonTransientException = false;
        private const bool KnownException = true;
        private const bool UnknownException = false;
        private OperationRetrySettings operationRetrySettings;

        public bool TryHandleException(ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings, out ExceptionHandlingResult result)
        {
            this.operationRetrySettings = retrySettings;

            if (exceptionInformation.Exception is TimeoutException || exceptionInformation.Exception is SocketException)
            {
                return ResolveAddressAndRetryResult(exceptionInformation, out result);
            }

            var wasHandled = HandleIfWebException(exceptionInformation, out result);
            if (wasHandled) return true;

            return UnhandledExceptionResult(out result);
        }

        private bool HandleIfWebException(ExceptionInformation exceptionInformation, out ExceptionHandlingResult result)
        {
            result = null;

            WebException we = TryGetWebExceptionFrom(exceptionInformation);

            if (we == null) return false;

            var errorResponse = we.Response as HttpWebResponse;

            if (we.Status == WebExceptionStatus.ProtocolError && errorResponse != null)
            {
                if (IsGatewayStatusCode(errorResponse.StatusCode))
                {
                    return ResolveAddressAndRetryResult(exceptionInformation, out result);
                }

                if (errorResponse.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    return RetryResult(exceptionInformation, out result);
                }
            }

            if (IsTransientRetryableWebException(we))
            {
                return ResolveAddressAndRetryResult(exceptionInformation, out result);
            }

            if (IsNonTransientRetryableWebException(we))
            {
                return RetryResult(exceptionInformation, out result);
            }

            return false;
        }

        private static WebException TryGetWebExceptionFrom(ExceptionInformation exceptionInformation)
        {
            return exceptionInformation.Exception as WebException ?? exceptionInformation.Exception.InnerException as WebException;
        }

        private static bool IsGatewayStatusCode(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.BadGateway ||
                   statusCode == HttpStatusCode.GatewayTimeout;
        }

        private static bool IsTransientRetryableWebException(WebException webException)
        {
            return webException.Status == WebExceptionStatus.Timeout ||
                   webException.Status == WebExceptionStatus.RequestCanceled ||
                   webException.Status == WebExceptionStatus.ConnectionClosed ||
                   webException.Status == WebExceptionStatus.ConnectFailure;
        }

        private static bool IsNonTransientRetryableWebException(WebException webException)
        {
            return webException.Status == WebExceptionStatus.ReceiveFailure ||
                   webException.Status == WebExceptionStatus.SendFailure ||
                   webException.Status == WebExceptionStatus.PipelineFailure;
        }

        private static bool UnhandledExceptionResult(out ExceptionHandlingResult result)
        {
            result = null;
            return UnknownException;
        }

        private bool ResolveAddressAndRetryResult(ExceptionInformation exceptionInformation, out ExceptionHandlingResult result)
        {
            result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, NonTransientException, this.operationRetrySettings, this.operationRetrySettings.DefaultMaxRetryCount);
            return KnownException;
        }

        private bool RetryResult(ExceptionInformation exceptionInformation, out ExceptionHandlingResult result)
        {
            result = new ExceptionHandlingRetryResult(exceptionInformation.Exception, TransientException, this.operationRetrySettings, this.operationRetrySettings.DefaultMaxRetryCount);
            return KnownException;
        }
    }
}
