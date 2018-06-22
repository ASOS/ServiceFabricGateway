namespace Gateway.Handlers
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class ClientCertificateHandler : WebRequestHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            this.AttachClientCertificates(request);
            return await base.SendAsync(request, cancellationToken);
        }

        private void AttachClientCertificates(HttpRequestMessage proxiedRequest)
        {
            this.ResetHandler();

            var context = proxiedRequest.GetRequestContext();
            if (context?.ClientCertificate != null)
            {
                this.ClientCertificates.Add(context.ClientCertificate);
            }
        }

        private void ResetHandler()
        {
            var certs = this.ClientCertificates;

            for (var i = certs.Count - 1; i >= 0; i--)
            {
                certs.RemoveAt(i);
            }
        }
    }
}