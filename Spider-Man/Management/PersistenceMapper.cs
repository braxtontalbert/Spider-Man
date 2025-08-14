using System.Collections.Generic;
using Newtonsoft.Json;
using ThunderRoad;

namespace Spider_Man.Management
{
    public class PersistenceMapper
    {
        public string characterID;
        public Dictionary<string, bool> sideData;
        public string ToJson() => JsonConvert.SerializeObject(this, Catalog.GetJsonNetSerializerSettings());
    }
}