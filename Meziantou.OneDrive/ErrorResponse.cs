using System.Collections.Generic;
using Newtonsoft.Json;

namespace Meziantou.OneDrive
{
    public class ErrorResponse
    {
        public Error Error { get; set; }

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalData { get; set; }
    }
}