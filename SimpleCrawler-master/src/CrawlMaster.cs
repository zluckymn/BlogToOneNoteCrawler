// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CrawlMaster.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The crawl master.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleCrawler
{
   
    using OpenQA.Selenium;
    using OpenQA.Selenium.Chrome;
    using OpenQA.Selenium.Remote;
    using OpenQA.Selenium.Support.UI;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;

    /// <summary>
    /// The crawl master.
    /// </summary>
    public class CrawlMaster
    {
        #region Constants

        /// <summary>
        /// The web url regular expressions.
        /// </summary>
       private const string WebUrlRegularExpressions = @"^(http|https)://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?";
       // private const string WebUrlRegularExpressions = @"((http|ftp|https)://)(([a-zA-Z0-9\._-]+\.[a-zA-Z]{2,6})|([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}))(:[0-9]{1,4})*(/[a-zA-Z0-9\&%_\./-~-]*)?";
        #endregion

        #region Fields

        /// <summary>
        /// The cookie container.
        /// </summary>
        private readonly CookieContainer cookieContainer;

        /// <summary>
        /// The random.
        /// </summary>
        private readonly Random random;

        /// <summary>
        /// The thread status.
        /// </summary>
        private readonly bool[] threadStatus;

        /// <summary>
        /// The threads.
        /// </summary>
        private readonly Thread[] threads;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CrawlMaster"/> class.
        /// </summary>
        /// <param name="settings">
        /// The settings.
        /// </param>
        public CrawlMaster(CrawlSettings settings)
        {
            this.cookieContainer = new CookieContainer();
            this.random = new Random();

            this.Settings = settings;
            this.threads = new Thread[settings.ThreadCount];
            this.threadStatus = new bool[settings.ThreadCount];
        }

        #endregion

        #region Public Events

        /// <summary>
        /// The add url event.
        /// </summary>
        public event AddUrlEventHandler AddUrlEvent;

        /// <summary>
        /// The crawl error event.
        /// </summary>
        public event CrawlErrorEventHandler CrawlErrorEvent;

        /// <summary>
        /// The data received event.
        /// </summary>
        public event DataReceivedEventHandler DataReceivedEvent;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the settings.
        /// </summary>
        public CrawlSettings Settings { get; private set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The crawl.
        /// </summary>
        public void Crawl()
        {
            this.Initialize();

            for (int i = 0; i < this.threads.Length; i++)
            {
                this.threads[i].Start(i);
                this.threadStatus[i] = false;
            }
        }

        /// <summary>
        /// The stop.
        /// </summary>
        public void Stop()
        {
            foreach (Thread thread in this.threads)
            {
                thread.Abort();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The config request.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        private IPProxy ConfigRequest(HttpWebRequest request)
        {
            request.UserAgent = this.Settings.UserAgent;
            request.CookieContainer = this.cookieContainer;
            request.AllowAutoRedirect = true;
            request.MediaType = "text/html";
            request.Headers["Accept-Language"] = "zh-CN,zh;q=0.8";

            if (this.Settings.Timeout > 0)
            {
                request.Timeout = this.Settings.Timeout;
            }

            //
            if (!string.IsNullOrEmpty(this.Settings.SimulateCookies))
            {

                SetCookie(request, this.Settings.SimulateCookies);
            }
           
            //添加代理ip列表,随机挑选ip

            var curIPProxy = Settings.GetIPProxy();
             SetProxy(request, curIPProxy);
            return curIPProxy;
        }

        /// <summary>
        /// 设置代理
        /// </summary>
        /// <param name="item">参数对象</param>
        private void SetProxy(HttpWebRequest request,IPProxy ipProxy)
        {
            if (ipProxy == null) return;
                string ProxyIp= ipProxy.IP;
                string ProxyPort= ipProxy.Port;
                string ProxyUserName = ipProxy.UserName;
                string ProxyPwd = ipProxy.PassWord;
                 //设置代理服务器
                if (ProxyIp.Contains(":"))
                {
                    string[] plist = ProxyIp.Split(':');
                    WebProxy myProxy = new WebProxy(plist[0].Trim(), Convert.ToInt32(plist[1].Trim()));
                    //建议连接
                    myProxy.Credentials = new NetworkCredential(ProxyUserName, ProxyPwd);
                    //给当前请求对象
                    request.Proxy = myProxy;
                }
                else
                {
                    if (!string.IsNullOrEmpty(ProxyPort))
                    {
                    WebProxy myProxy = new WebProxy(ProxyIp, Convert.ToInt32(ProxyPort));
                    if (!string.IsNullOrEmpty(ProxyUserName))
                    {
                        //建议连接
                        myProxy.Credentials = new NetworkCredential(ProxyUserName, ProxyPwd);
                    }
                    //给当前请求对象
                    request.Proxy = myProxy;
                    
                    }
                    else
                    {
                    WebProxy myProxy = new WebProxy(ProxyIp, false);
                    //建议连接
                    myProxy.Credentials = new NetworkCredential(ProxyUserName, ProxyPwd);
                    //给当前请求对象
                    request.Proxy = myProxy;
                }
            }
            
        }

        /// <summary>
        /// The crawl process.
        /// </summary>
        /// <param name="threadIndex">
        /// The thread index.
        /// </param>
        private void CrawlProcess_Abort(object threadIndex)
        {
            var currentThreadIndex = (int)threadIndex;
            while (true)
            {
                // 根据队列中的 Url 数量和空闲线程的数量，判断线程是睡眠还是退出
                if (UrlQueue.Instance.Count == 0)
                {
                    this.threadStatus[currentThreadIndex] = true;
                    if (!this.threadStatus.Any(t => t == false))
                    {
                        break;
                    }

                    Thread.Sleep(2000);
                    continue;
                }

                this.threadStatus[currentThreadIndex] = false;

                if (UrlQueue.Instance.Count == 0)
                {
                    continue;
                }

                UrlInfo urlInfo = UrlQueue.Instance.DeQueue();

                HttpWebRequest request = null;
                HttpWebResponse response = null;
                IPProxy curIPProxy = null;
                try
                {
                    if (urlInfo == null)
                    {
                        continue;
                    }

                    // 1~5 秒随机间隔的自动限速
                    if (this.Settings.AutoSpeedLimit)
                    {
                        int span = this.random.Next(1000, 5000);
                        Thread.Sleep(span);
                    }

                    // 创建并配置Web请求
                    request = WebRequest.Create(urlInfo.UrlString) as HttpWebRequest;
                    curIPProxy = this.ConfigRequest(request);//返回当前的代理地址
                    
                    if (request != null)
                    {
                        response = request.GetResponse() as HttpWebResponse;
                    }

                    if (response != null)
                    {
                        this.PersistenceCookie(response);

                        Stream stream = null;

                        // 如果页面压缩，则解压数据流
                        if (response.ContentEncoding == "gzip")
                        {
                            Stream responseStream = response.GetResponseStream();
                            if (responseStream != null)
                            {
                                stream = new GZipStream(responseStream, CompressionMode.Decompress);
                            }
                        }
                        else
                        {
                            stream = response.GetResponseStream();
                        }

                        using (stream)
                        {
                            string html = this.ParseContent(stream, response.CharacterSet);

                            this.ParseLinks(urlInfo, html);

                            if (this.DataReceivedEvent != null)
                            {
                                this.DataReceivedEvent(
                                    new DataReceivedEventArgs
                                        {
                                            Url = urlInfo.UrlString, 
                                            Depth = urlInfo.Depth, 
                                            Html = html, IpProx= curIPProxy
                                    });
                            }

                            if (stream != null)
                            {
                                stream.Close();
                            }
                        }
                    }
                }
                catch (WebException webEx)
                {
                    var ev = new CrawlErrorEventArgs
                    {
                        Url = urlInfo.UrlString,
                        Depth = urlInfo.Depth,
                        Exception = webEx,
                        IpProx = curIPProxy
                        
                    };
                    if (webEx.Status == WebExceptionStatus.Timeout|| webEx.Status == WebExceptionStatus.ProtocolError || webEx.Message.Contains("远程服务器返回错误") || webEx.Message.Contains("网关"))
                    {
                        //Settings.SetUnviableIP(curIPProxy);//设置为无效代理
                        ev.needChangeIp = true;
                    }
                    ev.needTryAgain = true;
                    if (this.CrawlErrorEvent != null)
                    {
                        if (urlInfo != null)
                        {
                            this.CrawlErrorEvent(ev
                              );
                        }
                    }
                }

                catch (Exception exception)
                {
                    var errorEV = new CrawlErrorEventArgs { Url = urlInfo.UrlString, Depth = urlInfo.Depth, Exception = exception, IpProx = curIPProxy };
                  
                    if (exception.Message.Contains("超时") || exception.Message.Contains("远程服务器返回错误"))
                    {
                       // Settings.SetUnviableIP(curIPProxy);//设置为无效代理
                       errorEV.needChangeIp = true;
                    }
                    errorEV.needTryAgain = true;
                    if (this.CrawlErrorEvent != null)
                    {
                        if (urlInfo != null)
                        {
                            this.CrawlErrorEvent(errorEV
                                );
                        }
                    }
                }
                finally
                {
                    if (request != null)
                    {
                        request.Abort();
                    }

                    if (response != null)
                    {
                        response.Close();
                    }
                }
            }
        }
        /// <summary>
        /// The crawl process.
        /// </summary>
        /// <param name="threadIndex">
        /// The thread index.
        /// </param>
        private void CrawlProcess(object threadIndex)
        {
            var currentThreadIndex = (int)threadIndex;
            while (true)
            {
                // 根据队列中的 Url 数量和空闲线程的数量，判断线程是睡眠还是退出
                if (UrlQueue.Instance.Count == 0)
                {
                    this.threadStatus[currentThreadIndex] = true;
                    if (!this.threadStatus.Any(t => t == false))
                    {
                        break;
                    }

                    Thread.Sleep(2000);
                    continue;
                }

                this.threadStatus[currentThreadIndex] = false;

                if (UrlQueue.Instance.Count == 0)
                {
                    continue;
                }

                UrlInfo urlInfo = UrlQueue.Instance.DeQueue();

              

                var curIPProxy = Settings.GetIPProxy();
                try
                {

                    if (urlInfo == null)
                    {
                        continue;
                    }

                    // 1~5 秒随机间隔的自动限速
                    if (this.Settings.AutoSpeedLimit)
                    {
                        try
                        {
                            var maxSecond = 5000;
                            var inSecond = 1000;
                            if (this.Settings.AutoSpeedLimitMinMSecond >= inSecond)
                            {
                                inSecond = this.Settings.AutoSpeedLimitMinMSecond;
                            }
                            if (this.Settings.AutoSpeedLimitMaxMSecond >= maxSecond)
                            {
                                maxSecond = this.Settings.AutoSpeedLimitMaxMSecond;
                            }

                            int span = this.random.Next(inSecond, maxSecond);

                            Thread.Sleep(span);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("AutoSpeedLimit"+ex.Message);
                        }
                    }
                    string html = string.Empty;
                    switch (Settings.CrawlMode)
                    {
                        case EnumCrawlMode.PhantomJsViaSelenium:
                            html = GetPhantomJsResult(urlInfo);
                            break;
                        case EnumCrawlMode.HttpHelper:
                        case EnumCrawlMode.SuperWebClient:
                            if (Settings.UseSuperWebClient)
                            {
                                html = GetSupperHttpResult(urlInfo);
                            }
                            else
                            {
                                html = GetHttpResult(urlInfo);
                            }
                            break;
                      

                    }
                    
                    if (!string.IsNullOrEmpty(html))
                    {
                        this.ParseLinks(urlInfo, html);
                    }
                     
                    if (this.DataReceivedEvent != null)
                            {
                                this.DataReceivedEvent(
                                    new DataReceivedEventArgs
                                    {
                                        Url = urlInfo.UrlString,
                                        Depth = urlInfo.Depth,
                                        Html = html,
                                        IpProx = curIPProxy,urlInfo=urlInfo
                                    });
                            }

                        
                }
                catch (WebException webEx)
                {
                  
                    var ev = new CrawlErrorEventArgs
                    {
                        Url = urlInfo.UrlString,
                        Depth = urlInfo.Depth,
                        Exception = webEx,
                        IpProx = curIPProxy,
                        urlInfo = urlInfo

                    };
                    if (webEx.Status == WebExceptionStatus.Timeout || webEx.Status == WebExceptionStatus.ProtocolError || webEx.Message.Contains("远程服务器返回错误") || webEx.Message.Contains("网关"))
                    {
                        //Settings.SetUnviableIP(curIPProxy);//设置为无效代理
                        ev.needChangeIp = true;
                    }
                    ev.needTryAgain = true;
                    if (this.CrawlErrorEvent != null)
                    {
                        if (urlInfo != null)
                        {
                            this.CrawlErrorEvent(ev
                              );
                        }
                    }
                }

                catch (Exception exception)
                {
                    var errorEV = new CrawlErrorEventArgs { Url = urlInfo.UrlString, Depth = urlInfo.Depth, Exception = exception, IpProx = curIPProxy , urlInfo=urlInfo};

                    if (exception.Message.Contains("超时") || exception.Message.Contains("远程服务器返回错误"))
                    {
                        // Settings.SetUnviableIP(curIPProxy);//设置为无效代理
                        errorEV.needChangeIp = true;
                    }
                    errorEV.needTryAgain = true;
                    if (this.CrawlErrorEvent != null)
                    {
                        if (urlInfo != null)
                        {
                            this.CrawlErrorEvent(errorEV
                                );
                        }
                    }
                }
                finally
                {
                    //if (request != null)
                    //{
                    //    request.Abort();
                    //}

                    //if (response != null)
                    //{
                    //    response.Close();
                    //}
                }
            }
        }
        /// <summary>
        /// 无头浏览器
        /// </summary>
        /// <param name="urlInfo"></param>
        /// <returns></returns>
        private string GetPhantomJsResult(UrlInfo urlInfo)
        {
            
            var pageSource = string.Empty;

            if (Settings.RemoteWebDriver == null) {
                ChromeOptions options = new ChromeOptions();

                //var proxy = new Proxy();
                //proxy.Kind = ProxyKind.Manual;
                //proxy.IsAutoDetect = false;
                //proxy.HttpProxy = SysAppConfig.ProxyHost + ":" + SysAppConfig.ProxyPort;
                //proxy.SslProxy = SysAppConfig.ProxyHost + ":" + SysAppConfig.ProxyPort;
                //options.Proxy = proxy;
                //options.AddArguments("--proxy-server=http://H1538UM3D6R2133P:511AF06ABED1E7AE@http-pro.abuyun.com:9010");
                options.AddArgument("ignore-certificate-errors");
                options.AddArgument("–incognito");
                options.AddArgument("disable-infobars");
                
                Settings.RemoteWebDriver = new ChromeDriver(options) ;
            }
            RemoteWebDriver driver = Settings.RemoteWebDriver;
            try
            {
                using (driver)
                {
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(Settings.operation.Timeout));
                    driver.Navigate().GoToUrl(string.Format(urlInfo.UrlString));
                
                    if (Settings.operation != null)
                    {
#pragma warning disable CS0618 // “ExpectedConditions”已过时:“The ExpectedConditions implementation in the .NET bindings is deprecated and will be removed in a future release. This portion of the code has been migrated to the DotNetSeleniumExtras repository on GitHub (https://github.com/DotNetSeleniumTools/DotNetSeleniumExtras)”
                      wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(@"//*[@id='js-player-title']/div/div[4]/div/span")));
#pragma warning restore CS0618 // “ExpectedConditions”已过时:“The ExpectedConditions implementation in the .NET bindings is deprecated and will be removed in a future release. This portion of the code has been migrated to the DotNetSeleniumExtras repository on GitHub (https://github.com/DotNetSeleniumTools/DotNetSeleniumExtras)”
                      driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(Settings.operation.Timeout);
                    }
                   
                    pageSource = driver.PageSource;
                }
                    //var cookies = driver.Manage().Cookies.AllCookies;
                    //var cookieStr = string.Join(";", cookies);
                    //Settings.SimulateCookies = cookieStr;
                return pageSource;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new Exception("执行GetPhantomJsResult出错");
            }
            finally
            {
                driver.Close();
                driver.Quit();
            }
#pragma warning disable CS0162 // 检测到无法访问的代码
            return pageSource;
#pragma warning restore CS0162 // 检测到无法访问的代码
        }

        /// <summary>
        /// 使用 SupperWebClient
        /// </summary>
        /// <param name="urlInfo"></param>
        /// <returns></returns>
        private string GetSupperHttpResult(UrlInfo urlInfo)
        {

            try
            {
               
                #region 进行url替换
                urlInfo = UrlInfoFix(urlInfo);
                #endregion
                var url = urlInfo.UrlString;
                var hi = Settings.hi;
                if (Settings.HeadSetDic != null)
                {
                    foreach (var key in Settings.HeadSetDic.Keys)
                    {
                        hi.HeaderSet(key, Settings.HeadSetDic[key]);
                    }
                }
                if (!string.IsNullOrEmpty(urlInfo.Authorization))
                {
                    Settings.hi.HeaderSet("Authorization", urlInfo.Authorization);
                }
                hi.Url = url;
                if (!string.IsNullOrEmpty(urlInfo.PostData))
                {
                    hi.PostData = urlInfo.PostData;
                }

                var ho = LibCurlNet.HttpManager.Instance.ProcessRequest(hi);

                return ho.TxtData;
            }
            catch (Exception ex)
            {
                throw new Exception("GetSupperHttpResult" + ex.Message);
            }
             
        }
        private UrlInfo UrlInfoFix(UrlInfo urlInfo)
        {
            #region 进行url替换
            if (Settings.LandFangIUserId != 0)
            {
                var appChangeUrl = new LandFangAppHelper();
                var fixUrl = appChangeUrl.FixIUserIdUrl(urlInfo.UrlString, Settings.LandFangIUserId.ToString());
                urlInfo.UrlString = fixUrl;
            }
        
            switch (Settings.CrawlerClassName)
            {
                case "WenShuAPPCrawler":
                    var reqToken = Toolslib.Str.Sub(urlInfo.PostData, "reqtoken\": \"", "\",");
                    if (string.IsNullOrEmpty(reqToken))
                    {
                        reqToken = Settings.AccessToken;
                    }
                    urlInfo.PostData = urlInfo.PostData.Replace(reqToken, WenShuAppHelper.GetRequestToken());
                    break;

                case "HuiCongMaterial":
                    var huiCongAppHelper = new HuiCongAppHelper();
                    var authorizationCode = huiCongAppHelper.GetHuiCongAuthorizationCode(urlInfo.UrlString);
                    if (authorizationCode != urlInfo.Authorization)
                    {
                        urlInfo.Authorization = authorizationCode;
                    }
                    break;
                case "JGJApp":
                    var jgjAppHelper = new JGJAppHelper();
                    var fixUrl = jgjAppHelper.FixJGJUrl(urlInfo.UrlString);
                    urlInfo.UrlString = fixUrl;
                    break;
            }

            return urlInfo;
            #endregion
        }
        private string GetHttpResult(UrlInfo urlInfo)
        {
            urlInfo = UrlInfoFix(urlInfo);
            var url = urlInfo.UrlString;
           
            HttpHelper http = new HttpHelper();
            HttpItem item = null;

            item = new HttpItem()
            {
                URL = url,//URL     必需项    
                                        //URL = "http://luckymn.cn/QuestionAnswer",
                Method = "get",//URL     可选项 默认为Get   
               // ContentType = "text/html",//返回类型    可选项有默认值 
                Timeout = this.Settings.Timeout,
                UserAgent = this.Settings.UserAgent,
                Allowautoredirect=this.Settings.Allowautoredirect
            };


            // item.Header.Add("Accept", "text/html, application/xhtml+xml, */*");


            if (!string.IsNullOrEmpty(urlInfo.PostData))
            {
                item.Method = "post";
                item.Postdata = urlInfo.PostData;
            }

         
            if (Settings.CurWebProxy != null)
            {
                item.WebProxy = Settings.CurWebProxy;
            }
            else
            {
                var curIPProxy = Settings.GetIPProxy();
                if (curIPProxy != null)
                {
                    item.ProxyIp = curIPProxy.IP;
                }
            }
            if (!string.IsNullOrEmpty(this.Settings.SimulateCookies))
            {
                item.Cookie = this.Settings.SimulateCookies;
            }
            if (!string.IsNullOrEmpty(this.Settings.ContentType))
            {
                item.ContentType = this.Settings.ContentType;
            }
            if (!string.IsNullOrEmpty(this.Settings.Referer))
            {
                item.Referer = this.Settings.Referer;
            }
            if (this.Settings.PostEncoding!= null)
            {
                item.PostEncoding = this.Settings.PostEncoding;
            }
            if (!string.IsNullOrEmpty(this.Settings.ContentType))
            {
                item.ContentType = this.Settings.ContentType;
            }
            if (!string.IsNullOrEmpty(this.Settings.Accept))
            {
                item.Accept = this.Settings.Accept;
            }
            if (!string.IsNullOrEmpty(urlInfo.Authorization))
            {
                item.Header.Add("Authorization", urlInfo.Authorization);
            }

            try
            {
                if (Settings.HeadSetDic != null)
                {
                    foreach (var key in Settings.HeadSetDic.Keys)
                    {
                        item.Header.Add(key, Settings.HeadSetDic[key]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetHttpResult:"+ex.Message);
            }
            //添加代理ip列表,随机挑选ip
            //创建并配置Web请求
            //request = WebRequest.Create(urlInfo.UrlString) as HttpWebRequest;
            //curIPProxy = this.ConfigRequest(request);//返回当前的代理地址
            var result = http.GetHtml(item);
            return result.Html;
        }
        /// <summary>
        /// The initialize.
        /// </summary>
        private void Initialize()
        {
            if (this.Settings.SeedsAddress != null && this.Settings.SeedsAddress.Count > 0)
            {
                foreach (string seed in this.Settings.SeedsAddress)
                {
                    if (Regex.IsMatch(seed, WebUrlRegularExpressions, RegexOptions.IgnoreCase))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(seed) { Depth = 1 });
                    }
                }
            }

            for (int i = 0; i < this.Settings.ThreadCount; i++)
            {
                var threadStart = new ParameterizedThreadStart(this.CrawlProcess);

                this.threads[i] = new Thread(threadStart);
            }

            ServicePointManager.DefaultConnectionLimit = 256;
        }

        /// <summary>
        /// The is match regular.
        /// </summary>
        /// <param name="url">
        /// The url.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool IsMatchRegular(string url)
        {
            bool result = false;

            if (this.Settings.RegularFilterExpressions != null && this.Settings.RegularFilterExpressions.Count > 0)
            {
                if (
                    this.Settings.RegularFilterExpressions.Any(
                        pattern => Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase)))
                {
                    result = true;
                }
            }
            else
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// The parse content.
        /// </summary>
        /// <param name="stream">
        /// The stream.
        /// </param>
        /// <param name="characterSet">
        /// The character set.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private string ParseContent(Stream stream, string characterSet)
        {
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            byte[] buffer = memoryStream.ToArray();

            Encoding encode = Encoding.ASCII;
            string html = encode.GetString(buffer);

            string localCharacterSet = characterSet;

            Match match = Regex.Match(html, "<meta([^<]*)charset=([^<]*)\"", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                localCharacterSet = match.Groups[2].Value;

                var stringBuilder = new StringBuilder();
                foreach (char item in localCharacterSet)
                {
                    if (item == ' ')
                    {
                        break;
                    }

                    if (item != '\"')
                    {
                        stringBuilder.Append(item);
                    }
                }

                localCharacterSet = stringBuilder.ToString();
            }

            if (string.IsNullOrEmpty(localCharacterSet))
            {
                localCharacterSet = characterSet;
            }

            if (!string.IsNullOrEmpty(localCharacterSet))
            {
                encode = Encoding.GetEncoding(localCharacterSet);
            }

            memoryStream.Close();

            return encode.GetString(buffer);
        }

        /// <summary>
        /// The parse links.
        /// </summary>
        /// <param name="urlInfo">
        /// The url info.
        /// </param>
        /// <param name="html">
        /// The html.
        /// </param>
        private void ParseLinks(UrlInfo urlInfo, string html)
        {
            if (this.Settings.Depth > 0 && urlInfo.Depth >= this.Settings.Depth)
            {
                return;
            }

            var urlDictionary = new Dictionary<string, string>();

            // Match match = Regex.Match(html, "(?i)<a .*?href=\"([^\"]+)\"[^>]*>(.*?)</a>");
            //var testStr = "<a href=\"http://baidu.com\" >融信鹤林花园</ a > ";
            //var testStr = "<A href=\"proDetail.asp? projectID = MTAyMjF8MjAxNS8xMC8yNnwyNA == \" target=_blank>阳光环站新城1#地...</a>";
            //  var firstIndex = html.IndexOf("<A href='result_new.asp");
            // var testStr = html.Substring(firstIndex,200);
            //2016.5.24修正<a href="xxx"><span>123</span></a>获取不到问题
            //Match match = Regex.Match(html.Replace("'","\""), "(?i)<a .*?href=[\",']([^\"]+)[\",'][^>]*>[^<]*</a>");
            Match match = Regex.Match(html.Replace("'", "\""), "(?i)<a .*?href=[\",']([^\"]+)[\",'][^>]*>" + @".*?</a>");
            while (match.Success)
            {
                // 以 href 作为 key
                string urlKey = match.Groups[1].Value;

                // 以 text 作为 value
                string urlValue = Regex.Replace(match.Groups[0].Value, "(?i)<.*?>", string.Empty);

                urlDictionary[urlKey] = urlValue;
                match = match.NextMatch();
            }

            foreach (var item in urlDictionary)
            {
                string href = item.Key;
                string text = item.Value;

                if (!string.IsNullOrEmpty(href))
                {
                    bool canBeAdd = true;

                    if (this.Settings.EscapeLinks != null && this.Settings.EscapeLinks.Count > 0)
                    {
                        if (this.Settings.EscapeLinks.Any(suffix => href.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
                        {
                            canBeAdd = false;
                        }
                    }

                    if (this.Settings.HrefKeywords != null && this.Settings.HrefKeywords.Count > 0)
                    {
                        if (!this.Settings.HrefKeywords.Any(href.Contains))
                        {
                            canBeAdd = false;
                        }
                    }

                    if (canBeAdd)
                    {
                        string url = href.Replace("%3f", "?")
                            .Replace("%3d", "=")
                            .Replace("%2f", "/")
                            .Replace("&amp;", "&");

                        if (string.IsNullOrEmpty(url) || url.StartsWith("#")
                            || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                            || url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        Uri baseUri = null;
                        Uri currentUri = null;
                        try
                        {

                             baseUri = new Uri(urlInfo.UrlString);
                             currentUri = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                                 ? new Uri(url)
                                                 : new Uri(baseUri, url);

                            url = currentUri.AbsoluteUri;
                        }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                        catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                        {
                            continue;
                        }
                        if (this.Settings.LockHost)
                        {
                            // 去除二级域名后，判断域名是否相等，相等则认为是同一个站点
                            // 例如：mail.pzcast.com 和 www.pzcast.com
                            if (baseUri.Host.Split('.').Skip(1).Aggregate((a, b) => a + "." + b)
                                != currentUri.Host.Split('.').Skip(1).Aggregate((a, b) => a + "." + b))
                            {
                                continue;
                            }
                        }

                        if (!this.IsMatchRegular(url))
                        {
                            continue;
                        }

                        var addUrlEventArgs = new AddUrlEventArgs { Title = text, Depth = urlInfo.Depth + 1, Url = url };
                        if (this.AddUrlEvent != null && !this.AddUrlEvent(addUrlEventArgs))
                        {
                            continue;
                        }

                        UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = urlInfo.Depth + 1 });
                    }
                }
            }
        }

        /// <summary>
        /// The persistence cookie.
        /// </summary>
        /// <param name="response">
        /// The response.
        /// </param>
        private void PersistenceCookie(HttpWebResponse response)
        {
            if (!this.Settings.KeepCookie)
            {
                return;
            }

            string cookies = response.Headers["Set-Cookie"];
            if (!string.IsNullOrEmpty(cookies))
            {
                var cookieUri =
                    new Uri(
                        string.Format(
                            "{0}://{1}:{2}/", 
                            response.ResponseUri.Scheme, 
                            response.ResponseUri.Host, 
                            response.ResponseUri.Port));

                this.cookieContainer.SetCookies(cookieUri, cookies);
            }
        }

        /// <summary>
        /// 设置Cookie
        /// </summary>
        /// <param name="item">Http参数</param>
        private void SetCookie(HttpWebRequest request,string cookie)
        {
            if (!string.IsNullOrEmpty(cookie)) request.Headers[HttpRequestHeader.Cookie] = cookie;
            
        }

        #endregion
    }
}