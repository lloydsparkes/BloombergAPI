/* Copyright 2012. Bloomberg Finance L.P.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:  The above
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
using System;
using Bloomberglp.Blpapi;

namespace Bloomberglp.BlpapiExamples.DemoApps
{

    public class SimpleIntradayTickExample
    {
        public static void Main(string[] args)
        {
            SimpleIntradayTickExample example = new SimpleIntradayTickExample();
            example.run(args);
            System.Console.WriteLine("Press ENTER to quit");
            System.Console.Read();
        }

        private DateTime getPreviousTradingDate()
        {
            DateTime tradedOn = DateTime.Now;
            tradedOn = tradedOn.AddDays(-1);

            if (tradedOn.DayOfWeek == DayOfWeek.Sunday)
            {
                tradedOn = tradedOn.AddDays(-2);
            }
            else if (tradedOn.DayOfWeek == DayOfWeek.Saturday)
            {
                tradedOn = tradedOn.AddDays(-1);
            }
            return tradedOn;
        }

        private void run(string[] args)
        {
            string serverHost = "localhost";
            int serverPort = 8194;

            SessionOptions sessionOptions = new SessionOptions();
            sessionOptions.ServerHost = serverHost;
            sessionOptions.ServerPort = serverPort;

            System.Console.WriteLine("Connecting to " + serverHost + ":" + serverPort);
            Session session = new Session(sessionOptions);
            bool sessionStarted = session.Start();
            if (!sessionStarted)
            {
                System.Console.Error.WriteLine("Failed to start session.");
                return;
            }
            if (!session.OpenService("//blp/refdata"))
            {
                System.Console.Error.WriteLine("Failed to open //blp/refdata");
                return;
            }
            Service refDataService = session.GetService("//blp/refdata");
            Request request = refDataService.CreateRequest("IntradayTickRequest");
            request.Set("security", "VOD LN Equity");
            request["eventTypes"].AppendValue("TRADE");
            request["eventTypes"].AppendValue("AT_TRADE");
            DateTime tradedOn = getPreviousTradingDate();
            request.Set("startDateTime", new Datetime(tradedOn.Year,
                                                      tradedOn.Month,
                                                      tradedOn.Day,
                                                      10, 30, 0, 0));
            request.Set("endDateTime", new Datetime(tradedOn.Year,
                                                    tradedOn.Month,
                                                    tradedOn.Day,
                                                    10, 35, 0, 0));
            request.Set("includeConditionCodes", true);

            System.Console.WriteLine("Sending Request: " + request);
            session.SendRequest(request, null);

            while (true)
            {
                Event eventObj = session.NextEvent();
                foreach (Message msg in eventObj)
                {
                    System.Console.WriteLine(msg.AsElement);
                }
                if (eventObj.Type == Event.EventType.RESPONSE)
                {
                    break;
                }
            }
        }
    }
}
