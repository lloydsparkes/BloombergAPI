/* Copyright 2020. Bloomberg Finance L.P.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the ""Software""), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions: The above
 * copyright notice and this permission notice shall be included in all copies
 * or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
 * IN THE SOFTWARE.
 */

namespace Bloomberglp.BlpapiExamples.UnitTests.Snippets.Resolver.ResolverTests
{
    using System.Linq;
    using Bloomberglp.Blpapi;
    using Bloomberglp.Blpapi.Test;
    using Bloomberglp.BlpapiExamples.UnitTests.Snippets.Resolver.ResolverLib;
    using Moq;
    using NUnit.Framework;

    using RegistrationParts = Bloomberglp.Blpapi.ServiceRegistrationOptions.RegistrationParts;

    public class ResolverUtilsTest
    {
        private const int InvalidAppId = 4321;

        private Mock<IProviderSession> mockProviderSession;
        private Mock<Identity> mockIdentity;

        [SetUp]
        public void SetUp()
        {
            this.mockProviderSession = new Mock<IProviderSession>();
            this.mockIdentity = new Mock<Identity>();
        }

        [TearDown]
        public void TearDown()
        {
            this.mockProviderSession.VerifyAll();
            this.mockIdentity.VerifyAll();
        }

        // This test demonstrates how to mock interactions with ProviderSession.
        // This test sets up the return value of ProviderSession.RegisterService
        // and verifies the input arguments.
        [Test]
        public void ResolutionServiceRegistration()
        {
            string serviceName = "//blp/mytestservice";

            ServiceRegistrationOptions registrationOptions = null;
            this.mockProviderSession.Setup(x => x.RegisterService(
                    serviceName,
                    this.mockIdentity.Object,
                    It.IsAny<ServiceRegistrationOptions>()))
                .Callback<string, Identity, ServiceRegistrationOptions>(
                    (s, i, o) => registrationOptions = o)
                .Returns(true);

            bool success = ResolverUtils.ResolutionServiceRegistration(
                this.mockProviderSession.Object,
                this.mockIdentity.Object,
                serviceName);

            // Verify that the service is registered with the expected options.
            Assert.IsTrue(success);
            Assert.NotNull(registrationOptions);

            const int ExpectedPriority = 123;
            Assert.AreEqual(
                ExpectedPriority,
                registrationOptions.ServicePriority);
            Assert.AreEqual(
                RegistrationParts.PART_SUBSCRIBER_RESOLUTION,
                registrationOptions.PartsToRegister & RegistrationParts.PART_SUBSCRIBER_RESOLUTION);
        }

        // This test demonstrates how to create a successful permission
        // response and verify its content is as expected.
        [Test]
        public void SuccessfulResolution()
        {
            Service service = ResolverUtilsTest.GetService();

            var cid = new CorrelationID();
            Event permissionEvent = ResolverUtilsTest.CreatePermissionEvent(
                cid,
                ResolverUtils.AllowedAppId);
            Message permissionRequest = permissionEvent.First();

            Event response = null;
            this.mockProviderSession.Setup(x => x.SendResponse(It.IsAny<Event>()))
                .Callback<Event>(e => response = e);

            ResolverUtils.HandlePermissionRequest(
                this.mockProviderSession.Object,
                service,
                permissionRequest);

            // Verify response has correct CorrelationID and MessageType.
            Assert.NotNull(response);
            Assert.AreEqual(Event.EventType.RESPONSE, response.Type);
            Message permissionResponse = response.First();
            Assert.AreEqual(cid, permissionResponse.CorrelationID);
            Assert.AreEqual(
                ResolverUtils.PermissionResponse,
                permissionResponse.MessageType);

            // Verify response has 2 successful topic permission entries.
            Assert.IsTrue(permissionResponse.HasElement(
                ResolverUtils.TopicPermissions));
            Element topicPermissions = permissionResponse.GetElement(
                ResolverUtils.TopicPermissions);
            const int NumTopics = 2;
            Assert.AreEqual(NumTopics, topicPermissions.NumValues);
            for (int i = 0; i < NumTopics; ++i)
            {
                Element topicPermission = topicPermissions.GetValueAsElement(i);
                Assert.IsTrue(topicPermission.HasElement(ResolverUtils.Result));
                Assert.Zero(topicPermission.GetElementAsInt32(ResolverUtils.Result));
            }
        }

