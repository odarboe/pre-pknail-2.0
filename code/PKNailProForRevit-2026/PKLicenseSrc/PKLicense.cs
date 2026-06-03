using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Net;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;

using Newtonsoft.Json;

using com.softwarekey.Client.Licensing;
using com.softwarekey.Client.Utils;

namespace PKLicense
{
    internal sealed class PKLicense
    {
#if (DEBUG)
        private const double cm_licenseRefreshIntervalDays = 0.0;
        private const double cm_dailyUsageGracePeriodMinutes = 1.0;
        private const int cm_backgroundPostUsageInterval = 10 * 1000;  // ten seconds
        private static TimeSpan cm_oneHourMetered = new TimeSpan(0, 2, 0);  // two minutes
#else
        private const double cm_licenseRefreshIntervalDays = 1.0;
        private const double cm_dailyUsageGracePeriodMinutes = 15.0;
        private const int cm_backgroundPostUsageInterval = 10 * 60 * 1000;  // ten minutes
        private static TimeSpan cm_oneHourMetered = new TimeSpan(1, 0, 0);  // one hour
#endif
        private const Int32 cm_minIdentifierMatches = 2;

        private const string cm_vendorAppDirName = @"PointKnown";
        private const string cm_licenseDirName = @"license";
        private const string cm_licenseFileName = @"PKNailProLicense.xml";
        private const string cm_licenseDataFileName = @"PKNailProLicenseData.json";

        private const Int32 cm_productID = 206296;
        private const string cm_encryptionKeyID = @"6b632682-b8c4-434f-99d5-5a4689836525";
        private const string cm_serverKey =   @"BgIAAACkAABSU0ExAAQAAAEAAQAhaJJXQgLA2DX9PgEDsLu0NVvZxj3Yz+xzdsZjb3Rz74z7HFECotNi3AmF6+TdiZzMkEvdTh"
                                            + @"gv0FtV/E1eDaAjSIEVwAqRXZeFkS0VWS9sxH4ivIh0F7t9io5X9NFWTS2OgYVOe3PsJLS9+JsPNqOZH754CPAnqH73wHfwHTWMxA==";
        private const string cm_clientKey =   @"BwIAAACkAABSU0EyAAQAAAEAAQDFKYbyVdSacNMAvY5COJwsUhzWvBU2Fk+462iTLT25mDxy+oghPapuuxV5lXQ58qBLc3jojq"
                                            + @"vE26sqewuYqCIDuJQef208GqXN/7SFk8/v0svnA6wrM2a9NnYTqlecVRP/5X7ZtEIFBOrh/EsG/zHzCLfUOQ1e3fpqoPxnYDP0"
                                            + @"uxvTbuJyY+eORbSPb9mfIyj1qjyV6/rdT8JaigjgtGOf0umkqEpp0Rdt5k4GVnE2x9R+DgcBPmvfwDnoNVvdwuSf5NfwPMm8+c"
                                            + @"GSsS4jMcXX6mYtMHt0sXEXxYJq7YghtLNNgKl8r+AFbPFhto9MuPmkJ3N+DlxX8bI2eachcVXSY0Y3f5a6hTlzGiAbSO0ddcOb"
                                            + @"U0lM3Tmu+WsA2CYAuWCYXY/+3EGlMAk9BGMONs7S3jnXBySA9jnzESgGKHHGmkmpFbWbe47e4gDUzRABzWHMXgvB14zyeW2xkY"
                                            + @"KWVU9IZ+FC8KEtuJiDZMLYFwOlM5w4iMSfqctEQ61rYqYD/cGVLWOtFd6zFIBGlqgYWWAORE83uC12RoVcADvTOdOtcMliPr/1"
                                            + @"sHT9b12Rhxhx/pfVtdr0mrgNhfdGnHcGnQqNcX8jFbVUQSF4bSTL2bx0VdzVpDEeBYznHRrRrjgz1DVljsXJ+oI1P5qAxphbyt"
                                            + @"WQFv9b0dnbZ1L/640F/Qxx4GOxKm/6jVw6b3nru9iMMEhKDawWY1bMngC+DkBZVeP8A3m0bTvMPVQWDGHVqGAyPOGGS0GGL0Cl"
                                            + @"hZc6xI3oJJo=";
        private const string cm_activationServiceUrl = @"https://secure.softwarekey.com/solo/webservices/XmlActivationService.asmx";
        private const string cm_licenseFileServiceUrl = @"https://secure.softwarekey.com/solo/webservices/XmlLicenseFileService.asmx";

