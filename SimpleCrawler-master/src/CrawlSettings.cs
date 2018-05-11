// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CrawlSettings.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The crawl settings.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleCrawler
{
    using OpenQA.Selenium.PhantomJS;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    public enum EnumCrawlMode
    {
        /// <summary>
        /// httpHelper
        /// </summary>
        HttpHelper=0,
        /// <summary>
        /// supperWebClient
        /// </summary>
        SuperWebClient=1,
        /// <summary>
        /// Selenium通信PhantomJs
        /// </summary>
        PhantomJsViaSelenium =2
    }
    /// <summary>
    /// The crawl settings.
    /// </summary>
    [Serializable]
    public class CrawlSettings
    {
        #region Fields

        /// <summary>
        /// The depth.
        /// </summary>
        private byte depth = 3;

        /// <summary>
        /// The lock host.
        /// </summary>
        private bool lockHost = true;

        /// <summary>
        /// The thread count.
        /// </summary>
        private byte threadCount = 1;

        /// <summary>
        /// The timeout.
        /// </summary>
        private int timeout = 15000;

        private string _loginAccount = string.Empty;
      
       
        /// <summary>
        /// The user agent.
        /// </summary>
        private string userAgent =
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.97 Safari/537.11";

        #endregion

        #region Constructors and Destructors
        /// <summary>
        /// 当前使用的ip
        /// </summary>
        public IPProxy curIPProxy { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="CrawlSettings"/> class.
        /// </summary>
        public CrawlSettings()
        {
            this.AutoSpeedLimit = false;
            this.EscapeLinks = new List<string>();
            this.KeepCookie = true;
            this.HrefKeywords = new List<string>();
            this.LockHost = true;
            this.RegularFilterExpressions = new List<string>();
            this.SeedsAddress = new List<string>();
        }

        #endregion

        #region Public Properties

        public EnumCrawlMode CrawlMode { get; set; }
        /// <summary>
        /// ipProxy list
        /// </summary>
        public List<IPProxy> IPProxyList { get; set; }
        /// <summary>
        /// 达到多少进行保存
        /// </summary>
        public int DBSaveCountLimit { get; set; }

        // <summary>
        /// 是否立即存储
        /// </summary>
        public int DBSaveImediate { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether auto speed limit.
        /// </summary>
        public bool AutoSpeedLimit { get; set; }

        /// <summary>
        /// 设定随机间隔最大毫秒5000
        /// </summary>
        public int AutoSpeedLimitMaxMSecond { get; set; }
        /// <summary>
        /// 设定随机间隔最小毫秒1000
        /// </summary>
        public int AutoSpeedLimitMinMSecond { get; set; }
        /// <summary>
        /// 上一次成功获取的时间
        /// </summary>
        public DateTime LastAvaiableTokenTime { get; set; }
        /// <summary>
        /// 上一次成功获取的时间
        /// </summary>
        public DateTime LastAvaiableTime { get; set; }
        /// <summary>
        /// 上一次成功获取的时间
        /// </summary>
        public DateTime LastLoginTime { get; set; }

        /// 当前url
        /// </summary>
        public string CurUrl { get; set; }

        /// 可用的访问token
        /// </summary>
        public string sign { get; set; }
        /// 可用的访问token
        /// </summary>
        public string timestamp { get; set; }
        /// 可用的访问token
        /// </summary>
        public string AppId { get; set; }
        /// 可用的访问token
        /// </summary>
        public string DeviceId { get; set; }
        /// 可用的访问token
        /// </summary>
        public string AccessToken { get; set; }

        /// 可用的访问refleshToken
        /// </summary>
        public string RefleshToken { get; set; }
        /// <summary>
        /// Gets or sets the depth.
        /// </summary>
        public byte Depth
        {
            get
            {
                return this.depth;
            }

            set
            {
                this.depth = value;
            }
        }

        /// <summary>
        /// Gets the escape links.
        /// </summary>
        public List<string> EscapeLinks { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether keep cookie.
        /// </summary>
        public bool KeepCookie { get; set; }

        /// <summary>
        /// Gets the href keywords.
        /// </summary>
        public List<string> HrefKeywords { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether lock host.
        /// </summary>
        public bool LockHost
        {
            get
            {
                return this.lockHost;
            }

            set
            {
                this.lockHost = value;
            }
        }

        /// <summary>
        /// Gets the regular filter expressions.
        /// </summary>
        public List<string> RegularFilterExpressions { get; private set; }

        /// <summary>
        /// Gets  the seeds address.
        /// </summary>
        public List<string> SeedsAddress { get; private set; }

        /// <summary>
        /// Gets or sets the thread count.
        /// </summary>
        public byte ThreadCount
        {
            get
            {
                return this.threadCount;
            }

            set
            {
                this.threadCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the timeout.
        /// </summary>
        public int Timeout
        {
            get
            {
                return this.timeout;
            }

            set
            {
                this.timeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the user agent.
        /// </summary>
        public string UserAgent
        {
            get
            {
                return this.userAgent;
            }

            set
            {
                this.userAgent = value;
            }
        }

        /// <summary>
        /// Gets or sets the user agent.
        /// </summary>
        public string Referer { get; set; }

        /// <summary>
        /// Gets or sets the user agent.
        /// </summary>
        public Encoding PostEncoding { get; set; }

        /// <summary>
        /// Gets or sets the user agent.
        /// </summary>
        public string LoginAccount
        {
            get
            {
                return this._loginAccount;
            }

            set
            {
                this._loginAccount = value;
            }
        }



        List<SimpleCrawler.LoginAccount> _curAccountList = new List<SimpleCrawler.LoginAccount>();
        /// <summary>
        /// Gets or sets the AllAccountList agent.
        /// </summary>
        public List<SimpleCrawler.LoginAccount> AllAccountList
        {
            get
            {
                return this._curAccountList;
            }

            set
            {
                this._curAccountList = value;
            }
        }
        /// <summary>
        /// 当前webip代理
        /// </summary>
        public WebProxy CurWebProxy { get; set; }
        /// <summary>
        /// 当前webip代理
        /// </summary>
        public string CurWebProxyString { get; set; }
        /// <summary>
        /// Gets  the userId.
        /// </summary>
        public int LandFangIUserId { get; set; }
        /// <summary>
        /// Gets  the simulateCookies.
        /// </summary>
        public string SimulateCookies { get;   set; }
        /// <summary>
        /// Gets  the ContentType.
        /// </summary>
        public string ContentType { get; set; }
        /// <summary>
        /// Gets  the CrawlerClassName.
        /// </summary>
        public string CrawlerClassName { get; set; }

        
        /// <summary>
        /// Gets  the Accept.
        /// </summary>
        public string Accept { get; set; }


        /// <summary>
        /// Gets  the IgnoreSucceedUrlToDB.是否不添加到数据库
        /// </summary>
        public bool IgnoreSucceedUrlToDB { get; set; }

        /// <summary>
        /// Gets  the IgnoreSucceedUrlToDB.是否不添加到数据库
        /// </summary>
        public int MaxReTryTimes { get; set; }

        /// <summary>
        ///每个账号最大可用爬取值
        /// </summary>
        public int MaxAccountCrawlerCount { get; set; }

        /// <summary>
        /// 失败的url是否尝试重新进行
        /// </summary>
        public bool IgnoreFailUrl { get; set; }

        /// <summary>
        /// head设置
        /// </summary>
        public Dictionary<string,string> HeadSetDic{ get; set; }
        /// <summary>
        /// 失败的url是否尝试重新进行
        /// </summary>
        public LibCurlNet.HttpInput hi { get; set; }
        /// <summary>
        /// 是否使用 UseSuperWebClient 
        /// </summary>
        public bool UseSuperWebClient { get; set; }
        /// <summary>
        /// 失败的url是否尝试重新进行
        /// </summary>
        public bool neeedChangeAccount { get; set; }
        /// 设置为无效ip
        /// </summary>
        /// <param name="curIPProxy"></param>
        public void SetUnviableIP(IPProxy _curIPProxy)
        {
            if (_curIPProxy != null)
            {
                var hitIpObj = IPProxyList.Where(c => c.IP == _curIPProxy.IP && c.Port == _curIPProxy.Port&&c.Unavaiable==false).FirstOrDefault();
                if (hitIpObj != null)
                {
                    hitIpObj.Unavaiable = true;
                    curIPProxy = null;
                    GetIPProxy();
                }
                 
            }
        }
        /// <summary>
        /// 随机获取一个IPProxy
        /// </summary>
        /// <returns></returns>
        public IPProxy GetIPProxy()
        {
            if (IPProxyList == null|| IPProxyList.Count()<=0) return null;
                if (curIPProxy != null && curIPProxy.Unavaiable == false)
            {
                return curIPProxy;
            }
            else { 
              
                //添加代理ip列表,随机挑选ip
                var avaiableIpList =  IPProxyList.Where(c => c.Unavaiable == false).ToList();
                if (avaiableIpList.Count() > 0)
                {
                    var rnd = new Random();
                    var index = rnd.Next(0, avaiableIpList.Count() - 1);
                    curIPProxy = avaiableIpList[index];
                     return curIPProxy;
                }
                return null;
            }
        }
        #region 无头浏览器组件对象
        public PhantomJSOptions _options { get; set; }//定义PhantomJS内核参数
        public PhantomJSDriverService _service { get; set; }//定义Selenium驱动配置
        public SeleniumScript script { get; set; }
        /// <summary>
        /// Gets or sets the operation.配合PhantomJs使用
        /// </summary>
        public SeleniumOperation operation { get; set; }
        /// <summary>
        /// 调用前初始化
        /// </summary>
        public void InitPhantomJs()
        {
            this._options = new PhantomJSOptions();//定义PhantomJS的参数配置对象
            this._service = PhantomJSDriverService.CreateDefaultService(Environment.CurrentDirectory);//初始化Selenium配置，传入存放phantomjs.exe文件的目录
            _service.IgnoreSslErrors = true;//忽略证书错误
            _service.WebSecurity = false;//禁用网页安全
            _service.HideCommandPromptWindow = true;//隐藏弹出窗口
            _service.LoadImages = false;//禁止加载图片
            _service.LocalToRemoteUrlAccess = true;//允许使用本地资源响应远程 URL
            var defaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/50.0.2661.102 Safari/537.36";
            if (!string.IsNullOrEmpty(UserAgent))
            {
                defaultUserAgent = UserAgent;
            }
            _options.AddAdditionalCapability(@"phantomjs.page.settings.userAgent", defaultUserAgent);
            if (!string.IsNullOrEmpty(CurWebProxyString))
            { 
                _service.ProxyType = "HTTP";//使用HTTP代理 {
                _service.Proxy = CurWebProxyString;//代理IP及端口
            }
            else
            {
                _service.ProxyType = "none";//不使用代理
            }
}
        #endregion

        #endregion
    }
}