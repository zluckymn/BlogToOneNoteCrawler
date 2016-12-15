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
using System.Web;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DataRule;

namespace SimpleCrawler.Demo
{
   internal partial class Program
    {
        #region fang99爬取初始化
        private static List<BsonDocument> base64MapList;
        private static List<BsonDocument> roomList = new List<BsonDocument>();
        public static Object lockThis = new System.Object();
        public static Object lockRoom = new System.Object();
        public static BloomFilter<string> BloomBuildingIds ;
        public class BuildingCls
        {
            public string projId { get; set; }
            public string buildId { get; set; }
            public string xmbh { get; set; }
            public BuildingCls(string _projId, string _buildId)
            {
                projId = _projId; buildId = _buildId;
            }
            public BuildingCls(string _projId, string _buildId, string _xmbh)
            {
                projId = _projId; buildId = _buildId; xmbh = _xmbh;
            }
        }

        /// <summary>
        /// 判断 url是否可添加，布隆条件过滤
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static bool URLCanAdd(string url)
        {
            #region //todo:是否重复building 临时
            var queryStr = GetQueryString(url);
            if (!string.IsNullOrEmpty(queryStr))
            {
                var dic = HttpUtility.ParseQueryString(queryStr);

                var buildId = dic["building"] != null ? dic["building"].ToString() : string.Empty;
                if (!string.IsNullOrEmpty(buildId))
                {
                    if (BloomBuildingIds.Contains(buildId)) { return false; }

                }
            }
            #endregion
            return true;
        }

        /// <summary>
        /// blog初始化设置
        /// </summary>
        private static void fang99Init()
        {
            //种子地址需要加布隆过滤
            //Settings.SeedsAddress.Add(string.Format("http://www.fang99.com/buycenter/buildinglistselect.aspx?projectid=0000003961&building=0000030963"));
            //Settings.SeedsAddress.Add(string.Format("http://www.fang99.com/fcywbllc/yszcx.aspx"));
            //Settings.SeedsAddress.Add(string.Format("http://www.fang99.com/buycenter/buildinglistselect.aspx?projectid=0000005301&building=0000031782&xmbh=00004729"));
            // Settings.HrefKeywords.Add(string.Format("buildinglistselect.aspx?"));
            // Settings.HrefKeywords.Add(string.Format("fcywbllc/yszcx.aspx"));

            //base64Map映射表
            base64MapList = dataop.FindAll("Crawler_Base64Map").SetFields("base64Str", "value", "preValue", "hash", "type").ToList();
            roomList = dataop.FindAll("Crawler_Room_Test").ToList();
            BloomBuildingIds = new BloomFilter<string>(200000);
            //var curBuildingIds = roomList.Select(c => c.Text("buildId_site")).Distinct().ToList();
            //foreach (var buildId in curBuildingIds)
            //{
            //    BloomBuildingIds.Add(buildId);
            //}
            var urlDic = new List<BuildingCls>();
            var path = "ProjectBuilding.txt";
            var reader = new StreamReader(path);
            for (string equipmentInfo = reader.ReadLine(); equipmentInfo != null; equipmentInfo = reader.ReadLine())
            {
                if (string.IsNullOrEmpty(equipmentInfo)) continue;
                var newStr = equipmentInfo.Split(new string[] { "\t", " " }, StringSplitOptions.RemoveEmptyEntries);
                if (newStr.Length >= 3)
                {
                    if (BloomBuildingIds.Contains(newStr[1])) continue;
                    urlDic.Add(new BuildingCls(newStr[0], newStr[1], newStr[2]));
                }
                else if (newStr.Length == 2)
                {
                    if (BloomBuildingIds.Contains(newStr[1])) continue;
                    urlDic.Add(new BuildingCls(newStr[0], newStr[1]));
                }
            }
            reader.Close();

            foreach (var dic in urlDic)
            {
                var url = string.Format("http://www.fang99.com/buycenter/buildinglistselect.aspx?projectid={0}&building={1}", dic.projId, dic.buildId);
                filter.Add(url);
                if (!string.IsNullOrEmpty(dic.xmbh))
                {
                    url = string.Format("http://www.fang99.com/buycenter/buildinglistselect.aspx?projectid={0}&building={1}&xmbh={2}", dic.projId, dic.buildId, dic.xmbh);
                    Settings.SeedsAddress.Add(url);
                    filter.Add(url);
                }
                else
                {
                    Settings.SeedsAddress.Add(url);
                }

                Settings.HrefKeywords.Add(string.Format("buildinglistselect.aspx?projectid={0}", dic.projId));
            }
        }


