using Bloomberglp.Blpapi;

namespace Bloomberg.API.Model.Enriched.BloombergTypes
{
    public interface IBloombergType
    {
        public void ReadElement(Element raw);
    }
}
