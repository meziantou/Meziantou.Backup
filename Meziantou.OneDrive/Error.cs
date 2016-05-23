using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Meziantou.OneDrive
{
    public class Error
    {
        public string Code { get; set; }

        public Error InnerError { get; set; }

        public string Message { get; set; }

        public string ThrowSite { get; set; }

        [JsonExtensionData]
        public IDictionary<string, object> AdditionalData { get; set; }

        public override string ToString()
        {
            var errorStringBuilder = new StringBuilder();

            if (!string.IsNullOrEmpty(Code))
            {
                errorStringBuilder.AppendFormat("Code: {0}", Code);
                errorStringBuilder.Append(Environment.NewLine);
            }

            if (!string.IsNullOrEmpty(ThrowSite))
            {
                errorStringBuilder.AppendFormat("Throw site: {0}", ThrowSite);
                errorStringBuilder.Append(Environment.NewLine);
            }

            if (!string.IsNullOrEmpty(Message))
            {
                errorStringBuilder.AppendFormat("Message: {0}", Message);
                errorStringBuilder.Append(Environment.NewLine);
            }

            if (InnerError != null)
            {
                errorStringBuilder.Append(Environment.NewLine);
                errorStringBuilder.Append("Inner error");
                errorStringBuilder.Append(Environment.NewLine);
                errorStringBuilder.Append(InnerError);
            }

            return errorStringBuilder.ToString();
        }
    }
}