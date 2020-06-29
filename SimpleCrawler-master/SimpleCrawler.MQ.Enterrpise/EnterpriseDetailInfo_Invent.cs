using Helper;
using Helper.ConsistentHash;
using MongoDB.Bson;
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
    public class EnterpriseDetailInfo_Invent : EnterpriseDetailInfoBase
    {
       
        /// <summary>
        /// 使用一直性hash将数据分片处理
        /// </summary>
        

        override
        public bool TestHashIsTrue()
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
            var tableName3IsTrue = tableName3 == "QCCEnterpriseKey_InventInfo_Hash_17";
            var dbIsTrue = dbName == "EnterpriseInventInfo" && dbName2 == "EnterpriseInventInfo";
            return dbIsTrue && tableName == "QCCEnterpriseKey_InventInfo_Hash_48" && tableName2 == "QCCEnterpriseKey_InventInfo_Hash_17";
        }
        override
        public void Start()
        {
             tableName = "QCCEnterpriseKey_InventInfo"; 
             dataBaseName = "EnterpriseInventInfo";
             virtualHost = "mz.core.enterprise_inventInfo";
             subscribeId = "inventInfo";
            //初始化表与数据库
            QuickConsistentHashHelper.Instance_EnterpriseGuid(tableName: tableName, dataBaseName: dataBaseName);

            TestQuickConsistentHashHelper();
            if (!TestHashIsTrue())
            {
                Console.WriteLine("hash分配不同机器无法匹配,按任意键退出");
                Console.ReadKey();
                return;
            }
            // mongoOp = MongoOpCollection.GetNew121MongoOp_MT("EnterpriseDetail"); ;
            MQHelper.Instance().Init(virtualHost);
            MQHelper.Instance().SubscribeAsync<string>(subscribeId, async (docStr) => {
                await DealData(docStr);
            });
            ShowTipMessage();
            // StartDBChangeHelper.StartDBChangeProcessQuick(mongoOp);
        }
         
    }
}
