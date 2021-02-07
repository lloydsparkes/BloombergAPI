using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bloomberg.API.HAPI
{
    public class BloombergResourceManager
    {
        private readonly BloombergClient client;

        public BloombergResourceManager(BloombergClient client)
        {
            this.client = client;
        }

        public void Initalise()
        {
            // Load Universes, field lists, requests and triggers
            var catalogs = client.GetCatalogs();
        } 
    }
}
