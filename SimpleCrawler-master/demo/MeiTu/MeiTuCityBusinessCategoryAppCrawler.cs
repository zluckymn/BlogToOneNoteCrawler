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
using Newtonsoft.Json.Linq;
using LibCurlNet;
using Helpers;

namespace SimpleCrawler.Demo
{
    /// <summary>
    /// 获取美团商圈poi商家
    /// 类别 var url = "http://api.meituan.com/group/v1/poi/cates/showlist?cityId=57&utm_source=qqcpd&utm_medium=android&utm_term=254&version_name=5.5.4&utm_content=864394010401414&utm_campaign=AgroupBgroupC0E0Gmerchant&ci=57&uuid=D0CA57CF673B1DF3B9D10A36C085A74C7B924190117AF510F9B7717FD432FEE2&msid=8643940104014141484816456943&__skck=09474a920b2f4c8092f3aaed9cf3d218&__skts=1484816498195&__skua=6c2f598f00063de23b4f9a091ab28e75&__skno=a44a95c4-b6f0-4786-9ae7-c8148dc6173b&__skcy=G8p1ahRd5ESh0nFAWLXcEc3bZos%3D";
    ///  分类商家详细数据var url = "http://api.meituan.com/group/v1/poi/select/cate/227?cityId=1&mypos=27.99765739075955,104.5658485649246&sort=smart&coupon=all&mpt_cate1=1&mpt_cate2=227&offset=0&limit=20&fields=phone,markNumbers,cityId,addr,lng,hasGroup,subwayStationId,cates,frontImg,chooseSitting,wifi,avgPrice,style,featureMenus,avgScore,name,parkingInfo,lat,cateId,introduction,showType,areaId,districtId,preferent,lowestPrice,cateName,areaName,zlSourceType,campaignTag,mallName,mallId,brandId,ktv,geo,historyCouponCount,recommendation,iUrl,isQueuing,payInfo,sourceType,abstracts,groupInfo,isSuperVoucher,discount&utm_source=qqcpd&utm_medium=android&utm_term=254&version_name=5.5.4&utm_content=864394010401414&utm_campaign=AgroupBgroupC504526522539939072_c0_e05f42bd28d2adba3eecf05d1ecaecbb5E7277946828438499584_c0Gmerchant&ci=1&uuid=D0CA57CF673B1DF3B9D10A36C085A74C7B924190117AF510F9B7717FD432FEE2&msid=8643940104014141484120580913&__skck=09474a920b2f4c8092f3aaed9cf3d218&__skts=1484122421711&__skua=6c2f598f00063de23b4f9a091ab28e75&__skno=40e3290e-2ece-4633-9f79-52c12b99859d&__skcy=AweFcpRBGJTcmneDmhDwDsCXpGM=";
    /// 商家 http://api.meituan.com/group/v1/deal/poi/875575?onsale=1&mpt_poiid=875575&offset=0&limit=200&fields=id%2Cslug%2Ccate%2Csubcate%2Cdtype%2Cctype%2Cmlls%2Csolds%2Cstatus%2Crange%2Cstart%2Cend%2Cimgurl%2Csquareimgurl%2Ctitle%2Chotelroomname%2Cprice%2Cvalue%2Cmname%2Cbrandname%2Crating%2Crate-count%2Csatisfaction%2Cmealcount%2Cnobooking%2CattrJson%2ChotelExt%2Ccampaigns%2Cterms%2Crecreason%2Cshowtype%2Cdeposit%2Csecurityinfo%2Coptionalattrs%2Cbookinginfo%2Cpricecalendar%2Cisappointonline%2Ccouponbegintime%2Ccouponendtime%2Crdploc%2Crdcount%2Cdigestion%2CisAvailableToday&client=android&utm_source=qqcpd&utm_medium=android&utm_term=254&version_name=5.5.4&utm_content=864394011605716&utm_campaign=AgroupBgroupC0E877070845972013056_c1Gmerchant&ci=62&uuid=D0CA57CF673B1DF3B9D10A36C085A74C6015FE2FD5D9B1BD74C44F92DF1B9293&msid=8643940116057161513766238616&__skck=09474a920b2f4c8092f3aaed9cf3d218&__skts=1513766861481&__skua=5e4ea32ce0e126b28acd2d6923eb0ff4&__skno=a0e37f9d-0ac0-487a-9ef8-491b8cd16dd8&__skcy=BrThNxubbsMxalCBRTWigIhJjgo%3D 
    ///  </summary>
    public class MeiTuCityBusinessCategoryAppCrawler : ISimpleCrawler
    {

       
        DataOperation dataop = null;
        private CrawlSettings Settings = null;
        LandFangAppHelper appHelper = new LandFangAppHelper();
        private Dictionary<string, string> columnMapDic = new Dictionary<string, string>();
      
