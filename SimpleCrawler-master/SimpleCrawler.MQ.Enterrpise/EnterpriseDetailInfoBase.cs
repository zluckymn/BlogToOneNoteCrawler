using Helper;
using Helper.ConsistentHash;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MZ.RabbitMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DataRule;

namespace SimpleCrawler.MQ.Enterrpise
{
    public class EnterpriseDetailInfoBase
    {
        static Dictionary<string, MongoOperation> mongoOpDic = new Dictionary<string, MongoOperation>();
        
        /// <summary>
        /// 使用一直性hash将数据分片处理
        /// </summary>
        internal   string tableName = "QCCEnterpriseKey_DetailInfo";//20200101-20200114的详细数据
        internal   string dataBaseName = "EnterpriseDetailInfo";
        internal   string virtualHost = "mz.core.enterprise_info";
        internal   string subscribeId = "enterpriseInfo";
        virtual
        public   bool TestHashIsTrue()
        {
            var guid = "p7e5d85910edd29d2dbf64b88e3032a8";
            var serverNode = GetEntTableNameByConsistenHash(guid);
            var tableName = serverNode.Type;
            var dbName = serverNode.Db;
            var guid2 = "f6ae344d2d8fd2ffe7ee1fe9e159a623";
            var serverNode2 = GetEntTableNameByConsistenHash(guid2);
            var tableName2 = serverNode2.Type;
            var dbName2 = serverNode2.Db;
            var guid3 = "73bd41e5d51380dc3ed490e93e217c6f";
            var serverNode3 = GetEntTableNameByConsistenHash(guid3);
            var tableName3 = serverNode2.Type;
            var dbName3 = serverNode2.Db;
            var tableName3IsTrue = tableName3 == "QCCEnterpriseKey_DetailInfo_Hash_17";
            var dbIsTrue = dbName == "EnterpriseDetailInfo" && dbName2 == "EnterpriseDetailInfo";
            return dbIsTrue && tableName == "QCCEnterpriseKey_DetailInfo_Hash_48" && tableName2 == "QCCEnterpriseKey_DetailInfo_Hash_17";
        }
#pragma warning disable CS1998 // 此异步方法缺少 "await" 运算符，将以同步方式运行。请考虑使用 "await" 运算符等待非阻止的 API 调用，或者使用 "await Task.Run(...)" 在后台线程上执行占用大量 CPU 的工作。
        public   async Task TestQuickConsistentHashHelper()
#pragma warning restore CS1998 // 此异步方法缺少 "await" 运算符，将以同步方式运行。请考虑使用 "await" 运算符等待非阻止的 API 调用，或者使用 "await Task.Run(...)" 在后台线程上执行占用大量 CPU 的工作。
        {
            Dictionary<string, int> dic = new Dictionary<string, int>();
            var tableHasCreateIndex = new List<string>();
            for (var index = 0; index <= 10000; index++)
            {
                var guid = Guid.NewGuid().ToString();
                var doc = new BsonDocument();
                doc.Set("guid", guid);
                var serverNode = GetEntTableNameByConsistenHash(doc);
                var tableName = serverNode.Type;
                var db = serverNode.Db;
                var key = $"{db}|{tableName}";
                if (!dic.ContainsKey(key))
                {
                    dic.Add(key, 1);
                }
                else
                {
                    dic[key] = ++dic[key];
                }
                if (!tableHasCreateIndex.Contains(tableName))
                {
                    MongoOperation mongoOp = GetMongoOp(db);
                    CreateSignalIndex(mongoOp, tableName, "guid");
                    tableHasCreateIndex.Add(tableName);
                }

            }
            foreach (var item in dic)
            {
                Console.WriteLine($"{item.Key}_{item.Value}");
            }
        }

        private void CreateSignalIndex(MongoOperation op, string tableName, string field, int order = 1)
        {

            IMongoIndexKeys keys = new IndexKeysDocument { { field, order } };
            IMongoIndexOptions options = IndexOptions.SetUnique(false).SetBackground(true);
            op.CreateIndex(tableName, keys, options);

        }

