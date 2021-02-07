using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Bloomberg.API.HAPI.Model
{
    public class BasicCollection<T> : BasicContainer
    {
        [JsonProperty("contains")]
        public T[] Contains { get; set; }

        [JsonProperty("totalItems")]
        public int Count { get; set; }

        [JsonProperty("pageCount")]
        public int PageCount { get; set; }

        [JsonProperty("view")]
        public BasicPaginationView View { get; set; }
    }
}
