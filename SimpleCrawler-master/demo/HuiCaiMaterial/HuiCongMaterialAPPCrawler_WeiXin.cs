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
using System.Web.UI.WebControls;
using Helper;

namespace SimpleCrawler.Demo
{
    /// <summary>
    /// https://esapi.org.hc360.com/interface/getinfos.html?pnum=1&psize=10&sys=tencent&kwd=%E9%98%B2%E7%81%AB%E9%97%A8&z=%E4%B8%AD%E5%9B%BD&index=basebusininfo&collapsef=providerid
    /// 
    ///  </summary>
    public class HuiCongMaterialAPPCrawler_WeiXin : SimpleCrawlerBase
    {

        
        
         
        private Dictionary<string, string> columnMapDic = new Dictionary<string, string>();
      
        private Hashtable  userCrawlerCountHashTable = new Hashtable();
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
       
        private BloomFilter<string> guidFilter;
      
       
 
        /// <summary>
        ///  构造函数
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public HuiCongMaterialAPPCrawler_WeiXin(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop):base(_Settings, _filter, _dataop)
        {
            DataTableName = "Material_HuiCong_WLM";//材料库
            DataTableCategoryName = "Material_Supplier_HuiCong_WLM";//材料供应商
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "guid";
            guidFilter = new BloomFilter<string>(9000000);
        }
        public bool isSpecialUrlMode = false;
      
        //家装建材 安防消防， 电子元件
        int pageSize = 200;//24
        int pageBeginNum = 1;
        // string materialUrl = "http://z.hc360.com/getmmtlast.cgi?dt=1&w={0}&v=59&e={1}&c=供应信息&n={2}&m=2&H=1&bt=0";
        //string materialUrl = "http://z.hc360.com/getmmtlast.cgi?dt=1&e={1}&c=供应信息&a=13&n={2}&m=2&H=1&fc=0&bt=0&w={0}&v=60&t=1";
        string materialUrl = "https://esapi.org.hc360.com/interface/getinfos.html?pnum={2}&psize={1}&sys=tencent&kwd={0}&z={3}&index=basebusininfo&collapsef=providerid";
        //将z.hc360改成 s.hc360 可用

        override
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {

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
             
            //Settings.CurWebProxy = GetWebProxy();
            Settings.ContentType = "application/x-www-form-urlencoded;charset=utf8;";
            this.Settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 MicroMessenger/7.0.12(0x17000c2d) NetType/WIFI";


            Settings.Referer = "esapi.org.hc360.com";


            var headSetDic = new Dictionary<string, string>();
            headSetDic.Add("Accept-Encoding", "br, gzip, deflate");
            //headSetDic.Add("Host", "esapi.org.hc360.com");
            
            Settings.HeadSetDic = headSetDic;
            //date=&end_date=&title=&content=&key=%E5%85%AC%E5%8F%B8&database=saic&search_field=all&search_type=yes&page=2
          
            Console.WriteLine("请输入关键字以逗号隔开,输入1代表从数据库读取");

            var _MatDataop = MongoOpCollection.Get38MongoOp_Old("PublicMat");
            var hitCateList = _MatDataop.FindAll("Material_Category", Query.EQ("nodeLevel", "2")).ToList();
            //var keyWordStr = Console.ReadLine();
            //if (string.IsNullOrEmpty(keyWordStr))
            //{
            //    //防水涂料50000中断开始
            //    keyWordStr = "钢木入户门,木质入户门,木质防火门,钢质防火门,弹性面漆,非弹性面漆,防水涂料,外墙隔热面漆,外墙弹性中层涂料,外墙浮雕中层,水性外墙底漆,水性外墙底漆（弹性专用）,外墙柔性腻子,真石漆,质感涂料,多彩漆,罩面清漆,外墙面砖,铝包木复合门窗,断桥铝门窗,铁艺栏杆,玻璃栏杆,百叶,耐力板组装式雨棚,铝合金雨棚,PC板材(阳光板、耐力板)雨棚,全钢结构雨棚,玻璃钢结构雨棚,EPS构件,GRC构件,挤塑板,模压型聚苯乙烯泡沫塑料,泡沫玻璃,泡沫混凝土,聚苯颗粒保温砂浆,无机保温砂浆,硬泡聚氨酯保温板";
            //    keyWordStr += "玻化砖,微晶石,釉面砖,大理石,人造石,花岗岩,玄关柜，水性外墙底漆,百叶,GRC构件,挤塑板,模压型聚苯乙烯泡沫塑料,泡沫玻璃,泡沫混凝土,聚苯颗粒保温砂浆,无机保温砂浆,硬泡聚氨酯保温板,衣柜,橱柜,浴室柜,踢脚线,洗手盆,洗衣盆,浴缸,坐便器,小便斗,拖把池,厨卫龙头,台盆龙头,洗衣机龙头,浴缸龙头,拖把池龙头,厨盆,花洒,淋浴柱,地漏,毛巾架,置物架,淋浴屏,PVC墙纸,软包,无纺布,纯纸,墙布,排气扇,吸顶灯,射灯,筒灯,水晶吊灯,灯管,条形铝扣板,方形铝扣板,实木复合木地板,实木地板,金刚板,抽油烟机,燃气灶,消毒柜,热水器,微波炉,空调,浴霸,正压式新风系统,负压式新风系统,可视对讲,室内涂料,光纤入户箱,户内配电箱,开关插座,内墙涂料,功能内墙涂料,内墙弹性涂料,内墙底漆,内墙耐水腻子";
                
            //}
            //var keyWordList = keyWordStr.Split(new string[] { ",", "、" }, StringSplitOptions.RemoveEmptyEntries);

            var allCountryCodeList = QuickMethodHelper.Instance().GetEnterpriseCountyCode();
            var filterKeyWord = new string[] { "香港", "澳门", "全国" };
            var specialCitys = new string[] { "上海", "北京", "天津", "重庆" };
            var allProvinceList = allCountryCodeList.Where(c => c.Text("type") == "0"&&!filterKeyWord.Any(d=> c.Text("name").Contains(d))).ToList();
            var allCityList = allCountryCodeList.Where(c => c.Text("type") == "1").ToList();
            foreach (var catObj in hitCateList)
            {
                var keyWord = catObj.Text("name");
                var categoryId = catObj.Text("categoryId");
                foreach (var province in allProvinceList) {
                    var provinceName = province.Text("name");
                    var cityStr = $"中国:{provinceName}";
                    var curUrl = string.Format(materialUrl, keyWord.EncodeUrl(), pageSize, pageBeginNum, cityStr.EncodeUrl());

                    if (!filter.ContainsAdd(curUrl))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { Depth = 1, UniqueKey = province.Text("code"), extraData= categoryId });
                    }
                   
                }
            }
            
           

             
            Console.WriteLine("正在加载账号数据");

 
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
         
