using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Jackett.Common.Models;
using Jackett.Common.Models.IndexerConfig;
using Jackett.Common.Models.IndexerConfig.Bespoke;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Indexers
{
    [ExcludeFromCodeCoverage]
    public class PornoLab : IndexerBase
    {
        public override string Id => "pornolab";
        public override string Name => "PornoLab";
        public override string Description => "PornoLab is a Semi-Private Russian site for Adult content";
        public override string SiteLink { get; protected set; } = "https://pornolab.net/";
        public override Encoding Encoding => Encoding.GetEncoding("windows-1251");
        public override string Language => "ru-RU";
        public override string Type => "semi-private";

        public override TorznabCapabilities TorznabCaps => SetCapabilities();

        private string LoginUrl => SiteLink + "forum/login.php";
        private string SearchUrl => SiteLink + "forum/tracker.php";

        protected string cap_sid = null;
        protected string cap_code_field = null;
        private static readonly Regex s_StripRussianRegex = new Regex(@"(\([\p{IsCyrillic}\W]+\))|(^[\p{IsCyrillic}\W\d]+\/ )|([\p{IsCyrillic} \-]+,+)|([\p{IsCyrillic}]+)");

        private new ConfigurationDataPornolab configData
        {
            get => (ConfigurationDataPornolab)base.configData;
            set => base.configData = value;
        }

        public PornoLab(IIndexerConfigurationService configService, WebClient wc, Logger l, IProtectionService ps, ICacheService cs)
            : base(configService: configService,
                   client: wc,
                   logger: l,
                   p: ps,
                   cacheService: cs,
                   configData: new ConfigurationDataPornolab())
        {
        }

        private TorznabCapabilities SetCapabilities()
        {
            var caps = new TorznabCapabilities();

            caps.Categories.AddCategoryMapping(1670, TorznabCatType.XXX, "Эротическое видео / Erotic & Softcore");
            caps.Categories.AddCategoryMapping(1768, TorznabCatType.XXX, "Эротические фильмы / Erotic Movies");
            caps.Categories.AddCategoryMapping(60, TorznabCatType.XXX, "Документальные фильмы / Documentary & Reality");
            caps.Categories.AddCategoryMapping(1671, TorznabCatType.XXX, "Эротические ролики и сайтрипы / Erotic Clips & SiteRips");
            caps.Categories.AddCategoryMapping(1644, TorznabCatType.XXX, "Нудизм-Натуризм / Nudity");

            caps.Categories.AddCategoryMapping(1672, TorznabCatType.XXX, "Зарубежные порнофильмы / Full Length Movies");
            caps.Categories.AddCategoryMapping(1111, TorznabCatType.XXXPack, "Паки полных фильмов / Full Length Movies Packs");
            caps.Categories.AddCategoryMapping(508, TorznabCatType.XXX, "Классические фильмы / Classic");
            caps.Categories.AddCategoryMapping(555, TorznabCatType.XXX, "Фильмы с сюжетом / Feature & Vignettes");
            caps.Categories.AddCategoryMapping(1845, TorznabCatType.XXX, "Гонзо-фильмы 1991-2010 / Gonzo 1991-2010");
            caps.Categories.AddCategoryMapping(1673, TorznabCatType.XXX, "Гонзо-фильмы 2011-2023 / Gonzo 2011-2023");
            caps.Categories.AddCategoryMapping(1112, TorznabCatType.XXX, "Фильмы без сюжета 1991-2010 / All Sex & Amateur 1991-2010");
            caps.Categories.AddCategoryMapping(1718, TorznabCatType.XXX, "Фильмы без сюжета 2011-2023 / All Sex & Amateur 2011-2023");
            caps.Categories.AddCategoryMapping(553, TorznabCatType.XXX, "Лесбо-фильмы / All Girl & Solo");
            caps.Categories.AddCategoryMapping(1143, TorznabCatType.XXX, "Этнические фильмы / Ethnic-Themed");
            caps.Categories.AddCategoryMapping(1646, TorznabCatType.XXX, "Видео для телефонов и КПК / Pocket РС & Phone Video");

            caps.Categories.AddCategoryMapping(1717, TorznabCatType.XXX, "Зарубежные фильмы в высоком качестве (DVD&HD) / Full Length Movies High-Quality");
            caps.Categories.AddCategoryMapping(1851, TorznabCatType.XXXDVD, "Эротические и Документальные видео (DVD) / Erotic, Documentary & Reality (DVD)");
            caps.Categories.AddCategoryMapping(1713, TorznabCatType.XXXDVD, "Фильмы с сюжетом, Классические (DVD) / Feature & Vignetts, Classic (DVD)");
            caps.Categories.AddCategoryMapping(512, TorznabCatType.XXXDVD, "Гонзо, Лесбо и Фильмы без сюжета (DVD) / Gonzo, All Girl & Solo, All Sex (DVD)");
            caps.Categories.AddCategoryMapping(1712, TorznabCatType.XXX, "Эротические и Документальные видео (HD Video) / Erotic, Documentary & Reality (HD Video)");
            caps.Categories.AddCategoryMapping(1775, TorznabCatType.XXX, "Фильмы с сюжетом, Классические (HD Video) / Feature & Vignettes, Classic (HD Video)");
            caps.Categories.AddCategoryMapping(1450, TorznabCatType.XXX, "Гонзо, Лесбо и Фильмы без сюжета (HD Video) / Gonzo, All Girl & Solo, All Sex (HD Video)");

            caps.Categories.AddCategoryMapping(1674, TorznabCatType.XXX, "Русское порно / Russian Video");
            caps.Categories.AddCategoryMapping(902, TorznabCatType.XXX, "Русские порнофильмы / Russian Full Length Movies");
            caps.Categories.AddCategoryMapping(1675, TorznabCatType.XXXPack, "Паки русских порнороликов / Russian Clips Packs");
            caps.Categories.AddCategoryMapping(36, TorznabCatType.XXX, "Сайтрипы с русскими актрисами 1991-2015 / Russian SiteRip's 1991-2015");
            caps.Categories.AddCategoryMapping(1830, TorznabCatType.XXX, "Сайтрипы с русскими актрисами 1991-2015 (HD Video) / Russian SiteRip's 1991-2015 (HD Video)");
            caps.Categories.AddCategoryMapping(1803, TorznabCatType.XXX, "Сайтрипы с русскими актрисами 2016-2023 / Russian SiteRip's 2016-2023");
            caps.Categories.AddCategoryMapping(1831, TorznabCatType.XXX, "Сайтрипы с русскими актрисами 2016-2023 (HD Video) / Russian SiteRip's 2016-2023 (HD Video)");
            caps.Categories.AddCategoryMapping(1741, TorznabCatType.XXX, "Русские Порноролики Разное / Russian Clips (various)");
            caps.Categories.AddCategoryMapping(1676, TorznabCatType.XXX, "Русское любительское видео / Russian Amateur Video");

            caps.Categories.AddCategoryMapping(1677, TorznabCatType.XXX, "Зарубежные порноролики / Clips");
            caps.Categories.AddCategoryMapping(1780, TorznabCatType.XXXPack, "Паки сайтрипов (HD Video) / SiteRip's Packs (HD Video)");
            caps.Categories.AddCategoryMapping(1110, TorznabCatType.XXXPack, "Паки сайтрипов (SD Video) / SiteRip's Packs (SD Video)");
            caps.Categories.AddCategoryMapping(1678, TorznabCatType.XXXPack, "Паки порнороликов по актрисам / Actresses Clips Packs");
            caps.Categories.AddCategoryMapping(1124, TorznabCatType.XXX, "Сайтрипы 1991-2010 (HD Video) / SiteRip's 1991-2010 (HD Video)");
            caps.Categories.AddCategoryMapping(1784, TorznabCatType.XXX, "Сайтрипы 2011-2012 (HD Video) / SiteRip's 2011-2012 (HD Video)");
            caps.Categories.AddCategoryMapping(1769, TorznabCatType.XXX, "Сайтрипы 2013 (HD Video) / SiteRip's 2013 (HD Video)");
            caps.Categories.AddCategoryMapping(1793, TorznabCatType.XXX, "Сайтрипы 2014 (HD Video) / SiteRip's 2014 (HD Video)");
            caps.Categories.AddCategoryMapping(1797, TorznabCatType.XXX, "Сайтрипы 2015 (HD Video) / SiteRip's 2015 (HD Video)");
            caps.Categories.AddCategoryMapping(1804, TorznabCatType.XXX, "Сайтрипы 2016 (HD Video) / SiteRip's 2016 (HD Video)");
            caps.Categories.AddCategoryMapping(1819, TorznabCatType.XXX, "Сайтрипы 2017 (HD Video) / SiteRip's 2017 (HD Video)");
            caps.Categories.AddCategoryMapping(1825, TorznabCatType.XXX, "Сайтрипы 2018 (HD Video) / SiteRip's 2018 (HD Video)");
            caps.Categories.AddCategoryMapping(1836, TorznabCatType.XXX, "Сайтрипы 2019 (HD Video) / SiteRip's 2019 (HD Video)");
            caps.Categories.AddCategoryMapping(1842, TorznabCatType.XXX, "Сайтрипы 2020 (HD Video) / SiteRip's 2020 (HD Video)");
            caps.Categories.AddCategoryMapping(1846, TorznabCatType.XXX, "Сайтрипы 2021 (HD Video) / SiteRip's 2021 (HD Video)");
            caps.Categories.AddCategoryMapping(1857, TorznabCatType.XXX, "Сайтрипы 2022 (HD Video) / SiteRip's 2022 (HD Video)");
            caps.Categories.AddCategoryMapping(1861, TorznabCatType.XXX, "Сайтрипы 2023 (HD Video) / SiteRip's 2023 (HD Video)");
            caps.Categories.AddCategoryMapping(1451, TorznabCatType.XXX, "Сайтрипы 1991-2010 / SiteRip's 1991-2010");
            caps.Categories.AddCategoryMapping(1788, TorznabCatType.XXX, "Сайтрипы 2011-2012 / SiteRip's 2011-2012");
            caps.Categories.AddCategoryMapping(1789, TorznabCatType.XXX, "Сайтрипы 2013 / SiteRip's 2013");
            caps.Categories.AddCategoryMapping(1792, TorznabCatType.XXX, "Сайтрипы 2014 / SiteRip's 2014");
            caps.Categories.AddCategoryMapping(1798, TorznabCatType.XXX, "Сайтрипы 2015 / SiteRip's 2015");
            caps.Categories.AddCategoryMapping(1805, TorznabCatType.XXX, "Сайтрипы 2016 / SiteRip's 2016");
            caps.Categories.AddCategoryMapping(1820, TorznabCatType.XXX, "Сайтрипы 2017 / SiteRip's 2017");
            caps.Categories.AddCategoryMapping(1826, TorznabCatType.XXX, "Сайтрипы 2018 / SiteRip's 2018");
            caps.Categories.AddCategoryMapping(1837, TorznabCatType.XXX, "Сайтрипы 2019 / SiteRip's 2019");
            caps.Categories.AddCategoryMapping(1843, TorznabCatType.XXX, "Сайтрипы 2020 / SiteRip's 2020");
            caps.Categories.AddCategoryMapping(1847, TorznabCatType.XXX, "Сайтрипы 2021 / SiteRip's 2021");
            caps.Categories.AddCategoryMapping(1856, TorznabCatType.XXX, "Сайтрипы 2022 / SiteRip's 2022");
            caps.Categories.AddCategoryMapping(1862, TorznabCatType.XXX, "Сайтрипы 2023 / SiteRip's 2023");
            caps.Categories.AddCategoryMapping(1707, TorznabCatType.XXX, "Сцены из фильмов / Movie Scenes (кроме SiteRip)");
            caps.Categories.AddCategoryMapping(284, TorznabCatType.XXX, "Порноролики Разное / Clips (various)");
            caps.Categories.AddCategoryMapping(1853, TorznabCatType.XXX, "Компиляции и Музыкальные порно клипы / Compilations & Porn Music Video (PMV)");
            caps.Categories.AddCategoryMapping(1823, TorznabCatType.XXX, "Порноролики в 3D и Virtual Reality (VR) / 3D & Virtual Reality Videos");

            caps.Categories.AddCategoryMapping(1800, TorznabCatType.XXX, "Японское и китайское порно / Japanese & Chinese Adult Video (JAV)");
            caps.Categories.AddCategoryMapping(1801, TorznabCatType.XXXPack, "Паки японских фильмов и сайтрипов / Full Length Japanese Movies Packs & SiteRip's Packs");
            caps.Categories.AddCategoryMapping(1719, TorznabCatType.XXX, "Японские фильмы и сайтрипы (DVD и HD Video) / Japanese Movies & SiteRip's (DVD & HD Video)");
            caps.Categories.AddCategoryMapping(997, TorznabCatType.XXX, "Японские фильмы и сайтрипы 1991-2014 / Japanese Movies & SiteRip's 1991-2014");
            caps.Categories.AddCategoryMapping(1818, TorznabCatType.XXX, "Японские фильмы и сайтрипы 2015-2023 / Japanese Movies & SiteRip's 2015-2023");
            caps.Categories.AddCategoryMapping(1849, TorznabCatType.XXX, "Китайские фильмы и сайтрипы (DVD и HD Video) / Chinese Movies & SiteRip's (DVD & HD Video)");
            caps.Categories.AddCategoryMapping(1815, TorznabCatType.XXX, "Архив (Японское и китайское порно)");

            caps.Categories.AddCategoryMapping(1723, TorznabCatType.XXX, "Фото и журналы / Photos & Magazines");
            caps.Categories.AddCategoryMapping(1726, TorznabCatType.XXX, "MetArt & MetModels");
            caps.Categories.AddCategoryMapping(883, TorznabCatType.XXXImageSet, "Эротические студии Разное / Erotic Picture Gallery (various)");
            caps.Categories.AddCategoryMapping(1759, TorznabCatType.XXXImageSet, "Паки сайтрипов эротических студий / Erotic Picture SiteRip's Packs");
            caps.Categories.AddCategoryMapping(1728, TorznabCatType.XXXImageSet, "Любительское фото / Amateur Picture Gallery");
            caps.Categories.AddCategoryMapping(1729, TorznabCatType.XXXPack, "Подборки по актрисам / Actresses Picture Packs");
            caps.Categories.AddCategoryMapping(38, TorznabCatType.XXXImageSet, "Подборки сайтрипов / SiteRip's Picture Packs");
            caps.Categories.AddCategoryMapping(1757, TorznabCatType.XXXImageSet, "Подборки сетов / Picture Sets Packs");
            caps.Categories.AddCategoryMapping(1735, TorznabCatType.XXXImageSet, "Тематическое и нетрадиционное фото / Misc & Special Interest Picture Packs");
            caps.Categories.AddCategoryMapping(1731, TorznabCatType.XXXImageSet, "Журналы / Magazines");
            caps.Categories.AddCategoryMapping(1802, TorznabCatType.XXX, "Архив (Фото)");

            caps.Categories.AddCategoryMapping(1745, TorznabCatType.XXX, "Хентай и Манга, Мультфильмы и Комиксы, Рисунки / Hentai & Manga, Cartoons & Comics, Artwork");
            caps.Categories.AddCategoryMapping(1679, TorznabCatType.XXX, "Хентай: основной подраздел / Hentai: main subsection");
            caps.Categories.AddCategoryMapping(1740, TorznabCatType.XXX, "Хентай в высоком качестве (DVD и HD) / Hentai DVD & HD");
            caps.Categories.AddCategoryMapping(1834, TorznabCatType.XXX, "Хентай: ролики 2D / Hentai: 2D video");
            caps.Categories.AddCategoryMapping(1752, TorznabCatType.XXX, "Хентай: ролики 3D / Hentai: 3D video");
            caps.Categories.AddCategoryMapping(1760, TorznabCatType.XXX, "Хентай: Манга / Hentai: Manga");
            caps.Categories.AddCategoryMapping(1781, TorznabCatType.XXX, "Хентай: Арт и HCG / Hentai: Artwork & HCG");
            caps.Categories.AddCategoryMapping(1711, TorznabCatType.XXX, "Мультфильмы / Cartoons");
            caps.Categories.AddCategoryMapping(1296, TorznabCatType.XXX, "Комиксы и рисунки / Comics & Artwork");

            caps.Categories.AddCategoryMapping(1838, TorznabCatType.XXX, "Игры / Games");
            caps.Categories.AddCategoryMapping(1750, TorznabCatType.XXX, "Игры: основной подраздел / Games: main subsection");
            caps.Categories.AddCategoryMapping(1756, TorznabCatType.XXX, "Игры: визуальные новеллы / Games: Visual Novels");
            caps.Categories.AddCategoryMapping(1785, TorznabCatType.XXX, "Игры: ролевые / Games: role-playing (RPG Maker and WOLF RPG Editor)");
            caps.Categories.AddCategoryMapping(1790, TorznabCatType.XXX, "Игры и Софт: Анимация / Software: Animation");
            caps.Categories.AddCategoryMapping(1827, TorznabCatType.XXX, "Игры: В разработке и Демо (основной подраздел) / Games: In Progress and Demo (main subsection)");
            caps.Categories.AddCategoryMapping(1828, TorznabCatType.XXX, "Игры: В разработке и Демо (ролевые) / Games: In Progress and Demo (role-playing - RPG Maker and WOLF RPG Editor)");
            caps.Categories.AddCategoryMapping(1829, TorznabCatType.XXX, "Обсуждение игр / Games Discussion");

            caps.Categories.AddCategoryMapping(11, TorznabCatType.XXX, "Нетрадиционное порно / Special Interest Movies & Clips");
            caps.Categories.AddCategoryMapping(1715, TorznabCatType.XXX, "Транссексуалы (DVD и HD) / Transsexual (DVD & HD)");
            caps.Categories.AddCategoryMapping(1680, TorznabCatType.XXX, "Транссексуалы / Transsexual");
            caps.Categories.AddCategoryMapping(1758, TorznabCatType.XXX, "Бисексуалы / Bisexual");
            caps.Categories.AddCategoryMapping(1682, TorznabCatType.XXX, "БДСМ / BDSM");
            caps.Categories.AddCategoryMapping(1733, TorznabCatType.XXX, "Женское доминирование и страпон / Femdom & Strapon");
            caps.Categories.AddCategoryMapping(1754, TorznabCatType.XXX, "Подглядывание / Voyeur");
            caps.Categories.AddCategoryMapping(1734, TorznabCatType.XXX, "Фистинг и дилдо / Fisting & Dildo");
            caps.Categories.AddCategoryMapping(1791, TorznabCatType.XXX, "Беременные / Pregnant");
            caps.Categories.AddCategoryMapping(509, TorznabCatType.XXX, "Буккаке / Bukkake");
            caps.Categories.AddCategoryMapping(1859, TorznabCatType.XXX, "Гэнг-бэнг / GangBang");
            caps.Categories.AddCategoryMapping(1685, TorznabCatType.XXX, "Мочеиспускание / Peeing");
            caps.Categories.AddCategoryMapping(1762, TorznabCatType.XXX, "Фетиш / Fetish");
            caps.Categories.AddCategoryMapping(1681, TorznabCatType.XXX, "Дефекация / Scat");

            caps.Categories.AddCategoryMapping(1683, TorznabCatType.XXX, "Архив (общий)");

            caps.Categories.AddCategoryMapping(1688, TorznabCatType.XXX, "Гей-порно / Gay Forum");
            caps.Categories.AddCategoryMapping(903, TorznabCatType.XXX, "Полнометражные гей-фильмы / Full Length Movies (Gay)");
            caps.Categories.AddCategoryMapping(1765, TorznabCatType.XXX, "Полнометражные азиатские гей-фильмы / Full-length Asian (Gay)");
            caps.Categories.AddCategoryMapping(1767, TorznabCatType.XXX, "Классические гей-фильмы (до 1990 года) / Classic Gay Films (Pre-1990's)");
            caps.Categories.AddCategoryMapping(1755, TorznabCatType.XXX, "Гей-фильмы в высоком качестве (DVD и HD) / High-Quality Full Length Movies (Gay DVD & HD)");
            caps.Categories.AddCategoryMapping(1787, TorznabCatType.XXX, "Азиатские гей-фильмы в высоком качестве (DVD и HD) / High-Quality Full Length Asian Movies (Gay DVD & HD)");
            caps.Categories.AddCategoryMapping(1763, TorznabCatType.XXXPack, "ПАКи гей-роликов и сайтрипов / Clip's & SiteRip's Packs (Gay)");
            caps.Categories.AddCategoryMapping(1777, TorznabCatType.XXX, "Гей-ролики в высоком качестве (HD Video) / Gay Clips (HD Video)");
            caps.Categories.AddCategoryMapping(1691, TorznabCatType.XXX, "Ролики, SiteRip'ы и сцены из гей-фильмов / Clips & Movie Scenes (Gay)");
            caps.Categories.AddCategoryMapping(1692, TorznabCatType.XXXImageSet, "Гей-журналы, фото, разное / Magazines, Photo, Rest (Gay)");
            caps.Categories.AddCategoryMapping(1720, TorznabCatType.XXX, "Архив (Гей-порно)");

            return caps;
        }

        public override async Task<ConfigurationData> GetConfigurationForSetup()
        {
            configData.CookieHeader.Value = null;
            var response = await RequestWithCookiesAsync(LoginUrl);
            var LoginResultParser = new HtmlParser();
            var LoginResultDocument = LoginResultParser.ParseDocument(response.ContentString);
            var captchaimg = LoginResultDocument.QuerySelector("img[src*=\"/captcha/\"]");
            if (captchaimg != null)
            {
                var captchaImage = await RequestWithCookiesAsync("https:" + captchaimg.GetAttribute("src"));
                configData.CaptchaImage.Value = captchaImage.ContentBytes;

                var codefield = LoginResultDocument.QuerySelector("input[name^=\"cap_code_\"]");
                cap_code_field = codefield.GetAttribute("name");

                var sidfield = LoginResultDocument.QuerySelector("input[name=\"cap_sid\"]");
                cap_sid = sidfield.GetAttribute("value");
            }
            else
            {
                configData.CaptchaImage.Value = null;
            }
            return configData;
        }

        public override async Task<IndexerConfigurationStatus> ApplyConfiguration(JToken configJson)
        {
            LoadValuesFromJson(configJson);

            var pairs = new Dictionary<string, string>
            {
                { "login_username", configData.Username.Value },
                { "login_password", configData.Password.Value },
                { "login", "Login" }
            };

            if (!string.IsNullOrWhiteSpace(cap_sid))
            {
                pairs.Add("cap_sid", cap_sid);
                pairs.Add(cap_code_field, configData.CaptchaText.Value);

                cap_sid = null;
                cap_code_field = null;
            }

            var result = await RequestLoginAndFollowRedirect(LoginUrl, pairs, CookieHeader, true, null, LoginUrl, true);
            await ConfigureIfOK(result.Cookies, result.ContentString != null && result.ContentString.Contains("Вы зашли как:"), () =>
            {
                var errorMessage = "Unknown error message, please report";
                var LoginResultParser = new HtmlParser();
                var LoginResultDocument = LoginResultParser.ParseDocument(result.ContentString);
                var errormsg = LoginResultDocument.QuerySelector("div");
                if (errormsg != null && errormsg.TextContent.Contains("Форум временно отключен"))
                    errorMessage = errormsg.TextContent;
                errormsg = LoginResultDocument.QuerySelector("h4[class=\"warnColor1 tCenter mrg_16\"]");
                if (errormsg != null)
                    errorMessage = errormsg.TextContent;

                throw new ExceptionWithConfigData(errorMessage, configData);
            });
            return IndexerConfigurationStatus.RequiresTesting;
        }

        protected override async Task<IEnumerable<ReleaseInfo>> PerformQuery(TorznabQuery query)
        {
            var releases = new List<ReleaseInfo>();
            var searchString = query.SanitizedSearchTerm;

            var qc = new List<KeyValuePair<string, string>> // NameValueCollection don't support cat[]=19&cat[]=6
            {
                {"o", "1"},
                {"s", "2"}
            };

            // if the search string is empty use the getnew view
            if (string.IsNullOrWhiteSpace(searchString))
            {
                qc.Add("nm", searchString);
            }
            else // use the normal search
            {
                searchString = searchString.Replace("-", " ");
                qc.Add("nm", searchString);
            }

            foreach (var cat in MapTorznabCapsToTrackers(query))
                qc.Add("f[]", cat);

            var searchUrl = SearchUrl + "?" + qc.GetQueryString();
            var results = await RequestWithCookiesAsync(searchUrl);
            if (!results.ContentString.Contains("Вы зашли как:"))
            {
                // re login
                await ApplyConfiguration(null);
                results = await RequestWithCookiesAsync(searchUrl);
            }
            try
            {
                var searchResultParser = new HtmlParser();
                var searchResultDocument = searchResultParser.ParseDocument(results.ContentString);

                var rows = searchResultDocument.QuerySelectorAll("table#tor-tbl > tbody > tr");
                foreach (var row in rows)
                {
                    try
                    {
                        var qDownloadLink = row.QuerySelector("a.tr-dl");
                        if (qDownloadLink == null) // Expects moderation
                            continue;

                        var qForumLink = row.QuerySelector("a.f");
                        var qDetailsLink = row.QuerySelector("a.tLink");
                        var qSize = row.QuerySelector("td:nth-child(6) u");
                        var link = new Uri(SiteLink + "forum/" + qDetailsLink.GetAttribute("href"));
                        var seederString = row.QuerySelector("td:nth-child(7) b").TextContent;
                        var seeders = string.IsNullOrWhiteSpace(seederString) ? 0 : ParseUtil.CoerceInt(seederString);

                        var forumid = ParseUtil.GetArgumentFromQueryString(qForumLink?.GetAttribute("href"), "f");
                        var title = configData.StripRussianLetters.Value
                            ? s_StripRussianRegex.Replace(qDetailsLink.TextContent, "")
                            : qDetailsLink.TextContent;
                        var size = ParseUtil.GetBytes(qSize.TextContent);
                        var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(8)").TextContent);
                        var grabs = ParseUtil.CoerceLong(row.QuerySelector("td:nth-child(9)").TextContent);
                        var publishDate = DateTimeUtil.UnixTimestampToDateTime(long.Parse(row.QuerySelector("td:nth-child(11) u").TextContent));

                        var release = new ReleaseInfo
                        {
                            MinimumRatio = 1,
                            MinimumSeedTime = 0,
                            Title = title,
                            Details = link,
                            Description = qForumLink.TextContent,
                            Link = link,
                            Guid = link,
                            Size = size,
                            Seeders = seeders,
                            Peers = leechers + seeders,
                            Grabs = grabs,
                            PublishDate = publishDate,
                            Category = MapTrackerCatToNewznab(forumid),
                            DownloadVolumeFactor = 1,
                            UploadVolumeFactor = 1
                        };
                        releases.Add(release);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"{Id}: Error while parsing row '{row.OuterHtml}':\n\n{ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                OnParseError(results.ContentString, ex);
            }

            return releases;
        }

        // referer link support
        public override async Task<byte[]> Download(Uri link)
        {
            var downloadlink = link;
            var response = await RequestWithCookiesAsync(link.ToString());
            var results = response.ContentString;
            var SearchResultParser = new HtmlParser();
            var SearchResultDocument = SearchResultParser.ParseDocument(results);
            var downloadSelector = "a[class=\"dl-stub dl-link\"]";
            var DlUri = SearchResultDocument.QuerySelector(downloadSelector);
            if (DlUri != null)
            {
                logger.Debug(string.Format("{0}: Download selector {1} matched:{2}", Id, downloadSelector, DlUri.OuterHtml));
                var href = DlUri.GetAttribute("href");
                downloadlink = new Uri(SiteLink + "forum/" + href);

            }
            else
            {
                logger.Error(string.Format("{0}: Download selector {1} didn't match:\n{2}", Id, downloadSelector, results));
                throw new Exception(string.Format("Download selector {0} didn't match", downloadSelector));
            }
            return await base.Download(downloadlink, RequestType.POST, link.ToString());
        }
    }
}
