using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SimpleCrawler;
using System.Net;
 

namespace MZ.Mongo
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
            /// <summary>
            /// 企查查app账号表
            /// </summary>
            public   static string QCCDeviceAccount="QCCDeviceAccount";
            public   static string SearchType = "EnterpriseGuidByKeyWord_APP";
            public   static string EnterpriseDetailInfoTableName = "QCCEnterpriseDetailInfo";
            public static string QCCEnterpriseKeyForInitTableName = "QCCEnterpriseKeyForInit";//待爬取的客户列表
            const string proxyHost = ConstParam.proxyHost;
            const string proxyPort = ConstParam.proxyPort;
            const string proxyUser = ConstParam.proxyUser;
            const string proxyPass = ConstParam.proxyPass;
            SimpleCrawler.HttpHelper http = new SimpleCrawler.HttpHelper();
            public DeviceInfo curDeviceInfo { get; set; }
          
            public QCCEnterpriseHelper()
            {
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
              
                var uStr = string.Format("https://"+ConstParam.qUrl+"/app/v1/base/getEntDetail?unique={0}&timestamp={1}&sign={2}", guid, curDeviceInfo.timestamp, curDeviceInfo.sign);
                var result= GetHttpHtml(uStr);
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
                var uStr = string.Format("https://"+ ConstParam.qUrl + "/app/v1/msg/getPossibleGenerateRelation?unique={0}&sign={1}&token={2}&timestamp={3}&from=h5",guid, curDeviceInfo.sign, curDeviceInfo.accessToken.Replace("Bearer", "").Trim(), curDeviceInfo.timestamp);
                var result= GetHttpHtml(uStr);
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
                if (html.Contains("成功"))
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


            public HttpResult GetPostData(UrlInfo curUrlObj)
            {
                //创建Httphelper参数对象
                SimpleCrawler.HttpItem item = new SimpleCrawler.HttpItem()
                {
                    URL = curUrlObj.UrlString,//URL     必需项    

                    ContentType = "application/x-www-form-urlencoded",//返回类型    可选项有默认值 

                    Timeout = 1500,
                    Accept = "*/*",
                    // Encoding = null,//编码格式（utf-8,gb2312,gbk）     可选项 默认类会自动识别
                    //Encoding = Encoding.Default,
                    Method = "post",//URL     可选项 默认为Get
                    //Timeout = 100000,//连接超时时间     可选项默认为100000
                    //ReadWriteTimeout = 30000,//写入Post数据超时时间     可选项默认为30000
                    //IsToLower = false,//得到的HTML代码是否转成小写     可选项默认转小写
                    //Cookie = "",//字符串Cookie     可选项
                    UserAgent = "okhttp/3.2.0",//用户的浏览器类型，版本，操作系统     可选项有默认值
                    
                    Postdata =curUrlObj.PostData,
                    // Allowautoredirect = true,
                    // Cookie = Settings.SimulateCookies
                };


                item.Header.Add("Accept-Encoding", "gzip");
                
                item.Header.Add("Authorization", curDeviceInfo.accessToken);
                //item.Header.Add("Accept-Language", "zh-CN");
                item.Header.Add("charset", "UTF-8");
                //item.Header.Add("X-Requested-With", "XMLHttpRequest");
                //请求的返回值对象
                item.WebProxy = GetWebProxy();
                var result = http.GetHtml(item);
                return result;
            }
            /// <summary>
            /// 刷新tokenrefresh_token=6b1b70165f328de31036d7b4e5731e96
            /// </summary>
            /// <returns></returns>
            public string GetAccessToken()
            {

                var url =ConstParam.AccessTokenUrl;
                var postData = string.Format("appId={0}&deviceId={1}&version=9.2.0&deviceType=android&os=&timestamp={2}&sign={3}", curDeviceInfo.appId, curDeviceInfo.deviceId, curDeviceInfo.timestamp, curDeviceInfo.sign);
                var result = GetPostData(new UrlInfo(url) { PostData = postData });
                if (result.Html.Contains("成功"))
                {
                    var token = Toolslib.Str.Sub(result.Html, "access_token\":\"", "\"");
                    if (!string.IsNullOrEmpty(token))
                        curDeviceInfo.accessToken = token;

                    var refleshToken = Toolslib.Str.Sub(result.Html, "refresh_token\":\"", "\"");
                    if (!string.IsNullOrEmpty(refleshToken))
                        curDeviceInfo.refreshToken = refleshToken;

                    return result.Html;
                }
           
                return string.Empty;
            }

            public string RefreshToken(bool forceRefresh = false)
            {

                    if (string.IsNullOrEmpty(curDeviceInfo.refreshToken))
                    {

                        return GetAccessToken();
                    }
              
                    var url = ConstParam.RefreshTokenUrl;
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
                    item.WebProxy = GetWebProxy();
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
            public string GetWebProxyString()
            {

                return string.Format("{0}:{1}@{2}:{3}", proxyUser, proxyPass, proxyHost, proxyPort);
            }
            public string GetWebBrowserProxyString()
            {

                return string.Format("{0}:{1}", proxyHost, proxyPort);
            }
            #endregion
        }
  
}
