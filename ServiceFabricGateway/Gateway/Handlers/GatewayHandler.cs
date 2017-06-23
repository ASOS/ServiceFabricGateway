using System;
using System.Fabric;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Gateway.Handlers
{
    public class GatewayHandler : DelegatingHandler
    {
        private readonly IClientProxy clientProxy;

        public GatewayHandler(IClientProxy clientProxy)
        {
            this.clientProxy = clientProxy ?? throw new ArgumentNullException(nameof(clientProxy));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var fabricAddress = new FabricAddress(request.RequestUri);
                return await clientProxy.ProxyToService(fabricAddress, request, cancellationToken);
            }
            catch (FabricAddress.InvalidFabricAddressException)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            catch (FabricServiceNotFoundException)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }
    }
}
