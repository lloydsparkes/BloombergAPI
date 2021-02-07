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
    using System;
    using Bloomberglp.Blpapi;
    using Bloomberglp.Blpapi.Test;
    using Bloomberglp.BlpapiExamples.UnitTests.MktNotifier.MktNotifierApp;
    using Bloomberglp.BlpapiExamples.UnitTests.MktNotifier.MktNotifierApp.Implementation;
    using Moq;
    using NUnit.Framework;

    class AuthorizerTest
    {
        private const string TestToken = "testToken";
        private static readonly Name AuthorizationSuccess =
            Name.GetName("AuthorizationSuccess");
        private static readonly Name RequestFailure =
            Name.GetName("RequestFailure");

        private Mock<ISession> mockSession;
        private Mock<ITokenGenerator> mockTokenGenerator;
        private Mock<IEventQueue> mockEventQueue;
        private Mock<Identity> mockIdentity;

        private Authorizer authorizer;
        private Service apiauthService;

        [SetUp]
        public void SetUp()
        {
            this.apiauthService = AuthorizerTest.LoadApiauthService();

            this.mockSession = new Mock<ISession>();
            this.mockEventQueue = new Mock<IEventQueue>();
            this.mockIdentity = new Mock<Identity>();
            this.mockTokenGenerator = new Mock<ITokenGenerator>();
            this.authorizer = new Authorizer(
                this.mockSession.Object,
                this.mockTokenGenerator.Object);

            this.mockSession.Setup(x => x.CreateIdentity())
                .Returns(this.mockIdentity.Object);
            this.mockSession.Setup(x => x.OpenService(AppOptions.DefaultAuthService))
                .Returns(true);
            this.mockSession.Setup(x => x.GetService(AppOptions.DefaultAuthService))
                .Returns(this.apiauthService);

            this.mockTokenGenerator.Setup(x => x.Generate(It.IsAny<IEventQueue>()))
                .Returns(AuthorizerTest.TestToken);
        }

        [TearDown]
        public void TearDown()
        {
            this.mockTokenGenerator.VerifyAll();
            this.mockEventQueue.VerifyAll();
        }

        //
        // Concern:
        // Verify that for a valid identity the authorization returns true.
        //
        // Plan:
        // 1. Create admin event to represent the authorization success.
        // 2. Set up EventQueue.NextEvent() to return the event.
        // 3. Verify the authorize is success and identity is returned as expected.
        //
        [Test]
        public void SuccessAuthorization()
        {
            Event @event = TestUtil.CreateEvent(Event.EventType.AUTHORIZATION_STATUS);
            SchemaElementDefinition elementDef = TestUtil.GetAdminMessageDefinition(
                AuthorizerTest.AuthorizationSuccess);
            TestUtil.AppendMessage(@event, elementDef);

            this.mockEventQueue.Setup(x => x.NextEvent(It.IsAny<long>()))
                .Returns(@event);

            Identity identity = this.authorizer.Authorize(
                AppOptions.DefaultAuthService,
                "auth_options",
                this.mockEventQueue.Object);

            Assert.AreEqual(this.mockIdentity.Object, identity);
        }

        //
        // Concern: Verify that the authorization throws an exception for an
        // invalid identity.
        //
        // Plan:
        // 1. Create admin event to represent the authorization failure.
        // 2. Set up EventQueue.NextEvent() to return the event.
        // 3. Verify that Exception is thrown.
        //
        [Test]
        public void AuthorizationFailure()
        {
            Event @event = TestUtil.CreateEvent(Event.EventType.AUTHORIZATION_STATUS);
            SchemaElementDefinition elementDef = TestUtil.GetAdminMessageDefinition(
                AuthorizerTest.RequestFailure);
            TestUtil.AppendMessage(@event, elementDef);

            this.mockEventQueue.Setup(x => x.NextEvent(It.IsAny<long>()))
                .Returns(@event);

            Assert.Throws<Exception>(() => this.authorizer.Authorize(
                AppOptions.DefaultAuthService,
                "DummyAuthOptions",
                this.mockEventQueue.Object));
        }

        private static Service LoadApiauthService()
        {
            Service service = TestUtil.DeserializeService(
                Resources.ServiceSchema.apiauthSchema);
            Assert.NotNull(service);
            return service;
        }
    }
}
