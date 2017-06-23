using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace Gateway
{
    public class ServiceDiscoveryClientProxy : IClientProxy
    {
        private readonly OperationRetrySettings operationRetrySettings;
        private readonly HttpCommunicationClientFactory communicationClientFactory;

        public ServiceDiscoveryClientProxy(HttpClient httpClient, IExceptionHandler exceptionHandler, OperationRetrySettings operationRetrySettings)
        {
            this.operationRetrySettings = operationRetrySettings;
            this.communicationClientFactory = new HttpCommunicationClientFactory(httpClient, exceptionHandler);
        }

        public async Task<HttpResponseMessage> ProxyToService(FabricAddress fabricAddress, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var client = new ServicePartitionClient<HttpCommunicationClient>(this.communicationClientFactory, fabricAddress.Uri, retrySettings: this.operationRetrySettings);

            return await client.InvokeWithRetryAsync(c =>
                {
                    var serviceUri = GetServiceUri(c.Url, request.RequestUri);
                    return ProxyRequest(serviceUri, request, c.HttpClient);
                },
                cancellationToken);
        }

        private Task<HttpResponseMessage> ProxyRequest(Uri serviceUri, HttpRequestMessage request, HttpClient client)
        {
            var proxiedRequest = request.Clone(serviceUri);

            return client.SendAsync(proxiedRequest);
        }

        private Uri GetServiceUri(Uri baseUri, Uri requestUri)
        {
            return new Uri(baseUri, requestUri.PathAndQuery);
        }
    }
}