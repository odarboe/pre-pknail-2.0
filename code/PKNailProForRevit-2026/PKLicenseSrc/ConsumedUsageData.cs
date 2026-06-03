using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PKLicense
{
    [System.Reflection.Obfuscation(Feature="renaming", ApplyToMembers=true)]
    [JsonObject(MemberSerialization.OptIn)]
    internal class ConsumedUsageData
    {
        private int m_reserveBalance;
        private List<UsageRecord> m_consumeTxnRecords;

        [JsonConstructor]
        internal ConsumedUsageData()
        {
            m_reserveBalance = 0;
            m_consumeTxnRecords = new List<UsageRecord>();
        }

        [JsonProperty]
        internal int ReserveBalance
        { get { return m_reserveBalance; } set { m_reserveBalance = value; } }

        [JsonProperty]
        internal List<UsageRecord> ConsumeTxnRecords
        { get { return m_consumeTxnRecords; } }

        internal string ToJson()
        { return JsonConvert.SerializeObject(this, consumedUsageRecordsJsonSerializerSettings); }

        private static JsonSerializerSettings consumedUsageRecordsJsonSerializerSettings
        {
            get
            {
                return new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    DateFormatString = @"yyyy-MM-ddTHH:mm:ss.fff"
                };
            }
        }
    }
}
