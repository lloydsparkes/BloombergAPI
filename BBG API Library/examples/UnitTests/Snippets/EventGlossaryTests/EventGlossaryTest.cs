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

namespace Bloomberglp.BlpapiExamples.UnitTests.Snippets.EventGlossaryTests
{
    using Bloomberglp.Blpapi;
    using Bloomberglp.Blpapi.Test;
    using NUnit.Framework;

    // The following test cases provide examples on how to mock different
    // events/messages supported by BLPAPI .Net SDK. The code to set up
    // expectation and verification of values is omitted from examples tests.
    public class EventGlossaryTest
    {
        [Test]
        public void ExampleEventSessionStarted()
        {
            var eventType = Event.EventType.SESSION_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("SessionStarted");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<SessionStarted>
   <initialEndpoints>
       <address>12.34.56.78:8194</address>
    </initialEndpoints>
   <initialEndpoints>
       <address>98.76.54.32:8194</address>
    </initialEndpoints>
</SessionStarted>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventSessionStartupFailure()
        {
            var eventType = Event.EventType.SESSION_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("SessionStartupFailure");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<SessionStartupFailure>
    <reason>
        <source>TestUtil</source>
        <errorCode>-1</errorCode>
        <category>CATEGORY</category>
        <description>for testing</description>
        <subcategory>SUBCATEGORY</subcategory>
     </reason>
</SessionStartupFailure>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventSessionTerminated()
        {
            var eventType = Event.EventType.SESSION_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("SessionTerminated");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<SessionTerminated>
    <reason>
        <source>TestUtil</source>
        <errorCode>-1</errorCode>
        <category>CATEGORY</category>
        <description>for testing</description>
        <subcategory>SUBCATEGORY</subcategory>
     </reason>
</SessionTerminated>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventSessionConnectionUp()
        {
            var eventType = Event.EventType.SESSION_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("SessionConnectionUp");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<SessionConnectionUp>
    <server>12.34.56.78:8194</server>
    <serverId>ny-hostname</serverId>
    <encryptionStatus>Clear</encryptionStatus>
    <compressionStatus>Uncompressed</compressionStatus>
</SessionConnectionUp>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventSessionConnectionDown()
        {
            var eventType = Event.EventType.SESSION_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("SessionConnectionDown");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<SessionConnectionDown>
    <server>12.34.56.78:8194</server>
    <serverId>ny-hostname</serverId>
</SessionConnectionDown>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventSessionClusterInfo()
        {
            var eventType = Event.EventType.SESSION_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("SessionClusterInfo");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<SessionClusterInfo>
    <name>clustername</name>
    <endpoints>
        <address>12.34.56.78:8194</address>
     </endpoints>
    <endpoints>
        <address>98.76.54.32:8194</address>
     </endpoints>
</SessionClusterInfo>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventSessionClusterUpdate()
        {
            var eventType = Event.EventType.SESSION_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("SessionClusterUpdate");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<SessionClusterUpdate>
    <name>clustername</name>
    <endpointsAdded>
        <address>12.34.56.78:8194</address>
     </endpointsAdded>
    <endpointsRemoved>
        <address>98.76.54.32:8194</address>
    </endpointsRemoved>
</SessionClusterUpdate>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventSlowConsumerWarning()
        {
            var eventType = Event.EventType.ADMIN;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("SlowConsumerWarning");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            TestUtil.AppendMessage(expectedEvent, schema);
        }

        [Test]
        public void ExampleEventSlowConsumerWarningCleared()
        {
            var eventType = Event.EventType.ADMIN;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("SlowConsumerWarningCleared");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<SlowConsumerWarningCleared>
    <eventsDropped>123</eventsDropped>
</SlowConsumerWarningCleared>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventDataLoss()
        {
            var eventType = Event.EventType.ADMIN;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("DataLoss");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<DataLoss>
    <id>id</id>
    <source>Test</source>
    <numMessagesDropped>1235</numMessagesDropped>
</DataLoss>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventServiceOpened()
        {
            var eventType = Event.EventType.SERVICE_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("ServiceOpened");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<ServiceOpened>
    <serviceName>//blp/myservice</serviceName>
</ServiceOpened>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventServiceOpenFailure()
        {
            var eventType = Event.EventType.SERVICE_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("ServiceOpenFailure");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<ServiceOpenFailure>
    <reason>
        <source>TestUtil</source>
        <errorCode>-1</errorCode>
        <category>CATEGORY</category>
        <description>for testing</description>
        <subcategory>SUBCATEGORY</subcategory>
     </reason>
</ServiceOpenFailure>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventServiceRegistered()
        {
            var eventType = Event.EventType.SERVICE_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("ServiceRegistered");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<ServiceRegistered>
    <serviceName>//blp/myservice</serviceName>
</ServiceRegistered>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventServiceRegisterFailure()
        {
            var eventType = Event.EventType.SERVICE_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("ServiceRegisterFailure");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<ServiceRegisterFailure>
    <reason>
        <source>TestUtil</source>
        <errorCode>-1</errorCode>
        <category>CATEGORY</category>
        <description>for testing</description>
        <subcategory>SUBCATEGORY</subcategory>
     </reason>
</ServiceRegisterFailure>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventServiceDeregistered()
        {
            var eventType = Event.EventType.SERVICE_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("ServiceDeregistered");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<ServiceDeregistered>
    <serviceName>//blp/myservice</serviceName>
</ServiceDeregistered>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventServiceUp()
        {
            var eventType = Event.EventType.SERVICE_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("ServiceUp");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<ServiceUp>
    <serviceName>//blp/myservice</serviceName>
    <endpoint>12.34.56.78</endpoint>
</ServiceUp>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventServiceDown()
        {
            var eventType = Event.EventType.SERVICE_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("ServiceDown");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<ServiceDown>
    <serviceName>//blp/myservice</serviceName>
    <endpoint>12.34.56.78</endpoint>
</ServiceDown>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventServiceAvailabilityInfo()
        {
            var eventType = Event.EventType.SERVICE_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("ServiceAvailabilityInfo");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<ServiceAvailabilityInfo>
    <serviceName>//blp/myservice</serviceName>
    <serverAdded>
        <address>12.34.56.78:8194</address>
     </serverAdded>
    <serverRemoved>
        <address>98.76.54.32:8194</address>
    </serverRemoved>
    <servers>12.34.56.78</servers>
    <servers>87.65.43.21</servers>
</ServiceAvailabilityInfo>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventResolutionSuccess()
        {
            var eventType = Event.EventType.RESOLUTION_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("ResolutionSuccess");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<ResolutionSuccess>
    <resolvedTopic>//blp/myservice/rtopic</resolvedTopic>
</ResolutionSuccess>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventResolutionFailure()
        {
            var eventType = Event.EventType.RESOLUTION_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("ResolutionFailure");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<ResolutionFailure>
    <reason>
        <source>TestUtil</source>
        <errorCode>-1</errorCode>
        <category>CATEGORY</category>
        <description>for testing</description>
        <subcategory>SUBCATEGORY</subcategory>
     </reason>
</ResolutionFailure>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventPermissionRequest()
        {
            var eventType = Event.EventType.REQUEST;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("PermissionRequest");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<PermissionRequest>
    <topics>topic1</topics>
    <topics>topic2</topics>
    <uuid>1234</uuid>
    <seatType>1234</seatType>
    <applicationId>1234</applicationId>
    <userName>someName</userName>
    <appName>myAppName</appName>
    <deviceAddress>myDevice</deviceAddress>
</PermissionRequest>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventPermissionResponse()
        {
            // Unlike the other admin messages, 'PermissionResponse' is not
            // delivered to applications by the SDK. It is used by resolvers to
            // respond to incoming 'PermissionRequest' messages. BLPAPI
            // applications are not expected to handle these messages.
            //
            // For testing if an application is publishing 'PermissionResponse'
            // messages with correct values, it is recommended to mock the
            // related 'IProviderSession.publish()' method to capture the
            // published events. See the provided testing examples for more
            // details.
        }

        [Test]
        public void ExampleEventTopicCreated()
        {
            var eventType = Event.EventType.TOPIC_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("TopicCreated");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<TopicCreated>
    <topic>mytopic</topic>
</TopicCreated>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventTopicCreateFailure()
        {
            var eventType = Event.EventType.TOPIC_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("TopicCreateFailure");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<TopicCreateFailure>
    <topic>mytopic</topic>
    <reason>
        <source>TestUtil</source>
        <errorCode>-1</errorCode>
        <category>CATEGORY</category>
        <description>for testing</description>
        <subcategory>SUBCATEGORY</subcategory>
     </reason>
</TopicCreateFailure>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventTopicDeleted()
        {
            var eventType = Event.EventType.TOPIC_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("TopicDeleted");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<TopicDeleted>
    <topic>mytopic</topic>
    <reason>TestUtil</reason>
</TopicDeleted>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventTopicSubscribed()
        {
            var eventType = Event.EventType.TOPIC_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("TopicSubscribed");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<TopicSubscribed>
    <topic>mytopic</topic>
    <topicWithOptions>topicwithopts</topicWithOptions>
</TopicSubscribed>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventTopicResubscribed()
        {
            var eventType = Event.EventType.TOPIC_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("TopicResubscribed");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<TopicResubscribed>
    <topic>mytopic</topic>
    <topicWithOptions>topicwithopts</topicWithOptions>
    <reason>TestUtil</reason>
</TopicResubscribed>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventTopicUnsubscribed()
        {
            var eventType = Event.EventType.TOPIC_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("TopicUnsubscribed");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<TopicUnsubscribed>
    <topic>mytopic</topic>
    <reason>TestUtil</reason>
</TopicUnsubscribed>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventTopicActivated()
        {
            var eventType = Event.EventType.TOPIC_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("TopicActivated");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<TopicActivated>
    <topic>mytopic</topic>
</TopicActivated>";
            formatter.FormatMessageXml(xmlMessageContent);
        }


        [Test]
        public void ExampleEventTopicDeactivated()
        {
            var eventType = Event.EventType.TOPIC_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("TopicDeactivated");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<TopicDeactivated>
    <topic>mytopic</topic>
    <reason>TestUtil</reason>
</TopicDeactivated>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventTopicRecap()
        {
            var eventType = Event.EventType.TOPIC_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("TopicRecap");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<TopicRecap>
    <topic>mytopic</topic>
    <isSolicited>true</isSolicited>
</TopicRecap>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventSubscriptionStarted()
        {
            var eventType = Event.EventType.SUBSCRIPTION_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("SubscriptionStarted");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<SubscriptionStarted>
    <exceptions>
        <reason>
            <source>TestUtil</source>
            <errorCode>-1</errorCode>
            <category>CATEGORY</category>
            <description>for testing</description>
            <subcategory>SUBCATEGORY</subcategory>
        </reason>
    </exceptions>
    <resubscriptionId>123</resubscriptionId>
    <streamIds>123</streamIds>
    <streamIds>456</streamIds>
    <receivedFrom>
        <address>12.34.56.78:8194</address>
    </receivedFrom>
    <reason>TestUtil</reason>
</SubscriptionStarted>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventSubscriptionFailure()
        {
            var eventType = Event.EventType.SUBSCRIPTION_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("SubscriptionFailure");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<SubscriptionFailure>
    <reason>
        <source>TestUtil</source>
        <errorCode>-1</errorCode>
        <category>CATEGORY</category>
        <description>for testing</description>
        <subcategory>SUBCATEGORY</subcategory>
    </reason>
    <failureDetails>
        <fieldId>field</fieldId>
        <reason>
            <source>TestUtil</source>
            <errorCode>-1</errorCode>
            <category>CATEGORY</category>
            <description>for testing</description>
            <subcategory>SUBCATEGORY</subcategory>
         </reason>
    </failureDetails>
    <resubscriptionId>123</resubscriptionId>
</SubscriptionFailure>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventSubscriptionStreamsActivated()
        {
            var eventType = Event.EventType.SUBSCRIPTION_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("SubscriptionStreamsActivated");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<SubscriptionStreamsActivated>
    <streams>
        <id>streamId</id>
        <endpoint>
            <address>12.34.56.78:8194</address>
        </endpoint>
    </streams>
    <reason>TestUtil</reason>
</SubscriptionStreamsActivated>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventSubscriptionStreamsDeactivated()
        {
            var eventType = Event.EventType.SUBSCRIPTION_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("SubscriptionStreamsDeactivated");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<SubscriptionStreamsDeactivated>
    <streams>
        <id>streamId</id>
        <endpoint>
            <address>12.34.56.78:8194</address>
        </endpoint>
    </streams>
    <reason>TestUtil</reason>
</SubscriptionStreamsDeactivated>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventSubscriptionTerminated()
        {
            var eventType = Event.EventType.SUBSCRIPTION_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("SubscriptionTerminated");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<SubscriptionTerminated>
    <reason>
        <source>TestUtil</source>
        <errorCode>-1</errorCode>
        <category>CATEGORY</category>
        <description>for testing</description>
        <subcategory>SUBCATEGORY</subcategory>
    </reason>
</SubscriptionTerminated>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventRequestTemplateAvailable()
        {
            var eventType = Event.EventType.ADMIN;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("RequestTemplateAvailable");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<RequestTemplateAvailable>
    <boundTo>
        <dataConnection>
            <address>12.34.56.78:8194</address>
        </dataConnection>
    </boundTo>
</RequestTemplateAvailable>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventRequestTemplatePending()
        {
            var eventType = Event.EventType.ADMIN;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("RequestTemplatePending");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            TestUtil.AppendMessage(expectedEvent, schema);
        }

        [Test]
        public void ExampleEventRequestTemplateTerminated()
        {
            var eventType = Event.EventType.ADMIN;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("RequestTemplateTerminated");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<RequestTemplateTerminated>
    <reason>
        <source>TestUtil</source>
        <errorCode>-1</errorCode>
        <category>CATEGORY</category>
        <description>for testing</description>
        <subcategory>SUBCATEGORY</subcategory>
    </reason>
</RequestTemplateTerminated>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventRequestFailure()
        {
            var eventType = Event.EventType.REQUEST_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("RequestFailure");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<RequestFailure>
    <reason>
        <source>TestUtil</source>
        <errorCode>-1</errorCode>
        <category>CATEGORY</category>
        <description>for testing</description>
        <subcategory>SUBCATEGORY</subcategory>
    </reason>
</RequestFailure>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventTokenGenerationSuccess()
        {
            var eventType = Event.EventType.TOKEN_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("TokenGenerationSuccess");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<TokenGenerationSuccess>
    <token>mytoken</token>
</TokenGenerationSuccess>";
            formatter.FormatMessageXml(xmlMessageContent);
        }

        [Test]
        public void ExampleEventTokenGenerationFailure()
        {
            var eventType = Event.EventType.TOKEN_STATUS;
            Event expectedEvent = TestUtil.CreateEvent(eventType);

            Name messageType = Name.GetName("TokenGenerationFailure");
            SchemaElementDefinition schema =
                TestUtil.GetAdminMessageDefinition(messageType);

            IMessageFormatter formatter = TestUtil.AppendMessage(
                expectedEvent,
                schema);
            string xmlMessageContent = @"
<TokenGenerationFailure>
    <reason>
        <source>TestUtil</source>
        <errorCode>-1</errorCode>
        <category>CATEGORY</category>
        <description>for testing</description>
        <subcategory>SUBCATEGORY</subcategory>
    </reason>
</TokenGenerationFailure>";
            formatter.FormatMessageXml(xmlMessageContent);
        }
    }
}
