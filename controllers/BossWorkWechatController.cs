using BrnMall.Core;
using BrnMall.Manager.Domain.WeChat;
using BrnMall.Manager.Manager.AddressBook;
using BrnMall.Manager.Manager.WeChat;
using BrnMall.Web.Framework;
using BrnMall.WeiXin.Sdk.Domain;
using BrnMall.WeiXin.Sdk.Work;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace BrnMall.Web.controllers
{
    public class BossWorkWechatController : BaseWebController
    {
        public string Setting()
        {
            string echostr = WebHelper.GetQueryString("echostr");
            //URL验证 (企业微信后台，更换回调URL)
            if (!string.IsNullOrEmpty(echostr))
            {
                return Validate();
            }

            var xmlData = DecodeData();
            var path = string.Format("{0}/boss_setting_{1}.log", Request.MapPath("/log/"), DateTime.Now.ToString("MMdd"));
            Log(xmlData, path);
            return "";
        }

        public string DataNotify()
        {
            string echostr = WebHelper.GetQueryString("echostr");
            //URL验证 (企业微信后台，更换回调URL)
            if (!string.IsNullOrEmpty(echostr))
            {
                return Validate();
            }

            var path = string.Format("{0}/boss_DataNotify{1}.log", Request.MapPath("/log/"), DateTime.Now.ToString("MMdd"));
            var xmlData = DecodeData();

            Log(xmlData, path);
            return "";
        }


        #region boss销售安装、回调

        /// <summary>
        /// 安装企业微信的应用
        /// </summary>
        public void Install()
        {
            int storeId = WebHelper.GetQueryInt("storeid", 0);
            if (storeId < 1)
            {
                Response.Write("授权失败：请选择店铺<script>setTimeout(function () { window.location.href = 'http://weadmin.711688.net.cn/storeadmin' }, 2000)</script>");
                return;
            }

            //获取ticket
            var ticket = SuiteTicketManager.GetSuiteTicket(2);
            //获取套餐凭证
            var suiteToken = BossHelper.Get_Suite_Token(ticket.SuiteTicket);
            //获取预授权码
            var authCode = WorkWeiXinSDK.Get_Pre_Auth_Code(suiteToken);

            //设置授权配置
            WorkWeiXinSDK.set_session_info(suiteToken, authCode, 1);

            //用户标识
            string state = "";
            //返回安装路径
            string url = BossHelper.GetInstallUrl(authCode, state, storeId.ToString());
            Response.Redirect(url);
        }


        public string InstallNotify()
        {
            string authCode = Request["auth_code"];
            int storeId = WebHelper.GetQueryInt("storeid", 0);

            //获取ticket
            var ticket = SuiteTicketManager.GetSuiteTicket(2);

            //获取套餐凭证
            var suiteToken = BossHelper.Get_Suite_Token(ticket.SuiteTicket);

            //获取永久授权码以及授权信息
            var pCode = WorkWeiXinSDK.Get_Permanent_Code(authCode, suiteToken);

            var agent_info = pCode.auth_info.Agent[0];
            var auth_corp = pCode.auth_corp_info;
            var model = new PermanentCodeInfo()
            {
                StoreId = storeId,
                permanent_code = pCode.permanent_code,  //永久授权码
                access_token = pCode.access_token,      //企业的access_token
                last_expires = DateTime.Now,            //access_token 的获取时间
                agentid = agent_info.agentid,
                app_name = agent_info.name,
                corpid = auth_corp.corpid,
                corp_full_name = auth_corp.corp_full_name,
                AddTime = DateTime.Now
            };

            PermanentCodeManager.Add(model);
      
            Redirect(WorkContext.MallConfig.SiteUrl);

            return string.Empty;
        }


        public string CmdNotify()
        {
            string echostr = WebHelper.GetQueryString("echostr");
            //URL验证 (企业微信后台，更换回调URL)
            if (!string.IsNullOrEmpty(echostr))
            {
                return Validate();
            }

            var xmlData = DecodeData();
            var infoType = SuiteTicketHelper.GetInfoType(xmlData).ToLower();
            if (infoType == "suite_ticket")
            {
                //成功时,从解密后的xml读取suite_ticket、suiteid
                Tuple<string, string> ticket = SuiteTicketHelper.GetTicketInfo(xmlData);
                SuiteTicketManager.Add(
                    new SuiteTicketInfo() { AppType = 2, SuiteId = ticket.Item1, SuiteTicket = ticket.Item2, AddTime = DateTime.Now }
                );
            }

            var path = string.Format("{0}/boss_cmd_{1}.log", Request.MapPath("/log/"), DateTime.Now.ToString("MMdd"));
            Log(xmlData, path);
            return "success";
        }



        string getXmlData()
        {
            string xmlData = string.Empty;
            byte[] data = Request.BinaryRead(Request.TotalBytes);
            xmlData = Encoding.UTF8.GetString(data);
            //xmlData = "<xml><ToUserName><![CDATA[tj5fc2cfeb571d64b6]]></ToUserName><Encrypt><![CDATA[0zIwTfNEbx4NFO0vWg/NVPjSmDtla7khisVNwJJB7ddy5feQpXOAnvKKPDdoiwQxlb7j36ofHX1zmJXJNXzoqIUEZuNxJMhJAQFgsX49QdZSKc9PMdJNCuXoWdm/DH/cUP2uL0KbZ09i77p8ZqdIKKN6T/fm86MQWge1i3hjAAA87xqgFCnFHyXcier6TM8Ph+bjuwnh0Kjk5rMyJTGiH+lzzT3hhNVNO3E9f6jByzLyZyOZBpD/6ZhJjBkiwdtNaMHy/4+RRw90wPIZ/FOQ/eHcaMMM1GUQcMsahBv2ZP6/27Z0PwRL5I5dFtQmECkvS8nsMgXTBFvW4hmo4lc3arfA3/TapNYHp27mkdYIfCjVCfe1GgZsKTtx66QVIUvg]]></Encrypt><AgentID><![CDATA[]]></AgentID></xml>";
            return xmlData;
        }

        string DecodeData()
        {
            string msg_signature = WebHelper.GetQueryString("msg_signature");
            string timestamp = WebHelper.GetQueryString("timestamp");
            string nonce = WebHelper.GetQueryString("nonce");
            string xmlData = getXmlData();   //加密过的xml

            return BossHelper.DecodeXml(msg_signature, timestamp, nonce, xmlData);
        }

        string Validate()
        {
            string sVerifyMsgSig = WebHelper.GetQueryString("msg_signature");
            string sVerifyTimeStamp = WebHelper.GetQueryString("timestamp");
            string sVerifyNonce = WebHelper.GetQueryString("nonce");
            string sVerifyEchoStr = WebHelper.GetQueryString("echostr");

            return BossHelper.Validate(sVerifyMsgSig, sVerifyTimeStamp, sVerifyNonce, sVerifyEchoStr);
        }

        void Log(string msg, string path)
        {
            using (StreamWriter sw = new StreamWriter(path, true))
            {
                sw.WriteLine(string.Format("{0}\t{1}", DateTime.Now.ToString(), msg));
            }
        }
        #endregion


        #region 网页授权:boss销售

        public void OauthLogin()
        {
            Response.Redirect(BossHelper.authorize("http://crm.711688.net.cn/BossWorkWechat/OauthCallback"));
        }

        public void OauthCallback(string code = "")
        {
            //第二步：根据code获取token失败

            //获取ticket
            var ticket = SuiteTicketManager.GetSuiteTicket(1);
            //获取套餐凭证
            Suite_Token suiteToken = BossHelper.Get_Suite_Token(ticket.SuiteTicket);

            var json = WorkWeiXinSDK.GetOauthUser(suiteToken.suite_access_token, code);
            if (json == null || json.errcode != 0)
            {
                //错误页面
                Response.Redirect(string.Format("/admin_wjk/pages/error.html?msg=登录授权失败,{0}", json.errmsg));
            }
            if (string.IsNullOrEmpty(json.UserId))
            {
                //找不到用户
                Response.Redirect("/admin_wjk/pages/error.html?msg=找不到用户");
            }

            var perCode = PermanentCodeManager.GetByCorpid(json.CorpId);
            if (perCode == null)
            {
                //找不到授权企业
                Response.Redirect("/admin_wjk/pages/error.html?msg=找不到授权企业");
            }

            var staff = StaffManager.GetByWxUserId(perCode.StoreId, json.UserId);
            if (staff == null)
            {
                //找不到成员
                Response.Redirect("/admin_wjk/pages/error.html?msg=找不到公司成员");
            }

            if (staff.AiState != 1)
            {
                Response.Redirect("/admin_wjk/pages/error.html?msg=未开通AI雷达");
            }

            WebHelper.SetCookie("wjk_staff", "storeid", staff.StoreId.ToString(), 90);
            WebHelper.SetCookie("wjk_staff", "staffid", staff.Id.ToString(), 90);
            WebHelper.SetCookie("wjk_staff", "uid", staff.Uid.ToString(), 90);
            WebHelper.SetCookie("wjk_staff", "aistate", staff.AiState.ToString(), 90);
            WebHelper.SetCookie("wjk_staff", "bossstate", staff.BossState.ToString(), 90);

            Response.Redirect("/admin_wjk/pages/bossRadarIndex.html?rand=" + new Random().Next(100000, 999999));
        }

        #endregion

    }
}