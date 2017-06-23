using System;

namespace Gateway
{
    public class ProxyToServiceInvokeException : Exception
    {
        public Uri ResolvedServiceUri { get; }

        public ProxyToServiceInvokeException(Exception exception, Uri resolvedServiceUri) : base(exception.Message, exception)
        {
            ResolvedServiceUri = resolvedServiceUri;
        }
    }
}