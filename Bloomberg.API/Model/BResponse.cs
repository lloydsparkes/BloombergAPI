using System;
using System.Collections.Generic;
using System.Text;
using Bloomberg.API.HAPI.Parser;

namespace Bloomberg.API.Model
{
    public class BEntry
    {
        public IDictionary<string, string> Fields { get; set; }
    }
    
    public class BTimeSeriesEntry
    {
        public IDictionary<DateTime, BEntry> Series { get; set; } = new Dictionary<DateTime, BEntry>();
    }

    public enum ResponseSource
    {
        Terminal,
        Hapi
    }

    public class BResponse
    {
        public DateTime StartTimestamp { get; set; }
        public DateTime EndTimestamp { get; set; }

        public IList<string> Fields { get; set; } = new List<string>();
        public IDictionary<string, BEntry> Data { get; set; } = new Dictionary<string, BEntry>();
        public IDictionary<string, BTimeSeriesEntry> TimeSeries { get; set; } = new Dictionary<string, BTimeSeriesEntry>();
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        
        public void AddHeader((string key, string value) header)
        {
            Headers.Add(header.key, header.value);
        }

        public void AddField(string parseField)
        {
            Fields.Add(parseField);
        }

        public void AddEntry((string identifier, int returnCode, IDictionary<string, string> data) result)
        {
            if (result.identifier != null && result.data != null && !Data.ContainsKey(result.identifier))
            {
                Data.Add(result.identifier, new BEntry{Fields = result.data});
            }
        }

        public string GetValue(string ticker, string field)
        {
            if (Data.ContainsKey(ticker))
            {
                var entries = Data[ticker];
                if (entries.Fields.ContainsKey(field))
                {
                    return entries.Fields[field];
                }
            }

            return null;
        }

        public void AddTimeSeriesEntry((string identifier, int returnCode, DateTime date, IDictionary<string, string> data) result)
        {
            if (result.identifier != null && result.data != null && !Data.ContainsKey(result.identifier))
            {
                if (!TimeSeries.ContainsKey(result.identifier))
                {
                    TimeSeries.Add(result.identifier, new BTimeSeriesEntry());
                }

                var entry = TimeSeries[result.identifier];
                if (!entry.Series.ContainsKey(result.date))
                {
                    entry.Series.Add(result.date, new BEntry {Fields = result.data});
                }
            }
        }

        public string GetDateFormat(string def = "MM/dd/yyyy")
        {
            var value = def;
            if (Headers.ContainsKey(BloombergResponseParser.HeaderDateFormat)) value =  Headers[BloombergResponseParser.HeaderDateFormat];

            if (value.Contains("mm")) value = value.Replace("mm", "MM");
            return value;
        }
        
        public ResponseSource Source { get; set; }
    }
}