        // TODO: move this
#if (IS_SELECT_REMOTE_OR_LOCAL_SERVER)
        private const string cm_licenseServiceBaseURL = @"http://localhost:8080/paygoServer/webapi/license/";
#else
        private const string cm_licenseServiceBaseURL = @"https://paygo.pointknown.com/webapi/license/";
#endif
        private const string cm_paygoServiceTokenHeaderName = @"X-Paygo-Service-Token";

        private string m_pkError = @"";

        private License m_skLicense;
        private PKLicenseData m_licenseData;
        private bool m_isActivated;
        private bool m_isValidThisSession;
        private bool m_isSystemDateTimeValid;

        private Thread m_postUsageRecordsThread = null;
        private AutoResetEvent m_postUsageRecordsSignal = new AutoResetEvent(false);

        // ***** IMPORTANT ****** IMPORTANT ****** IMPORTANT ******
        // These must match the trigger codes defined in the product options in SOLO
        // Once a code is assigned, it can never be changed
        //
        // NOTE: In future product versions, if all disabled/legacy licenses are expired
        //       and replaced, the trigger code and associated business logic can be deleted
        //       (but don't renumber the other trigger codes!)
        private enum soloTriggerCode
        {
            Dummy = 0,                  // This is the default for a new, "empty" license
            Perpetual = 1,
            NotForResale_INVALID = 10,  // See NOTE above
            Subscription_LEGACY = 11,
            MeteredDaily = 14,
            MeteredHourly = 15,
            Subscription = 16
        }

        internal PKLicense()
        {
            m_skLicense = new License(new AuthorEncryptionKey(cm_encryptionKeyID, cm_clientKey, cm_serverKey), true, true, cm_productID, @"",
                                  new List<SystemIdentifierAlgorithm>(new SystemIdentifierAlgorithm[] { new NicIdentifierAlgorithm(),
                                                                                                        new ComputerNameIdentifierAlgorithm(),
                                                                                                        new HardDiskVolumeSerialIdentifierAlgorithm() }));
            SystemDateTimeValidation dtValidation = new SystemDateTimeValidation(true, 70.0);
            dtValidation.AddTimeServerCheck("0.pool.ntp.org");
            dtValidation.AddTimeServerCheck("1.pool.ntp.org");
            dtValidation.AddTimeServerCheck("time.windows.com");
            m_isSystemDateTimeValid = dtValidation.Validate();

#if (BYPASS_LICENSE_CHECK)
            m_isActivated = false;
            m_licenseData = new PKLicenseData();
            m_isValidThisSession = true;
#else
            m_isActivated = m_skLicense.LoadFile(licenseFilePath);
            if (m_isActivated)
            {
                bool isIgnored = loadLicenseData();
                double daysSinceLastRefresh = DateTime.UtcNow.Subtract(m_skLicense.SignatureDate).TotalDays;
                if (IsExpired || IsExpirationWarning || daysSinceLastRefresh >= cm_licenseRefreshIntervalDays)
                {
                    bool isRefreshSucceeded = refreshLicense();
                    if (!isRefreshSucceeded)
                    {
                        // TODO: post to the Paygo error reporting web service
                    }
                }
            }
            m_isValidThisSession = isValidNow;
#endif
        }

        internal int LicenseID  { get { return m_skLicense.LicenseID; } }
        internal string InstallationName  { get { return m_skLicense.InstallationName; } }
        internal string CompanyName  { get { return m_skLicense.CompanyName; } }

