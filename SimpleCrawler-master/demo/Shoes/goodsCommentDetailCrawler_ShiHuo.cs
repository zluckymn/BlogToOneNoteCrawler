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

namespace SimpleCrawler.Demo
{

    /// <summary>
    /// 识别货物
    /// https://www.shihuo.cn/app_swoole_zone/getDgComment?platform=ios&timestamp=1575460800000&v=6.6.5&token=97fe6e57bc6fd56193a36ebed21b88ea&id=977&level=2&page=1&page_size=20&tag_id=
    ///
    /// </summary>
    public class goodsCommentDetailCrawler_ShiHuo : SimpleCrawlerBase
    {


#pragma warning disable CS0414 // 字段“goodsCommentDetailCrawler_ShiHuo.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“goodsCommentDetailCrawler_ShiHuo.isUpdate”已被赋值，但从未使用过它的值
        const int takeCount = 20;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public goodsCommentDetailCrawler_ShiHuo(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "GoodsComment_ShiHuo";//房间
            DataTableCategoryName = "Goods_App_ShiHuo";//商品
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "";
        }
        List<BsonDocument> allHitObjList;
        public void initialUrl(int pageIndex=1)
        {
            allHitObjList = FindDataForUpdate(dataTableName: DataTableCategoryName, fields:new string[] { "guid","baseCatId","secCatId","curCatName","curBrandName"});
            //初始化布隆过滤器
            foreach (var hitObj in allHitObjList)
            {
                var guid = hitObj.Text("guid");
               // var curUrl = $"https://www.shihuo.cn/app_swoole_zone/getDgComment?platform=ios&timestamp={GetTimeStamp()}&v=6.6.5&token=491916154a65d6e239dba8a97fb73788&id={guid}&level=2&page={pageIndex}&page_size={takeCount}&tag_id=";
                var curUrl = $"https://www.shihuo.cn/app_swoole_zone/getComment?platform=ios&timestamp={GetTimeStamp()}&v=6.6.5&token=354157d151d97e0757ac0ab05f57c140&id={guid}&page={pageIndex}&page_size={takeCount}&sort=all&tag_id=0";

                GenerateTokenUrl(hitObj, curUrl, pageIndex);
            }
        }
        public void GenerateTokenUrl(BsonDocument hitObj, string curUrl, int pageIndex=1)
        {
            curUrl = ReplaceUrlParam(curUrl, "page", pageIndex.ToString());//页码替换
            var token = TokenGenerateHelper.TokenGenerater_ShiHuo(curUrl);
            curUrl = ReplaceUrlParam(curUrl, "token", token);//token替换
            if (!filter.Contains(curUrl))
            {
                UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { UniqueKey = hitObj.ToJson() });
                filter.Add(curUrl);// 防止执行2次
            }
        }

        override
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //种子地址需要加布隆过滤
            //Settings.Depth = 4;
            //代理ip模式
            //种子地址需要加布隆过滤
            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.ThreadCount = 1;
            Settings.MaxReTryTimes = 5;
            Settings.ContentType = "application/json";
            Settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 MicroMessenger/7.0.5(0x17000523) NetType/WIFI Language/zh_CN";
            Settings.Accept = "*/*";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "br, gzip, deflate");
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            initialUrl();
            base.SettingInit();



        }
#pragma warning disable CS0414 // 字段“goodsCommentDetailCrawler_ShiHuo.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“goodsCommentDetailCrawler_ShiHuo.noCountTimes”已被赋值，但从未使用过它的值
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            JObject jsonObj = GetJsonObject(hmtl);
            var result = jsonObj["data"];
            var items = result["comments"];
            var catInfo = GetBsonDocument(args.urlInfo.UniqueKey);
            var pageStr = GetUrlParam(args.Url, "page");
             
            if (items != null)
            {
                if (items.Count() <= 0)
                {
                    ShowMessage($"无数据{args.urlInfo.PostData}");
                    return;
                }
                // "guid","curSeriesName","curCatName","curBrandName"
                foreach (var item in items)
                {
                    var bsonDoc = GetBsonDocument(item);
                    var guid = $"{bsonDoc.Text("content")}{bsonDoc.Text("intro")}{bsonDoc.Text("product_id")}".GetHashCode().ToString();
                    bsonDoc.Set("guid", guid);
                    bsonDoc.Set("goods_id", catInfo.Text("guid"));
                    bsonDoc.Set("baseCatId", catInfo.Text("baseCatId"));
                    bsonDoc.Set("secCatId", catInfo.Text("secCatId"));
                    bsonDoc.Set("curCatName", catInfo.Text("curCatName"));
                    bsonDoc.Set("type", catInfo.Text("goods_app"));
                    bsonDoc.Set("curBrandName", catInfo.Text("curBrandName"));
                    PushData(bsonDoc);
                }
            }

            if (items.Count() < takeCount || UrlQueue.Instance.Count == 0)
            {
                ShowMessage($"已到最后一行{items.Count()}");
                //取出第二个分类，并更新
                //initialUrl(catInfo, 1)
                var updateDoc = new BsonDocument();
                updateDoc.Set("guid", catInfo.Text("guid"));
                updateDoc.Set(updatedField, updatedValue);
                UpdateData(updateDoc, dataTable: DataTableCategoryName);
                return;
            }

            if (pageStr != "")
            {

                if (int.TryParse(pageStr, out int pageIndex))
                {

                    GenerateTokenUrl(catInfo, args.Url, pageIndex + 1);
                    ShowMessage($"获取{pageIndex}页数据并初始化成功");
                }
                else
                {
                    ShowMessage($"转换{pageIndex}出错1");
                }

            }


            ShowStatus();
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
                JObject jsonObj = GetJsonObject(args.Html);
                var result = jsonObj["msg"];

                if (result.ToString() == "ok")//需要编写被限定IP的处理
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
