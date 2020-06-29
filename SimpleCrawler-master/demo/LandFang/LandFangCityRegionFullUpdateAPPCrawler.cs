namespace SimpleCrawler.Demo
{
    using Helpers;
    using HtmlAgilityPack;
    using MongoDB.Bson;
    using MongoDB.Driver;
    using MongoDB.Driver.Builders;
    using Newtonsoft.Json.Linq;
    using SimpleCrawler;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Web;
    using Yinhe.ProcessingCenter;
    using Yinhe.ProcessingCenter.DataRule;

    public class LandFangCityRegionFullUpdateAPPCrawler : ISimpleCrawler
    {
        private DataOperation dataop = null;
        private CrawlSettings Settings = null;
        private LandFangAppHelper appHelper = new LandFangAppHelper();
        private Dictionary<string, string> columnMapDic = new Dictionary<string, string>();
        private int pageCount = 11;
        private int pageSize = 100;
        private Hashtable userCrawlerCountHashTable = new Hashtable();
        private BloomFilter<string> filter;
        private const string _DataTableName = "LandFang";
        private List<BsonDocument> landUrlList = new List<BsonDocument>();
        private BloomFilter<string> updateCityNameList = new BloomFilter<string>(0x3e8);
        public bool isSpecialUrlMode = false;
        private List<BsonDocument> cityList = new List<BsonDocument>();

        public LandFangCityRegionFullUpdateAPPCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            this.Settings = _Settings;
            this.filter = _filter;
            this.dataop = _dataop;
        }

        public bool CanAddUrl(AddUrlEventArgs args)
        {
            return true;
        }


        public void SettingInit()
        {
            this.Settings.IPProxyList = new List<IPProxy>();
            this.Settings.IgnoreSucceedUrlToDB = true;
            this.Settings.ThreadCount = 1;
            this.Settings.DBSaveCountLimit = 1;
            this.Settings.MaxReTryTimes = 30;
            this.Settings.UserAgent = "android_tudi%7EGT-P5210%7E4.2.2";
            Dictionary<string, string> dictionary = new Dictionary<string, string> {
                {
                    "imei",
                    "133524413725754"
                },
                {
                    "version",
                    "2.5.0"
                },
                {
                    "ispos",
                    "1"
                },
                {
                    "app_name",
                    "android_tudi"
                },
                {
                    "iscard",
                    "1"
                },
                {
                    "connmode",
                    "Wifi"
                },
                {
                    "model",
                    "GT-P5210"
                },
                {
                    "posmode",
                    "gps%2Cwifi"
                },
                {
                    "company",
                    "-10000"
                }
            };
            this.Settings.HeadSetDic = dictionary;
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("正在处理城市全库更新");
            string[] fields = new string[] { "name", "cityCode", "type", "provinceCode" };
            this.cityList = this.dataop.FindAll("LandFangCityEXURL").SetFields(fields).ToList<BsonDocument>();
            LandFangAppHelper helper = new LandFangAppHelper();
            string urlString = this.appHelper.InitCityFormatUrl("", this.pageSize.ToString(), "1");
            UrlInfo target = new UrlInfo(urlString)
            {
                Depth = 1
            };
            UrlQueue.Instance.EnQueue(target);
            Console.WriteLine("正在加载账号数据");
            this.Settings.RegularFilterExpressions.Add("luckymnXXXXXXXXXXXXXXXXXX");
            if (!this.SimulateLogin())
            {
                Console.WriteLine("模拟登陆失败");
            }
        }

        public void DataReceive(DataReceivedEventArgs args)
        {
            if (this.userCrawlerCountHashTable.ContainsKey(this.Settings.LandFangIUserId))
            {
                int num3 = int.Parse(this.userCrawlerCountHashTable[this.Settings.LandFangIUserId].ToString());
                this.userCrawlerCountHashTable[this.Settings.LandFangIUserId] = num3 + 1;
                if (num3 >= this.Settings.MaxAccountCrawlerCount)
                {
                    this.SimulateLogin();
                }
            }
            else
            {
                this.userCrawlerCountHashTable.Add(this.Settings.LandFangIUserId, 1);
            }
            Newtonsoft.Json.Linq.JObject obj2 = Newtonsoft.Json.Linq.JObject.Parse(args.Html);
            Newtonsoft.Json.Linq.JToken token = obj2["resulDic"];
            int pageIndex = int.Parse(GetUrlParam(args.Url, "pindex"));
            int allRecordCount = int.Parse(obj2["Total"].ToString());
            string keyWord = (args.urlInfo.Authorization == null) ? string.Empty : args.urlInfo.Authorization;
            foreach (Newtonsoft.Json.Linq.JToken token2 in (IEnumerable<Newtonsoft.Json.Linq.JToken>) token)
            {
                StorageData data;
                BsonDocument document = new BsonDocument();
                string str2 = token2["sParcelId"].ToString();
                string str3 = token2["sParceName"].ToString();
                string cityName = token2["sCity"].ToString();
                string regionName = token2["sArea"].ToString();
                string provinceName = token2["sProvince"].ToString();
                string str6 = token2["sDealStatus"].ToString();
                if (cityName.EndsWith("市"))
                {
                    char[] trimChars = new char[] { '市' };
                    cityName = cityName.TrimEnd(trimChars);
                }
                if (provinceName.EndsWith("省"))
                {
                    char[] trimChars = new char[] { '省' };
                    provinceName = provinceName.TrimEnd(trimChars);
                }
                BsonDocument hitCity = (from c in this.cityList
                    where (cityName == c.Text("name")) && (c.Text("type") == "1")
                    select c).FirstOrDefault<BsonDocument>();
                if (hitCity == null)
                {
                    this.NeedFixRegion(cityName, provinceName, regionName, "城市");
                    continue;
                }
                BsonDocument bsonDoc = (from c in this.cityList
                    where (c.Int("type") == 0) && (c.Text("provinceCode") == hitCity.Text("provinceCode"))
                    select c).FirstOrDefault<BsonDocument>();
                if (bsonDoc == null)
                {
                    Console.WriteLine("省份不存在" + provinceName);
                    return;
                }
                provinceName = bsonDoc.Text("name");
                string tempRegionName = regionName.Replace("本级", "").Replace("区", "").Replace("县", "");
                BsonDocument document3 = (from c in this.cityList
                    where ((c.Int("type") == 2) && c.Text("name").Contains(tempRegionName)) && (c.Text("provinceCode") == hitCity.Text("provinceCode"))
                    select c).FirstOrDefault<BsonDocument>();
                if (document3 == null)
                {
                    if (!regionName.Contains("本级"))
                    {
                        Console.WriteLine("县市不存在" + regionName);
                        document3 = (from c in this.cityList
                            where ((c.Int("type") == 2) && c.Text("name").Contains("其他")) && (c.Text("provinceCode") == hitCity.Text("provinceCode"))
                            select c).FirstOrDefault<BsonDocument>();
                        this.NeedFixRegion(cityName, provinceName, regionName, "县市");
                        return;
                    }
                    regionName = "";
                }
                else
                {
                    regionName = document3.Text("name");
                }
                string str7 = string.Format("http://land.fang.com/market/{0}.html", str2.ToLower());
                IMongoQuery[] queries = new IMongoQuery[] { Query.EQ("url", str7) };
                BsonDocument document4 = this.dataop.FindOneByQuery(this.DataTableName, Query.Or(queries));
                if (document4 == null)
                {
                    if (!this.filter.Contains(str7))
                    {
                        document.Add("guid", str2);
                        document.Add("url", str7);
                        document.Add("所在地", cityName);
                        if (!string.IsNullOrEmpty(provinceName))
                        {
                            document.Add("地区", provinceName);
                        }
                        if (!string.IsNullOrEmpty(regionName))
                        {
                            document.Add("县市", regionName);
                        }
                        document.Add("name", str3);
                        document.Add("needUpdate", "1");
                        Console.WriteLine(string.Format("{0}添加{1}剩余url:{2}", str3, this.Settings.LandFangIUserId, UrlQueue.Instance.Count));
                        this.filter.Add(str7);
                        data = new StorageData {
                            Document = document,
                            Name = this.DataTableName,
                            Type = StorageType.Insert
                        };
                        DBChangeQueue.Instance.EnQueue(data);
                    }
                }
                else if (str6 != document4.Text("交易状况"))
                {
                    document.Add("needUpdate", "1");
                    data = new StorageData {
                        Document = document,
                        Query = Query.EQ("url", str7),
                        Name = this.DataTableName,
                        Type = StorageType.Update
                    };
                    DBChangeQueue.Instance.EnQueue(data);
                }
            }
            Console.WriteLine(string.Format("{0}剩余url:{1}", this.Settings.LandFangIUserId, UrlQueue.Instance.Count));
            if ((pageIndex == 1) && (pageIndex < allRecordCount))
            {
                this.InitNextUrl(keyWord, allRecordCount, pageIndex, this.pageSize);
            }
        }

        public void ErrorReceive(CrawlErrorEventArgs args)
        {
        }

        private static string GetGuidFromUrl(string url)
        {
            int num = url.LastIndexOf("/");
            int num2 = url.LastIndexOf(".");
            if ((num != -1) && (num2 != -1))
            {
                if (num > num2)
                {
                    int num3 = num;
                    num = num2;
                    num2 = num3;
                }
                return url.Substring(num + 1, (num2 - num) - 1);
            }
            return string.Empty;
        }

        public string GetInnerText(HtmlNode node)
        {
            if ((node == null) || string.IsNullOrEmpty(node.InnerText))
            {
                throw new NullReferenceException();
            }
            return node.InnerText;
        }

        private static string GetQueryString(string url)
        {
            int index = url.IndexOf("?");
            if (index != -1)
            {
                return url.Substring(index + 1, (url.Length - index) - 1);
            }
            return string.Empty;
        }

        public string[] GetStrSplited(string str)
        {
            string[] separator = new string[] { ":", "：" };
            return str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string GetUrlParam(string url, string name)
        {
            NameValueCollection values = HttpUtility.ParseQueryString(GetQueryString(url));
            return ((values[name] != null) ? values[name].ToString() : string.Empty);
        }

        public WebProxy GetWebProxy()
        {
            return new WebProxy { 
                Address = new Uri(string.Format("{0}:{1}", "http://proxy.abuyun.com", "9010")),
                Credentials = new NetworkCredential("H1538UM3D6R2133P", "511AF06ABED1E7AE")
            };
        }

        public string GetWebProxyCurl()
        {
            return string.Format("http://{0}:{1}@{2}:{3}", new object[] { "H1538UM3D6R2133P", "511AF06ABED1E7AE", "proxy.abuyun.com", "9010" });
        }

        public string GetXYValue(int startIndex, int allLength, string html)
        {
            StringBuilder builder = new StringBuilder();
            if (startIndex >= allLength)
            {
                return string.Empty;
            }
            int num = startIndex + 1;
            startIndex = num;
            char ch = html[num];
            while (ch != '"')
            {
                builder.AppendFormat(ch.ToString(), new object[0]);
                num = startIndex + 1;
                startIndex = num;
                if (num < allLength)
                {
                    ch = html[startIndex];
                }
                else
                {
                    break;
                }
            }
            return builder.ToString();
        }

        private void InitNextUrl(string keyWord, int allRecordCount, int pageIndex, int pageSize)
        {
            int num3;
            int num = (allRecordCount / pageSize) + 1;
            for (int i = 2; i <= this.pageCount; i = num3 + 1)
            {
                string urlString = this.appHelper.InitCityFormatUrl(HttpUtility.UrlEncode(keyWord), pageSize.ToString(), i.ToString());
                UrlInfo target = new UrlInfo(urlString) {
                    Depth = 1,
                    Authorization = keyWord
                };
                UrlQueue.Instance.EnQueue(target);
                num3 = i;
            }
        }

        private void IPInvalidProcess(IPProxy ipproxy)
        {
        }

        public bool IPLimitProcess(DataReceivedEventArgs args)
        {
            if (!args.Html.Contains("Object moved") && args.Html.Contains("resulDic"))
            {
                return false;
            }
            return true;
        }

        public void NeedFixRegion(string cityName, string provinceName, string regionName, string type)
        {
            if (!this.updateCityNameList.Contains(cityName) && (this.dataop.FindCount(this.DataTableUpdateCity, Query.EQ("cityName", cityName)) < 0))
            {
                Console.WriteLine("城市不存在" + cityName);
                this.updateCityNameList.Add(cityName);
                BsonDocument document = new BsonDocument {
                    { 
                        "cityName",
                        cityName
                    },
                    { 
                        "provinceName",
                        provinceName
                    },
                    { 
                        "regionName",
                        regionName
                    },
                    { 
                        "type",
                        type
                    },
                    { 
                        "date",
                        DateTime.Now.ToString("yyyy-MM-dd")
                    }
                };
                Console.WriteLine("检测到新城市,输入1进行添加");
                StorageData target = new StorageData {
                    Document = document,
                    Name = this.DataTableUpdateCity,
                    Type = StorageType.Insert
                };
                DBChangeQueue.Instance.EnQueue(target);
            }
        }


        public bool SimulateLogin()
        {
            if (this.Settings.LandFangIUserId == 0)
            {
                BsonDocument bsonDoc = this.dataop.FindOneByQuery(this.DataTableNameAccount, Query.EQ("userName", "savegod523"));
                if (bsonDoc != null)
                {
                    this.Settings.LandFangIUserId = bsonDoc.Int("LandFangIUserId");
                }
                if (this.Settings.LandFangIUserId == 0)
                {
                    this.Settings.LandFangIUserId = 0xa68e;
                }
            }
            this.Settings.LandFangIUserId = new Random().Next(0xd05, 0x2310e);
            this.Settings.MaxAccountCrawlerCount = new Random().Next(50, 200);
            StorageData target = new StorageData {
                Name = this.DataTableNameAccount,
                Document = new BsonDocument().Add("LandFangIUserId", this.Settings.LandFangIUserId.ToString()),
                Query = Query.EQ("userName", "savegod523"),
                Type = StorageType.Update
            };
            DBChangeQueue.Instance.EnQueue(target);
            this.StartDBChangeProcess();
            return true;
        }

        private void StartDBChangeProcess()
        {
            List<StorageData> source = new List<StorageData>();
            while (DBChangeQueue.Instance.Count > 0)
            {
                StorageData item = DBChangeQueue.Instance.DeQueue();
                if (item != null)
                {
                    source.Add(item);
                }
            }
            if ((source.Count<StorageData>() > 0) && (this.dataop.BatchSaveStorageData(source).Status > Status.Successful))
            {
                foreach (StorageData data2 in source)
                {
                    DBChangeQueue.Instance.EnQueue(data2);
                }
            }
        }

        public string ValeFix(string str)
        {
            return str.Replace("\n", "").Replace("\r", "").Trim();
        }

        public string DataTableName
        {
            get
            {
                return "LandFang";
            }
        }

        public string DataTableUpdateCity
        {
            get
            {
                return "LandFangCityNeedUpdated";
            }
        }

        public string DataTableNameURL
        {
            get
            {
                return "LandFangURL";
            }
        }

        public string DataTableNameSpecialURL
        {
            get
            {
                return "LandFangSpecialURL";
            }
        }

        public string DataTableNameCity
        {
            get
            {
                return "LandFangCityURL";
            }
        }

        public string DataTableNameAccount
        {
            get
            {
                return "LandFangAccount";
            }
        }
    }
}