        internal bool IsActivated  { get { return m_isActivated; } }
        internal bool IsValid  { get { return m_isValidThisSession; } }
        internal bool IsPerpetual  { get { return (m_skLicense.TriggerCode == (int)soloTriggerCode.Perpetual); } }
        internal bool IsSubscription
        {
            get
            {
                return (   m_skLicense.TriggerCode == (int)soloTriggerCode.Subscription
                        || m_skLicense.TriggerCode == (int)soloTriggerCode.Subscription_LEGACY);
            }
        }
        internal bool IsMeteredDaily  { get { return (m_skLicense.TriggerCode == (int)soloTriggerCode.MeteredDaily); } }
        internal bool IsMeteredHourly  { get { return (m_skLicense.TriggerCode == (int)soloTriggerCode.MeteredHourly); } }
        internal bool IsRequireUsageRecording
        {
            get
            {
                return (   m_skLicense.TriggerCode == (int)soloTriggerCode.MeteredDaily
                        || m_skLicense.TriggerCode == (int)soloTriggerCode.MeteredHourly
                        || m_skLicense.TriggerCode == (int)soloTriggerCode.Subscription);
            }
        }

        internal DateTime ExpirationDate  { get { return m_skLicense.EffectiveEndDate.Date; } }
        internal bool IsExpired  { get { return (IsSubscription && DateTime.Now.Date > ExpirationDate); } }
        internal int ExpirationWarningLevel
        {
            get { return ((IsSubscription) ? m_licenseData.ExpirationWarning : 0); }
            set
            {
                m_licenseData.ExpirationWarning = value;
                saveLicenseData();
            }
        }
        internal bool IsExpirationWarning
        { get { return (IsSubscription && !IsExpired && DaysToExpiration <= m_licenseData.ExpirationWarning); } }
        internal int DaysToExpiration  { get { return (int)Math.Round((ExpirationDate - DateTime.Now.Date).TotalDays); } }

        internal bool IsMetered  { get { return (IsMeteredDaily || IsMeteredHourly); } }
        internal bool IsInsufficientBalance 
        {
            get
            {
                if (!IsMetered) { return false; }
                if (m_licenseData.ReserveBalance < 0) { return true; }
                if (m_licenseData.ReserveBalance == 0)
                {
                    if (m_licenseData.UsageRecords.Count == 0) { return true; }
                    UsageRecord record = m_licenseData.UsageRecords[m_licenseData.UsageRecords.Count - 1];
                    DateTime nowLocal = DateTime.Now;
                    if (   (soloTriggerCode)m_skLicense.TriggerCode == soloTriggerCode.MeteredDaily
                        && record.StartDateTimeLocal.Date < nowLocal.Date)
                    { return true; }
                    else if ((soloTriggerCode)m_skLicense.TriggerCode == soloTriggerCode.MeteredHourly
                             && nowLocal.Subtract(record.MeterStartDateTimeLocal) > cm_oneHourMetered)
                    { return true; }
                }
                return false;
            }
        }
        internal bool IsBalanceWarning  { get { return (IsMetered && m_licenseData.ReserveBalance <= UsageUnitsWarningLevel); } }
        internal int UsageUnitsWarningLevel
        {
            get
            {
                if (IsMeteredDaily) { return m_licenseData.UsageUnitsDailyWarning; }
                else if (IsMeteredHourly) { return m_licenseData.UsageUnitsHourlyWarning; }
                else { return 0; }
            }
            set
            {
                if (IsMeteredDaily) { m_licenseData.UsageUnitsDailyWarning = value; }
                else if (IsMeteredHourly) { m_licenseData.UsageUnitsHourlyWarning = value; }
                saveLicenseData();
            }
        }
        internal int DefaultUsageAddAmount
        {
            get { return m_licenseData.DefaultUsageAddAmount; }
            set
            {
                m_licenseData.DefaultUsageAddAmount = value;
                saveLicenseData();
            }
        }
        internal int ReserveBalance  { get { return (m_licenseData.ReserveBalance); } }
        internal string UsageUnitsStr  { get { return ((IsMeteredDaily) ? @"days" : @"hours"); } }

        internal string LastUserID
        {
            get { return m_licenseData.LastUserID; }
            set
            {
                m_licenseData.LastUserID = value;
                saveLicenseData();
            }
        }
        internal bool IsTrial { get { return (m_skLicense.UserDefinedNumber1 != 0); } }
        internal int TrialUsageBalance { get { return m_skLicense.UserDefinedNumber2; } }
        private string encryptionKey { get { return (m_skLicense != null) ? m_skLicense.UserDefinedString1 : @""; } }
        private string serviceToken { get { return (m_skLicense != null) ? m_skLicense.UserDefinedString2 : @""; } }

