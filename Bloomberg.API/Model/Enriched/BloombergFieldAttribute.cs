using System;

namespace Bloomberg.API.Model.Enriched
{
    public class BloombergFieldAttribute : Attribute
    {
        public BloombergFieldAttribute(string name, Type dataType)
        {
            Name = name;
            DataType = dataType;
        }

        public string Name { get; set; }
        public Type DataType { get; set; }
        public bool UseIndexAsKeyDictionary { get; set; }
        public bool UseProvidedIndexes { get; set; }
        public int KeyIndex { get; set; }
        public int ValueIndex { get; set; }
    }
}