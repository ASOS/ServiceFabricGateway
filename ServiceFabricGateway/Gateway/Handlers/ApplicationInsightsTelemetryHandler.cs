using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Gateway.Handlers
{
    public class ApplicationInsightsTelemetryHandler : DelegatingHandler
    {
        private readonly TelemetryClient client;

        public ApplicationInsightsTelemetryHandler(TelemetryClient client)
        {
            this.client = client;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var stopWatch = new Stopwatch();
            var startDate = DateTimeOffset.Now;
            HttpStatusCode responseStatus = HttpStatusCode.InternalServerError;

            var method = request.Method.ToString().ToUpper();
            var name = $"{method} {request.RequestUri}";

            var requestTelemetry = new RequestTelemetry
            {
                Name = name,
                Timestamp = startDate,
                Url = request.RequestUri
            };

            stopWatch.Start();

            try
            {
                var response = await base.SendAsync(request, cancellationToken);
                responseStatus = response.StatusCode;
                return response;
            }
            catch (ProxyToServiceInvokeException e)
            {
                TrackException(requestTelemetry, e.InnerException, e.ResolvedServiceUri.AbsoluteUri);
                throw;
            }
            catch (Exception e)
            {
                TrackException(requestTelemetry, e);
                throw;
            }
            finally
            {
                stopWatch.Stop();
                TrackRequest(requestTelemetry, responseStatus, stopWatch.Elapsed);
            }
        }

        private void TrackException(RequestTelemetry requestTelemetry, Exception ex, string resolvedServiceUri = null)
        {
            var exceptionTelemtry = new ExceptionTelemetry(ex);

            exceptionTelemtry.Context.Operation.ParentId = requestTelemetry.Id;

            if (!string.IsNullOrWhiteSpace(resolvedServiceUri))
            {
                exceptionTelemtry.Properties.Add("Resolved Service Uri", resolvedServiceUri);
            }

            client.TrackException(exceptionTelemtry);
        }

        private void TrackRequest(RequestTelemetry requestTelemetry, HttpStatusCode responseStatus, TimeSpan duration)
        {
            var statusCode = (int)responseStatus;
            requestTelemetry.Duration = duration;
            requestTelemetry.ResponseCode = statusCode.ToString();
            requestTelemetry.Success = statusCode < 400;
            client.TrackRequest(requestTelemetry);
        }
    }
}