        // This test demonstrates how to create a failure permission
        // response and verify its content is as expected.
        [Test]
        public void FailedResolution()
        {
            Service service = ResolverUtilsTest.GetService();

            var cid = new CorrelationID(1);
            Event permissionEvent = ResolverUtilsTest.CreatePermissionEvent(
                cid,
                ResolverUtilsTest.InvalidAppId);
            Message permissionRequest = permissionEvent.First();

            Event response = null;
            this.mockProviderSession.Setup(x => x.SendResponse(It.IsAny<Event>()))
                .Callback<Event>(e => response = e);

            ResolverUtils.HandlePermissionRequest(
                this.mockProviderSession.Object,
                service,
                permissionRequest);

            // Verify response has correct CorrelationID and MessageType.
            Assert.NotNull(response);
            Assert.AreEqual(Event.EventType.RESPONSE, response.Type);
            Message permissionResponse = response.First();
            Assert.AreEqual(cid, permissionResponse.CorrelationID);
            Assert.AreEqual(
                ResolverUtils.PermissionResponse,
                permissionResponse.MessageType);

            // Verify response has 2 failed topic permission entries.
            Assert.IsTrue(permissionResponse.HasElement(ResolverUtils.TopicPermissions));
            Element topicPermissions = permissionResponse.GetElement(
                ResolverUtils.TopicPermissions);
            const int NumTopics = 2;
            Assert.AreEqual(NumTopics, topicPermissions.NumValues);

            for (int i = 0; i < NumTopics; ++i)
            {
                Element topicPermission = topicPermissions.GetValueAsElement(i);
                Assert.IsTrue(topicPermission.HasElement(ResolverUtils.Result));
                Assert.AreEqual(
                    1,
                    topicPermission.GetElementAsInt32(ResolverUtils.Result));

                Assert.IsTrue(topicPermission.HasElement(ResolverUtils.Reason));
                Element reason = topicPermission.GetElement(ResolverUtils.Reason);
                Assert.IsTrue(reason.HasElement(ResolverUtils.Category));
                Assert.AreEqual(
                    ResolverUtils.NotAuthorized,
                    reason.GetElementAsString(ResolverUtils.Category));
            }
        }

        private static Service GetService()
        {
            const string Schema = @"
<ServiceDefinition xsi:schemaLocation=""http://bloomberg.com/schemas/apidd apidd.xsd""
                   name=""test-svc""
                   version=""1.0.0.0""
                   xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <service name=""//blp-test/test-svc"" version=""1.0.0.0"">
    <event name=""Events"" eventType=""EventType"">
        <eventId>1</eventId>
    </event>
    <defaultServiceId>12345</defaultServiceId>
    <publisherSupportsRecap>false</publisherSupportsRecap>
    <authoritativeSourceSupportsRecap>false</authoritativeSourceSupportsRecap>
    <SubscriberResolutionServiceId>12346</SubscriberResolutionServiceId>
  </service>
  <schema>
      <sequenceType name=""EventType"">
         <element name=""price"" type=""Float64"" minOccurs=""0"" maxOccurs=""1""/>
      </sequenceType>
   </schema>
</ServiceDefinition>";

            Service service = TestUtil.DeserializeService(Schema);
            Assert.NotNull(service);
            return service;
        }

        private static Event CreatePermissionEvent(
            CorrelationID cid,
            int applicationId)
        {
            var props = new MessageProperties();
            props.SetCorrelationId(cid);

            // Create a Request event
            Event request = TestUtil.CreateEvent(Event.EventType.REQUEST);

            SchemaElementDefinition schemaDef = TestUtil.GetAdminMessageDefinition(
                ResolverUtils.PermissionRequest);

            string content = $@"
<PermissionRequest>
    <topics>topic1</topics>
    <topics>topic2</topics>
    <serviceName>//blp/mytestservice</serviceName>
    <applicationId>{applicationId}</applicationId>
</PermissionRequest>";

            IMessageFormatter formatter =
                TestUtil.AppendMessage(request, schemaDef, props);
            formatter.FormatMessageXml(content);

            return request;
        }
    }
}
