using System;
using System.Collections.Generic;
using System.Text;

namespace Bloomberg.API.Model
{
    public class BTable
    {
        public string KeyColumn { get; set; }
        public string[] Columns { get; set; }
        public IReadOnlyDictionary<object, object[]> Rows { get; set; }
    }
}
