using System;
using Newtonsoft.Json;

namespace Meziantou.OneDrive
{
    internal class OneDriveItemConverter : JsonConverter
    {
        private readonly OneDriveClient _client;

        public OneDriveItemConverter(OneDriveClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            _client = client;
        }

        public override bool CanConvert(Type type)
        {
            return type == typeof(OneDriveItem);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var obj = new OneDriveItem(_client);
            if (obj == null)
                throw new JsonSerializationException("No object created.");

            serializer.Populate(reader, obj);
            return obj;

        }

        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}