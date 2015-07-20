﻿using CsQuery;
using Jackett.Models;
using Jackett.Services;
using Jackett.Utils;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Jackett.Indexers
{
    public class IPTorrents : BaseIndexer, IIndexer
    {
        private readonly string SearchUrl = "";
        CookieContainer cookies;
        HttpClientHandler handler;
        HttpClient client;

        public IPTorrents(IIndexerManagerService i, Logger l)
            : base(name: "IPTorrents",
                description: "Always a step ahead.",
                link: new Uri("https://iptorrents.com"),
                caps: TorznabCapsUtil.CreateDefaultTorznabTVCaps(),
                manager: i,
                logger: l)
        {
            SearchUrl = SiteLink + "t?q=";
            cookies = new CookieContainer();
            handler = new HttpClientHandler
            {
                CookieContainer = cookies,
                AllowAutoRedirect = true,
                UseCookies = true,
            };
            client = new HttpClient(handler);
        }

        public async Task<ConfigurationData> GetConfigurationForSetup()
        {
            await client.GetAsync(SiteLink);
            var config = new ConfigurationDataBasicLogin();
            return (ConfigurationData)config;
        }

        public async Task ApplyConfiguration(JToken configJson)
        {

            var config = new ConfigurationDataBasicLogin();
            config.LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string> {
				{ "username", config.Username.Value },
				{ "password", config.Password.Value }
			};

            var content = new FormUrlEncodedContent(pairs);
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Post;
            message.Content = content;
            message.RequestUri = SiteLink;
            message.Headers.Referrer = SiteLink;
            message.Headers.UserAgent.ParseAdd(BrowserUtil.ChromeUserAgent);

            var response = await client.SendAsync(message);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!responseContent.Contains("/my.php"))
            {
                CQ dom = responseContent;
                var messageEl = dom["body > div"].First();
                var errorMessage = messageEl.Text().Trim();
                throw new ExceptionWithConfigData(errorMessage, (ConfigurationData)config);
            }
            else
            {
                var configSaveData = new JObject();
                cookies.DumpToJson(SiteLink, configSaveData);
                SaveConfig(configSaveData);
                IsConfigured = true;
            }
        }

        HttpRequestMessage CreateHttpRequest(Uri uri)
        {
            var message = new HttpRequestMessage();
            message.Method = HttpMethod.Get;
            message.RequestUri = uri;
            message.Headers.UserAgent.ParseAdd(BrowserUtil.ChromeUserAgent);
            return message;
        }

        public void LoadFromSavedConfiguration(Newtonsoft.Json.Linq.JToken jsonConfig)
        {
            cookies.FillFromJson(SiteLink, jsonConfig, logger);
            IsConfigured = true;
        }

        public async Task<ReleaseInfo[]> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.SanitizedSearchTerm + " " + query.GetEpisodeSearchString();
            var episodeSearchUrl = SearchUrl + HttpUtility.UrlEncode(searchString);

            var request = CreateHttpRequest(new Uri(episodeSearchUrl));
            var response = await client.SendAsync(request);
            var results = await response.Content.ReadAsStringAsync();

            try
            {
                CQ dom = results;

                var rows = dom["table.torrents > tbody > tr"];
                foreach (var row in rows.Skip(1))
                {
                    var release = new ReleaseInfo();

                    var qRow = row.Cq();

                    var qTitleLink = qRow.Find("a.t_title").First();
                    release.Title = qTitleLink.Text().Trim();
                    release.Description = release.Title;
                    release.Guid = new Uri(SiteLink + qTitleLink.Attr("href"));
                    release.Comments = release.Guid;

                    DateTime pubDate;
                    var descString = qRow.Find(".t_ctime").Text();
                    var dateString = descString.Split('|').Last().Trim();
                    dateString = dateString.Split(new string[] { " by " }, StringSplitOptions.None)[0];
                    var dateValue = ParseUtil.CoerceFloat(dateString.Split(' ')[0]);
                    var dateUnit = dateString.Split(' ')[1];
                    if (dateUnit.Contains("minute"))
                        pubDate = DateTime.Now - TimeSpan.FromMinutes(dateValue);
                    else if (dateUnit.Contains("hour"))
                        pubDate = DateTime.Now - TimeSpan.FromHours(dateValue);
                    else if (dateUnit.Contains("day"))
                        pubDate = DateTime.Now - TimeSpan.FromDays(dateValue);
                    else if (dateUnit.Contains("week"))
                        pubDate = DateTime.Now - TimeSpan.FromDays(7 * dateValue);
                    else if (dateUnit.Contains("month"))
                        pubDate = DateTime.Now - TimeSpan.FromDays(30 * dateValue);
                    else if (dateUnit.Contains("year"))
                        pubDate = DateTime.Now - TimeSpan.FromDays(365 * dateValue);
                    else
                        pubDate = DateTime.MinValue;
                    release.PublishDate = pubDate;

                    var qLink = row.ChildElements.ElementAt(3).Cq().Children("a");
                    release.Link = new Uri(SiteLink + qLink.Attr("href"));

                    var sizeStr = row.ChildElements.ElementAt(5).Cq().Text().Trim();
                    var sizeVal = ParseUtil.CoerceFloat(sizeStr.Split(' ')[0]);
                    var sizeUnit = sizeStr.Split(' ')[1];
                    release.Size = ReleaseInfo.GetBytes(sizeUnit, sizeVal);

                    release.Seeders = ParseUtil.CoerceInt(qRow.Find(".t_seeders").Text().Trim());
                    release.Peers = ParseUtil.CoerceInt(qRow.Find(".t_leechers").Text().Trim()) + release.Seeders;

                    releases.Add(release);
                }
            }
            catch (Exception ex)
            {
                OnParseError(results, ex);
            }

            return releases.ToArray();
        }

        public async Task<byte[]> Download(Uri link)
        {
            var request = CreateHttpRequest(link);
            var response = await client.SendAsync(request);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            return bytes;
        }
    }
}