        internal string LicenseType
        {
            get
            {
                string retval = @"Not Activated";
                if (m_isActivated)
                {
                    switch (m_skLicense.TriggerCode)
                    {
                        case (int)soloTriggerCode.Dummy:
                            retval = @"Dummy";
                            break;
                        case (int)soloTriggerCode.Perpetual:
                            retval = @"Perpetual Node-locked";
                            break;
                        case (int)soloTriggerCode.MeteredDaily:
                            retval = @"Metered Usage - Daily";
                            break;
                        case (int)soloTriggerCode.MeteredHourly:
                            retval = @"Metered Usage - Hourly";
                            break;
                        case (int)soloTriggerCode.Subscription:
                        case (int)soloTriggerCode.Subscription_LEGACY:
                            retval = @"Subscription";
                            break;
                        default:
                            retval = @"Unknown";
                            break;
                    }
                }
                return retval;
            }
        }

        internal string ErrorMessage
        {
            get
            {
                if (m_pkError.Length > 0) { return m_pkError; }
                else if (m_skLicense.LastError.ErrorNumber != LicenseError.ERROR_NONE)
                {
                    string retval = m_skLicense.LastError.ErrorString;
                    if (m_skLicense.LastError.ErrorException != null)
                    { retval += @" (exception: " + m_skLicense.LastError.ErrorException.Message + @")"; }
                    return retval;
                }
                else { return @""; }
            }
        }

        [System.Reflection.Obfuscation(Feature="renaming", ApplyToMembers=true)]
        [JsonObject(MemberSerialization.OptIn)]
        private class systemIdentifierData
        {
            [JsonProperty]
            private string type;
            [JsonProperty]
            private string valueHash;

            internal systemIdentifierData() { }
            internal systemIdentifierData(string t, string vh)
            {
                type = t;
                valueHash = vh;
            }

            internal static string GetAsJson()
            {
                List<systemIdentifierData> identifiers = new List<systemIdentifierData>();

                // Create a temporary writable license so we can extract the system identifiers
                WritableLicense tempLicense
                    = new WritableLicense(new AuthorEncryptionKey(cm_encryptionKeyID, cm_clientKey, cm_serverKey), true, true, cm_productID, @"",
                                          new List<SystemIdentifierAlgorithm>(new SystemIdentifierAlgorithm[] {
                                                                              new ServerHostNameIdentifierAlgorithm(),
                                                                              new DomainNameIdentifierAlgorithm(),
                                                                              new NicIdentifierAlgorithm(),
                                                                              new ComputerNameIdentifierAlgorithm(),
                                                                              new HardDiskVolumeSerialIdentifierAlgorithm()}));

                List<SystemIdentifier> currentSystemIdentifiers = tempLicense.CurrentIdentifiers;
                foreach (SystemIdentifier si in currentSystemIdentifiers)
                {
                    identifiers.Add(new systemIdentifierData(si.Type, si.ValueHash));
                }

                return JsonConvert.SerializeObject(identifiers);
            }
        }

        [System.Reflection.Obfuscation(Feature="renaming", ApplyToMembers=true)]
        [JsonObject(MemberSerialization.OptIn)]
        internal class LicenseActivationData
        {
            [JsonProperty]
            internal string licenseID;
            [JsonProperty]
            internal string expirationDate;
            [JsonProperty]
            internal string lastInstallationName;
            [JsonProperty]
            internal string customerPassword;

            [JsonConstructor]
            internal LicenseActivationData(string lid, string ed, string lin, string cp)
            {
                licenseID = lid;
                expirationDate = ed;
                lastInstallationName = lin;
                customerPassword = cp;
            }
        }

        internal bool GetAvailableLicenses(string userID, string password,
                                         out List<LicenseActivationData> availableLicenses)
        {
            availableLicenses = null;

            string jsonBody = systemIdentifierData.GetAsJson();
            string url = cm_licenseServiceBaseURL;
            jsonClient client = new jsonClient(url);
            client.SetBasicAuth(userID, password);
            bool retval = client.Post(jsonBody);
            if (retval == true)
            {
                if (client.IsStatusOK)
                {
                    availableLicenses = JsonConvert.DeserializeObject<List<LicenseActivationData>>(client.ResponseBody);
                }
                else if (client.StatusCode == HttpStatusCode.Unauthorized)
                {
                    m_pkError = @"Invalid User ID and/or password, try again";
                    retval = false;
                }
                else
                {
                    m_pkError = client.StatusDescription;
                    retval = false;
                }
            }
            else
            { m_pkError = client.ErrorMessage; }

            return retval;
        }

