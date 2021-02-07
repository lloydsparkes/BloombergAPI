using System.Collections.Generic;
using Newtonsoft.Json;

namespace Bloomberg.API.Model.Enriched
{
    public class EnrichedBResponse : BResponse
    {
        public EnrichedBResponse(){}

        public EnrichedBResponse(IEnumerable<EnrichedBRecord> records)
        {
            Records = records;
        }

        public IEnumerable<EnrichedBRecord> Records { get; set; }
    }
}
