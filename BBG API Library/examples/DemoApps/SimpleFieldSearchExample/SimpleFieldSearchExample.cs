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
    public class SimpleFieldSearchExample
    {
        private const String APIFLDS_SVC = "//blp/apiflds";
        private const int ID_LEN = 13;
        private const int MNEMONIC_LEN = 36;
        private const int DESC_LEN = 40;
        private const String PADDING =
            "                                            ";
        private static readonly Name FIELD_ID = new Name("id");
        private static readonly Name FIELD_MNEMONIC = new Name("mnemonic");
        private static readonly Name FIELD_DATA = new Name("fieldData");
        private static readonly Name FIELD_DESC = new Name("description");
        private static readonly Name FIELD_INFO = new Name("fieldInfo");
        private static readonly Name FIELD_ERROR = new Name("fieldError");
        private static readonly Name FIELD_MSG = new Name("message");

        private String d_serverHost;
        private int d_serverPort ;

        public static void Main(String[] args)
        {
            SimpleFieldSearchExample example = new SimpleFieldSearchExample();
            example.run(args);

            System.Console.WriteLine("Press ENTER to quit");
            System.Console.Read();
        }

        private void run(String[] args)
        {
            d_serverHost = "localhost";
            d_serverPort = 8194;

            SessionOptions sessionOptions = new SessionOptions();
            sessionOptions.ServerHost = d_serverHost;
            sessionOptions.ServerPort = d_serverPort;

            System.Console.WriteLine("Connecting to " + d_serverHost
                                            + ":" + d_serverPort);
            Session session = new Session(sessionOptions);
            bool sessionStarted = session.Start();
            if (!sessionStarted)
            {
                System.Console.WriteLine("Failed to start session.");
                return;
            }
            if (!session.OpenService(APIFLDS_SVC))
            {
                System.Console.WriteLine("Failed to open service: "
                    + APIFLDS_SVC);
                return;
            }

            Service fieldInfoService = session.GetService(APIFLDS_SVC);
            Request request = fieldInfoService.CreateRequest(
                "FieldSearchRequest");
            request.Set("searchSpec", "last price");
            Element exclude = request.GetElement("exclude");
            exclude.SetElement("fieldType", "Static");
            request.Set("returnFieldDocumentation", false);

            System.Console.WriteLine("Sending Request: " + request);
            session.SendRequest(request, null);

            while (true)
            {
                try
                {
                    Event eventObj = session.NextEvent();
                    foreach (Message msg in eventObj)
                    {
                        if (eventObj.Type != Event.EventType.RESPONSE &&
                            eventObj.Type != Event.EventType.PARTIAL_RESPONSE)
                        {
                            continue;
                        }

                        Element fields = msg.GetElement(FIELD_DATA);
                        int numElements = fields.NumValues;

                        printHeader();
                        for (int i = 0; i < numElements; i++)
                        {
                            printField(fields.GetValueAsElement(i));
                        }
                        System.Console.WriteLine();
                    }
                    if (eventObj.Type == Event.EventType.RESPONSE) break;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("Got Exception:" + ex);
                }
            }
        }
        private void printField(Element field)
        {
            String fldId, fldMnemonic, fldDesc;

            fldId = field.GetElementAsString(FIELD_ID);
            if (field.HasElement(FIELD_INFO))
            {
                Element fldInfo = field.GetElement(FIELD_INFO);
                fldMnemonic = fldInfo.GetElementAsString(FIELD_MNEMONIC);
                fldDesc = fldInfo.GetElementAsString(FIELD_DESC);

                System.Console.WriteLine(padString(fldId, ID_LEN) +
                                    padString(fldMnemonic, MNEMONIC_LEN) +
                                    padString(fldDesc, DESC_LEN));
            }
            else
            {
                Element fldError = field.GetElement(FIELD_ERROR);
                fldDesc = fldError.GetElementAsString(FIELD_MSG);

                System.Console.WriteLine("\n ERROR: " + fldId + " - " + fldDesc);
            }
        }

        private void printHeader()
        {
            System.Console.WriteLine(padString("FIELD ID", ID_LEN) +
                                      padString("MNEMONIC", MNEMONIC_LEN) +
                                      padString("DESCRIPTION", DESC_LEN));
            System.Console.WriteLine(padString("-----------", ID_LEN) +
                                      padString("-----------", MNEMONIC_LEN) +
                                      padString("-----------", DESC_LEN));
        }

        private static String padString(String str, int width)
        {
            if (str.Length >= width || str.Length >= PADDING.Length) return str;
            else return str + PADDING.Substring(0, width - str.Length);
        }

        private bool parseCommandLine(String[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                if (string.Compare(args[i], "-ip", true) == 0)
                {
                    d_serverHost = args[i + 1];
                    ++i;
                }
                else if (string.Compare(args[i], "-p", true) == 0)
                {
                    d_serverPort = int.Parse(args[i + 1]);
                    ++i;
                }
                else if (string.Compare(args[i], "-h", true) == 0)
                {
                    printUsage();
                    return (false);
                }
                else
                {
                    System.Console.WriteLine("Ignoring unknown option:" + args[i]);
                }
            }
            return (true);
        }

        private void printUsage()
        {
            System.Console.WriteLine("Usage:");
            System.Console.WriteLine(
               "    Retrieve field information in categorized form");
            System.Console.WriteLine("        [-ip <ipAddress> default = "
                                          + d_serverHost + " ]");
            System.Console.WriteLine("        [-p  <tcpPort>   default = "
                                          + d_serverPort + " ]");
            System.Console.WriteLine("        [-h  print this message and quit]\n");
        }
    }
}
