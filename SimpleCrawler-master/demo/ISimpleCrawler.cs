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
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DataRule;

namespace SimpleCrawler.Demo
{
    /// <summary>
    /// 事务执行接口，不同的事务对象都继承该接口
    /// </summary>
    public interface ISimpleCrawler
    {
        void SettingInit();//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        void DataReceive(DataReceivedEventArgs args);//数据接收处理
        bool CanAddUrl(AddUrlEventArgs args);//url处理
        void ErrorReceive(CrawlErrorEventArgs args);//void错误处理
        bool IPLimitProcess(DataReceivedEventArgs args);//Ip被限定处理，需要编写IP被限制的规则
        string DataTableName { get; }//数据表明存储名
        string DataTableNameURL { get; }//数据表URL明存储名
        bool SimulateLogin();//模拟登陆
  
    }

    /// <summary>
    /// 事务对象工厂
    /// </summary>
    public class SimpleCrawlerFactory
    {
        private static SimpleCrawlerFactory _instance = new SimpleCrawlerFactory();
        /// <summary>
        /// 返回工厂实例
        /// </summary>
        public static SimpleCrawlerFactory Instance
        {
            get { return _instance; }
        }

        /// <summary>
        /// 创建具体事务对象
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public ISimpleCrawler Create(string Name, CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            ISimpleCrawler myExecuteTran = null;
            try
            {
                Type type = Type.GetType(Name, true);
                myExecuteTran = (ISimpleCrawler)Activator.CreateInstance(type, _Settings, _filter, _dataop);
            }
            catch (TypeLoadException e)
            {

            }
            return myExecuteTran;
        }
     
           
  }
}
