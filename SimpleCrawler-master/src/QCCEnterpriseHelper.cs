using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SimpleCrawler;
using System.Net;
using LibCurlNet;
using Helper;

namespace SimpleCrawler
{
    public class DeviceInfo
    {
        public string appId { get; set; }
        public string deviceToken { get; set; }
        public string refreshToken { get; set; }
        public string accessToken { get; set; }
        public string deviceId { get; set; }
        public string timestamp { get; set; }
        public string sign { get; set; }
        public string isBusy { get; set; }
        public override string ToString()
        {
            return string.Format("appId:{0}\r deviceId={1}\r timestamp={2}\r sign={3} \r refreshToken={4} \r accessToken={5}\r  isBusy:{6} ", appId, deviceId, timestamp, sign, refreshToken, accessToken, isBusy);
        }

    }

    /// <summary>
    /// 通过模拟账号App远程获取数据
    /// </summary>
    public class QCCEnterpriseHelper
    {
        public static bool USEWEBPROXY=false;
        static HttpInput hi = new HttpInput();
        /// <summary>
        /// 企查查app账号表
        /// </summary>
        public static string QCCDeviceAccount = "QCCDeviceAccount";
        public static string SearchType = "EnterpriseGuidByKeyWord_APP";
        public static string EnterpriseDetailInfoTableName = "QCCEnterpriseKeyDetailInfo";
        public static string EnterpriseInventInfoTableName = "QCCEnterpriseKeyInventInfo";
        public static string EnterpriseInfoTableName = "QCCEnterpriseKeyInfo";
        public static string QCCEnterpriseKeyForInitTableName = "QCCEnterpriseKeyForInit";//待爬取的客户列表
        public static string QCCEnterpriseKeySettings = "QCCEnterpriseKeySettings";//设定
        const string proxyHost = ConstParam.proxyHost;
        const string proxyPort = ConstParam.proxyPort;
        const string proxyUser = ConstParam.proxyUser;//"H1880S335RB41F8P";////HVW8J9B1F7K4W83P
        const string proxyPass = ConstParam.proxyPass;//"ECB2CD5B9D783F4E";////C835A336CD070F9D
        SimpleCrawler.HttpHelper http = new SimpleCrawler.HttpHelper();
        public DeviceInfo curDeviceInfo { get; set; }
        public WebProxy webProxy { get; set; }
        public static string globalCookie = "UM_distinctid=1622d665bae936-052965f44f23f1-3f3c5906-13c680-1622d665bafa0b; zg_did=%7B%22did%22%3A%20%221622d665bb97bf-070712a0301052-3f3c5906-13c680-1622d665bba39e%22%7D; _uab_collina=152118010952145927565571; OUTFOX_SEARCH_USER_ID_NCOO=1288148251.0606568; PHPSESSID=o4dlngeo96u1i5cq5vk5lnct12; _umdata=0823A424438F76AB80096595D960132A89C7706B558044C70BC8D901883012912E8A37388DCD9DD3CD43AD3E795C914CC2334F10781D1FBF011516E672729F80; CNZZDATA1254842228=1302766149-1521176508-%7C1523926451; hasShow=1; Hm_lvt_3456bee468c83cc63fb5147f119f1075=1521438089,1523929600; acw_tc=AQAAAJ7p+DxGLgoAIkg9Ownt/w6WK1pz; Hm_lpvt_3456bee468c83cc63fb5147f119f1075=1523929876; zg_de1d1a35bfa24ce29bbf2c7eb17e6c4f=%7B%22sid%22%3A%201523929599350%2C%22updated%22%3A%201523931667236%2C%22info%22%3A%201523344260986%2C%22superProperty%22%3A%20%22%7B%7D%22%2C%22platform%22%3A%20%22%7B%7D%22%2C%22utm%22%3A%20%22%7B%7D%22%2C%22referrerDomain%22%3A%20%22%22%2C%22cuid%22%3A%20%22879c4214c9af7992afc6f2c4291b7ae3%22%7D";
        public string GetWebProxyString()
        {
            if (!USEWEBPROXY) { return string.Empty; }
            return string.Format("{0}:{1}@{2}:{3}", proxyUser, proxyPass, proxyHost.Replace("http://", ""), proxyPort);
        }
         static QCCEnterpriseHelper()
        {
            HttpManager.Instance.InitWebClient(hi, true, 30, 30);
          
        }