        override
        /// <summary>
        /// http://z.hc360.com/getmmtlast.cgi?dt=1&w=单开&v=59&e=1&c=供应信息&n=2&m=2&H=1&bt=0
        ///  </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            JObject jsonObj = JObject.Parse(args.Html);
            var curPageSize = GetUrlParam(args.Url, "psize");//每页个数默认200
            var catName=HttpUtility.UrlDecode(GetUrlParam(args.Url, "kwd"));//每页个数默认24
            var curRecordIndexStr = GetUrlParam(args.Url, "pnum");//当前序号
           
            var curRecordIndex =int.Parse(curRecordIndexStr);
         
            var allRecordCount = jsonObj["recordCount"];//总记录个数
            var beginPageIndex = int.Parse(jsonObj["beginPageIndex"].ToString());//总记录个数
            var endPageIndex = int.Parse(jsonObj["endPageIndex"].ToString());//总记录个数
            var searchResult = jsonObj["recordList"];//当前记录个数

            var provinceCode = args.urlInfo.UniqueKey;
            var categoryId= args.urlInfo.extraData;
            foreach (var item in searchResult)
            {
                var bsonDoc = item.ToString().GetBsonDocFromJson();
                var id = bsonDoc.Text("id");
                var bcid = bsonDoc.Text("bcid");
                bsonDoc.Set("catName", catName);
                if (!string.IsNullOrEmpty(id)) {
                    bsonDoc.Set("guid", id);
                }
                else
                {
                    bsonDoc.Set("guid", bcid);
                }
                
                bsonDoc.Set("provinceCode", provinceCode);
                //AddData(bsonDoc);
                PushData(bsonDoc);

                //获取供应商ID
                var providerId = bsonDoc.Text("providerId");
                var providerTablename = DataTableCategoryName;
                var providerDoc=new BsonDocument();
                providerDoc.Set("guid", providerId);
                PushData(bsonDoc,dataTable: providerTablename);
            }
            
            ShowStatus();
            Console.WriteLine(HttpUtility.UrlDecode(args.Url));
            if (curRecordIndex==1&& beginPageIndex< endPageIndex)
            {
                InitNextUrl(args.urlInfo,catName,  curRecordIndex, endPageIndex, pageSize);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="allRecordCount"></param>
        /// <param name="curNum"></param>
        /// <returns></returns>
        private void InitNextUrl(UrlInfo urlInfo,string keyWord,int curRecordIndex, int endPageIndex,  int pageSize)
        {
            
            while (curRecordIndex < endPageIndex)
           {
                curRecordIndex = curRecordIndex + 1;
                var curUrl = ReplaceUrlParam(urlInfo.UrlString, "pnum", curRecordIndex.ToString());
                urlInfo.UrlString = curUrl;
                if (!filter.ContainsAdd(curUrl)) {
                    UrlQueue.Instance.EnQueue(urlInfo);
                }
                
                //return;
            }
        }

        /// <summary>
        /// IP限定处理，ip被限制 账号被限制跳转处理
        /// </summary>
        /// <param name="args"></param>
        override
        public bool IPLimitProcess(DataReceivedEventArgs args)
        {
            try
            {


                if (args.Html.Contains("recordList"))//需要编写被限定IP的处理
                {
                    return false;
                }
                else
                {
                    Console.WriteLine(args.Url);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return true;
            }
        }

    }

}
