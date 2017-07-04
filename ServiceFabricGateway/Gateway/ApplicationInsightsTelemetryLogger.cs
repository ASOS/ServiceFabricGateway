using System;
using System.Net;
using System.Net.Http;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Gateway
{
    public class ApplicationInsightsTelemetryLogger : ITelemetryLogger
    {
        private readonly TelemetryClient client;

        public ApplicationInsightsTelemetryLogger(TelemetryClient client)
        {
            this.client = client;
        }

        public void RequestCompleted(HttpRequestMessage request, DateTimeOffset startDate, HttpStatusCode responseCode, TimeSpan duration)
        {
            var requestTelemetry = CreateRequestTelemetry(request, startDate);
            TrackRequest(requestTelemetry, responseCode, duration);
        }

        public void ErrorOccurred(Exception exception)
        {
            TrackException(exception);
        }

        private static RequestTelemetry CreateRequestTelemetry(HttpRequestMessage request, DateTimeOffset startDate)
        {
            var requestTelemetry = new RequestTelemetry
            {
                Name = $"{request.Method.ToString().ToUpper()} {request.RequestUri}",
                Timestamp = startDate,
                Url = request.RequestUri
            };
            return requestTelemetry;
        }

        private void TrackRequest(RequestTelemetry requestTelemetry, HttpStatusCode responseStatus, TimeSpan duration)
        {
            var statusCode = (int)responseStatus;
            requestTelemetry.Duration = duration;
            requestTelemetry.ResponseCode = statusCode.ToString();
            requestTelemetry.Success = statusCode < 400;
            client.TrackRequest(requestTelemetry);
        }

        private void TrackException(Exception ex)
        {
            var exceptionTelemtry = new ExceptionTelemetry(ex);

            foreach (string key in ex.Data.Keys)
            {
                exceptionTelemtry.Properties.Add(key, ex.Data[key].ToString());
            }

            client.TrackException(exceptionTelemtry);
        }
    }
}