        public QCCEnterpriseHelper()
        {
            
            if (USEWEBPROXY)
            {
                hi.CurlObject.SetOpt(LibCurlNet.CURLoption.CURLOPT_PROXY, GetWebProxyString());
            }
            webProxy = GetWebProxy();
            curDeviceInfo = new DeviceInfo();//默认设备
            curDeviceInfo.appId = "80c9ef0fb86369cd25f90af27ef53a9e";
            curDeviceInfo.refreshToken = "b30c9f1ab0a094668001463e63a2a561";
            curDeviceInfo.accessToken = "Bearer OGM2M2YzZmYtZDg3Zi00OGIzLWE1ODMtMWRmNWNiZDY1NGNl";
            curDeviceInfo.deviceId = "rtxepUOxiEaxlIqsBJJ7vRiq";
            curDeviceInfo.timestamp = "1477038756509";
            curDeviceInfo.sign = "e647d5856a52e8ee6ce5d5f7adbd646575a5984c";

        }

        #region 企业数据
        /// <summary>
        ///企业详细
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public HttpResult GetEnterpriseDetailInfo(string guid)
        {

            var uStr = string.Format("https://app.qichacha.net/app/v1/base/getEntDetail?unique={0}&timestamp={1}&sign={2}", guid, curDeviceInfo.timestamp, curDeviceInfo.sign);
            var result = GetHttpHtml(uStr);

            return result;
        }
        /// <summary>
        /// 企业背后疑似关系
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public HttpResult GetEnterpriseBackDetailInfo(string guid)
        {

            //企业背后关系详细
            var uStr = string.Format("https://app.qichacha.net/app/v1/msg/getPossibleGenerateRelation?unique={0}&sign={1}&token={2}&timestamp={3}&from=h5", guid, curDeviceInfo.sign, curDeviceInfo.accessToken.Replace("Bearer", "").Trim(), curDeviceInfo.timestamp);
            // uStr = string.Format("https://appv2.qichacha.net/app/v1/msg/getPossibleGenerateRelation?unique={0}&sign={1}&token={2}&timestamp={3}&from=h5", guid, curDeviceInfo.sign, curDeviceInfo.accessToken.Replace("Bearer", "").Trim(), curDeviceInfo.timestamp);
            var result = GetHttpHtml(uStr);
            return result;
        }

        /// <summary>
        /// 企业背后疑似关系
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public HttpResult GetEnterpriseInventInfo(string guid)
        {
            //企业背后关系详细
            //var uStr = string.Format("https://appv2.qichacha.net/app/v2/base/getInvestments?unique={0}&province=&cityCode=&pageIndex=1&industrycode=&startdateyear=&sign={1}&timestamp={2}", guid, curDeviceInfo.sign, curDeviceInfo.timestamp);
            var uStr = string.Format("http://www.qichacha.com/cms_map?keyNo={0}", guid);
            var result = GetHttpHtml(uStr);
            return result;
        }

        /// <summary>
        /// 通过企业名称获取企业信息
        /// </summary>
        /// <param name="名称"></param>
        /// <returns></returns>
        public HttpResult GetEnterpriseInfoByName(string name)
        {
            //企业背后关系详细
            var guidUrl = string.Format("https://www.qichacha.com/gongsi_getList?key={0}", name);
            var urlInfo = new UrlInfo(guidUrl) { Depth = 1, PostData = string.Format("key={0}&type=undefined", name) };
            var result = GetPostDataKeyWordEnhence(urlInfo);
            return result;
        }
        #endregion

        /// <summary>
        /// 数据是否正确
        /// </summary>
        /// <returns></returns>
        public bool IsDataSucceed(HttpResult result)
        {
            var html = result.Html;
            
            if (html.Contains("成功")||html.Contains("KeyNo"))
            {
                return true;
            }
            if ((html.Contains("异常访问") || html.Contains("失败") || html.Contains("失效")))
            {
                return false;
            }
            return false;
        }
        /// <summary>
        /// 账号是否异常
        /// </summary>
        /// <returns></returns>
        public bool IsAccountException(HttpResult result)
        {
            var html = result.Html;
            if ((html.Contains("异常访问")))
            {
                return true;
            }
            return false;
        }

        #region 基础处理类


