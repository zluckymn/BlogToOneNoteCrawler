using DotNet.Utilities;
using HtmlAgilityPack;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Yinhe.ProcessingCenter;

using Yinhe.ProcessingCenter.DataRule;
using System.Collections;
using Helper;

namespace SimpleCrawler.Demo
{
    /// <summary>
    /// 使用土地云接口 获取土地周边幼儿园
    /// </summary>
    public class LandFangKindergartenDetailWebCrawler_TuDiYun : SimpleCrawlerBase
    {
        string DataTableCityName_TuDiYun = "LandFangKingardenDetailWebCrawler";
        MongoOperation dataop_old = MongoOpCollection.Get121MongoOp("SimpleCrawler");
        int maxCount_ChangeProxy = 30;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public LandFangKindergartenDetailWebCrawler_TuDiYun(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "LandFang_TuDiYun";
            DataTableCategoryName = "LandFang";
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "guid";
            Settings = _Settings; filter = _filter; dataop = _dataop;
            updatedField = "isKindergarten";
        }

        public void initialUrl()
        {
            var takeCount = 10000;

            // var cityNames = "合肥,泉州,自贡,株洲,上饶,济南,漳州,厦门,三亚,梅州,惠州,西安,唐山".Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            //var partName = "所在地";
            //var orQuery = Query.In(partName, cityNames.Select(c => (BsonValue)c));
            var query = Query.And(Query.Exists(updatedField, false),Query.Exists("x",true));
            var landList = dataop_old.FindAll(DataTableCategoryName, query).SetFields(new string[] { "x","y","url", "所在地","地区" }).SetLimit(takeCount);//土地url

            //globalTotalCount = (int)dataop_old.FindCount(DataTableCategoryName, query);
            var allCityList = dataop_old.FindAll(DataTableCityName_TuDiYun).ToList();                                                                                                                                            // Console.WriteLine("待处理数据{0}个", landList.Count());
           
            foreach (var landObj in landList)
            {
                var lng = landObj.Double("x");
                var lat = landObj.Double("y");
                var cityName = landObj.Text("所在地");
                var hitCity = allCityList.Where(c => c.Text("cityName") == cityName).FirstOrDefault();
                if (hitCity == null)
                {
                   
                    cityName = landObj.Text("地区");
                    hitCity = allCityList.Where(c => c.Text("cityName") == cityName).FirstOrDefault();
                    continue;
                }
                if (hitCity == null)
                {
                    ShowMessage($"无法找到城市{ cityName}");
                    continue;
                }
                var cityCode = hitCity.Text("cityCode");
                var url = landObj.Text("url");//http://land.fang.com/market/2e81878c-eb62-4687-971f-01b174817207.html
                var guid = GetGuidFromUrl(url, "/", ".");
                // var guid = cityObj.Text("sParcelID");
                if (!string.IsNullOrEmpty(guid))
                {
                    var cityId = hitCity.Text("cityId");

                    var timestamp = QuickMethodHelper.Instance().GetTimeStamp();
                    var curUrl = $"https://land.3fang.com/landsupplyplandetail/getNewAmbitusSupportingInfo?referer=&t=90201.26379188632&x-csrf-token-land-egg=-4-tThxeRHcqHyN9AS_1kJZf";
                    var postData = new BsonDocument();
                    postData.Set("cityId", cityId);
                    postData.Set("landLat", lat);
                    postData.Set("landLng", lng);
                    postData.Set("targetDistance", 3000);
                    //postData.Set("poiAdapterDataInfoList")
                    //postData.Set("poiAdapterDataInfoList")
                    //UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { Depth = 1, UniqueKey = guid, Authorization = url });

                }

            }
        }

        public override void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {

            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            //var ipProxyList = dataop.FindAllByQuery("IPProxy", Query.NE("status", "1")).ToList();
            // Settings.IPProxyList.AddRange(ipProxyList.Select(c => new IPProxy(c.Text("ip"))).Distinct());
            // Settings.IPProxyList.Add(new IPProxy("1.209.188.180:8080"));
            Settings.IgnoreSucceedUrlToDB = true;
            //Settings.IgnoreFailUrl = false;
            Settings.MaxReTryTimes = 100;
            Settings.AutoSpeedLimit = true;
            
            Settings.ThreadCount =4;
            Settings.DBSaveCountLimit = 1;
            // Settings.CurWebProxy = GetWebProxy();
            Settings.Referer = "mdizhu.3fang.com";
            this.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.102 Safari/537.36 Edge/18.18362";
            Settings.Accept = "application/json, text/plain, */*";
            Settings.SimulateCookies = "x-csrf-token-land-egg=-4-tThxeRHcqHyN9AS_1kJZf; c1a62c64b8d12de9871f7c76230dae6a=5; unique_cookie=U_quzu0hblgs3q6w0e54xt7a0x62ak9aopb1x*4; global_cookie=quzu0hblgs3q6w0e54xt7a0x62ak9aopb1x";
            var headSetDic = new Dictionary<string, string>();
            headSetDic.Add("Accept-Encoding", "br, gzip, deflate");
            Settings.HeadSetDic = headSetDic;
            Console.WriteLine("正在获取已存在的url数据");
            initialUrl();
            Console.WriteLine("正在加载账号数据");



            ///不进行地址爬取
            Settings.RegularFilterExpressions.Add(@"luckymnXXXXXXXXXXXXXXXXXX");

            if (SimulateLogin())
            {
                //  Console.WriteLine("zluckymn模拟登陆成功");
            }
            else
            {
                Console.WriteLine("模拟登陆失败");
            }


        }

