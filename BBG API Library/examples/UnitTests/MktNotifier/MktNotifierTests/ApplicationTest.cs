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
    using System.Collections.Generic;
    using Bloomberglp.Blpapi;
    using Bloomberglp.BlpapiExamples.UnitTests.MktNotifier.MktNotifierApp;
    using Moq;
    using NUnit.Framework;

    internal class ApplicationTest
    {
        private Mock<ISession> mockSession;
        private Mock<IAuthorizer> mockAuthorizer;
        private Mock<ISubscriber> mockSubscriber;
        private Mock<Identity> mockIdentity;

        private AppOptions appOptions;
        private Application application;

        [SetUp]
        public void SetUp()
        {
            this.mockSession = new Mock<ISession>();
            this.mockAuthorizer = new Mock<IAuthorizer>();
            this.mockSubscriber = new Mock<ISubscriber>();
            this.mockIdentity = new Mock<Identity>();

            this.appOptions = new AppOptions();
            this.application = new Application(
                this.mockSession.Object,
                this.mockAuthorizer.Object,
                this.mockSubscriber.Object,
                this.appOptions);
        }

        [TearDown]
        public void TearDown()
        {
            this.mockSession.VerifyAll();
            this.mockAuthorizer.VerifyAll();
            this.mockSubscriber.VerifyAll();
            this.mockIdentity.VerifyAll();
        }

        //
        // Concern:
        // Verify that if Session fails to start, no authorization and
        // subscriptions are made.
        //
        // Plan:
        // Set up Session.Start() to return false.
        //
        [Test]
        public void SessionStartFail()
        {
            this.mockSession.Setup(x => x.Start()).Returns(false);
            this.application.Run();
        }

        //
        // Concern:
        // Verify that if authorization fails, no subscriptions are made.
        //
        // Plan:
        // 1. Set up Session.Start() to return true.
        // 2. Set up IAuthorizer.Authorize() to throw Exception.
        //
        [Test]
        public void SessionAuthorizeFail()
        {
            this.mockSession.Setup(x => x.Start()).Returns(true);
            this.mockAuthorizer.Setup(x => x.Authorize(
                    this.appOptions.AuthService,
                    this.appOptions.AuthOptions,
                    null))
                .Throws<Exception>();

            Assert.Throws<Exception>(() => this.application.Run());
        }

        //
        // Concern:
        // Verify correct auth service and auth options are used for authorization.
        //
        [Test]
        public void AuthorizeWithConfig()
        {
            this.mockSession.Setup(x => x.Start()).Returns(true);

            this.appOptions.AuthService = "authService";
            this.appOptions.AuthOptions = "app=test:app";

            this.mockAuthorizer.Setup(x => x.Authorize(
                    this.appOptions.AuthService,
                    this.appOptions.AuthOptions,
                    null))
                .Returns(this.mockIdentity.Object)
                .Verifiable();

            this.application.Run();

            this.mockSubscriber.Verify(x => x.Subscribe(
                It.IsAny<string>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                this.mockIdentity.Object));
        }

        //
        // Concern:
        // Verify that correct service, topics, fields and options are used
        // when subscribing.
        //
        [Test]
        public void SubscribeWithConfig()
        {
            this.mockSession.Setup(x => x.Start()).Returns(true);

            this.appOptions.Service = "mktdataService";
            this.appOptions.Topics.Add("IBM US Equity");
            this.appOptions.Topics.Add("MSFT US Equity");

            this.appOptions.Fields.Add("LAST_PRICE");
            this.appOptions.Fields.Add("BID");
            this.appOptions.Fields.Add("ASK");

            this.appOptions.Options.Add("option1");
            this.appOptions.Options.Add("option2");

            this.mockAuthorizer.Setup(x => x.Authorize(
                    this.appOptions.AuthService,
                    this.appOptions.AuthOptions,
                    null))
                .Returns(this.mockIdentity.Object);

            this.application.Run();

            this.mockSubscriber.Verify(x => x.Subscribe(
                this.appOptions.Service,
                this.appOptions.Topics,
                this.appOptions.Fields,
                this.appOptions.Options,
                this.mockIdentity.Object));
        }
    }
}
