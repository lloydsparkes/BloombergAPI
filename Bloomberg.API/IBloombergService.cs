using System.Collections.Generic;
using System.Threading.Tasks;
using Bloomberg.API.Model;

namespace Bloomberg.API
{
    public interface IBloombergService
    {
        /// <summary>
        /// Sends a BRequest and returns the BResponse
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<BResponse> RequestAsync(BRequest request);
        
        bool IsConnected { get; }
    }
}