        internal bool Activate(string licenseID, string customerPassword, string instName)
        {
            // Do not attempt to activate a license that is already activated
            if (m_isActivated)
            {
                m_pkError = @"License is already activated";
                return false;
            }

            // Activate the license on this computer and save the license file
            bool retval;
            m_skLicense.InstallationName = instName;
            string licenseContent = @"";
            int licenseIDAsInt = Int32.Parse(licenseID);
            retval = m_skLicense.ActivateInstallationLicenseFile(licenseIDAsInt, customerPassword, ref licenseContent);
            if (retval)
            { retval = saveLicense(licenseContent); }

            if (retval && m_skLicense.Load(licenseContent))
            {
                m_isActivated = true;

                // Clear out the license data in case a license data file was left over from a previous activation
                m_licenseData = new PKLicenseData();
                saveLicenseData();

                m_isValidThisSession = isValidNow;
            }
            else
            { retval = false; }

            return retval;
        }

        internal bool Deactivate()
        {
            if (!m_isActivated)
            {
                m_pkError = @"Can't deactivate; no activated license";
                return false;
            }

            if (IsMetered)  { return false; }

            // TODO: post any errors to error reporting service
            bool isIgnored = postUsageRecords(30 * 1000, true);

            if (!m_skLicense.DeactivateInstallation()) { return false; }

            try
            {
                File.Delete(licenseFilePath);
                if (File.Exists(licenseDataFilePath))
                { File.Delete(licenseDataFilePath); }
            }
            catch (Exception e)
            {
                m_pkError = @"Could not delete license file or license data file: " + e.Message;
                return false;
            }
            m_licenseData = new PKLicenseData();
            m_isActivated = false;
            m_isValidThisSession = false;

            return true;
        }

