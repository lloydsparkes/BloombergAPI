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
    using Bloomberglp.Blpapi;
    using Bloomberglp.Blpapi.Test;
    using Bloomberglp.BlpapiExamples.UnitTests.MktNotifier.MktNotifierApp.Implementation;
    using Moq;
    using NUnit.Framework;

    class TokenGeneratorTest
    {
        private static readonly Name TokenSuccess = Name.GetName("TokenGenerationSuccess");
        private static readonly Name TokenFailure = Name.GetName("TokenGenerationFailure");

        private Mock<ISession> mockSession;
        private Mock<IEventQueue> mockEventQueue;

        private TokenGenerator tokenGenerator;

        [SetUp]
        public void SetUp()
        {
            this.mockSession = new Mock<ISession>();
            this.mockEventQueue = new Mock<IEventQueue>();
            this.tokenGenerator = new TokenGenerator(this.mockSession.Object);
        }

        [TearDown]
        public void TearDown()
        {
            this.mockSession.VerifyAll();
            this.mockEventQueue.VerifyAll();
        }

        //
        // Concern: Verify that in case of successful token generation, a valid
        // token is received by the application.
        //
        // Plan:
        // 1. Create a 'TOKEN_STATUS' event using TestUtil.CreateEvent().
        // 2. Obtain schema for 'TokenGenerationSuccess' using TestUtil.GetAdminMessageDefinition().
        // 3. Append a 'TokenGenerationSuccess' message using TestUtil.AppendMessage().
        // 4. Using the returned formatter, format the message. In this example
        //    the message is represented by XML
        //   <TokenGenerationSuccess><token>dummyToken</token></TokenGenerationSuccess>.
        // 5. Verify the actual token is correct.
        //
        [Test]
        public void TokenGenerationSuccess()
        {
            Event @event = TestUtil.CreateEvent(Event.EventType.TOKEN_STATUS);
            SchemaElementDefinition elementDef =
                TestUtil.GetAdminMessageDefinition(TokenGeneratorTest.TokenSuccess);
            IMessageFormatter formatter = TestUtil.AppendMessage(@event, elementDef);

            string expectedToken = "dummyToken";
            string messageContent = $@"
<TokenGenerationSuccess>
    <token>{expectedToken}</token>
</TokenGenerationSuccess>";
            formatter.FormatMessageXml(messageContent);

            this.mockSession.Setup(x => x.GenerateToken(
                    It.IsAny<CorrelationID>(),
                    this.mockEventQueue.Object))
                .Returns(new CorrelationID());
            this.mockEventQueue.Setup(x => x.NextEvent()).Returns(@event);

            string token = this.tokenGenerator.Generate(this.mockEventQueue.Object);
            Assert.AreEqual(expectedToken, token);
        }

        //
        // Concern:
        // Verify that in case of failure in token generation, an empty
        // token is received by the application.
        //
        // Plan:
        // 1. Create a 'TOKEN_STATUS' event using TestUtil.CreateEvent().
        // 2. Obtain schema for 'TokenGenerationFailure' using TestUtil.GetAdminMessageDefinition().
        // 3. Append 'TokenGenerationFailure' message using TestUtil.AppendMessage().
        // 4. Using the returned formatter, format the message. In this example
        //    the message is represented by XML containing the reason of failure.
        // 5. Verify that the actual token is null.
        //
        [Test]
        public void TokenGenerationFailure()
        {
            Event @event = TestUtil.CreateEvent(Event.EventType.TOKEN_STATUS);
            SchemaElementDefinition elementDef =
                TestUtil.GetAdminMessageDefinition(TokenGeneratorTest.TokenFailure);
            IMessageFormatter formatter = TestUtil.AppendMessage(@event, elementDef);

            string messageContent = @"
<TokenGenerationFailure>
    <reason>
        <source>apitkns (apiauth) on n795</source>
        <errorCode>3</errorCode>
        <category>NO_AUTH</category>
        <description>App not in emrs.</description>
        <subcategory>INVALID_APP</subcategory>
    </reason>
</TokenGenerationFailure>";
            formatter.FormatMessageXml(messageContent);

            this.mockSession.Setup(x => x.GenerateToken(
                    It.IsAny<CorrelationID>(),
                    this.mockEventQueue.Object))
                .Returns(new CorrelationID());
            this.mockEventQueue.Setup(x => x.NextEvent()).Returns(@event);

            string token = this.tokenGenerator.Generate(this.mockEventQueue.Object);
            Assert.IsNull(token);
        }
    }
}