        public HttpResult GetPostDataKeyWordEnhence(UrlInfo curUrlObj, string refer = "", bool useProxy = true)
        {
            //创建Httphelper参数对象

            //curUrlObj.PostData = string.Format("key=安徽省合肥市荣事达大道568号511室 程华&type=undefined");
            HttpItem item = new HttpItem()
            {
                URL = curUrlObj.UrlString,//URL     必需项    

                ContentType = "application/x-www-form-urlencoded; charset=UTF-8",//返回类型    可选项有默认值 

                Timeout = 1500,
                Accept = "*/*",
                Encoding = null,//编码格式（utf-8,gb2312,gbk）     可选项 默认类会自动识别
                //Encoding = Encoding.Default,
                Method = "post",//URL     可选项 默认为Get
                //Timeout = 100000,//连接超时时间     可选项默认为100000
                //ReadWriteTimeout = 30000,//写入Post数据超时时间     可选项默认为30000
                //IsToLower = false,//得到的HTML代码是否转成小写     可选项默认转小写
                //Cookie = "",//字符串Cookie     可选项
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.181 Safari/537.36",//用户的浏览器类型，版本，操作系统     可选项有默认值
                Referer = "https://www.qichacha.com/",//来源URL     可选项
                Postdata = curUrlObj.PostData,
                Allowautoredirect = true,
                Cookie = globalCookie, 
                KeepAlive = true,  

            };
          
            //item.WebProxy = GetWebProxy();
             item.PostEncoding = System.Text.Encoding.GetEncoding("utf-8");
            var result = http.GetHtml(item);
            if (string.IsNullOrEmpty(result.Html))
            {

            }
            return result;
        }

        public HttpResult GetPostData(UrlInfo curUrlObj)
        {
            hi.Url = curUrlObj.UrlString.ToString();
            //hi.Refer = "https://appv2.qichacha.net";
            hi.PostData = curUrlObj.PostData;
            hi.UserAgent = "okhttp/3.6.0";
            hi.HeaderSet("Content-Type", "application/x-www-form-urlencoded");
            // hi.HeaderSet("Content-Length","154");
            // hi.HeaderSet("Connection","Keep-Alive");
            hi.HeaderSet("Accept-Encoding", "gzip");
            if (string.IsNullOrEmpty(curDeviceInfo.accessToken))
            {
                RefreshToken();
            }
            hi.HeaderSet("Authorization", string.Format("Bearer {0}", curDeviceInfo.accessToken));
            var ho = LibCurlNet.HttpManager.Instance.ProcessRequest(hi);
            if (ho.IsOK && !ho.TxtData.Contains("成功"))
            {

                
                return new HttpResult() { StatusCode = HttpStatusCode.OK, Html = ho.TxtData };
            }
            else
            {
                return new HttpResult() { StatusCode = HttpStatusCode.Forbidden };
            }
        }
        /// <summary>
        /// 刷新tokenrefresh_token=6b1b70165f328de31036d7b4e5731e96
        /// </summary>
        /// <returns></returns>
        public string GetAccessToken()
        {
 

            hi.Url = "https://appv2.qichacha.net/app/v1/admin/getAccessToken";
            //hi.Refer = "https://appv2.qichacha.net";
            hi.PostData = string.Format("appId={0}&deviceId={1}&version=10.0.4&deviceType=android&os=&timestamp={2}&sign={3}", curDeviceInfo.appId, curDeviceInfo.deviceId, curDeviceInfo.timestamp, curDeviceInfo.sign);
            
            hi.UserAgent = "okhttp/3.6.0";
            hi.HeaderSet("Content-Type", "application/x-www-form-urlencoded");
            // hi.HeaderSet("Content-Length","154");
            // hi.HeaderSet("Connection","Keep-Alive");
            hi.HeaderSet("Accept-Encoding", "gzip");
            hi.HeaderSet("Authorization", string.Format("Bearer {0}", curDeviceInfo.accessToken));
            if (USEWEBPROXY)
            {
                hi.CurlObject.SetOpt(LibCurlNet.CURLoption.CURLOPT_PROXY, GetWebProxyString());
            }
            var ho = LibCurlNet.HttpManager.Instance.ProcessRequest(hi);
            if (ho.IsOK && ho.TxtData.Contains("成功"))
            {
                var token = Toolslib.Str.Sub(ho.TxtData, "access_token\":\"", "\"");
                if (!string.IsNullOrEmpty(token))
                    curDeviceInfo.accessToken = token;

                var refleshToken = Toolslib.Str.Sub(ho.TxtData, "refresh_token\":\"", "\"");
                if (!string.IsNullOrEmpty(refleshToken))
                    curDeviceInfo.refreshToken = refleshToken;
                return ho.TxtData;
            }
          
           

        
            return string.Empty;
        }