        /// <summary>
        ///fang99爬取初始化
        /// </summary>
        /// <param name="args"></param>
        private static string fang99InitProcess(DataReceivedEventArgs args)
        {
            if ("http://www.fang99.com/" == args.Url) return string.Empty;
            // 在此处解析页面，可以用类似于 HtmlAgilityPack（页面解析组件）的东东、也可以用正则表达式、还可以自己进行字符串分析
            // if ("http://www.fang99.com/buycenter/buildinglistselect.aspx?projectid=0000003961&building=0000030963" != args.Url) return;


            List<StorageData> updateStorageDataList = new List<StorageData>();//更新列表
            List<string> addFloorInfoList = new List<string>();
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var projId = string.Empty;//项目id
            var buildId = string.Empty;//楼栋号
            var projectName = string.Empty;//项目名称
            var buildName = string.Empty;//楼栋名称
            var uninName = string.Empty;//单元名称
            var floorName = string.Empty;//楼层名称

            var queryStr = GetQueryString(args.Url);
            if (!string.IsNullOrEmpty(queryStr))
            {
                var dic = HttpUtility.ParseQueryString(queryStr);
                projId = dic["projectid"] != null ? dic["projectid"].ToString() : string.Empty;
                buildId = dic["building"] != null ? dic["building"].ToString() : string.Empty;
            }

            var parseHtml = htmlDoc.DocumentNode.InnerText;
            var tables = htmlDoc.GetElementbyId("div_houselist").Elements("table");
            //获取其他漏洞可能造成死循环
            var otherBuilding = htmlDoc.GetElementbyId("lzlist").SelectNodes("table/tr/td").Where(c => c.Attributes["id"] != null).ToList();
            //楼栋是1的才添加
            //foreach (var obuild in otherBuilding)
            //{

            //    var otherBuildId = obuild.Attributes["id"].Value;
            //    if(!buildingIds.Contains(otherBuildId))//防止重复采集building
            //    { 
            //        var otherSeed = string.Format("http://www.fang99.com/buycenter/buildinglistselect.aspx?projectid={0}&building={1}", projId,otherBuildId);
            //        UrlQueue.Instance.EnQueue(new UrlInfo(otherSeed) { Depth = 1 });
            //        buildingIds.Add(otherBuildId);
            //    }
            //}


            var project = htmlDoc.DocumentNode.SelectSingleNode("//span[@class='sf_xq_xmmc']");
            //楼栋开始
            if (project != null)
            {
                projectName = project.InnerText.Replace(" ", "").Trim();
            }
            else
            {
                return string.Empty;
            }
            var build = htmlDoc.DocumentNode.SelectSingleNode("//a[@class='cur ztys2']");
            if (build != null)
            {
                buildName = build.InnerText.Replace("幢", "").Replace(" ", "").Trim();
            }

            var curFloorStr = string.Empty;
            //遍历楼层数组
            foreach (var table in tables)
            {
                //楼层层开始
                if (table.Attributes["class"] == null || table.Attributes["class"].Value != "ztys2")
                {
                    var curFloorDiv = table.SelectNodes(@"tr/td/div").FirstOrDefault();
                    if (curFloorDiv != null && curFloorDiv.Attributes["class"] != null && curFloorDiv.Attributes["class"].Value == "lzbt_ygsf")
                    {
                        curFloorStr = curFloorDiv.InnerText.Replace(" ", "").Trim();
                        continue;
                    }
                    continue;
                }
                if (addFloorInfoList.Contains(curFloorStr))
                {
                    continue;
                }
                else
                {
                    addFloorInfoList.Add(curFloorStr);
                }
                //curFloorStr值为：2幢1单元26 层（4）户，解析
                #region curFloorStr值为：2幢1单元26 层（4）户，解析

                var floorStrLen = curFloorStr.Length;
                var buildEndIndex = curFloorStr.IndexOf("幢");
                var uninEndIndex = curFloorStr.IndexOf("单元");
                var floorEndIndex = curFloorStr.IndexOf("层");
                if (buildEndIndex == -1)
                    buildEndIndex = 0;
                if (uninEndIndex == -1)
                    uninEndIndex = 0;
                if (floorEndIndex == -1)
                    floorEndIndex = 0;
                if (floorStrLen >= buildEndIndex + 1 && floorStrLen > (uninEndIndex - buildEndIndex - 1))
                    uninName = curFloorStr.Substring(buildEndIndex + 1, uninEndIndex - buildEndIndex - 1);
                if (floorStrLen > uninEndIndex + 2 && floorStrLen > (floorEndIndex - uninEndIndex - 1))
                    floorName = curFloorStr.Substring(uninEndIndex + 2, floorEndIndex - uninEndIndex - 2);
                //floorName = String.Format("{0:00}", int.Parse(floorName));
                #endregion
                var dirPath = string.Format("d:/fang99/{0}/{1}/{2}", projectName, buildName, curFloorStr);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                var trs = table.ChildNodes.Where(c => c.Name == "tr");
                ///获取当前页面的 房间列表

                var projBuildRoomList = roomList.Where(c => c.Text("projectName") == projectName && c.Text("buildName") == buildName).ToList();
                List<string> columnNameList = new List<string>();
                var trIndex = 0;
                foreach (var tr in trs)
                {

                    var index = 0;
                    if (!tr.InnerHtml.Contains("img"))//第一行为表头
                    {
                        // columnNameList = new List<string>();
                        //房间号,层数,房屋户型,房屋用途,房屋类型,建筑面积,房屋状态
                        columnNameList = tr.ChildNodes.Where(c => c.Name == "td").Select(c => c.InnerText.Trim()).ToList();

                        continue;
                    }

                    if (!tr.HasChildNodes)
                    {
                        continue;
                    }
                    trIndex++;
                    var tds = tr.ChildNodes.Where(c => c.Name == "td").ToList();
                    if (tds.Count <= 0) continue;
                    var floorIndex = 0;
                    var floorFormatstr = floorName;
                    if (int.TryParse(floorName, out floorIndex))
                    {
                        floorFormatstr = String.Format("{0:00}", floorIndex);
                    }
                    var roomName = string.Format("{0}{1}{2}", uninName, floorFormatstr, String.Format("{0:00}", trIndex)); //房间号31001
                    //此处需要注意虽然算出来的roomName=10006 但是其图片的hash的值 为1005 转变为添加1005 而10006永远没添加导致每次执行处理会增加一条                                                                                                                          //添加房间处理
                    var roomDoc = projBuildRoomList.Where(c => c.Text("roomNum") == roomName).FirstOrDefault();
                    var needChange = false;
                    if (roomDoc == null)
                    {
                        roomDoc = new BsonDocument();
                        roomDoc.Set("roomNum", roomName);//房间详情
                        roomDoc.Set("projectName", projectName);//项目名
                        roomDoc.Set("buildName", buildName);//楼栋
                                                            // roomDoc.Set("floorName", floorName);//层数
                        roomDoc.Set("floorInfo", curFloorStr);//层数
                        roomDoc.Set("projId_site", projId);//项目Id
                        roomDoc.Set("buildId_site", buildId);//楼栋Id
                        needChange = true;
                    }


                    foreach (var td in tds)
                    {
                        var curColumnName = columnNameList[index];
                        var curMongoColumnName = string.Empty;
                        var base64Doc = new BsonDocument();//base64映射表
                        if (!td.HasChildNodes)
                        {
                            continue;
                        }
                        index++;
                        switch (curColumnName)
                        {
                            case "建筑面积"://建筑面积需要保存图片进行识别
                                {
                                    //base64Doc.Set("value", columnBase64);
                                    curMongoColumnName = "buildArea";
                                    break;
                                }
                            case "房间号":
                                base64Doc.Set("type", "1");
                                base64Doc.Set("preValue", roomName);
                                base64Doc.Set("roomNum", string.Format("{0}{1}", curFloorStr, roomName));
                                curMongoColumnName = "roomNum";
                                break;
                            case "层数":
                                base64Doc.Set("type", "2");
                                base64Doc.Set("preValue", floorName);
                                curMongoColumnName = "floor";
                                break;
                            case "房屋户型":
                                base64Doc.Set("type", "3");
                                curMongoColumnName = "pattern";
                                break;
                            case "房屋用途":
                                base64Doc.Set("type", "4");
                                curMongoColumnName = "useType";
                                break;
                            case "房屋类型":
                                base64Doc.Set("type", "5");
                                curMongoColumnName = "type";
                                break;
                            case "房屋状态":
                                //base64Doc.Set("type", "6");//不用保存图片
                                curMongoColumnName = "status";
                                break;
                            default:
                                continue;//开启下一次

                        }
                        var img = td.ChildNodes.Where(c => c.Name == "img").FirstOrDefault();
                        var imgFileName = string.Empty;
                        var columnBase64 = string.Empty;//图片的字符串映射识别
                        var columnBase64Value = string.Empty;//转换后的base字符串
                        var imgPth = string.Empty;
                        if (img != null)
                        {
                            ///ashx/texttopic.ashx?prarm=E4VXEdxoMWv4D4l6VQts1y0YlmCa8pkc&amp;color=Gray
                            imgPth = string.Format("http://www.fang99.com/{0}", img.Attributes["src"].Value.ToString().Replace("color=Gray", ""));
                            var imgQueryStr = GetQueryString(imgPth);
                            imgFileName = string.Format("{0}/{1}-{2}.jpg", dirPath, trIndex, curColumnName);
                            //获取查询字符串中的base64码
                            if (!string.IsNullOrEmpty(imgQueryStr))
                            {
                                var dic = HttpUtility.ParseQueryString(imgQueryStr);
                                columnBase64 = dic["prarm"] != null ? dic["prarm"].ToString() : string.Empty;
                                var filePathHashCode = columnBase64.GetHashCode().ToString();
                                base64Doc.Set("hash", filePathHashCode);
                                imgFileName = string.Format("D:/fang99base64/{0}.jpg", filePathHashCode);
                                var curMongoColumnNameBase64 = curMongoColumnName + "_base64";//roomNumbase64
                                if (roomDoc.Text(curMongoColumnNameBase64) != columnBase64)//保存房间字段
                                {
                                    if (!string.IsNullOrEmpty(roomDoc.Text(curMongoColumnNameBase64)))
                                    {
                                        roomDoc.Set(curMongoColumnNameBase64 + "_bak", roomDoc.Text(curMongoColumnNameBase64));//备份上一次的base64防止算法改变
                                    }
                                    roomDoc.Set(curMongoColumnNameBase64, columnBase64);//房间详情
                                    needChange = true;
                                }
                            }
                            BsonDocument existBase64Doc = new BsonDocument();
                            lock (lockThis)
                            {
                                existBase64Doc = base64MapList.Where(c => c.Text("base64Str") == columnBase64).FirstOrDefault();
                            }
                            if (existBase64Doc == null)
                            {
                                base64Doc.Set("imagePath", imgFileName);
                                base64Doc.Set("base64Str", columnBase64);
                                StorageData itemUpdate = new StorageData
                                {
                                    Document = base64Doc,
                                    Name = "Crawler_Base64Map",
                                    Type = StorageType.Insert
                                };
                                updateStorageDataList.Add(itemUpdate);
                                lock (lockThis)
                                {
                                    base64MapList.Add(base64Doc);//防止重复存取
                                }

                                if (curMongoColumnName != "roomNum")//不存房号
                                {
                                    if (!File.Exists(imgFileName))
                                        lock (lockRoom)
                                        {
                                            ImgSave(imgPth, imgFileName);
                                        }
                                }

                            }
                            else
                            {
                                columnBase64Value = existBase64Doc.Text("value");//这里只能使用value而不能使用预想值，可能导致无限添加
                                if (!string.IsNullOrEmpty(columnBase64Value) && roomDoc.Text(curMongoColumnName) != columnBase64Value)
                                {

                                    roomDoc.Set(curMongoColumnName, columnBase64Value);//房间详情转义后的值
                                    needChange = true;
                                }
                            }
                        }
                        else
                        {
                            var statusText = td.InnerText.Replace(" ", "").Trim();
                            if (roomDoc.Text(curMongoColumnName) != statusText)
                            {
                                roomDoc.Set(curMongoColumnName, statusText);//状态  
                                needChange = true;
                            }

                        }



                    }//td循环结束
                    if (needChange)
                    {
                        if (string.IsNullOrEmpty(roomDoc.Text("_id")))//添加
                        {
                            StorageData itemUpdate = new StorageData
                            {
                                Document = roomDoc,
                                Name = "Crawler_Room_Test",
                                Type = StorageType.Insert
                            };
                            updateStorageDataList.Add(itemUpdate);
                            //roomList.Add(roomDoc);//防止重复存取
                        }
                        else//更新
                        {
                            StorageData itemUpdate = new StorageData
                            {
                                Document = roomDoc,
                                Name = "Crawler_Room_Test",
                                Query = Query.And(Query.EQ("buildId", roomDoc.Text("buildId")), Query.EQ("roomNum", roomDoc.Text("roomNum"))),
                                Type = StorageType.Update
                            };
                            updateStorageDataList.Add(itemUpdate);
                        }
                    }


                }//tr结束

            }

            if (updateStorageDataList.Count() > 0)
            {

                var result = dataop.BatchSaveStorageData(updateStorageDataList);
                if (result.Status != Status.Successful)
                {
                    Console.WriteLine(string.Format("{0}更新失败", args.Url));
                }

                //updateStorageDataList.Clear();
            }
            //var url = args.Url;
            return parseHtml;

        }


