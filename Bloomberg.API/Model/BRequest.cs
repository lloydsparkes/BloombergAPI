using System;
using System.Collections.Generic;
using System.Text;

namespace Bloomberg.API.Model
{
    public class BRequest
    {
        public BRequest(string identifier, string field)
        {
            Identifiers.Add(identifier);
            Fields.Add(field);
        }

        public BRequest(string identifier, string[] fields)
        {
            Identifiers.Add(identifier);
            Fields.AddRange(fields);
        }

        public BRequest(string[] identifiers, string[] fields)
        {
            Identifiers.AddRange(identifiers);
            Fields.AddRange(fields);
        }

        public BRequest()
        {

        }

        public void AddItem(string identifier, string field, IReadOnlyDictionary<string, object> overrides = null)
        {
            if (!Identifiers.Contains(identifier))
            {
                Identifiers.Add(identifier);
            }

            if (!Fields.Contains(field))
            {
                Fields.Add(field);
            }

            Overrides.Add(new BOverride(identifier, field, overrides));
        }

        public bool IsHistorical { get; set; }
        public DateTime? HistoricalFromDate { get; set; }
        public DateTime? HistoricalEndDate { get; set; }
        
        public ICollection<string> Identifiers { get; set; } = new List<string>();
        public ICollection<string> Fields { get; set; } = new List<string>();

        public ICollection<BOverride> Overrides { get; set; } = new List<BOverride>();
    }
}
