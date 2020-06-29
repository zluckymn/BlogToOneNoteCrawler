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
    class Program
    {
     
         
        static void Main(string[] args)
        {
         
            QuickConsistentHashHelper.Instance_EnterpriseDetailInfo().GetHashItem("9445f1deba10017756f11f042ba77a5e");
            Console.WriteLine("1 企业详情接收  2企业背后关系 3企业投资关系");
            var type = Console.ReadLine();
            switch (type)
            {
                case "2":
                    var entDetailDealHelper_bg = new EnterpriseDetailInfo_Background();
                    entDetailDealHelper_bg.Start();
                    break;
                case "3":
                    var entDetailDealHelper_event = new EnterpriseDetailInfo_Invent();
                    entDetailDealHelper_event.Start();
                    break;
                case "1":
                  default:
                    var entDetailDealHelper = new EnterpriseDetailInfoBase();
                    entDetailDealHelper.Start();
                    break;
            }
           
            var getInput = Console.ReadLine().ToLower();
            while (getInput == "q" || getInput == "exit")
            {
                getInput = Console.ReadLine().ToLower();
            }
            MQHelper.Instance().Dispose();
           // StartDBChangeHelper.StartDBChangeProcessQuick(mongoOp);
        }
 
    }
}