        private Hashtable  userCrawlerCountHashTable = new Hashtable();
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private BloomFilter<string> filter;

        private static string DateStr = DateTime.Now.ToString("yyyy-MM");
        private static string _DataTableName = "CityEnterpriseInfo_MT_"+ DateStr;//存储的数据库表明
        List<BsonDocument> landUrlList = new List<BsonDocument>();

        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableName
        {
            get { return _DataTableName; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableNameURL
        {
            get { return _DataTableName + "URL"; }

        }
        public string DataTableNameSpecialURL
        {
            get { return _DataTableName + "SpecialURL"; }

        }
        /// <summary>
        /// 城市信息
        /// </summary>
        public string DataTableNameCity
        {
            get { return "CityInfo_MT"; }

        }
        /// <summary>
        /// 城市信息
        /// </summary>
        public string DataTableNameCityCategory
        {
            get { return "CityCategoryInfo_MT_"+ DateStr; }

        }
        /// <summary>
        /// 城市信息
        /// </summary>
        public string DataTableNameCityCategoryName
        {
            get { return "CityCategoryName_MT"; }

        }
        /// <summary>
        /// 模拟登陆账号
        /// </summary>
        public string DataTableNameAccount
        {
            get { return _DataTableName + "Account"; }

        }
        ///// <summary>
        /////  分类信息
        ///// </summary>
        //public string DataTableNameCategory
        //{
        //    get { return "CategoryInfo_MT"; }

        //}

        /// <summary>
        ///  构造函数
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public MeiTuCityBusinessCategoryAppCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
        }
        public bool isSpecialUrlMode = false;
        /// <summary>
        /// 代理
        /// </summary>
        /// <returns></returns>
        public WebProxy GetWebProxy()
        {
            // 设置代理服务器
            var proxy = new WebProxy();
            proxy.Address = new Uri(string.Format("{0}:{1}", "http://proxy.abuyun.com", "9010"));
            proxy.Credentials = new NetworkCredential("H1538UM3D6R2133P", "511AF06ABED1E7AE");
            return proxy;
        }
        public string GetWebProxyString()
        {
           return string.Format("{0}:{1}@{2}:{3}", "H1538UM3D6R2133P", "511AF06ABED1E7AE", "proxy.abuyun.com", "9010");
        }
        private string __skcy { get; set; }
        private string __skua { get; set; }
        private string __skno { get; set; }
        private string __skck { get; set; }
        private string __skts { get; set; }
        /// <summary>
        /// 获取城市分类url
        /// </summary>
        /// <param name="cityId"></param>
        private string GetCityCatListUrl(string cityId)
        {
            var url = string.Format("http://api.meituan.com/group/v1/poi/cates/showlist?cityId={0}&utm_source=qqcpd&utm_medium=android&utm_term=254&version_name=5.5.4&utm_content=864394010401414&utm_campaign=AgroupBgroupC0E0Gmerchant&ci=57&uuid=D0CA57CF673B1DF3B9D10A36C085A74C7B924190117AF510F9B7717FD432FEE2&msid=8643940104014141484816456943&__skck={1}&__skts={2}&__skua={3}&__skno={4}&__skcy={5}",cityId, __skck, __skts, __skua, __skno, __skcy);
            return url;
        }
        /// <summary>
        /// 是否目录url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private bool isCateListUrl(string url)
        {
            if (url.Contains("http://api.meituan.com/group/v1/poi/cates/showlist"))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 获取城市分类下的商家数据
        /// </summary>
        /// <param name="cityId">城市Id</param>
        /// <param name="catId">分类Id</param>
        /// <param name="limit">一次取多少条</param>
        ///  <param name="parentCatId">父目录Id</param>
        private string GetCityCatEnterpriseListUrl(string cityId,string catId , string limit, string parentCatId="0")
        {
            var url = string.Empty;
            //&mypos=27.99765739075955,104.5658485649246
            if (parentCatId != "0")
            {
                url = string.Format("http://api.meituan.com/group/v1/poi/select/cate/{1}?cityId={0}&sort=smart&coupon=all&mpt_cate1={2}&mpt_cate2={1}&offset=0&limit={3}&fields=phone%2CmarkNumbers%2CcityId%2Caddr%2Clng%2ChasGroup%2CsubwayStationId%2Ccates%2CfrontImg%2CchooseSitting%2Cwifi%2CavgPrice%2Cstyle%2CfeatureMenus%2CavgScore%2Cname%2CparkingInfo%2Clat%2CcateId%2Cintroduction%2CshowType%2CareaId%2CdistrictId%2Cpreferent%2ClowestPrice%2CcateName%2CareaName%2CzlSourceType%2CcampaignTag%2CmallName%2CmallId%2CbrandId%2Cktv%2Cgeo%2ChistoryCouponCount%2Crecommendation%2CiUrl%2CisQueuing%2CpayInfo%2CsourceType%2Cabstracts%2CgroupInfo%2CisSuperVoucher%2Cdiscount&utm_source=qqcpd&utm_medium=android&utm_term=254&version_name=5.5.4&utm_content=864394010401414&utm_campaign=AgroupBgroupC0E7277946828438499584_c4Gmerchant&ci=57&uuid=D0CA57CF673B1DF3B9D10A36C085A74C7B924190117AF510F9B7717FD432FEE2&msid=8643940104014141484816456943&__skck=09474a920b2f4c8092f3aaed9cf3d218&__skts={4}&__skua={5}&__skno={6}&__skcy={7}",  cityId, catId,parentCatId,limit, __skck, __skts, __skua, __skno, __skcy);
            }
            else
            {
                url = string.Format("http://api.meituan.com/group/v1/poi/select/cate/{1}?cityId={0}&sort=smart&coupon=all&offset=0&limit={2}&fields=phone%2CmarkNumbers%2CcityId%2Caddr%2Clng%2ChasGroup%2CsubwayStationId%2Ccates%2CfrontImg%2CchooseSitting%2Cwifi%2CavgPrice%2Cstyle%2CfeatureMenus%2CavgScore%2Cname%2CparkingInfo%2Clat%2CcateId%2Cintroduction%2CshowType%2CareaId%2CdistrictId%2Cpreferent%2ClowestPrice%2CcateName%2CareaName%2CzlSourceType%2CcampaignTag%2CmallName%2CmallId%2CbrandId%2Cktv%2Cgeo%2ChistoryCouponCount%2Crecommendation%2CiUrl%2CisQueuing%2CpayInfo%2CsourceType%2Cabstracts%2CgroupInfo%2CisSuperVoucher%2Cdiscount&utm_source=qqcpd&utm_medium=android&utm_term=254&version_name=5.5.4&utm_content=864394010401414&utm_campaign=AgroupBgroupC0E7277946828438499584_c4Gmerchant&ci=57&uuid=D0CA57CF673B1DF3B9D10A36C085A74C7B924190117AF510F9B7717FD432FEE2&msid=8643940104014141484816456943&__skck=09474a920b2f4c8092f3aaed9cf3d218&__skts={3}&__skua={4}&__skno={5}&__skcy={6}", cityId, catId, limit, __skck, __skts, __skua, __skno, __skcy);
            }
             return url;
        }
        /// <summary>
        /// 是否商家目录url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private bool isEnterpriseListUrl(string url)
        {
            if (url.Contains("http://api.meituan.com/group/v1/poi/select/cate"))
            {
                return true;
            }
            return false;
        }
        List<BsonDocument> allHitCityList = new List<BsonDocument>();
        List<BsonDocument> allCityCategoryNameList = new List<BsonDocument>();
     
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {

            allCityCategoryNameList = dataop.FindAll(DataTableNameCityCategoryName).ToList();
            //种子地址需要加布隆过滤

            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            //var ipProxyList = dataop.FindAllByQuery("IPProxy", Query.NE("status", "1")).ToList();
            // Settings.IPProxyList.AddRange(ipProxyList.Select(c => new IPProxy(c.Text("ip"))).Distinct());
            // Settings.IPProxyList.Add(new IPProxy("1.209.188.180:8080"));
            Settings.IgnoreSucceedUrlToDB = true;
            Settings.ThreadCount = 1;
            Settings.DBSaveCountLimit = 1;
            Settings.UseSuperWebClient = true;
            //Settings.CurWebProxy = GetWebProxy();
            Settings.ContentType = "application/x-www-form-urlencoded";
            this.Settings.UserAgent = "AiMeiTuan /samsung-4.4.2-GT-I9300-900x1440-320-5.5.4-254-864394010401414-qqcpd";
            Settings.hi = new HttpInput();
            HttpManager.Instance.InitWebClient(Settings.hi, true, 30, 30);
            if (!string.IsNullOrEmpty(Settings.CurWebProxyString))
            {
                Settings.hi.CurlObject.SetOpt(LibCurlNet.CURLoption.CURLOPT_PROXY, Settings.CurWebProxyString);
            }
            var headSetDic = new Dictionary<string, string>();
            __skcy="CSJl8p2O4tbR2VGkjdZ3Kxs2Igo=";
            __skua = "4eb0ecaa0317917e9556ee7cc8082100";
            __skno = "ed144add-29a8-4fac-bec9-5bce189c29ed";
            __skck = "09474a920b2f4c8092f3aaed9cf3d218";
            __skts = "1484303621395";
            Settings.hi.HeaderSet("Accept-Encoding", "gzip");
            Settings.hi.HeaderSet("__skcy", __skcy);
            Settings.hi.HeaderSet("__skua", __skua);
            Settings.hi.HeaderSet("__skno", __skno);
            Settings.hi.HeaderSet("__skck", __skck);
            Settings.hi.HeaderSet("__skts", __skts);
            //Settings.SimulateCookies = "JSESSIONID=1jzs29iilbldmqq0hye30umzj";
            Settings.HeadSetDic = headSetDic;
            //date=&end_date=&title=&content=&key=%E5%85%AC%E5%8F%B8&database=saic&search_field=all&search_type=yes&page=2
             
            Console.WriteLine("正在获取城市数据");
            //rank 为S A B C D E F G
            //allHitCityList = dataop.FindAllByQuery(DataTableNameCity, Query.EQ("rank", "S")).SetFields("cityId", "rank", "name").ToList();
            allHitCityList = dataop.FindAll(DataTableNameCity).SetFields("cityId", "rank", "name").ToList();
            Console.WriteLine("待处理数据{0}个", allHitCityList.Count);

            foreach (var cityObj in allHitCityList)
            {
                var catUrl = GetCityCatListUrl(cityObj.Text("cityId"));
                UrlQueue.Instance.EnQueue(new UrlInfo(catUrl) { Depth = 1 });
            }
            Console.WriteLine("正在加载账号数据");


            //Settings.HrefKeywords.Add(string.Format("/market/"));//先不加其他的

            //Settings.HrefKeywords.Add(string.Format("data/land/_________0_"));//先不加其他的
            ////是否guid
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

        private static string GetGuidFromUrl(string url)
        {
            var beginStrIndex = url.LastIndexOf("/");
            var endStrIndex = url.LastIndexOf(".");
            if (beginStrIndex != -1 && endStrIndex != -1)
            {
                if (beginStrIndex > endStrIndex)
                {
                    var temtp = beginStrIndex;
                    beginStrIndex = endStrIndex;
                    endStrIndex = temtp;
                }
                var queryStr = url.Substring(beginStrIndex + 1, endStrIndex - beginStrIndex - 1);
                return queryStr;
            }
            return string.Empty;
        }
        private Hashtable cityCatHashTable = new Hashtable();

        private string GetJsonValue(JToken json, string elemName)
        {
            if (json[elemName] == null) return string.Empty;
            return json[elemName].ToString();
        }
        /// <summary>
        /// 分类目录获得
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private void CategoryJsonDataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            JObject jsonObj = JObject.Parse(hmtl);
            var curCityId = GetUrlParam(args.Url, "cityId");
            var cityObj = allHitCityList.Where(c => c.Text("cityId") == curCityId).FirstOrDefault();
            if (cityObj == null)
            {
                Console.WriteLine("城市不存在");
                return;
            }
            var curCityList = new List<BsonDocument>();
            var data = jsonObj["data"];
            if (data != null)
            {
                foreach (var catObj in data.ToList())
                {
                   //if (GetJsonValue(catObj, "name").Contains("全部")) continue;
                    var curCatDoc = new BsonDocument();
                    curCatDoc.Add("id", GetJsonValue(catObj, "id"));
                    curCatDoc.Add("index", GetJsonValue(catObj, "index"));
                    curCatDoc.Add("parentID", GetJsonValue(catObj, "parentID"));
                    curCatDoc.Add("count", GetJsonValue(catObj, "count"));
                    curCatDoc.Add("name", GetJsonValue(catObj, "name"));
                    curCatDoc.Add("level", "1");
                    curCatDoc.Add("cityName", cityObj.Text("name"));
                    curCatDoc.Add("cityId", cityObj.Text("cityId"));
                    if (catObj["list"] != null)
                    {
                        curCatDoc.Add("hasChildeNode", "1");
                        var subCatObjList = catObj["list"].ToList();
                        foreach (var subCatObj in subCatObjList)
                        {
                            //if (catObj["name"].Contains("全部")) continue;
                            var subCatDoc = new BsonDocument();
                            subCatDoc.Add("id", GetJsonValue(subCatObj, "id"));
                            subCatDoc.Add("index", GetJsonValue(subCatObj, "index"));
                            subCatDoc.Add("parentID", GetJsonValue(subCatObj, "parentID"));
                            subCatDoc.Add("count", GetJsonValue(subCatObj, "count"));
                            subCatDoc.Add("name", GetJsonValue(subCatObj, "name"));
                            subCatDoc.Add("level", "2");
                            subCatDoc.Add("cityName", cityObj.Text("name"));
                            subCatDoc.Add("cityId", cityObj.Text("cityId"));
                            curCityList.Add(subCatDoc);
                        }
                    }
                    curCityList.Add(curCatDoc);
                }
                cityCatHashTable.Add(curCityId, curCityList);

            }
            ////获取叶子节点
            //var leafNodeList = curCityList.Where(c => c.Int("hasChildeNode") == 0).ToList();
            //foreach (var hitCatObj in leafNodeList)
            //{
            //    //生成爬取详细信息链接
            //    var hitEnterpriseUrl = GetCityCatEnterpriseListUrl(curCityId, hitCatObj.Text("id"), hitCatObj.Text("count"), hitCatObj.Text("parentID"));
            //    UrlQueue.Instance.EnQueue(new UrlInfo(hitEnterpriseUrl) { Depth = 1 });
            //}
            foreach (var catDoc in curCityList)
            {
                var query = Query.And(Query.EQ("cityId", curCityId), Query.EQ("id", catDoc.Text("id")));
                var hitCount = dataop.FindCount(DataTableNameCityCategory, query);
                if (hitCount <= 0)
                {
                    if (catDoc.Text("name").Contains("?"))
                    {
                        var hitCatObj = allCityCategoryNameList.Where(c => c.Text("id") == catDoc.Text("id")).FirstOrDefault();
                        if (hitCatObj != null)
                        {
                            catDoc.Set("name", hitCatObj.Text("name"));
                        }
                    }
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = catDoc, Name = DataTableNameCityCategory, Query = query, Type = StorageType.Insert });
                }
                else
                {
                  //  DBChangeQueue.Instance.EnQueue(new StorageData() { Document = catDoc, Name = DataTableNameCityCategory, Query = Query.EQ("cityId", cityId), Type = StorageType.Update });
                }
            }
            Console.WriteLine("获取分类{0}", curCityList.Count);
       }
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// <sParcelID><![CDATA[de6aebb8-d7c7-4067-8c73-12eb0836b1ae]]></sParcelID><sParcelName><![CDATA[松山湖金多港]]></sParcelName>
        ///{{ "sParcelID" : "de6aebb8-d7c7-4067-8c73-12eb0836b1ae", "sParcelName" : "松山湖金多港", "sParcelSN" : "2014WT038", "sParcelArea" : "广东省", "sParcelAreaCity" : "东莞市", "sParcelAreaDis" : "", "fparcelarea" : "35916.27㎡", "fcollectingarea" : "暂无", "fbuildarea" : "35916.27㎡", "fplanningarea" : "71832.54㎡", "sPlotratio" : "≤2", "sremiseway" : "挂牌", "sservicelife" : "50年", "sparcelplace" : "松山湖金多港", "sparcelextremes" : "松山湖金多港", "sConforming" : "其它用地", "sdealstatus" : "中止交易", "istartdate" : "2014-07-02", "ienddate" : "2014-07-16", "dAnnouncementDate" : "2014-06-12", "finitialprice" : "2874.00万元", "sbidincrements" : "50万元", "sperformancebond" : "750万元", "fInitialFloorPrice" : "400.10元/㎡", "stransactionsites" : "东莞市国土资源局", "sconsulttelephone" : "076926983723", "fcoordinateax" : "", "fcoordinateay" : "", "mapurl" : "https://api.map.baidu.com/staticimage?markers=&width=500&height=500&zoom=12&scale=1", "Land_fAvgPremiumRate" : "暂无", "Land_sParcelMemo" : "暂无", "Land_sTransferee" : "暂无", "Land_fInitialUnitPrice" : "800.19万元", "icompletiondate" : "1900-01-01", "fclosingcost" : "0.00万元", "fprice" : "0.00元/㎡", "sGreeningRate" : "暂无", "Land_sCommerceRate" : "暂无", "Land_sBuildingDensity" : "≤35", "Land_sLimitedHeight" : "暂无", "Land_bIsSecurityHousing" : "无", "sAnnouncementNo" : "WGJ2014050", "readcount" : "12", "isread" : "1", "isfavorite" : "0", "sImages" : "", "sImages_o" : "", "usertype" : "", "message" : "了解房企拿地状况，地块项目进展等信息，请加入数据库会员，更多专享服务为您量身打造！" }}
        ///  </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
          
           
            if (isCateListUrl(args.Url))
            {
                CategoryJsonDataReceive(args);
                return;
            }
            if (isEnterpriseListUrl(args.Url))
            {


            }
           
