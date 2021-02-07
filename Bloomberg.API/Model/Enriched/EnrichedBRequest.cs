using System.Collections.Generic;
using System.Linq;

namespace Bloomberg.API.Model.Enriched
{
    /// <summary>
    /// Represents a reference data request to bloomberg. Details the data required and the securities (including yellow keys) it is required for
    /// </summary>
    public class EnrichedBRequest
    {
        /// <summary>
        /// Allows for the fetching of ISINs without the yellow key
        /// </summary>
        /// <param name="identifer"></param>
        /// <returns></returns>
        public static string FormatIdentifier(string identifer)
        {
            identifer = identifer.ToUpper();

            if (identifer.Contains(" "))
            {
                // has a yellow key
                return identifer;
            }
            if (identifer.IsIsin())
            {
                return $"/ISIN/{identifer}";
            }
            if (identifer != null && identifer.StartsWith("BB"))
            {
                return "/BBGID/" + identifer;
            }
            if (identifer.IsCusip())
            {
                return $"/CUSIP/{identifer}";
            }

            //could raise error but lets just pass if through
            return identifer;
        }

        public EnrichedBRequest()
        {
            Fields = new List<BloombergFields>();
            Identifiers = new List<string>();
            Overrides = new List<BOverride>();
        }

        /// <summary>
        /// Creates a new request
        /// </summary>
        /// <param name="fields">A set of BloombergFields to collect data for</param>
        /// <param name="identifiers">A set of Bloomberg identifiers (including Yellow Key)</param>
        public EnrichedBRequest(IEnumerable<BloombergFields> fields, IEnumerable<string> identifiers, IDictionary<string, string> overrides = null)
        {
            Fields = fields.ToList();
            Identifiers = identifiers.ToList();
            Overrides = new List<BOverride>();

            if (overrides != null && overrides.Any())
            {
                foreach (var identifer in Identifiers)
                {
                    foreach (var field in Fields)
                    {
                        Overrides.Add(new BOverride(identifer, field.BloombergFieldName(), overrides.ToDictionary(x => x.Key, y => (object) y.Value)));
                    }
                }
            }
        }

        public ICollection<BloombergFields> Fields { get; private set; }
        public ICollection<string> Identifiers { get; private set; }
        public ICollection<BOverride> Overrides { get; private set; }
        
        public BRequest ToBRequest()
        {
            var request = new BRequest(Identifiers.ToArray(), Fields.Select(x => x.BloombergFieldName()).ToArray());
            request.Overrides.AddRange(Overrides);
            return request;
        }
    }
}