        internal bool ReserveUsage(string userID, string password, int usageToReserve)
        {
            if (!m_isActivated)
            {
                m_pkError = @"No license is activated on this computer";
                return false;
            }
            if (!IsMetered)
            {
                m_pkError = @"Can't add usage to a non-metered license";
                return false;
            }

            if (!postUsageRecords(30 * 1000, true)) { return false; }

            string url = cm_licenseServiceBaseURL + m_skLicense.LicenseID.ToString() + @"/reservedUsage";
            string jsonBody
                = @"{ ""currentReserve"":""" + ReserveBalance
                + @""", ""reserveRequested"":""" + usageToReserve.ToString()
                + @""", ""postDateTimeLocal"":""" + toISOString(DateTime.Now)
                + @""", ""installationName"":""" + m_skLicense.InstallationName + @""" }";
            jsonClient client = new jsonClient(url);
            client.SetBasicAuth(userID, password);
            client.SetRequestHeader(cm_paygoServiceTokenHeaderName, serviceToken);
            if (client.Post(jsonBody))
            {
                if (client.IsStatusOK)
                {
                    m_licenseData.ReserveBalance += usageToReserve;
                    m_isValidThisSession = isValidNow;
                    // TODO: don't ignore, figure out how to deal
                    bool isIgnored = saveLicenseData();
                    return true;
                }
                else if (client.StatusCode == HttpStatusCode.Unauthorized)
                {
                    m_pkError = @"Invalid User ID and/or password, try again";
                    return false;
                }
                else
                {
                    m_pkError = client.StatusDescription;
                    if (client.ResponseContentType.ToLower() == @"text/plain")
                    { m_pkError += @": " + client.ResponseBody; }
                    return false;
                }
            }
            else
            {
                m_pkError = client.ErrorMessage;
                return false;
            }
        }

        internal bool RecordUsage(string projectName, string userName)
        {
            bool retval;
            lock (this)
            { retval = recordUsageWorker(projectName, userName); }
            return retval;
        }

        internal void StartBackgroundPostUsageRecordsThread()
        {
            m_postUsageRecordsThread = new Thread(new ThreadStart(postUsageRecordsThreadWorker));
            m_postUsageRecordsThread.Name = @"postUsageRecords";
            m_postUsageRecordsThread.IsBackground = true;
            m_postUsageRecordsThread.Start();
        }

        internal void StopBackgroundPostUsageRecordsThread()
        {
            if (m_postUsageRecordsThread != null)
            {
                m_postUsageRecordsSignal.Set();
                m_postUsageRecordsThread.Join();
            }
        }

        private bool recordUsageWorker(string projectName, string userName)
        {
            if (!IsRequireUsageRecording) { return true; }
            if (IsInsufficientBalance)  { return false; }

            DateTime nowLocal = DateTime.Now;  // NOTE: in local time
            soloTriggerCode triggerCode = (soloTriggerCode)m_skLicense.TriggerCode;
            UsageRecord record = null;
            List<UsageRecord> records = m_licenseData.UsageRecords;
            if (records.Count == 0 || records[records.Count - 1].StartDateTimeLocal.Date < nowLocal.Date)
            {
                // Starting a new day with a new usage record
                record = m_licenseData.AddUsageRecord(nowLocal, m_skLicense.InstallationName, projectName, userName, nowLocal);
                if (triggerCode == soloTriggerCode.MeteredHourly)
                { m_licenseData.ConsumeUsageUnit(nowLocal); }
            }
            else if (   records[records.Count - 1].ProjectName != projectName
                     || records[records.Count - 1].UserID != userName)
            {
                // The project  or user name has changed since the last usage
                if (triggerCode == soloTriggerCode.MeteredDaily)
                {
                    // With daily metering, we can't have separate records for each project;
                    // the one daily record will contain the last project/user worked on on that day
                    record = records[records.Count - 1];
                    record.ProjectName = projectName;
                    record.UserID = userName;
                }
                else
                {
                    // Add a new record for the new project; for metering purposes, use
                    // the meter start time from the previous record to continue metering without disruption
                    record = m_licenseData.AddUsageRecord(nowLocal, m_skLicense.InstallationName, projectName, userName,
                                                          records[records.Count - 1].MeterStartDateTimeLocal);
                }
            }
            else
            {
                // Continue with the latest record
                record = records[records.Count - 1];
            }

            // Determine whether we have consumed another unit of usage
            switch (triggerCode)
            {
                case soloTriggerCode.MeteredDaily:
                    if (record.Usage == 0 && nowLocal.Subtract(record.StartDateTimeLocal).TotalMinutes > cm_dailyUsageGracePeriodMinutes)
                    { m_licenseData.ConsumeUsageUnit(nowLocal); }
                    break;

                case soloTriggerCode.MeteredHourly:
                    if (nowLocal.Subtract(record.MeterStartDateTimeLocal) > cm_oneHourMetered)
                    { m_licenseData.ConsumeUsageUnit(nowLocal); }
                    break;

                default:
                    break;
            }

            record.CaptureCount++;
            record.EndDateTimeLocal = nowLocal;
            if (!saveLicenseData())
            {
                // TODO: handle this by making a call to the (yet-to-be-created) error
                // reporting service
            }

            return true;
        }

        private void postUsageRecordsThreadWorker()
        {
            bool isIgnored;
            do
            {
                isIgnored = postUsageRecords(5 * 1000, false);
            }
            while (!m_postUsageRecordsSignal.WaitOne(cm_backgroundPostUsageInterval));
            // Force a post to push all updates up to the server
            isIgnored = postUsageRecords(5 * 1000, true);
        }

        private bool postUsageRecords(int webServiceTimeout, Boolean isForce)
        {
            if (!m_isActivated)
            {
                m_pkError = @"No license is activated on this computer";
                return false;
            }
            if (!IsRequireUsageRecording) { return true; }

            bool retval = true;
            lock (this)
            {
                List<UsageRecord> records = m_licenseData.UsageRecords;
                if (   isForce
                    || (records.Count > 1)
                    || (records.Count == 1 && records[0].MeterStartDateTimeLocal > records[0].postDateTimeLocal))
                {
                    DateTime nowLocal = DateTime.Now;
                    DateTime nowUTC = DateTime.UtcNow;
                    foreach (UsageRecord record in records)
                    {
                        record.postDateTimeLocal = nowLocal;
                        record.postDateTimeUtc = nowUTC;
                    }
                    string jsonBody = m_licenseData.UsageData.ToJson();
                    string url = cm_licenseServiceBaseURL + m_skLicense.LicenseID.ToString() + @"/consumedUsage";
                    jsonClient client = new jsonClient(url, webServiceTimeout);
                    client.SetRequestHeader(cm_paygoServiceTokenHeaderName, serviceToken);

                    if (client.Post(jsonBody))
                    {
                        if (client.IsStatusOK)
                        {
                            if (records.Count > 1)
                            {
                                // Clear all but the most recent usage record (which may still be active)
                                UsageRecord lastRecordToPreserve = m_licenseData.UsageRecords[records.Count - 1];
                                records.Clear();
                                records.Add(lastRecordToPreserve);
                            }

                            if (!saveLicenseData())
                            {
                                // TODO: send error report
                                retval = false;
                            }
                        }
                        else
                        {
                            // TODO: send error report
                            m_pkError = @"License server error: " + client.StatusDescription;
                            retval = false;
                        }
                    }
                    else
                    {
                        m_pkError = client.ErrorMessage;
                        retval = false;
                    }
                }
            }

            return retval;
        }

        private bool refreshLicense()
        {
            if (!m_isActivated)  { return false; }

            string licenseContent = @"";
            if (!m_skLicense.RefreshLicense(ref licenseContent))
            { return false; }

            if (m_skLicense.Load(licenseContent) && saveLicense(licenseContent))
            { return true; }
            else
            { return false; }
        }

        private bool saveLicense(string lfContent)
        {
            try
            { File.WriteAllText(licenseFilePath, lfContent); }
            catch (Exception e)
            {
                m_pkError = @"Could not save license file: " + e.Message;
                return false;
            }
            return true;
        }

        private bool saveLicenseData()
        {
            string licenseDataJson = m_licenseData.ToJson();
            string licenseDataJsonEnc = encryptString(licenseDataJson);
            try
            { File.WriteAllText(licenseDataFilePath, licenseDataJsonEnc); }
            catch (Exception e)
            {
                m_pkError = @"Could not save license data file: " + e.Message;
                return false;
            }
            return true;
        }

        private bool loadLicenseData()
        {
            if (File.Exists(licenseDataFilePath))
            {
                try
                {
                    string licenseDataJsonEnc = File.ReadAllText(licenseDataFilePath);
                    string licenseDataJson = decryptString(licenseDataJsonEnc);
                    m_licenseData = PKLicenseData.FromJson(licenseDataJson);
                    return true;
                }
                catch (Exception e)
                {
                    m_pkError = @"Could not load license from file: " + e.Message;
                    return false;
                }
            }
            else
            {
                m_licenseData = new PKLicenseData();
                return true;
            }
        }

        private string encryptString(string strToEncrypt)
        {
            return (encryptionKey.Length > 0) ? StringEncryption.SimpleEncryptWithPassword(strToEncrypt, encryptionKey) : strToEncrypt;
        }

        private string decryptString(string strToDecrypt)
        {
            return (encryptionKey.Length > 0) ? StringEncryption.SimpleDecryptWithPassword(strToDecrypt, encryptionKey) : strToDecrypt; 
        }

        private bool isValidNow
        {
            get
            {
                if (!m_isActivated)  { return false; }

                if (m_skLicense.TriggerCode == (int)soloTriggerCode.NotForResale_INVALID)
                {
                    m_pkError = @"Not For Resale licenses are no longer valid";
                    return false;
                }
                if (IsSubscription && !m_isSystemDateTimeValid)
                {
                    m_pkError = @"License is invalid because the system time is incorrect";
                    return false;
                }
                if (m_skLicense.NumberOfMatchingIdentifiers < cm_minIdentifierMatches)
                {
                    m_pkError = @"License is invalid because it has been moved to another computer";
                    return false;
                }
                if (IsSubscription && IsExpired)
                {
                    m_pkError = @"License is expired";
                    return false;
                }
                if (IsMetered && IsInsufficientBalance)
                {
                    m_pkError = @"License usage balance is insufficient";
                    return false;
                }

                return true;
            }
        }

        private static string vendorAppDataDirPath
        {
            get
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                return Path.Combine(appDataPath, cm_vendorAppDirName);
            }
        }

        private static string licenseDirPath
        { get { return Path.Combine(vendorAppDataDirPath, cm_licenseDirName); } }

        private static string licenseFilePath
        { get  { return Path.Combine(licenseDirPath, cm_licenseFileName); } }

        private static string licenseDataFilePath
        { get { return Path.Combine(licenseDirPath, cm_licenseDataFileName); } }

        private static string toISOString(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Utc)
            { return dt.ToString(@"yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"); }
            else
            { return dt.ToString(@"yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffzzz"); }
        }

        private class jsonClient
        {
            private HttpWebRequest m_request;
            private HttpStatusCode m_responseStatusCode;
            private string m_responseStatusDesc;
            private string m_responseContentType;
            private string m_responseBody;
            private string m_errorMsg;

            internal jsonClient(string url, int timeout)
            {
                construct(url, timeout);
            }

            internal jsonClient(string url)
            {
#if (DEBUG)
                int timeout = 60 * 60 * 1000;  // Allows for debugging
#else
                int timeout = 30 * 1000;
#endif
                construct(url, timeout);
            }

            internal void SetBasicAuth(string username, string password)
            {
                byte[] encodedAuth = Encoding.UTF8.GetBytes(username + @":" + password);
                string authHeaderValue = @"Basic " + Convert.ToBase64String(encodedAuth);
                m_request.Headers.Add(HttpRequestHeader.Authorization, authHeaderValue);
            }

            internal void SetRequestHeader(string name, string value)
            {
                m_request.Headers.Add(name, value);
            }

            internal string ResponseBody  { get { return m_responseBody; } }
            internal HttpStatusCode StatusCode  { get { return m_responseStatusCode; } }
            internal string StatusDescription  { get { return m_responseStatusDesc; } }
            internal string ResponseContentType  { get { return m_responseContentType; } }
            internal bool IsStatusOK
            {
                get
                {
                    return (   m_responseStatusCode == HttpStatusCode.OK
                            || m_responseStatusCode == HttpStatusCode.Created);
                }
            }
            internal string ErrorMessage  { get { return m_errorMsg; } }

            internal bool Get()
            { return send(@"GET", @""); }

            internal bool Get(string jsonBody)
            { return send(@"GET", jsonBody); }

            internal bool Post(string jsonBody)
            { return send(@"POST", jsonBody); }

            private bool send(string method, string jsonBody)
            {
                bool retval = true;
                m_errorMsg = @"";

                try
                {
                    m_request.Method = method;
                    if (jsonBody != null && jsonBody.Length > 0)
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(jsonBody);
                        Stream inStrm = m_request.GetRequestStream();
                        inStrm.Write(bytes, 0, bytes.Length);
                        inStrm.Close();
                    }

                    handleResponse((HttpWebResponse)m_request.GetResponse());
                }
                catch (WebException we)
                {
                    switch (we.Status)
                    {
                        case WebExceptionStatus.ProtocolError:
                            handleResponse((HttpWebResponse)we.Response);
                            break;
                        case WebExceptionStatus.Timeout:
                            m_errorMsg = @"Service unavailable; please try again later or contact support";
                            retval = false;
                            break;
                        case WebExceptionStatus.NameResolutionFailure:
                        case WebExceptionStatus.ProxyNameResolutionFailure:
                        case WebExceptionStatus.ConnectFailure:
                            m_errorMsg = @"No internet connection; try again after connecting to the Internet";
                            retval = false;
                            break;
                        default:
                            m_errorMsg = @"Web service error: " + we.Message;
                            retval = false;
                            break;
                    }
                }
                catch (Exception e)
                {
                    m_errorMsg = @"Error: " + e.Message;
                    retval = false;
                }

                return retval;
            }

            private void handleResponse(HttpWebResponse response)
            {
                m_responseStatusCode = response.StatusCode;
                m_responseStatusDesc = response.StatusDescription;
                m_responseContentType = response.Headers[HttpResponseHeader.ContentType];
                Stream outStrm = response.GetResponseStream();
                StreamReader rdr = new StreamReader(outStrm);
                m_responseBody = rdr.ReadToEnd();
                rdr.Close();
                outStrm.Close();
                response.Close();
            }

            private void construct(string url, int timeout)
            {
                m_request = (HttpWebRequest)WebRequest.Create(url);
                m_request.ContentType = @"application/json";
                m_request.Timeout = timeout;
            }
        }
    }
}
