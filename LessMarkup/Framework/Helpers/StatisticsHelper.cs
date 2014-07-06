/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using LessMarkup.DataFramework;
using LessMarkup.DataObjects.Statistics;
using LessMarkup.Engine.Logging;
using LessMarkup.Engine.Response;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.Helpers
{
    public class StatisticsHelper
    {
        class HostEntry
        {
            public long LastCheck { get; set; }
        }

        private readonly IDomainModelProvider _domainModelProvider;
        private const int StoreDetailedHistoryDays = 7;
        private const int ActivateMoveThreshold = 500;
        private const int ArchiveCheckInterval = 1000*60*60*30;
        private readonly static object _archiveSync = new object();
        private readonly static Dictionary<long, HostEntry> _lastArchiveCheck = new Dictionary<long, HostEntry>();
        private readonly ISiteMapper _siteMapper;

        public StatisticsHelper(IDomainModelProvider domainModelProvider, ISiteMapper siteMapper)
        {
            _domainModelProvider = domainModelProvider;
            _siteMapper = siteMapper;
        }

        public static void FlagError(string message)
        {
            HttpContext.Current.Items[Constants.RequestItemKeys.ErrorFlag] = message;
        }

        private void ArchiveOldItems()
        {
            var siteId = _siteMapper.SiteId;
            if (!siteId.HasValue)
            {
                return;
            }

            HostEntry hostEntry;

            lock (_archiveSync)
            {
                if (!_lastArchiveCheck.TryGetValue(siteId.Value, out hostEntry))
                {
                    hostEntry = new HostEntry { LastCheck = 0 };
                    _lastArchiveCheck[siteId.Value] = hostEntry;
                }
            }

            if (hostEntry.LastCheck != 0 && Environment.TickCount - hostEntry.LastCheck <= ArchiveCheckInterval)
            {
                return;
            }

            lock (hostEntry)
            {
                if (hostEntry.LastCheck != 0 && Environment.TickCount - hostEntry.LastCheck <= ArchiveCheckInterval)
                {
                    return;
                }

                hostEntry.LastCheck = Environment.TickCount;

                var dayFrom = DateTime.UtcNow.AddDays(-StoreDetailedHistoryDays).Date.Ticks;

                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var collection = domainModel.GetSiteCollection<AddressHistory>().Where(h => h.Date < dayFrom);

                    if (collection.Count() < ActivateMoveThreshold)
                    {
                        return;
                    }

                    foreach (var day in collection.GroupBy(h => h.Date).Select(h => new
                    {
                        Day = h.Key, 
                        Requests = h.Sum(v => v.Requests),
                        Received = h.Sum(v => v.Received),
                        Sent = h.Sum(v => v.Sent),
                        Errors = h.Sum(v => v.HasError),
                        MobileRequests = h.Sum(v => v.MobileRequests),
                        Entries = h.Select(v => v.AddressHistoryId)
                    }))
                    {
                        var record = domainModel.GetSiteCollection<DaySummaryHistory>().SingleOrDefault(h => h.Day == day.Day);
                        if (record == null)
                        {
                            record = new DaySummaryHistory {Day = day.Day};
                            domainModel.GetSiteCollection<DaySummaryHistory>().Add(record);
                        }
                        record.Errors += day.Errors;
                        record.Requests += day.Requests;
                        record.Received += day.Received;
                        record.Sent += day.Sent;
                        record.MobileRequests += day.MobileRequests;

                        foreach (var recordId in day.Entries)
                        {
                            var history = domainModel.GetSiteCollection<AddressHistory>().SingleOrDefault(h => h.AddressHistoryId == recordId);
                            if (history != null)
                            {
                                domainModel.GetSiteCollection<AddressHistory>().Remove(history);
                            }
                        }
                    }

                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();
                }
            }
        }

        public void LogRequest()
        {
            try
            {
                var httpContext = HttpContext.Current;
                var request = httpContext.Request;
                var browser = request.Browser;
                var url = request.Url.ToString();
                if (url.Length > 200)
                {
                    url = url.Substring(0, 200);
                }
                if (string.IsNullOrEmpty(url))
                {
                    return;
                }
                var hasErrors = httpContext.Items.Contains(Constants.RequestItemKeys.ErrorFlag);

                var requestPath = request.Path;

                var isResourceRequest = requestPath.StartsWith("/Content/") || requestPath.StartsWith("/Images/") || requestPath.StartsWith("/Scripts/") || requestPath.StartsWith("/bundles/");

                using (var domainModel = _domainModelProvider.Create())
                {
                    var day = DateTime.UtcNow.Date.Ticks;
                    var addressString = request.UserHostAddress;
                    byte[] addressBytes = null;
                    IPAddress binaryAddress;
                    if (!string.IsNullOrEmpty(addressString) && IPAddress.TryParse(addressString, out binaryAddress))
                    {
                        addressBytes = binaryAddress.GetAddressBytes();
                    }

                    var address = (addressBytes == null || addressBytes.Length != 4) ? 0 : (long)BitConverter.ToUInt32(addressBytes.Reverse().ToArray(), 0);
                    var country = domainModel.GetCollection<AddressToCountry>().FirstOrDefault(c => c.From <= address && c.To >= address);

                    var responseFilter = (ResponseFilterStream) httpContext.Items[Constants.RequestItemKeys.ResponseFilter];

                    var writtenBytes = responseFilter != null ? responseFilter.BytesWritten : 0;

                    var history = new AddressHistory
                    {
                        CountryId = country != null ? country.CountryId : (long?) null,
                        Date = day,
                        HasError = hasErrors ? 1 : 0,
                        Ip = address,
                        Received = request.TotalBytes,
                        Sent = writtenBytes,
                        Requests = 1,
                        Query = url,
                        Error = hasErrors ? ((string) httpContext.Items[Constants.RequestItemKeys.ErrorFlag]) : null,
                        Created = DateTime.UtcNow.Ticks,
                        MobileRequests = browser.IsMobileDevice ? 1 : 0,
                        Crawler = browser.Crawler ? 1 : 0,
                        Resource = isResourceRequest ? 1 : 0,
                        UserAgent = request.UserAgent
                    };

                    domainModel.GetSiteCollection<AddressHistory>().Add(history);
                    domainModel.SaveChanges();
                }

                ArchiveOldItems();
            }
            catch (Exception e)
            {
                this.LogException(e);
            }
        }
    }
}
