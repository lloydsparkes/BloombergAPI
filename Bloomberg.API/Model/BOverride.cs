using System;
using System.Collections.Generic;
using System.Text;

namespace Bloomberg.API.Model
{
    public class BOverride
    {
        public string Identifier { get; set; }
        public string Field { get; set; }
        public IReadOnlyDictionary<string, object> Overrides { get; set; }

        public BOverride(string identifier, string field, IReadOnlyDictionary<string, object> overrides)
        {
            Identifier = identifier;
            Field = field;
            Overrides = overrides;
        }
    }
}