        public static string ConvertHexToString(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// <sParcelID><![CDATA[de6aebb8-d7c7-4067-8c73-12eb0836b1ae]]></sParcelID><sParcelName><![CDATA[松山湖金多港]]></sParcelName>
        ///{{ "sParcelID" : "de6aebb8-d7c7-4067-8c73-12eb0836b1ae", "sParcelName" : "松山湖金多港", "sParcelSN" : "2014WT038", "sParcelArea" : "广东省", "sParcelAreaCity" : "东莞市", "sParcelAreaDis" : "", "fparcelarea" : "35916.27㎡", "fcollectingarea" : "暂无", "fbuildarea" : "35916.27㎡", "fplanningarea" : "71832.54㎡", "sPlotratio" : "≤2", "sremiseway" : "挂牌", "sservicelife" : "50年", "sparcelplace" : "松山湖金多港", "sparcelextremes" : "松山湖金多港", "sConforming" : "其它用地", "sdealstatus" : "中止交易", "istartdate" : "2014-07-02", "ienddate" : "2014-07-16", "dAnnouncementDate" : "2014-06-12", "finitialprice" : "2874.00万元", "sbidincrements" : "50万元", "sperformancebond" : "750万元", "fInitialFloorPrice" : "400.10元/㎡", "stransactionsites" : "东莞市国土资源局", "sconsulttelephone" : "076926983723", "fcoordinateax" : "", "fcoordinateay" : "", "mapurl" : "https://api.map.baidu.com/staticimage?markers=&width=500&height=500&zoom=12&scale=1", "Land_fAvgPremiumRate" : "暂无", "Land_sParcelMemo" : "暂无", "Land_sTransferee" : "暂无", "Land_fInitialUnitPrice" : "800.19万元", "icompletiondate" : "1900-01-01", "fclosingcost" : "0.00万元", "fprice" : "0.00元/㎡", "sGreeningRate" : "暂无", "Land_sCommerceRate" : "暂无", "Land_sBuildingDensity" : "≤35", "Land_sLimitedHeight" : "暂无", "Land_bIsSecurityHousing" : "无", "sAnnouncementNo" : "WGJ2014050", "readcount" : "12", "isread" : "1", "isfavorite" : "0", "sImages" : "", "sImages_o" : "", "usertype" : "", "message" : "了解房企拿地状况，地块项目进展等信息，请加入数据库会员，更多专享服务为您量身打造！" }}
        ///  </summary>
        /// <param name="args">url参数</param>
        public override void DataReceive(DataReceivedEventArgs args)
        {
            if (CanLoadNewData())
            {
                initialUrl();
            }
      

            var hmtl = args.Html;
            var jObject = args.Html.GetJobjectFromJson();
            


            var guid = args.urlInfo.UniqueKey;
            var urlGuid = args.urlInfo.Authorization;
            var oldUpdateDoc = new BsonDocument();
            if (jObject != null)
            {
                var data = jObject["data"];
                if (data != null)
                {
                    //foreach (var priceRatioItem in data)
                  //  {
                        var regionDoc = data.ToString().GetBsonDocFromJson();
                        regionDoc.Set("guid", guid);
                        PushData(regionDoc);
                        if (!string.IsNullOrEmpty(regionDoc.Text("gdLng")) && regionDoc.Text("gdLng") != "BsonNull")
                        {
                            oldUpdateDoc.Set("x", regionDoc.Text("gdLng"));
                        }
                        if (!string.IsNullOrEmpty(regionDoc.Text("gdLat")) && regionDoc.Text("gdLat") != "BsonNull")
                        {
                            oldUpdateDoc.Set("y", regionDoc.Text("gdLat"));
                        }
                        if (!string.IsNullOrEmpty(regionDoc.Text("gdCoord"))&& regionDoc.Text("gdCoord")!= "BsonNull")
                        {
                            oldUpdateDoc.Set("gdCoord", regionDoc.Text("gdCoord"));
                        }

                 //   }
                    oldUpdateDoc.Set(updatedField, 1);
                    dataop_old.UpdateOrInsert(DataTableCategoryName, Query.EQ("url", urlGuid), oldUpdateDoc);
                    ShowStatus();
                }
            }
            //自动切换ip
            var curCount = updateCount + addCount;
            if (curCount > 0 && curCount % maxCount_ChangeProxy == 0)
            {
                //手动更新ip
                QuickProxyPoolHelper.Instance().ExecChangeIp();
            }

        }


        /// <summary>
        /// IP限定处理，ip被限制 账号被限制跳转处理
        /// </summary>
        /// <param name="args"></param>
        public override bool IPLimitProcess(DataReceivedEventArgs args)
        {
            if (args.Html.Contains("projectList"))//需要编写被限定IP的处理
            {
                return false;
            }
            else
            {
                QuickProxyPoolHelper.Instance().ExecChangeIp();
                return true;
            }



        }



    }

}
