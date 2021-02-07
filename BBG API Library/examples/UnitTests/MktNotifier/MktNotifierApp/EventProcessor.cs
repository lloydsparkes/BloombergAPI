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

namespace Bloomberglp.BlpapiExamples.UnitTests.MktNotifier.MktNotifierApp
{
    using Bloomberglp.Blpapi;
    using EventType = Bloomberglp.Blpapi.Event.EventType;

    /// <summary>
    /// Handles all the incoming session events and triggers business logic.
    /// </summary>
    internal class EventProcessor
    {
        private readonly INotifier notifier;
        private readonly IComputeEngine computeEngine;

        public EventProcessor(INotifier notifier, IComputeEngine computeEngine)
        {
            this.notifier = notifier;
            this.computeEngine = computeEngine;
        }

        public void ProcessEvent(Event @event, ISession session) {
            foreach (Message msg in @event) {
                EventType eventType = @event.Type;
                if (eventType == EventType.SESSION_STATUS) {
                    this.notifier.LogSessionState(msg);
                } else if (eventType == EventType.SUBSCRIPTION_STATUS) {
                    this.notifier.LogSubscriptionState(msg);
                } else if (eventType == EventType.SUBSCRIPTION_DATA) {
                    if (msg.HasElement(AppOptions.LastPrice)) {
                        double lastPrice = msg.GetElementAsFloat64(AppOptions.LastPrice);
                        double result = this.computeEngine.ComplexCompute(lastPrice);
                        this.notifier.SendToTerminal(result);
                    }
                } else {
                    break;
                }
            }
        }
    }
}
