using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Gateway.Handlers
{
    public class TelemetryHandler : DelegatingHandler
    {
        private readonly Func<ITelemetryLogger> loggerFactory;

        public TelemetryHandler(Func<ITelemetryLogger> loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var logger = loggerFactory();
            var stopWatch = new Stopwatch();
            var startDate = DateTimeOffset.Now;
            var responseStatus = HttpStatusCode.InternalServerError;

            stopWatch.Start();

            try
            {
                var response = await base.SendAsync(request, cancellationToken);
                responseStatus = response.StatusCode;
                return response;
            }
            catch (ProxyToServiceInvokeException e)
            {
                logger.ErrorOccurred(e);
                throw;
            }
            catch (Exception e)
            {
                logger.ErrorOccurred(e);
                throw;
            }
            finally
            {
                stopWatch.Stop();
                logger.RequestCompleted(request, startDate, responseStatus, stopWatch.Elapsed);
            }
        }
    }
}