        /// <summary>
        /// 数据到达
        /// </summary>
        /// <param name="args"></param>
        private static void fang99DataReceive(DataReceivedEventArgs args)
        {
            if (!args.Url.Contains("buildinglistselect")) return;//url过滤)
            if (!URLCanAdd(args.Url)) return;
            var parseHtml = fang99InitProcess(args);
            if (parseHtml.Contains("访问出错"))//被限定ip
            {
                IPInvalidProcess(args.IpProx);
            }
        
 
        }

        /// <summary>
        /// http://www.fang99.com/
        /// </summary>
        /// <param name="imgPath"></param>
        private static void ImgSave(string imgUrl, string imgPath)
        {

            try
            {
                //命名空间System.Net下的HttpWebRequest类
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(imgUrl);
                //参照浏览器的请求报文 封装需要的参数 这里参照ie9
                //浏览器可接受的MIME类型
                request.Accept = "image/webp,*/*;q=0.8";
                //包含一个URL，用户从该URL代表的页面出发访问当前请求的页面
                request.Referer = "http://www.fang99.com/";
                //浏览器类型，如果Servlet返回的内容与浏览器类型有关则该值非常有用
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.1; Trident/5.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; .NET4.0C; .NET4.0E)";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                //请求方式
                request.Method = "Get";
                //是否保持常连接
                request.KeepAlive = false;
                request.Headers.Add("Accept-Encoding", "gzip, deflate,sdch");

                //响应
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //判断响应的信息是否为压缩信息 若为压缩信息解压后返回
                if (response.ContentEncoding == "gzip")
                {
                    MemoryStream ms = new MemoryStream();
                    GZipStream zip = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress);
                    byte[] buffer = new byte[1024];
                    int l = zip.Read(buffer, 0, buffer.Length);
                    while (l > 0)
                    {
                        ms.Write(buffer, 0, l);
                        l = zip.Read(buffer, 0, buffer.Length);
                    }


                    //result = Encoding.UTF8.GetString(ms.ToArray());

                    FileStream fs = new FileStream(imgPath, FileMode.OpenOrCreate);
                    BinaryWriter w = new BinaryWriter(fs);
                    w.Write(ms.ToArray());
                    fs.Close();
                    ms.Close();

                    ms.Dispose();
                    zip.Dispose();
                }

            }
            catch (Exception exception)
            {

                //   throw;
            }
        }

        #endregion
    }
}
