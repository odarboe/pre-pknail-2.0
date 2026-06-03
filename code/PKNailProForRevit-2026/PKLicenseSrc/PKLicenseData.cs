using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PKLicense
{
    [System.Reflection.Obfuscation(Feature="renaming", ApplyToMembers=true)]
    [JsonObject(MemberSerialization.OptIn)]
    internal class PKLicenseData
    {
        private const int cm_USAGE_UNITS_DAILY_WARNING_DEFAULT = 5;
        private const int cm_USAGE_UNITS_HOURLY_WARNING_DEFAULT = 24;
        private const int cm_EXPIRATION_WARNING_DEFAULT = 15;

        private ConsumedUsageData m_usageData;
        private int m_usageUnitsDailyWarning;
        private int m_usageUnitsHourlyWarning;
        private int m_expirationWarning;
        private int m_defaultUsageAddAmount;
        private string m_lastUserID;

        [JsonConstructor]
        internal PKLicenseData()
        {
            m_usageData = new ConsumedUsageData();
            m_usageUnitsDailyWarning = cm_USAGE_UNITS_DAILY_WARNING_DEFAULT;
            m_usageUnitsHourlyWarning = cm_USAGE_UNITS_HOURLY_WARNING_DEFAULT;
            m_expirationWarning = cm_EXPIRATION_WARNING_DEFAULT;
            m_defaultUsageAddAmount = 0;
            m_lastUserID = @"";
        }

        [JsonProperty]
        internal ConsumedUsageData UsageData
        { get { return m_usageData; } }

        internal int ReserveBalance
        { get { return m_usageData.ReserveBalance; } set { m_usageData.ReserveBalance = value; } }

        internal List<UsageRecord> UsageRecords
        { get { return m_usageData.ConsumeTxnRecords; } }

        [JsonProperty]
        internal int UsageUnitsDailyWarning
        { get { return m_usageUnitsDailyWarning; } set { m_usageUnitsDailyWarning = value; } }

        [JsonProperty]
        internal int UsageUnitsHourlyWarning
        { get { return m_usageUnitsHourlyWarning; } set { m_usageUnitsHourlyWarning = value; } }

        [JsonProperty]
        internal int ExpirationWarning
        { get { return m_expirationWarning; } set { m_expirationWarning = value; } }

        [JsonProperty]
        internal int DefaultUsageAddAmount
        { get { return m_defaultUsageAddAmount; } set { m_defaultUsageAddAmount = value; } }

        [JsonProperty]
        internal string LastUserID
        { get { return m_lastUserID; } set { m_lastUserID = value; } }

        internal UsageRecord AddUsageRecord(DateTime usageStartDateTimeLocal, string installationName, string projectName, string userName,
                                            DateTime meterStartDateTimeLocal)
        {

            UsageRecord record = new UsageRecord(usageStartDateTimeLocal, usageStartDateTimeLocal, 0, 0,
                                                 installationName, projectName, userName,
                                                 meterStartDateTimeLocal, DateTime.MinValue, DateTime.MinValue);
            UsageRecords.Add(record);
            return record;
        }

        internal void ConsumeUsageUnit(DateTime consumedDateTimeLocal)
        {
            UsageRecord record = UsageRecords[UsageRecords.Count - 1];
            record.Usage++;
            ReserveBalance--;
            record.MeterStartDateTimeLocal = consumedDateTimeLocal;
        }

        internal string ToJson()
        { return JsonConvert.SerializeObject(this, camelCase); }

        internal string UsageRecordsToJson()
        { return JsonConvert.SerializeObject(UsageRecords, camelCase); }

        internal static PKLicenseData FromJson(string json)
        { return JsonConvert.DeserializeObject<PKLicenseData>(json); }

        private static JsonSerializerSettings camelCase
        { get { return new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }; } }
    }
}
