# ServiceFabricGateway
Alternative to the Service Fabric Reverse Proxy that allows for more flexible route mappings

The gateway provides a similar function to the Service Fabric reverse proxy provided by Microsoft. This means it provides the same high level features:

- Gateway/Reverse proxy to microservices within a Service Fabric cluster
- Service resolution/call/retry loop
- Ensures microservices do not need bind to a particular port
- Provides a probe that can be utilized by a load balancer

However, the gateway provides additional functionality that is not present in the reverse proxy:

- Supports case insensitive URLs
- Restricts access to microservices intended to be for internal use only
- Ensures microservices can be accessed via the standard ASOS API addressing conventions.

### Conventions

The gateway extracts the second and third segment of a uri and applies a convention to discover and route the request to a fabric hosted service.  

| Convention  | Example  |
|-------------|----------|
| Application and service names must be lower case | mobileapp-checkout |
| Application name must follow the convention {domain}-{service} | mobileapp-checkout-payment |
| Internal service names must be post-fixed with the following to ensure they are not exposed by the gateway | mobileapp-checkout-payment-internal |

For example, for the uri https://www.foo.com/mobileapp-checkout/payment/v1/foo the second segment is "mobileapp-checkout" and the third segment is "payment" which map to the {domain} and {service} as shown above.

When following these conventions this means that the address of a service within the cluster will look like this:

  fabric:/{domain}-{service}/{service}

Example:

  fabric:/mobileapp-checkout-payment/payment
  
You must ensure the ApplicationParameters xml config file "Name" attribute in the "Application" element must apply the Application name  convention as shown below:

```html
<Application xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" Name="fabric:/mobileapp-checkout-payment" xmlns="http://schemas.microsoft.com/2011/01/fabric">
```
The reasons for these conventions are:

- Ensures that the URL conventions the organisation use can continue to work without change.
- Ensures that we do not have to put all APIs for a domain in a single Service Fabric application. For example if the application convention was just "{domain}" then all APIs in the mobileapp domain would need to be in this single package.

The only limitation of this convention is that it means you cannot have more than one public facing API in a Service Fabric application.

### Security

When the gateway calls an API that is exposed via HTTPS the certificate validation checks would normally fail. This is because .NET will validate that the domain name that the API is exposed on matches one of the subject alternative names on the certificate.

Services within the cluster are registered with the Service Fabric naming service based on either their FQDN or IP address. To mitigate this the default checks had to be overridden.

The gateway takes the thumbprint of the certificate the API has provided back to the gateway, which the gateway then uses to perform a lookup on the My/Personal store in the Windows Certificate Store. If there is a match then the certificate validation check completes successfully.

If the certificate does not exist then the gateway falls back to using the result of the standard .NET certificate validation process.