            //var updateBson = new BsonDocument();
            //var cityId = GetUrlParam(args.Url, "cityId");
            //if (string.IsNullOrEmpty(cityId)) return;
            //updateBson.Add("detailInfo", args.Html);
            //updateBson.Add("isUpdated", "1");//更新了数据
            //Console.WriteLine(string.Format("{0}更新",  cityId));
            ////updateBson.Set("url", hitUrl);
            //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBson, Name = DataTableName, Query = Query.EQ("cityId", cityId), Type = StorageType.Update });


        }

        /// <summary>
        /// 获取url对应查询参数
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetUrlParam(string url, string name)
        {
            var queryStr = GetQueryString(url);
            var dic = HttpUtility.ParseQueryString(queryStr);
            var industryCode = dic[name] != null ? dic[name].ToString() : string.Empty;//行业代码
            return industryCode;
        }


        /// <summary>
        /// 获取url对应查询参数
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetQueryString(string url)
        {
            var queryStrIndex = url.IndexOf("?");
            if (queryStrIndex != -1)
            {
                var queryStr = url.Substring(queryStrIndex + 1, url.Length - queryStrIndex - 1);
                return queryStr;
            }
            return string.Empty;
        }


        public string GetXYValue(int startIndex, int allLength, string html)
        {
            var hitResult = new StringBuilder();
            if (startIndex >= allLength) return string.Empty;
            var curChart = html[++startIndex];
            while (curChart != '"')
            {
                hitResult.AppendFormat(curChart.ToString());
                if (++startIndex < allLength)
                {
                    curChart = html[startIndex];
                }
                else
                {
                    break;
                }
            }
            return hitResult.ToString();
        }
        public string ValeFix(string str)
        {
            return str.Replace("\n", "").Replace("\r", "").Trim();
        }

