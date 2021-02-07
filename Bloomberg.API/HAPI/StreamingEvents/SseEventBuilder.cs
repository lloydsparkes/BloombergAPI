using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bloomberg.API.HAPI.Model;

namespace Bloomberg.API.HAPI.StreamingEvents
{
    /// <summary>
    /// This class constructs a new event from event stream lines.
    /// </summary>
    public class SseEventBuilder
    {
        private const string DDefaultType = "message";
        private const string DFieldNameData = "data";
        private const string DFieldNameId = "id";
        private const string DFieldNameRetry = "retry";
        private const string DFieldNameType = "event";
        private const string DFieldPattern = "(?<field>event|id|data|retry):?( ?(?<value>.*))";
        private const char DLineSeparator = '\n';
        private const char DValueMark = ':';

        private static readonly Regex DFieldParser = new Regex(DFieldPattern);

        private readonly string dOrigin;
        private string dType = DDefaultType;
        private string dId = null;
        private readonly List<string> dData = new List<string>();
        private readonly List<string> dComments = new List<string>();
        private int? dRetry = null;

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="origin">A URI the stream came from.</param>
        public SseEventBuilder(string origin)
        {
            dOrigin = origin;
        }

        /// <summary>
        /// Parses the specified line according to the SSE standard rules
        /// and makes the line a part of the event being constructed.
        /// </summary>
        /// <param name="line">Single event stream line.</param>
        public void AddLine(string line)
        {
            if (line.StartsWith(DValueMark.ToString()))
            {
                dComments.Add(line.Substring(1));
                return;
            }

            var match = DFieldParser.Match(line);
            if (!match.Success)
            {
                // Unknown field names are ignored according to the standard.
                Console.Error.WriteLine($"Invalid SSE line: {line}");
                return;
            }

            var field = match.Groups["field"].Value;
            var value = match.Groups["value"].Value;
            switch (field)
            {
                case DFieldNameType:
                    dType = value;
                    break;
                case DFieldNameId:
                    dId = value;
                    break;
                case DFieldNameData:
                    dData.Add(value);
                    break;
                case DFieldNameRetry:
                    if (int.TryParse(value, out int retry))
                    {
                        dRetry = retry;
                    }
                    break;
            }
        }

        /// <summary>
        /// Makes a new event using the fields collected so far.
        /// </summary>
        /// <returns>A new event.</returns>
        public SseEvent MakeEvent()
        {
            return new SseEvent(
                dOrigin,
                dType,
                dId,
                dData.Any() ? string.Join(DLineSeparator.ToString(), dData) : null,
                dComments.Any() ? string.Join(DLineSeparator.ToString(), dComments) : null,
                dRetry);
        }
    }
}