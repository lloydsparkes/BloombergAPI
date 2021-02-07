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

namespace Bloomberglp.BlpapiExamples.UnitTests.MktNotifier.MktNotifierApp.Implementation
{
    using Bloomberglp.Blpapi;

    internal class TokenGenerator : ITokenGenerator
    {
        private static readonly Name TokenSuccess =
            Name.GetName("TokenGenerationSuccess");
        private static readonly Name TokenFailure =
            Name.GetName("TokenGenerationFailure");
        private static readonly Name Token = Name.GetName("token");

        private readonly ISession session;

        public TokenGenerator(ISession session)
        {
            this.session = session;
        }

        public string Generate(IEventQueue eventQueue = null)
        {
            IEventQueue tokenEventQueue = eventQueue ?? new EventQueue();
            this.session.GenerateToken(new CorrelationID(), tokenEventQueue);

            string token = null;
            Event @event = tokenEventQueue.NextEvent();
            foreach (Message msg in @event) {
                Name messageType = msg.MessageType;

                if (TokenGenerator.TokenSuccess.Equals(messageType))
                {
                    token = msg.GetElementAsString(Token);
                    break;
                }

                if (TokenGenerator.TokenFailure.Equals(messageType))
                {
                    break;
                }
            }

            return token;
        }
    }
}
