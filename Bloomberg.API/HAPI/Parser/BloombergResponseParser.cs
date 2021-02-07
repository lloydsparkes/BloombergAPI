using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bloomberg.API.Model;
using Bloomberg.API.Model.Enriched;
using Bloomberg.API.Model.Enriched.BloombergTypes;
using Microsoft.Extensions.Logging;

namespace Bloomberg.API.HAPI.Parser
{
    public static class BloombergResponseParser
    {
        private enum ParserState
        {
            Search,
            Headers,
            Fields,
            Data,
            End
        }

        public const string StartOfFile = "START-OF-FILE";
        public const string EndOfFile = "END-OF-FILE";
        public const string StartOfFields = "START-OF-FIELDS";
        public const string EndOfFields = "END-OF-FIELDS";
        public const string StartOfData = "START-OF-DATA";
        public const string EndOfData = "END-OF-DATA";
        public const string TimeStarted = "TIMESTARTED";
        public const string TimeFinished = "TIMEFINISHED";
        public const string HeaderProgramName = "PROGRAMNAME";
        public const string HeaderDateFormat = "DATEFORMAT";

        public static BResponse Parse(StreamReader blob, ILogger logger)
        {
            var state = ParserState.Search;
            var response = new BResponse();
            string line;

            while ((line = blob.ReadLine()) != null && state != ParserState.End)
            {
                Console.WriteLine(line);
                if (line.StartsWith("#")) continue; // Ignore comment lines
                if (line.Length == 0) continue;

                switch (state)
                {
                    case ParserState.Search:
                        if (line == StartOfFile) state = ParserState.Headers;
                        if (line == StartOfData) state = ParserState.Data;
                        if (line.StartsWith(TimeStarted)) response.StartTimestamp = ParseTimestamp(line);
                        if (line.StartsWith(TimeFinished)) response.EndTimestamp = ParseTimestamp(line);
                        if (line == EndOfFile) state = ParserState.End;
                        break;
                    case ParserState.Headers:
                        if (line == StartOfFields) state = ParserState.Fields;
                        // Parse headers
                        else response.AddHeader(ParseHeader(line));
                        break;
                    case ParserState.Fields:
                        if (line == EndOfFields) state = ParserState.Search;
                        // Parse fields
                        else response.AddField(ParseField(line));
                        break;
                    case ParserState.Data:
                        if (line == EndOfData) state = ParserState.Search;
                        // Process Line
                        if (response.Headers.ContainsKey(HeaderProgramName) && response.Headers[HeaderProgramName] == "gethistory")
                        {
                            string dateFormat = response.GetDateFormat();

                            response.AddTimeSeriesEntry(ParseDatedDataLine(line, response.Fields.ToList(), dateFormat, logger));
                        }
                        else
                        {
                            response.AddEntry(ParseDataLine(line, response.Fields.ToList()));
                        }

                        break;
                }
            }

            response.Source = ResponseSource.Hapi;
            return response;
        }

        private static (string identifier, int returnCode, DateTime date, IDictionary<string, string> results) ParseDatedDataLine(string line, List<string> fields, string dateFormat, ILogger logger)
        {
            var bits = line.Split('|');
            if (bits.Length >= 3)
            {
                var identifier = bits[0];
                var returnCode = bits[1].ToInt();
                var numOfFields = bits[2].ToInt();

                if (returnCode == 10 || returnCode == 11 || string.IsNullOrWhiteSpace(bits[3]))
                {
                    return (identifier, returnCode, DateTime.MinValue, null);
                }

                DateTime date;
                if (!DateTime.TryParseExact(bits[3], dateFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out date))
                {
                    // deal with error
                    logger.LogWarning($"Could not parse date from Bloomberg: '{bits[3]}' - format expected {dateFormat}");
                    return (null, 0, DateTime.MinValue, null);
                }

                var resultDict = fields.ToDictionary(x => x, y => bits[fields.IndexOf(y) + 4].Trim());
                return (identifier, returnCode, date, resultDict);
            }

            return (null, 0, DateTime.MinValue, null);
        }

