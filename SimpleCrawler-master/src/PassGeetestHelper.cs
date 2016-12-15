
using LibCurlNet;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;


namespace SimpleCrawler.Demo
{
    public class GeetestResult
    {
        public string ValidCode { get; set; }
        public string Challenge { get; set; }
        public string LastPoint { get; set; }
        public bool Status { get; set; } = false;
    }

    public class PassGeetestHelper
    {
        //private string _capUrl = "http://www.qixin.com/service/gtregister?t={0}&_={0}";
        private string _capUrl = "http://www.qichacha.com/index_getcap?rand=t={0}&_={0}";
        
        public string GetCapUrl {

            get {
                return _capUrl;
            }
            set {
                _capUrl = value;
            }

        }
        public const string ip = "101.200.187.122:9600";
        public string GetLastPoint(HttpInput hi)
        {
            hi.Url = string.Format("http://{0}/login_biz/query_money.oko?uid=01161add5a3c4c55bd9c133baa9effd0", ip);
            var ho = HttpManager.Instance.ProcessRequest(hi);
            if (ho.IsOK)
            {
               return ho.TxtData.Replace("remain money:", "");
            }
            return string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hi"></param>
        /// <param name="postFormatStr">geetest_challenge={0}&geetest_validate={1}&geetest_seccode={1}%7Cjordan</param>
        /// <param name="validUrl">http://www.qixin.com/service/gt-validate-for-chart</param>
        /// <returns></returns>
        public GeetestResult PassGeetest(HttpInput hi,string postFormatStr,string validUrl,string cookie="")
        {
            var result = new GeetestResult();
            if (string.IsNullOrEmpty(postFormatStr))
            {
                postFormatStr = "geetest_challenge={0}&geetest_validate={1}&geetest_seccode={1}%7Cjordan";
            }
            //HttpInput hi = new HttpInput();
            //HttpManager.Instance.InitWebClient(hi, true, 30, 30);
            //Random rand = new Random(Environment.TickCount);
            hi.Url = string.Format("http://{0}/login_biz/query_money.oko?uid=01161add5a3c4c55bd9c133baa9effd0",ip);
            var ho = HttpManager.Instance.ProcessRequest(hi);
            if (ho.IsOK)
            {
                result.LastPoint = ho.TxtData.Replace("remain money:","");
                Console.WriteLine("剩余点数{0}", ho.TxtData);
                int curPoint = 0;
                if (int.TryParse(ho.TxtData, out curPoint))
                {
                    if (curPoint <= 1000)
                    {
                        result.LastPoint = ho.TxtData;
                        result.Status = false;
                        return result;
                    }
                }
            }

            // 首先调用登录API
            //1:登录打码平台 ,这是我的API接口地址，^_^
            // hi.Url = "http://120.27.110.11:9600/login_biz/login.oko?uid=01161add5a3c4c55bd9c133baa9effd0";
            // hi.Url = " http://115.28.134.207:9600/passgeetest/fuckgee.oko?uid=01161add5a3c4c55bd9c133baa9effd0&data=1|gt|challenge|http";
            //hi.Url = "http://115.28.134.207:9600/passgeetest/fuckgee.oko?uid=01161add5a3c4c55bd9c133baa9effd0&data=1|{0}|{1}|http";
            // 请求访问
            //hi.EnableProxy = true;
            //hi.ProxyIP = "127.0.0.1";
            //hi.ProxyPort = 8888;
            //ho = HttpManager.Instance.ProcessRequest(hi);

            if (true)
            {
                // 获得了过码接口地址
                string vcode_url = "http://"+ip+"/passgeetest/fuckgee.oko?uid=01161add5a3c4c55bd9c133baa9effd0&data=1|{0}|{1}|http|refer";

            _rt1:
                if (!string.IsNullOrEmpty(cookie))
                {
                    hi.SetCookie(cookie);
                    
                }
                // 请求验证码
                //hi.Url = "http://www.qixin.com/service/gtregister?t=14664817340727&_=1466481710608";
                //hi.Url = string.Format("http://www.qixin.com/service/gtregister?_={0}", GetTimeLikeJS());
                 hi.Url = string.Format(_capUrl, GetTimeLikeJS(), GetTimeLikeJS());
                //hi.Cookies = Settings.SimulateCookies;
                ho = HttpManager.Instance.ProcessRequest(hi);
                if (ho.IsOK)
                {
                    //{"success":1,"gt":"68bb53762881989c3ca8e86c4621dcdb","challenge":"94490fd25aedbc2b83a843a89e2c15ad"}
                    var gt = Toolslib.Str.Sub(ho.TxtData, "gt\":\"", "\"");
                    var challenge= Toolslib.Str.Sub(ho.TxtData, "challenge\":\"", "\"");
                    if (string.IsNullOrEmpty(gt) || string.IsNullOrEmpty(challenge))
                    {
                        var rand = new Random().Next(1000, 5000);
                        Console.WriteLine("获取gtchallenge失败！等待10秒重试");
                        Thread.Sleep(rand);
                        result.Status = false;
                        return result;
                    }

                    hi.Url =string.Format(vcode_url, gt, challenge);
                    var TempPostData = "data=" + gt + "|" + challenge;

                    Console.WriteLine("给过码接口的POST数据为:" + TempPostData);
                    ho = HttpManager.Instance.ProcessRequest(hi);
                    string kkk = ho.TxtData;
                    if (kkk != null&& kkk.StartsWith("success:"))
                    {

                        string[] lastvcode = kkk.Replace("success:", string.Empty).Split(new char[] { '|' });
                        result.ValidCode = lastvcode[0];
                        result.Challenge = lastvcode[1];
                        //{"success":1,","momo_pic_verify_token":"44fb42329028fd1c40b66ec0a8e08375","ec":200,"em":"ok"}
                        // 登录
                        // hi.Cookies = Settings.SimulateCookies;
                        if (!string.IsNullOrEmpty(validUrl))//在验证一次
                        {
                            hi.Url = validUrl;
                            hi.PostData = string.Format(postFormatStr, lastvcode[1], lastvcode[0]);
                            ho = HttpManager.Instance.ProcessRequest(hi);
                            if (ho.IsOK && (ho.TxtData.Contains("succ") || ho.TxtData.Contains("1")))
                            {
                                result.Status = true;
                             
                                return result;

                            }
                        }
                        else {
                            result.Status = true;

                            return result;
                        }
                    }
                    else
                    {
                        Thread.Sleep(2000);
                        Console.WriteLine("重试");
                        goto _rt1;
                    }
                }
                else
                {
                    Thread.Sleep(2000);
                    Console.WriteLine("重试");
                    goto _rt1;
                }


            }
            return result;
        }

        private  long GetTimeLikeJS()

        {

            long lLeft = 621355968000000000;

            DateTime dt = DateTime.Now;

            long Sticks = (dt.Ticks - lLeft) / 10000;

            return Sticks;

        }

    }

}