        virtual
        public void Start()
        {
            TestQuickConsistentHashHelper();
            if (!TestHashIsTrue())
            {
                Console.WriteLine("hash分配不同机器无法匹配,按任意键退出");
                Console.ReadKey();
                return;
            }
            //初始化表与数据库
            QuickConsistentHashHelper.Instance_EnterpriseGuid(tableName: tableName, dataBaseName: dataBaseName);
            // mongoOp = MongoOpCollection.GetNew121MongoOp_MT("EnterpriseDetail"); ;
            MQHelper.Instance().Init(virtualHost);
            MQHelper.Instance().SubscribeAsync<string>(subscribeId, async (docStr) => {
                await DealData(docStr);
            });
            ShowTipMessage();
           
            // StartDBChangeHelper.StartDBChangeProcessQuick(mongoOp);
        }

        internal ConsistentHashNode GetEntTableNameByConsistenHash(string guid)
        {
            var serverNode = QuickConsistentHashHelper.Instance_EnterpriseGuid().GetHashItem(guid);
            return serverNode;
        }
#pragma warning disable CS1998 // 此异步方法缺少 "await" 运算符，将以同步方式运行。请考虑使用 "await" 运算符等待非阻止的 API 调用，或者使用 "await Task.Run(...)" 在后台线程上执行占用大量 CPU 的工作。
        internal MongoOperation GetMongoOp(string dataBase)
        {
            var key = $"{dataBase}";
            //获取连接字符串
            MongoOperation mongoOp = null;
            if (mongoOpDic.ContainsKey(key))
            {
                mongoOp = mongoOpDic[key];
            }
            else
            {
                mongoOp = MongoOpCollection.GetNew121MongoOp_MT(dataBase);
                mongoOpDic.Add(key, mongoOp);
            }
            return mongoOp;
        }

        virtual
        internal async Task DealData(string docStr)
         #pragma warning restore CS1998 // 此异步方法缺少 "await" 运算符，将以同步方式运行。请考虑使用 "await" 运算符等待非阻止的 API 调用，或者使用 "await Task.Run(...)" 在后台线程上执行占用大量 CPU 的工作。
        {
            var doc = docStr.GetBsonDocFromJson();
            var guid = doc.Text("guid");
            //获取一致性hash对应的表名
            var serverNode = GetEntTableNameByConsistenHash(doc);
            var entHashTableName = serverNode.Type;
            var dataBase = serverNode.Db;
            if (string.IsNullOrEmpty(entHashTableName))
            {
                entHashTableName = tableName;
            }
            
            //获取连接字符串
            MongoOperation mongoOp = GetMongoOp(dataBase);
             

            var query = Query.EQ("guid", guid);
            Console.WriteLine($"开始执行{guid}");
            var hasExist = mongoOp.FindCount(entHashTableName, query) > 0;
            if (hasExist)
            {
                StartDBChangeHelper.SaveStorageDataToDB(mongoOp, new StorageData() { Document = doc, Name = entHashTableName, Query = Query.EQ("guid", guid), Type = StorageType.Update });

            }
            else
            {
                StartDBChangeHelper.SaveStorageDataToDB(mongoOp, new StorageData() { Document = doc, Name = entHashTableName, Query = Query.EQ("guid", guid), Type = StorageType.Insert });
            }

            Console.WriteLine($"{guid}执行结束位置：{tableName}{entHashTableName}");
        }

        internal ConsistentHashNode GetEntTableNameByConsistenHash(BsonDocument doc)
        {
            return GetEntTableNameByConsistenHash(doc.Text("guid"));
        }



        internal void ShowMessage(string str)
        {
            Console.WriteLine(str);
        }
        internal void ShowTipMessage()
        {  
             Console.WriteLine($"当前设置:{tableName}{dataBaseName}{virtualHost}{subscribeId}");
;            Console.WriteLine("输入q或者exit 进行退出");
        }
    }
}