        public string RefreshToken(bool forceRefresh = false)
        {

            if (string.IsNullOrEmpty(curDeviceInfo.refreshToken))
            {

                return GetAccessToken();
            }

            var url = "https://appv2.qichacha.net/app/v1/admin/refreshToken";
            var postData = string.Format("refreshToken={0}&timestamp={1}&appId={2}&sign={3}", curDeviceInfo.refreshToken, curDeviceInfo.timestamp, curDeviceInfo.appId, curDeviceInfo.sign);
            var result = GetPostData(new UrlInfo(url) { PostData = postData });
            if (result.Html.Contains("成功"))
            {
                var token = Toolslib.Str.Sub(result.Html, "access_token\":\"", "\"");
                if (!string.IsNullOrEmpty(token))
                    curDeviceInfo.accessToken = token;
                //ShowMessageInfo("操作成功开始保存RefreshToken" + curDeviceInfo.AccessToken, true);
                //SaveDeviceToken();
                return result.Html;
            }
            else
            {
                return GetAccessToken();

            }


            //return string.Empty;
        }

        /// <summary>
        /// 返回请求数据
        /// </summary>
        /// <param name="curUrlObj"></param>
        /// <returns></returns>
        public SimpleCrawler.HttpResult GetHttpHtml(string url)
        {
            url = FixUrlSignStr(url);//修饰字符串
            // return GetPostDataFix(curUrlObj, accessToken);
            SimpleCrawler.HttpResult result = new SimpleCrawler.HttpResult();
            try
            {
                var item = new SimpleCrawler.HttpItem()
                {
                    URL = url,
                    Method = "get",//URL     可选项 默认为Get   
                    // ContentType = "text/html",//返回类型    可选项有默认值 
                    UserAgent = "okhttp/3.2.0",
                    ContentType = "application/x-www-form-urlencoded",
                };

                // item.Header.Add("Content-Type", "application/x-www-form-urlencoded");
                // hi.HeaderSet("Content-Length","154");
                // hi.HeaderSet("Connection","Keep-Alive");

                item.Header.Add("Accept-Encoding", "gzip");
                item.Header.Add("Authorization", curDeviceInfo.accessToken);
                item.Cookie = globalCookie;
                if (USEWEBPROXY)
                {
                    item.WebProxy = webProxy;
                }
                result = http.GetHtml(item);

            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (WebException ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {

            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (TimeoutException ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {

            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {

            }
            return result;
        }

        public string FixUrlSignStr(string url)
        {

            var _timestamp = HttpHelper.GetUrlParam(url, "timestamp");
            var _sign = HttpHelper.GetUrlParam(url, "sign");
            var _token = HttpHelper.GetUrlParam(url, "token");
            if (!string.IsNullOrEmpty(_timestamp) && _timestamp != curDeviceInfo.timestamp)
            {
                url = url.Replace(_timestamp, curDeviceInfo.timestamp);
            }
            if (!string.IsNullOrEmpty(_sign) && _sign != curDeviceInfo.sign)
            {
                url = url.Replace(_sign, curDeviceInfo.sign);
            }
            if (!string.IsNullOrEmpty(_token) && _sign != curDeviceInfo.accessToken)
            {
                url = url.Replace(_token, curDeviceInfo.accessToken.Replace("Bearer", "").Trim());
            }
            return url;
        }


        public WebProxy GetWebProxy()
        {

            // 设置代理服务器
            var proxy = new WebProxy();
            proxy.Address = new Uri(string.Format("{0}:{1}", proxyHost, proxyPort));
            proxy.Credentials = new NetworkCredential(proxyUser, proxyPass);
            return proxy;
        }



        /// <summary>
        /// App模拟登陆，返回设备信息
        /// </summary>
        /// <returns></returns>
        private DeviceInfo AppAutoLoin(DeviceInfo deviceInfo, Dictionary<string, string> allAccountHashMapList, string phoneNum, string pwdNormal)
        {
            RefreshToken();
            var hashPwd = string.Empty;//加密后的hash
            //var hitHashObj = allAccountHashMapList.Where(c => c.Key == pwdNormal).FirstOrDefault();
            if (allAccountHashMapList.ContainsKey(pwdNormal))
            {
                hashPwd = allAccountHashMapList[pwdNormal];
                var _url = new UrlInfo("https://appv2.qichacha.net/app/v1/admin/login");
                _url.PostData = string.Format("loginType=2&accountType=1&account={0}&password={1}&identifyCode=&key=&token=&timestamp={2}&sign={3}", phoneNum, hashPwd, deviceInfo.timestamp, deviceInfo.sign);
                var result = GetPostData(_url);
                if (result.StatusCode == HttpStatusCode.OK && result.Html.Contains("成功"))
                {
                    var token = Toolslib.Str.Sub(result.Html, "access_token\":\"", "\"");
                    if (!string.IsNullOrEmpty(token))
                        deviceInfo.accessToken = token;
                    return deviceInfo;
                }

            }
            ///无对应密码或者登陆失败
            return null;
        }
        #endregion
    }

}
