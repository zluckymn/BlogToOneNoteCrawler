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
    /// 使用土地云接口 获取城市区县的地块房价比
    /// </summary>
    public class LandFangDetailAPPCrawler_HouseLandRadio_TuDiYun : SimpleCrawlerBase
    {

      
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public LandFangDetailAPPCrawler_HouseLandRadio_TuDiYun(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "LandFang_TuDiYun";
            DataTableCategoryName = "LandFang_City_TuDiYun";
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "guid";
            Settings = _Settings; filter = _filter; dataop = _dataop;
        }
        public string year = "";
        public bool isSpecialUrlMode = false;
       
        public override void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //种子地址需要加布隆过滤
            if (string.IsNullOrEmpty(year))
            {
                Console.WriteLine("请输入房价比年份");
                year = Console.ReadLine();
            }
            if (!int.TryParse(year, out int yearInt))
            {
                year = "2020";
            }
            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            //var ipProxyList = dataop.FindAllByQuery("IPProxy", Query.NE("status", "1")).ToList();
            // Settings.IPProxyList.AddRange(ipProxyList.Select(c => new IPProxy(c.Text("ip"))).Distinct());
            // Settings.IPProxyList.Add(new IPProxy("1.209.188.180:8080"));
            Settings.IgnoreSucceedUrlToDB = true;
            //Settings.IgnoreFailUrl = false;
            Settings.MaxReTryTimes = 0;
            //Settings.AutoSpeedLimit = true;
            //Settings.AutoSpeedLimitMaxMSecond = 2000;
            //Settings.AutoSpeedLimitMaxMSecond = 1000;
            Settings.ThreadCount = 1;
            Settings.DBSaveCountLimit = 1;
            // Settings.CurWebProxy = GetWebProxy();
            Settings.Referer = "mdizhu.3fang.com";
            this.Settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148";
            Settings.Accept = "application/json, text/plain, */*";
            //  var headSetDic = new Dictionary<string, string>();
            //// headSetDic.Add("Accept-Encoding", "gzip");
            //// hi.HeaderSet("Content-Length","154");
            //// hi.HeaderSet("Connection","Keep-Alive");
            ////headSetDic.Add("imei", "9e06e50488a0eabda56f2c8d77357438ba9b15d4");
            ////// headSetDic.Add("Host", "appapi.3g.fang.com");
            ////headSetDic.Add("sfut", "D5050A08EB4210E18A2C5887855BA2879168C8C72FFCE4B92C8DCE2DD553989EC08298703ACF1040FF1AC300889F9C1B8F7B41A7BB208C87AB792668C4BC31AB16B00686E40A107AB92F736A8E90DB24838E326ABBA45226; sfyt=Lt1rDF57nxt_WzZojwbvXLtPE0_LvrKAiHlEUnoUv3427eguxhqRiQJANAPnqbk9;");
            ////// headSetDic.Add("user-agent", "android_tudi%7EGT-P5210%7E4.2.2");
            //////hi.HeaderSet("user-agent", "android_tudi%7EGT-P5210%7E4.2.2");
            ////headSetDic.Add("global_cookie", "fvroqa9v7gfloaurht6dulgwh1bk983hpxz");
        
            //Settings.HeadSetDic = headSetDic;
            //date=&end_date=&title=&content=&key=%E5%85%AC%E5%8F%B8&database=saic&search_field=all&search_type=yes&page=2
            Settings.SimulateCookies = "unique_cookie=U_vbymzuv6x7tybgu3y9gvh04it13k98857m0*2; dizhu-city=%7B%22cityName%22%3A%22%E5%8E%A6%E9%97%A8%22%2C%22region%22%3A%22%E7%8F%A0%E4%B8%89%E8%A7%92%22%2C%22fatherCode%22%3A%22X%22%2C%22hot%22%3A%22%22%2C%22cityId%22%3A%2203c15b7a-6ec0-49ff-ba3e-1f6c15cb1197%22%2C%22cityCode%22%3A%22350200%22%2C%22rules%22%3A%5B%220%22%2C%221%22%2C%221%22%2C%221%22%2C%221%22%2C%221%22%2C%221%22%2C%221%22%2C%221%22%2C%221%22%2C%221%22%5D%2C%22cityAb%22%3A%22xm%22%7D; imei=9e06e50488a0eabda56f2c8d77357438ba9b15d4; sfut=D5050A08EB4210E18A2C5887855BA2879168C8C72FFCE4B92C8DCE2DD553989EC08298703ACF1040FF1AC300889F9C1B8F7B41A7BB208C87AB792668C4BC31AB16B00686E40A107AB92F736A8E90DB24838E326ABBA45226; sfyt=Lt1rDF57nxt_WzZojwbvXLtPE0_LvrKAiHlEUnoUv3427eguxhqRiQJANAPnqbk9; unique_cookie=U_vbymzuv6x7tybgu3y9gvh04it13k98857m0; global_cookie=fvroqa9v7gfloaurht6dulgwh1bk983hpxz";

            Console.WriteLine("正在获取已存在的url数据");

            var allHitCityIdList = dataop.FindAll(DataTableName).Where(c=>c.Text("year")==year).Select(c => c.Text("cityId")).ToList();
            var cityList = dataop.FindAll(DataTableCategoryName).Where(c=>!allHitCityIdList.Contains(c.Text("cityId"))).ToList();//土地url
            Console.WriteLine("待处理数据{0}个", cityList.Count);
            
            foreach (var cityObj in cityList)
            {
                var cityId = cityObj.Text("cityId");
                var timestamp = QuickMethodHelper.Instance().GetTimeStamp();
                var url =$"https://mdizhu.3fang.com/ndb/proxy/cache-core/1.0/thematicmap/getLandAndHousePriceRatioMap?cityId={cityId}&year={year}&request_transaction={timestamp}";//http://land.fang.com/market/2e81878c-eb62-4687-971f-01b174817207.html
                UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 ,UniqueKey= cityId });
               
            }
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
 
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// <sParcelID><![CDATA[de6aebb8-d7c7-4067-8c73-12eb0836b1ae]]></sParcelID><sParcelName><![CDATA[松山湖金多港]]></sParcelName>
        ///{{ "sParcelID" : "de6aebb8-d7c7-4067-8c73-12eb0836b1ae", "sParcelName" : "松山湖金多港", "sParcelSN" : "2014WT038", "sParcelArea" : "广东省", "sParcelAreaCity" : "东莞市", "sParcelAreaDis" : "", "fparcelarea" : "35916.27㎡", "fcollectingarea" : "暂无", "fbuildarea" : "35916.27㎡", "fplanningarea" : "71832.54㎡", "sPlotratio" : "≤2", "sremiseway" : "挂牌", "sservicelife" : "50年", "sparcelplace" : "松山湖金多港", "sparcelextremes" : "松山湖金多港", "sConforming" : "其它用地", "sdealstatus" : "中止交易", "istartdate" : "2014-07-02", "ienddate" : "2014-07-16", "dAnnouncementDate" : "2014-06-12", "finitialprice" : "2874.00万元", "sbidincrements" : "50万元", "sperformancebond" : "750万元", "fInitialFloorPrice" : "400.10元/㎡", "stransactionsites" : "东莞市国土资源局", "sconsulttelephone" : "076926983723", "fcoordinateax" : "", "fcoordinateay" : "", "mapurl" : "https://api.map.baidu.com/staticimage?markers=&width=500&height=500&zoom=12&scale=1", "Land_fAvgPremiumRate" : "暂无", "Land_sParcelMemo" : "暂无", "Land_sTransferee" : "暂无", "Land_fInitialUnitPrice" : "800.19万元", "icompletiondate" : "1900-01-01", "fclosingcost" : "0.00万元", "fprice" : "0.00元/㎡", "sGreeningRate" : "暂无", "Land_sCommerceRate" : "暂无", "Land_sBuildingDensity" : "≤35", "Land_sLimitedHeight" : "暂无", "Land_bIsSecurityHousing" : "无", "sAnnouncementNo" : "WGJ2014050", "readcount" : "12", "isread" : "1", "isfavorite" : "0", "sImages" : "", "sImages_o" : "", "usertype" : "", "message" : "了解房企拿地状况，地块项目进展等信息，请加入数据库会员，更多专享服务为您量身打造！" }}
        ///  </summary>
        /// <param name="args">url参数</param>
        public override void DataReceive(DataReceivedEventArgs args)
        {
          
            var hmtl = args.Html;
            var jObject = args.Html.GetJobjectFromJson();
            var cityId = args.urlInfo.UniqueKey;
            if (jObject != null)
            {
                var data = jObject["data"];
                if (data != null)
                {
                    var priceRatio = data["priceRatio"];
                    if (priceRatio != null)
                    {
                        foreach (var priceRatioItem in priceRatio)
                        {
                            var regionDoc = priceRatioItem.ToString().GetBsonDocFromJson();
                            regionDoc.Set("cityId", cityId);
                            regionDoc.Set("year", year);
                            regionDoc.Set("guid", (year+regionDoc.Text("geoheyId")).EncodeMD5());
                            PushData(regionDoc);
                        }
                    }
 
                }
          }



      

        }
 
   
        /// <summary>
        /// IP限定处理，ip被限制 账号被限制跳转处理
        /// </summary>
        /// <param name="args"></param>
        public override bool IPLimitProcess(DataReceivedEventArgs args)
        {
            if (args.Html.Contains("priceRatio"))//需要编写被限定IP的处理
            {
                return false;
            }
            else
            {
                return true;
            }

           
             
        }
      

        
    }

}
