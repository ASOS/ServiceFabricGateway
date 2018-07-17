using System;
using System.Fabric;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Web.Http;
using Gateway.Handlers;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Owin;

namespace Gateway
{
    public static class Startup
    {
        private const int DefaultRetries = 3;
        private static readonly TimeSpan NotSetTimeout = TimeSpan.Zero;

        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public static void ConfigureApp(IAppBuilder appBuilder)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) =>
            {
                string certThumbprint;

                using (var cert2 = new X509Certificate2(certificate))
                {
                    certThumbprint = cert2.Thumbprint;
                }

                using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var matchedCerts = store.Certificates.Find(X509FindType.FindByThumbprint, certThumbprint, true);

                    if (matchedCerts.Count > 0)
                    {
                        return true;
                    }
                }

                return errors == SslPolicyErrors.None;
            };
            
            ConfigurationPackage configurationPackage = GetConfigurationPackage();

            var client = CreateHttpClient(configurationPackage);
            var maxRetryCount = GetMaxRetries(configurationPackage);
            var defaultBackoffInterval = TimeSpan.Zero;
            var operationRetrySettings = new OperationRetrySettings(
                maxRetryBackoffIntervalOnTransientErrors: defaultBackoffInterval,
                maxRetryBackoffIntervalOnNonTransientErrors: defaultBackoffInterval,
                defaultMaxRetryCount: maxRetryCount);

            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            config.MessageHandlers.Add(new TelemetryHandler(() => new ApplicationInsightsTelemetryLogger(CreateTelemetryClient(configurationPackage))));
            config.MessageHandlers.Add(new ProbeHandler());
            config.MessageHandlers.Add(new GatewayHandler(new ServiceDiscoveryClientProxy(client, new HttpExceptionHandler(), operationRetrySettings)));
            appBuilder.UseWebApi(config);
        }

        private static ConfigurationPackage GetConfigurationPackage()
        {
            return FabricRuntime.GetActivationContext().GetConfigurationPackageObject("Config");
        }

        private static int GetMaxRetries(ConfigurationPackage configurationPackage)
        {
            int retries;

            if (!int.TryParse(configurationPackage.Settings.Sections["Retries"].Parameters["Attempts"].Value, out retries))
            {
                // Assume a default policy
                retries = DefaultRetries;
            }

            return retries;
        }

        private static TimeSpan GetHttpClientTimeout(ConfigurationPackage configurationPackage)
        {
            TimeSpan timeout;

            if (!TimeSpan.TryParse(configurationPackage.Settings.Sections["HttpClient"].Parameters["Timeout"].Value, out timeout))
            {
                timeout = NotSetTimeout;
            }

            return timeout;
        }

        private static HttpClient CreateHttpClient(ConfigurationPackage configurationPackage)
        {
            var timeout = GetHttpClientTimeout(configurationPackage);
            var httpClient = new HttpClient(new ClientCertificateHandler());

            if (timeout != NotSetTimeout)
            {
                httpClient.Timeout = timeout;
            }

            return httpClient;
        }

        private static TelemetryClient CreateTelemetryClient(ConfigurationPackage configurationPackage)
        {
            var telemetry = configurationPackage.Settings.Sections["Telemetry"];
            var instrumentationKey = telemetry.Parameters["InstrumentationKey"].Value;
            bool disableTelemetry;

            bool.TryParse(telemetry.Parameters["DisableTelemetry"].Value, out disableTelemetry);

            TelemetryConfiguration.Active.InstrumentationKey = instrumentationKey;
            TelemetryConfiguration.Active.DisableTelemetry = disableTelemetry;

            return new TelemetryClient(TelemetryConfiguration.Active);
        }
    }
}
