using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;


namespace Gateway.Tests
{
    [TestFixture]
    public abstract class HttpMessageExtensionsSpecification
    {
        protected HttpRequestMessage Expected { get; private set; }

        protected HttpRequestMessage Actual { get; private set; }

        [SetUp]
        public void Setup()
        {
            Expected = CreateExpected();

            Actual = Expected.Clone(new Uri("http://overridden")).Result;
        }

        protected abstract HttpRequestMessage CreateExpected();

        [Test]
        public void then_the_method_is_cloned()
        {
            Assert.That(Actual.Method, Is.EqualTo(Expected.Method));
        }

        [Test]
        public void then_the_uri_is_overridden()
        {
            Assert.That(Actual.RequestUri, Is.EqualTo(new Uri("http://overridden")));
        }

        [Test]
        public void then_the_version_is_cloned()
        {
            Assert.That(Actual.Version, Is.EqualTo(Expected.Version));
        }

        [Test]
        public void then_the_headers_are_clone()
        {
            Assert.That(Actual.Headers, Is.EquivalentTo(Expected.Headers));
        }

        [Test]
        public void then_the_properties_are_cloned()
        {
            Assert.That(Actual.Properties, Is.EquivalentTo(Expected.Properties));
        }
    }

    [TestFixture]
    public class GivenAPostRequest : HttpMessageExtensionsSpecification
    {
        protected override HttpRequestMessage CreateExpected()
        {
            var expected = new HttpRequestMessage(HttpMethod.Post, "http://original");
            expected.Version = HttpVersion.Version11;
            expected.Headers.Add("X-MyCustomHeader", "HeaderValue");
            expected.Properties.Add("MyTestProperty", "PropertyValue");
            expected.Content = new StringContent("{\"test\":\"data\"}", Encoding.UTF8, "application/json");

            return expected;
        }

        [Test]
        public async Task then_the_content_is_cloned()
        {
            CollectionAssert.AreEquivalent(Expected.Content.Headers, Actual.Content.Headers);
            Assert.That(Expected.Content, Is.Not.SameAs(Actual.Content));
            FileAssert.AreEqual(await Expected.Content.ReadAsStreamAsync(), await Actual.Content.ReadAsStreamAsync());
        }
    }

    [TestFixture]
    public class GivenAGetRequest : HttpMessageExtensionsSpecification
    {
        protected override HttpRequestMessage CreateExpected()
        {
            var expected = new HttpRequestMessage(HttpMethod.Get, "http://original");
            expected.Version = HttpVersion.Version10;
            expected.Headers.Add("X-MyCustomHeader", "HeaderValue");
            expected.Properties.Add("MyTestProperty", "PropertyValue");

            return expected;
        }

        [Test]
        public void then_no_content_is_assigned()
        {
            Assert.That(Actual.Content, Is.Null);
        }
    }

    [TestFixture]
    public class RepeatedCloneTests
    {
        [Test]
        public async Task when_cloning_a_request_more_than_once_no_exceptions_are_thrown()
        {
            var baseContentBytes = await new StringContent("{\"test\":\"data\"}", Encoding.UTF8, "application/json").ReadAsByteArrayAsync();
            var contentStreamBase = new MemoryStream(baseContentBytes, false);
            var contentStream = new UnseekableStream(contentStreamBase);
            var content = new StreamContent(contentStream);
            var request = new HttpRequestMessage(HttpMethod.Post, "http://www.test.com")
            {
                Content = content
            };

            var clone1 = await request.Clone(new Uri("http://overriden"));
            var clone2 = await request.Clone(new Uri("http://overriden2"));

            // Copying the cloned stream into a new stream simulates the failure on the second attempt
            // See the PrepareContent method and associated resources
            // https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/StreamContent.cs
            // https://github.com/dotnet/corefx/blob/ea706ea8cf9d95a2a8e1adb66d73e08f02175466/src/System.Net.Http/src/Resources/Strings.resx#L110
            await clone1.Content.CopyToAsync(new MemoryStream());
            await clone2.Content.CopyToAsync(new MemoryStream());
        }

        private class UnseekableStream : Stream
        {
            private readonly MemoryStream inner;

            public UnseekableStream(MemoryStream inner)
            {
                this.inner = inner;
            }

            public override void Flush()
            {
                this.inner.Flush();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return this.inner.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                this.inner.SetLength(value);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return this.inner.Read(buffer, offset, count);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                this.inner.Write(buffer, offset, count);
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length => inner.Length;

            public override long Position
            {
                get { return inner.Position; }
                set { inner.Position = value; }
            }
        }
    }
}