        public string GetInnerText(HtmlNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.InnerText)) { throw new NullReferenceException(); }
            return node.InnerText;
        }

        public string[] GetStrSplited(string str)
        {
            var strArr = str.Split(new string[] { ":", "：" }, StringSplitOptions.RemoveEmptyEntries);
            return strArr;
        }
        /// <summary>
        /// IP限定处理，ip被限制 账号被限制跳转处理
        /// </summary>
        /// <param name="args"></param>
        public bool IPLimitProcess(DataReceivedEventArgs args)
        {
            if (args.Html.Contains("Object moved"))//需要编写被限定IP的处理
            {
                return true;
            }

            var html = args.Html;

            if (isCateListUrl(args.Url))
            {
                if (html.Contains("data"))
                {
                    return false;
                }
            }
            if (isEnterpriseListUrl(args.Url))
            {
                if (html.Contains("data"))
                {
                    return false;
                }
                
            }
            return true;
        }
        /// <summary>
        /// url处理,是否可添加
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool CanAddUrl(AddUrlEventArgs args)
        {

            return true;
        }

        /// <summary>
        /// void错误处理
        /// </summary>
        /// <param name="args"></param>
        public void ErrorReceive(CrawlErrorEventArgs args)
        {


        }

        /// <summary>
        /// 模拟登陆，ip代理可能需要用到
        /// </summary>
        /// <returns></returns>
        public bool SimulateLogin()
        {
            return true;
#pragma warning disable CS0162 // 检测到无法访问的代码
            if (Settings.LandFangIUserId == 0)
#pragma warning restore CS0162 // 检测到无法访问的代码
            {
                var hitAccount = dataop.FindOneByQuery(DataTableNameAccount, Query.EQ("userName", "savegod523"));
                if (hitAccount != null)
                {
                    Settings.LandFangIUserId = hitAccount.Int("LandFangIUserId");
                }
                if (Settings.LandFangIUserId == 0)
                {
                    Settings.LandFangIUserId = 42638;//初始化
                }
            }
            // Settings.LandFangIUserId = Settings.LandFangIUserId + 1;
            Settings.LandFangIUserId = new Random().Next(3333, 143630);
            Settings.MaxAccountCrawlerCount = new Random().Next(50,200);
            DBChangeQueue.Instance.EnQueue(new StorageData()
            {
                Name = DataTableNameAccount,
                Document = new BsonDocument().Add("LandFangIUserId", Settings.LandFangIUserId.ToString()),
                Query = Query.EQ("userName", "savegod523"), Type=StorageType.Update
            });
            StartDBChangeProcess();
            return true;
             
        }

        /// <summary>
        /// ip无效处理
        /// </summary>
        private void IPInvalidProcess(IPProxy ipproxy)
        {
           
        }

        /// <summary>
        /// 对需要更新的队列数据更新操作进行批量处理,可考虑异步执行
        /// </summary>
        private void StartDBChangeProcess()
        {

            List<StorageData> updateList = new List<StorageData>();
            while (DBChangeQueue.Instance.Count > 0)
            {
                var curStorage = DBChangeQueue.Instance.DeQueue();
                if (curStorage != null)
                {
                    updateList.Add(curStorage);
                }
            }
            if (updateList.Count() > 0)
            {
                var result = dataop.BatchSaveStorageData(updateList);
                if (result.Status != Status.Successful)//出错进行重新添加处理
                {
                    foreach (var storageData in updateList)
                    {
                        DBChangeQueue.Instance.EnQueue(storageData);
                    }
                }
            }



        }
    }

}
