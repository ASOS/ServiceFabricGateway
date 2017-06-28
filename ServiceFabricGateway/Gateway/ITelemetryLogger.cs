using System;
using System.Net;
using System.Net.Http;

namespace Gateway
{
    public interface ITelemetryLogger
    {
        void RequestCompleted(HttpRequestMessage request, DateTimeOffset startDate, HttpStatusCode responseCode, TimeSpan duration);

        void ErrorOccurred(Exception exception);

        void ErrorOccurred(ProxyToServiceInvokeException exception);
    }
}