        private static (string identifier, int returnCode, IDictionary<string, string> results) ParseDataLine(string line, List<string> fields)
        {
            var bits = line.Split('|');
            if (bits.Length >= 3)
            {
                var identifier = bits[0];
                var returnCode = bits[1].ToInt();
                var numOfFields = bits[2].ToInt();

                if (returnCode == 10 || returnCode == 11)
                {
                    return (identifier, returnCode, null);
                }

                var resultDict = fields.ToDictionary(x => x, y => bits[fields.IndexOf(y) + 3].Trim());
                return (identifier, returnCode, resultDict);
            }

            return (null, 0, null);
        }

        private static string ParseField(string line)
        {
            return line.Trim();
        }

        private static (string key, string value) ParseHeader(string line)
        {
            var bits = line.Split('=');
            return (bits[0], bits[1]);
        }

        private static DateTime ParseTimestamp(string line)
        {
            var bits = line.Split('=')[1];
            DateTime result;

            if (DateTime.TryParseExact(bits, "ddd MMM  d HH:mm:ss 'BST' yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out result))
                return result;
            if (DateTime.TryParseExact(bits, "ddd MMM d HH:mm:ss 'BST' yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out result))
                return result;
            if (DateTime.TryParseExact(bits, "ddd MMM  d HH:mm:ss 'GMT' yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out result))
                return result;
            if (DateTime.TryParseExact(bits, "ddd MMM d HH:mm:ss 'GMT' yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out result))
                return result;

            throw new InvalidDataException($"Could not parse {bits} int to a valid DateTime object");
        }

        public static object ParseComplexTable(BloombergFields field, string rawString, string dateFormat, ILogger logger)
        {
            if (rawString == null || rawString.Length == 0) return null;
            var delim = rawString[0];
            var bits = rawString.Split(delim);

            var dimensions = int.Parse(bits[1]);
            var rows = int.Parse(bits[2]);
            var columns = dimensions > 1 ? int.Parse(bits[3]) : 1;

            var resultantRows = new List<object[]>();
            var currentRow = new object[columns];

            var bitIndex = dimensions + 2;

            int col = 0;
            int colType = 0;
            while (bitIndex < bits.Length)
            {
                if (string.IsNullOrWhiteSpace(bits[bitIndex])) break;
                if (colType == 0) colType = int.Parse(bits[bitIndex]);
                else
                {
                    object data = null;
                    switch (colType)
                    {
                        case 1:
                        case 11: // Treat Currency as a string
                        case 4: // Treat Security Name as a string
                            data = bits[bitIndex];
                            break;
                        case 2:
                        case 3: // Treat prices as a number
                        case 13:
                            data = decimal.Parse(bits[bitIndex]);
                            break;
                        case 5:
                            data = DateTime.ParseExact(bits[bitIndex], dateFormat, CultureInfo.CurrentCulture);
                            break;
                        case 10:
                            data = bits[bitIndex] == "Y";
                            break;
                        case 12:
                            data = int.Parse(bits[bitIndex]);
                            break;
                        default:
                            logger.LogError($"Could not correctly parse DataType of index {0} from response - please add code support for this type of required - defaulted to string");
                            data = bits[bitIndex];
                            break;
                    }

                    colType = 0;
                    currentRow[col] = data;
                    col++;
                    if (col == columns)
                    {
                        resultantRows.Add(currentRow);
                        currentRow = new object[columns];
                        col = 0;
                    }
                }

                bitIndex++;
            }

            // Now turn this basic object into our final required object.
            if (BloombergFieldExtensions.IsEnumerableOf(field.BloombergType(), typeof(IBloombergType)))
            {
                var type = field.BloombergType();
                var innerType = BloombergFieldExtensions.GetInnerType(type);
                var listType = typeof(List<>).MakeGenericType(innerType);

                var list = Activator.CreateInstance(listType);
                foreach (var row in resultantRows)
                {
                    var newItem = Activator.CreateInstance(innerType);
                    newItem.GetType().GetMethod("ReadBits").Invoke(newItem, new[] {row});
                    list.GetType().GetMethod("Add").Invoke(list, new[] {newItem});
                }

                return list;
            }

            if (field.BloombergType() == typeof(IDictionary<DateTime, decimal>))
            {
                var dict = new Dictionary<DateTime, decimal>();

                int keyIndex = 0, valueIndex = 1;
                if (field.UseProvidedIndexes())
                {
                    (keyIndex, valueIndex) = field.ProvidedIndexes();
                }

                foreach (var row in resultantRows)
                {
                    if (row.Length > keyIndex && row.Length > valueIndex)
                    {
                        if (row[keyIndex] is DateTime key && row[valueIndex] is decimal value)
                        {
                            if (!dict.ContainsKey(key))
                            {
                                dict.Add(key, value);
                            }
                            else
                            {
                                logger.LogWarning($"While parsing {field} got duplicate keys {key}-> {value}");
                            }
                        }
                        else
                        {
                            logger.LogWarning($"While processing row of {field} key is type of {row[keyIndex].GetType().Name} instead of DateTime OR value is type of {row[valueIndex].GetType().Name} instead of decimal");
                        }
                    }
                    else
                    {
                        logger.LogWarning($"While processing row of {field}. KeyIndex ({keyIndex}) or ValueIndex ({valueIndex}) is out of bounds (Bound = {row.Length})");
                    }
                }

                return dict;
            }

            if (field.BloombergType() == typeof(IDictionary<string, string>))
            {
                var dict = new Dictionary<string, string>();

                int keyIndex = 0, valueIndex = 1;
                if (field.UseProvidedIndexes())
                {
                    (keyIndex, valueIndex) = field.ProvidedIndexes();
                }

                foreach (var row in resultantRows)
                {
                    if (row.Length > keyIndex && row.Length > valueIndex)
                    {
                        if (!dict.ContainsKey(row[keyIndex].ToString()))
                        {
                            dict.Add(row[keyIndex].ToString(), row[valueIndex].ToString());
                        }
                        else
                        {
                            logger.LogWarning($"While parsing {field} got duplicate keys {row[keyIndex]}-> {row[valueIndex]}");
                        }
                    }
                    else
                    {
                        logger.LogWarning($"While processing row of {field}. KeyIndex ({keyIndex}) or ValueIndex ({valueIndex}) is out of bounds (Bound = {row.Length})");
                    }
                }

                return dict;
            }

            if (field.BloombergType() == typeof(IDictionary<int, string>))
            {
                var dict = new Dictionary<int, string>();

                int keyIndex = 0, valueIndex = 1;
                if (field.UseProvidedIndexes())
                {
                    (keyIndex, valueIndex) = field.ProvidedIndexes();
                }

                int rowIndex = 1;
                foreach (var row in resultantRows)
                {
                    int key = 0;

                    if (field.UseIndexAsKeyDictionary())
                    {
                        key = rowIndex;
                    }
                    else if (row.Length > keyIndex)
                    {
                        if (row[keyIndex] is decimal decKey)
                        {
                            row[keyIndex] = Convert.ToInt32(decKey);
                        } 
                        else if (row[keyIndex] is int k)
                        {
                            key = k;
                        }
                        else
                        {
                            logger.LogWarning($"While processing row of {field} key is type of {row[keyIndex].GetType().Name} instead of int");
                        }
                    }
                    else
                    {
                        logger.LogWarning($"While processing row of {field}. KeyIndex ({keyIndex}) is out of bounds (Bound = {row.Length})");
                    }

                    if (row.Length > valueIndex)
                    {
                        if (!dict.ContainsKey(key))
                        {
                            dict.Add(key, row[valueIndex].ToString());
                        }
                        else
                        {
                            logger.LogWarning($"While parsing {field} got duplicate keys {key}-> {row[valueIndex]}");
                        }
                    }
                    else
                    {
                        logger.LogWarning($"While processing row of {field}. KeyIndex ({keyIndex}) or ValueIndex ({valueIndex}) is out of bounds (Bound = {row.Length})");
                    }

                    rowIndex++;
                }

                return dict;
            }

            throw new Exception($"{field.BloombergType().Name} is not supported.");
        }
    }
}
