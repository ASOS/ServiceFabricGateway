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
        private readonly string serviceFabricEndpoint;
        private readonly RequestTelemetry requestTelemetry = new RequestTelemetry();

        public ApplicationInsightsTelemetryLogger(TelemetryClient client, string serviceFabricEndpoint)
        {
            this.client = client;
            this.serviceFabricEndpoint = serviceFabricEndpoint;
        }

        public void RequestCompleted(HttpRequestMessage request, DateTimeOffset startDate, HttpStatusCode responseCode, TimeSpan duration)
        {
            ConfigureRequestTelemetry(request, startDate);
            TrackRequest(responseCode, duration);
        }

        public void ErrorOccurred(Exception exception)
        {
            TrackException(exception);
        }

        private void ConfigureRequestTelemetry(HttpRequestMessage request, DateTimeOffset startDate)
        {
            this.requestTelemetry.Name = $"{request.Method.ToString().ToUpper()} {request.RequestUri}";
            this.requestTelemetry.Timestamp = startDate;
            this.requestTelemetry.Url = request.RequestUri;
            this.requestTelemetry.Properties.Add("ServiceFabricEndpoint", this.serviceFabricEndpoint);
        }

        private void TrackRequest(HttpStatusCode responseStatus, TimeSpan duration)
        {
            var statusCode = (int)responseStatus;
            this.requestTelemetry.Duration = duration;
            this.requestTelemetry.ResponseCode = statusCode.ToString();
            this.requestTelemetry.Success = statusCode < 400;
            client.TrackRequest(this.requestTelemetry);
        }

        private void TrackException(Exception ex)
        {
            var exceptionTelemtry = new ExceptionTelemetry(ex);
            exceptionTelemtry.Context.Operation.ParentId = this.requestTelemetry.Id;

            foreach (string key in ex.Data.Keys)
            {
                exceptionTelemtry.Properties.Add(key, ex.Data[key].ToString());
            }

            client.TrackException(exceptionTelemtry);
        }
    }
}