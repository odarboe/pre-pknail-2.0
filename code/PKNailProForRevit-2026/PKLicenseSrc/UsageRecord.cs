using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

namespace PKLicense
{
    [System.Reflection.Obfuscation(Feature="renaming", ApplyToMembers=true)]
    [JsonObject(MemberSerialization.OptIn)]
    internal class UsageRecord
    {
        private DateTime m_startDateTimeLocal;
        private DateTime m_endDateTimeLocal;
        private int m_usage;
        private int m_captureCount;
        private string m_installationName;
        private string m_projectName;
        private string m_userID;
        private DateTime m_meterStartDateTimeLocal;
        private DateTime m_postDateTimeLocal;
        private DateTime m_postDateTimeUtc;

        internal UsageRecord(DateTime startDateTimeLocal, DateTime endDateTimeLocal, int usage,
                             string installationName, string userID, string projectName)
        {
            m_startDateTimeLocal = startDateTimeLocal;
            m_endDateTimeLocal = endDateTimeLocal;
            m_usage = usage;
            m_captureCount = 0;
            m_installationName = installationName;
            m_projectName = projectName;
            m_userID = userID;
            m_meterStartDateTimeLocal = DateTime.MinValue;
            m_postDateTimeLocal = DateTime.MinValue;
            m_postDateTimeUtc = DateTime.MinValue;
        }

        [JsonConstructor]
        internal UsageRecord(DateTime startDateTimeLocal, DateTime endDateTimeLocal, int usage, int captureCount,
                             string installationName, string projectName, string userID,
                             DateTime meterStartDateTimeLocal, DateTime postDateTimeLocal, DateTime postDateTimeUtc)
        {
            m_startDateTimeLocal = startDateTimeLocal;
            m_endDateTimeLocal = endDateTimeLocal;
            m_usage = usage;
            m_captureCount = captureCount;
            m_installationName = installationName;
            m_projectName = projectName;
            m_userID = userID;
            m_meterStartDateTimeLocal = meterStartDateTimeLocal;
            m_postDateTimeLocal = postDateTimeLocal;
            m_postDateTimeUtc = postDateTimeUtc;
        }

        [JsonProperty]
        internal DateTime StartDateTimeLocal  { get { return m_startDateTimeLocal; } }
        [JsonProperty]
        internal DateTime EndDateTimeLocal  { get { return m_endDateTimeLocal; } set { m_endDateTimeLocal = value; } }
        [JsonProperty]
        internal int Usage  { get { return m_usage; } set { m_usage = value; } }
        [JsonProperty]
        internal int CaptureCount  { get { return m_captureCount; } set { m_captureCount = value; } }
        [JsonProperty]
        internal string InstallationName  { get { return m_installationName; } }
        [JsonProperty]
        internal string ProjectName  { get { return m_projectName; } set { m_projectName = value; } }
        [JsonProperty]
        internal string UserID  { get { return m_userID; } set { m_userID = value; } }
        [JsonProperty]
        internal DateTime MeterStartDateTimeLocal  { get { return m_meterStartDateTimeLocal; } set { m_meterStartDateTimeLocal = value; } }
        [JsonProperty]
        internal DateTime postDateTimeLocal  { get { return m_postDateTimeLocal; } set { m_postDateTimeLocal = value; } }
        [JsonProperty]
        internal DateTime postDateTimeUtc  { get { return m_postDateTimeUtc; } set { m_postDateTimeUtc = value; } }
    }
}
