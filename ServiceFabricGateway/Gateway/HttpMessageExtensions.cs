using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Gateway
{
    public static class HttpMessageExtensions
    {
        // NOTE: Adapted from http://stackoverflow.com/questions/21467018/how-to-forward-an-httprequestmessage-to-another-server
        public static async Task<HttpRequestMessage> Clone(this HttpRequestMessage req, Uri newUri)
        {
            HttpRequestMessage clone = new HttpRequestMessage(req.Method, newUri);

            if (req.Method != HttpMethod.Get)
            {
                var memoryStream = new MemoryStream();
                await req.Content.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                clone.Content = new StreamContent(memoryStream);

                foreach (var contentHeader in req.Content.Headers)
                {
                    clone.Content.Headers.Add(contentHeader.Key, contentHeader.Value);
                }
            }
            clone.Version = req.Version;

            foreach (KeyValuePair<string, object> prop in req.Properties)
            {
                clone.Properties.Add(prop);
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in req.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }
}