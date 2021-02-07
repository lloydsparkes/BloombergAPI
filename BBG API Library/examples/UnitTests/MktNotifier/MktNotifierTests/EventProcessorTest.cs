/* Copyright 2020. Bloomberg Finance L.P.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions: The above
 * copyright notice and this permission notice shall be included in all copies
 * or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
 * IN THE SOFTWARE.
 */

namespace Bloomberglp.BlpapiExamples.UnitTests.MktNotifier.MktNotifierTests
{
    using System.IO;
    using System.Text;
    using Bloomberglp.Blpapi;
    using Bloomberglp.Blpapi.Test;
    using Bloomberglp.BlpapiExamples.UnitTests.MktNotifier.MktNotifierApp;
    using Moq;
    using NUnit.Framework;

    class EventProcessorTest
    {
        private Mock<ISession> mockSession;
        private Mock<INotifier> mockNotifier;
        private Mock<IComputeEngine> mockComputeEngine;

        private EventProcessor eventProcessor;

        [SetUp]
        public void SetUp()
        {
            this.mockSession = new Mock<ISession>();
            this.mockNotifier = new Mock<INotifier>();
            this.mockComputeEngine = new Mock<IComputeEngine>();
            this.eventProcessor = new EventProcessor(
                this.mockNotifier.Object,
                this.mockComputeEngine.Object);
        }

        [TearDown]
        public void TearDown()
        {
            this.mockNotifier.VerifyAll();
            this.mockComputeEngine.VerifyAll();
            this.mockSession.VerifyAll();
        }

        //
        // Concern: Verify the notifier receives 'SessionStarted' message.
        //
        // Plan:
        // 1. Create a SessionStatus admin event using TestUtil.CreateEvent().
        // 2. Obtain the message schema using TestUtil.GetAdminMessageDefinition().
        // 3. Append a message of type 'SessionStarted' using TestUtil.AppendMessage().
        // 4. Verify INotifier.LogSessionState() is called and save the message
        //    which should be passed from EventProcessor to INotifier.
        // 5. Verify that the actual messages is correct.
        //
        [Test]
        public void NotifierReceivesSessionStarted()
        {
            Name messageType = Name.GetName("SessionStarted");

            Event @event = TestUtil.CreateEvent(Event.EventType.SESSION_STATUS);
            SchemaElementDefinition elementDef =
                TestUtil.GetAdminMessageDefinition(messageType);
            TestUtil.AppendMessage(@event, elementDef);

            Message message = null;
            this.mockNotifier.Setup(x => x.LogSessionState(It.IsAny<Message>()))
                .Callback<Message>(msg => message = msg);
            this.eventProcessor.ProcessEvent(@event, this.mockSession.Object);

            Assert.NotNull(message);
            Assert.AreEqual(messageType, message.MessageType);
        }

        //
        // Concern: Verify that notifier receives 'SubscriptionStarted' message.
        //
        // Plan:
        // 1. Create a 'SubscriptionStatus' admin event using TestUtil.CreateEvent().
        // 2. Obtain the message schema using TestUtil.GetAdminMessageDefinition().
        // 3. Append a message of type 'SubscriptionStarted' TestUtil.AppendMessage().
        // 4. Verify INotifier.LogSubscriptionState() is called and save the
        //    message which should be passed from EventProcessor to INotifier.
        // 5. Verify that the actual messages is correct.
        //
        [Test]
        public void NotifierReceivesSubscriptionStarted()
        {
            Name messageType = Name.GetName("SubscriptionStarted");

            Event @event = TestUtil.CreateEvent(Event.EventType.SUBSCRIPTION_STATUS);
            SchemaElementDefinition elementDef
                = TestUtil.GetAdminMessageDefinition(messageType);
            TestUtil.AppendMessage(@event, elementDef);

            Message message = null;
            this.mockNotifier.Setup(x => x.LogSubscriptionState(It.IsAny<Message>()))
                .Callback<Message>(msg => message = msg);
            this.eventProcessor.ProcessEvent(@event, this.mockSession.Object);

            Assert.NotNull(message);
            Assert.AreEqual(messageType, message.MessageType);
        }

        //
        // Concern: Verify that:
        // IComputeEngine receives correct LAST_PRICE and INotifier sends
        // correct value to terminal.
        //
        // Plan:
        // 1. Obtain the service by deserializing its schema.
        // 2. Create a SubscriptionEvent using TestUtil.CreateEvent().
        // 3. Obtain the element schema definition from the service.
        // 4. Append a message of type 'MarketDataEvents' using TestUtil.AppendMessage().
        // 5. Format the message using formatter returned by TestUtil.AppendMessage().
        //    In this example the message is represented by XML
        //    <MarketDataEvents><LAST_PRICE>142.80</LAST_PRICE></MarketDataEvents>.
        // 6. Set up IComputeEngine.ComplexCompute() to return a pre-defined value.
        // 7. Verify that IComputeEngine gets correct value and INotifier sends
        //    correct value to terminal.
        //
        [Test]
        public void NotifierReceivesSubscriptionData()
        {
            double lastPrice = 142.80;
            Event @event = EventProcessorTest.CreateSubscriptionDataWithLastPrice(lastPrice);

            double expectedComputeResult = 200.0;
            this.mockComputeEngine.Setup(x => x.ComplexCompute(lastPrice))
                .Returns(expectedComputeResult);

            this.eventProcessor.ProcessEvent(@event, this.mockSession.Object);

            this.mockNotifier.Verify(x => x.SendToTerminal(expectedComputeResult));
        }

        private static Event CreateSubscriptionDataWithLastPrice(double lastPrice)
        {
            Service service;
            byte[] mktdataSchema = Encoding.UTF8.GetBytes(
                Resources.ServiceSchema.mktdataSchema);
            using (var memStream = new MemoryStream(mktdataSchema))
            {
                service = TestUtil.DeserializeService(memStream);
                Assert.NotNull(service);
            }

            Event @event = TestUtil.CreateEvent(Event.EventType.SUBSCRIPTION_DATA);
            Name messageType = Name.GetName("MarketDataEvents");
            SchemaElementDefinition schemaDef = service.GetEventDefinition(
                messageType);
            IMessageFormatter formatter = TestUtil.AppendMessage(@event, schemaDef);

            string messageContent = "<MarketDataEvents>" +
                                    $"    <LAST_PRICE>{lastPrice}</LAST_PRICE>" +
                                    "</MarketDataEvents>";

            formatter.FormatMessageXml(messageContent);
            return @event;
        }
    }
}
