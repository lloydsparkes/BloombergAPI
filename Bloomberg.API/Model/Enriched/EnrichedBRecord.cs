using System.Collections.Generic;

namespace Bloomberg.API.Model.Enriched
{
    public class EnrichedBRecord
    {
        public EnrichedBRecord(string identifier, IDictionary<BloombergFields, object> pairs)
        {
            Identifier = identifier;
            Pairs = pairs;
        }

        /// <summary>
        /// The security this record is for
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// The key value pars of data
        /// </summary>
        public IDictionary<BloombergFields, object> Pairs { get; set; }

        /// <summary>
        /// Gets the Value out for a given key in a Type Safe Way
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="field"></param>
        /// <returns></returns>
        public T GetValue<T>(BloombergFields field)
        {
            return (T) Pairs[field];
        }
    }
}
