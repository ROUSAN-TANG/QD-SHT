using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Pigeon.Utility;
using System.Threading;

namespace Pigeon.Sht.BLL
{
    public class Publish : BasePost
    {

        public List<Models.EProduct> Product { get; set; }
        public Publish()
        {
        }

        public void SetData(List<Models.EProduct> items)
        {
            Product = items;
            PostData = new NameValuelist();
        }

        private readonly object m_lockObject = new object();
        public void Post(object obj)
        {
            Data = (Models.EPublishData)obj;
            if (null == Data) return;
            Data.Company = new Models.ECompany();
            ShowLog(string.Format("[{0}],准备发布信息；", Data.WebSite.Encoding.BodyName));

            foreach (Models.EProduct item in Product)
            {
                lock (m_lockObject)
                {
                    try
                    {
                        Data.UserID = item.UserID;
                        if (Data.WebSite.ID == 4)
                        {
                            if (Data.Company.UserID != item.UserID && !Login.UserLogin(Data, item.UserID))
                            {
                                SendMessage(201, "登陆失败，放弃操作。\r\n -> " + Data.Products[0].Title);
                                BLL.Product.SetPublishLog(Data.Products[0].ProductID, string.Format("[{0}]{1}", Data.WebSite.Name, "发布失败 登陆失败，放弃操作。 \r\n -> " + Data.Products[0].Title));
                                continue;
                            }
                        }
                        else
                        {
                            if (Data.Company.UserID != item.UserID && !Login.UserLogin(Data, item.UserID))
                            {
                                SendMessage(201, "登陆失败，放弃操作。\r\n -> " + Data.Products[0].Title);
                                BLL.Product.SetPublishLog(Data.Products[0].ProductID, string.Format("[{0}]{1}", Data.WebSite.Name, "发布失败 登陆失败，放弃操作。 \r\n -> " + Data.Products[0].Title));
                                continue;
                            }
                        }
                        //发布信息
                        Business.Send(this.Data, item);
                    }
                    catch (Exception ex)
                    {
                        SendMessage(500, ex.Message);
                    }
                    Thread.Sleep(1);
                }
            }
            OnComplete(this);
        }
    }
}