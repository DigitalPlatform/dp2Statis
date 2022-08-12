using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DigitalPlatform.LibraryClientOpenApi
{
    // https://github.com/RicoSuter/NSwag/issues/1991#issuecomment-518600843
    public partial class dp2libraryClient
    {
        partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
        {
            settings.ContractResolver = new SafeContractResolver();
        }

        class SafeContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var jsonProp = base.CreateProperty(member, memberSerialization);
                jsonProp.Required = Required.Default;
                return jsonProp;
            }
        }
    }
}
