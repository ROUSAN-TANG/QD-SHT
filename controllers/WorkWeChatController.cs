using BrnMall.Core;
using BrnMall.Manager.Domain.AddressBook;
using BrnMall.Manager.Domain.WeChat;
using BrnMall.Manager.Manager.AddressBook;
using BrnMall.Manager.Manager.WeChat;
using BrnMall.Services.WJK;
using BrnMall.Web.Framework;
using BrnMall.WeiXin.Sdk.Domain;
using BrnMall.WeiXin.Sdk.Work;
using Newtonsoft.Json;
using QIDong.WeApp.Util;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace BrnMall.Web.controllers
{
    public class WorkWeChatController : BaseWebController
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
            var path = string.Format("{0}/work_setting_{1}.log", Request.MapPath("/log/"), DateTime.Now.ToString("MMdd"));
            Log(xmlData, path);
            return "";
        }

    
        /// <summary>
        /// 通讯录通知
        /// </summary>
        /// <returns></returns>
        public string UserNotify()
        {
            return UserValidate();
        }

        public void InstallNotifyByUser()
        {
            string authCode = Request["auth_code"];
            int storeId = WebHelper.GetQueryInt("storeid", 0);

            //获取ticket
            var ticket = SuiteTicketManager.GetSuiteTicket(1);

            //获取套餐凭证
            Suite_Token suiteToken = UserHelper.Get_Suite_Token(ticket.SuiteTicket);

            //获取永久授权码以及授权信息
            PermanentCode pCode = WorkWeiXinSDK.Get_Permanent_Code(authCode, suiteToken);

            Agent_Info agent_info = pCode.auth_info.Agent[0];
            Auth_Corp_Info auth_corp = pCode.auth_corp_info;
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
        }

        public string DataNotify()
        {
            string echostr = WebHelper.GetQueryString("echostr");
            //URL验证 (企业微信后台，更换回调URL)
            if (!string.IsNullOrEmpty(echostr))
            {
                return Validate();
            }

            var path = string.Format("{0}/work_data_{1}.log", Request.MapPath("/log/"), DateTime.Now.ToString("MMdd"));
            var xmlData = DecodeData();

            Log(xmlData, path);
            return "";
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
                //Log("suite_ticket: " + xmlData);

                //成功时,从解密后的xml读取suite_ticket、suiteid
                Tuple<string, string> ticket = SuiteTicketHelper.GetTicketInfo(xmlData);
                SuiteTicketManager.Add(
                    new SuiteTicketInfo() { AppType = 1, SuiteId = ticket.Item1, SuiteTicket = ticket.Item2, AddTime = DateTime.Now }
                );
            }

            var path = string.Format("{0}/work_cmd_{1}.log", Request.MapPath("/log/"), DateTime.Now.ToString("MMdd"));
            Log(xmlData, path);
            return "success";
        }

  

        #region AI销售安装、回调

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
            var ticket = SuiteTicketManager.GetSuiteTicket(1);
            //获取套餐凭证
            Suite_Token suiteToken = AIHelper.Get_Suite_Token(ticket.SuiteTicket);
            //获取预授权码
            PreAuthCode authCode = WorkWeiXinSDK.Get_Pre_Auth_Code(suiteToken);

            //设置授权配置
            WorkWeiXinSDK.set_session_info(suiteToken, authCode, 1);

            //用户标识
            string state = "";
            //返回安装路径
            string url = AIHelper.GetInstallUrl(authCode, state, storeId.ToString());
            Response.Redirect(url);
        }

        /// <summary>
        /// 安装应用后的回调
        /// </summary>
        /// <returns></returns>
        public void InstallNotify()
        {
            string authCode = Request["auth_code"];
            int storeId = WebHelper.GetQueryInt("storeid", 0);

            //获取ticket
            var ticket = SuiteTicketManager.GetSuiteTicket(1);

            //获取套餐凭证
            Suite_Token suiteToken = AIHelper.Get_Suite_Token(ticket.SuiteTicket);

            //获取永久授权码以及授权信息
            PermanentCode pCode = WorkWeiXinSDK.Get_Permanent_Code(authCode, suiteToken);

            Agent_Info agent_info = pCode.auth_info.Agent[0];
            Auth_Corp_Info auth_corp = pCode.auth_corp_info;
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

            //导入部门
            ImportDepartment(storeId);
            //导入部门成员
            ImportUser(storeId);

            Redirect(WorkContext.MallConfig.SiteUrl);
        }

        #endregion

 

        #region 部门、员工

        /// <summary>
        /// 导入部门
        /// </summary>
        /// <param name="storeId"></param>
        public void ImportDepartment(int storeId)
        {
            var permanentCode = WorkWeChats.GetAccessToken(storeId);
            var list = WorkWeiXinSDK.GetDepartmentList(permanentCode.access_token, string.Empty);

            var oldList = DepartmentManager.GetList(storeId);
            //导入微信通讯录的部门
            foreach (var item in list.department)
            {
                //部门不存在,创建部门
                var old = oldList.Find(x => x.Name.ToLower().Equals(item.name.ToLower()));
                if (old == null)
                {
                    var newModel = new Department() { StoreId = storeId, Name = item.name, Parentid = item.parentid, Sort = item.order, wxDepartmentId = item.id, AddTime = DateTime.Now };
                    DepartmentManager.Add(newModel);
                    //Response.Write($"{item.name} 导入成功 (新增)<br/>");
                }
                else
                {
                    old.Name = item.name;
                    old.Parentid = item.parentid;
                    old.Sort = item.order;
                    old.wxDepartmentId = item.id;
                    DepartmentManager.Update(old);
                    //Response.Write($"{item.name} 导入成功 (更新)<br/>");
                }
            }
        }

        public void ImportUser(int storeId)
        {
            var permanentCode = WorkWeChats.GetAccessToken(storeId);

            //已存在的成员
            var oldUserList = StaffManager.GetListByStoreId(storeId);
            //已存在的部门
            var oldDeptList = DepartmentManager.GetList(storeId);
            //已存在的员工部门关系
            var oldUnionList = StaffDepartmentManager.GetListByStoreId(storeId);

            var wxDeptId = oldDeptList.Find(x => x.wxDepartmentId > 0 && x.Parentid == 0);
            var list = WorkWeiXinSDK.GetUserList(permanentCode.access_token, wxDeptId.wxDepartmentId.ToString());

            //导入微信通讯录的成员
            foreach (var item in list.userlist)
            {
                //成员不存在,创建成员
                var old = oldUserList.Find(x => x.Name.ToLower().Equals(item.name.ToLower()));
                if (old == null)
                {
                    //创建用户
                    var userInfo = Staffs.CreateUser(storeId, item.name);
                    //开通聊天的用户
                    WebIMUtil.ImportUser(userInfo);

                    old = new Staff() { StoreId = storeId, Uid = userInfo.Uid, Name = item.name, wxUserId = item.userid, Wxid = string.Empty, Avater = string.Empty, AiState = 0, BossState = 0, MobileState = "1", Company = permanentCode.corp_full_name, ClickCount = 0, SignUpCount = 0, UpCount = 0, ShareCount = 0, QrCode = "", Sign = string.Empty, Mobile = string.Empty, Phone = string.Empty, Email = string.Empty, wxMobile = string.Empty, Address = string.Empty, Position = string.Empty, Department = string.Empty, Addtime = DateTime.Now };
                    StaffManager.Add(old);

                    //Response.Write($"{item.name} 导入成功 (新增) " + userInfo.Uid + "<br/>");
                }
                else
                {
                    //创建用户
                    if (old.Uid < 1)
                    {
                        //创建用户
                        var userInfo = Staffs.CreateUser(storeId, item.name);
                        //开通聊天的用户
                        WebIMUtil.ImportUser(userInfo);
                        old.Uid = userInfo.Uid;
                    }

                    StaffManager.Update(old);
                    //Response.Write($"{item.name} 导入成功 (更新)<br/>");
                }

                //导入所属部门
                foreach (var departmentId in item.department)
                {
                    //用微信的部门ID，找到系统的部门ID
                    var dept = oldDeptList.Find(x => x.wxDepartmentId == departmentId);
                    var deptId = dept != null ? dept.Id : 0;

                    //判断 员工ID，系统部门ID是否相同
                    var oldUnion = oldUnionList.Find(x => x.StaffId == old.Id && x.DepartmentId == deptId);
                    if (oldUnion == null)
                    {
                        StaffDepartment newUnion = new StaffDepartment() { DepartmentId = deptId, StaffId = old.Id, StoreId = storeId };
                        StaffDepartmentManager.Add(newUnion);
                    }
                }
            }
        }

        public void GetUserDetail(int storeId, string userid, string access_token = "")
        {
            if (string.IsNullOrEmpty(access_token))
            {
                var permanentCode = WorkWeChats.GetAccessToken(storeId);
                access_token = permanentCode.access_token;
            }
            var model = WorkWeiXinSDK.GetUser(access_token, userid);
            if (model != null)
                Response.Write(JsonConvert.SerializeObject(model));
        }

        #endregion

        #region 网页授权:Ai销售

        public void OauthLogin()
        {
            Response.Redirect(AIHelper.authorize("http://crm.711688.net.cn/workwechat/OauthCallback"));
        }

        public void OauthCallback(string code = "")
        {
            //第二步：根据code获取token失败

            //获取ticket
            var ticket = SuiteTicketManager.GetSuiteTicket(1);
            //获取套餐凭证
            Suite_Token suiteToken = AIHelper.Get_Suite_Token(ticket.SuiteTicket);

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


        #region 员工、部门 测试用例
        /// <summary>
        /// 部门列表
        /// </summary>
        /// <param name="storeId"></param>
        public void DepartmentList(int storeId)
        {
            var permanentCode = WorkWeChats.GetAccessToken(storeId);
            var list = WorkWeiXinSDK.GetDepartmentList(permanentCode.access_token, string.Empty);
            foreach (var item in list.department)
            {
                Response.Write(item.name + "<br/>");
            }
        }

        public void CreateDepartment(int storeId, string name = "销售二部")
        {
            var permanentCode = WorkWeChats.GetAccessToken(storeId);
            Response.Write(WorkWeiXinSDK.CreateDepartment(permanentCode.access_token, name, 3, 19));
        }


        /// <summary>
        /// 员工列表
        /// </summary>
        /// <param name="storeId"></param>
        public void UserList(int storeId, int deptId = 0)
        {
            var permanentCode = WorkWeChats.GetAccessToken(storeId);
            var list = WorkWeiXinSDK.GetUserList(permanentCode.access_token, deptId.ToString());
            foreach (var item in list.userlist)
            {
                Response.Write(item.name + "<br/>");
            }
        }

        #endregion

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

            return AIHelper.DecodeXml(msg_signature, timestamp, nonce, xmlData);
        }


        void Log(string msg)
        {
            Sdk.WeiXin.Tool.LogUtil.WriteLog(msg);
        }

        void Log(string msg, string path)
        {
            using (StreamWriter sw = new StreamWriter(path, true))
            {
                sw.WriteLine(string.Format("{0}\t{1}", DateTime.Now.ToString(), msg));
            }
        }

        #region 新的应用部署时，服务器验证代码
        /// <summary>
        /// 验证
        /// </summary>
        /// <returns></returns>
        string Validate()
        {
            string sVerifyMsgSig = WebHelper.GetQueryString("msg_signature");
            string sVerifyTimeStamp = WebHelper.GetQueryString("timestamp");
            string sVerifyNonce = WebHelper.GetQueryString("nonce");
            string sVerifyEchoStr = WebHelper.GetQueryString("echostr");

            return AIHelper.Validate(sVerifyMsgSig, sVerifyTimeStamp, sVerifyNonce, sVerifyEchoStr);
        }


        string UserValidate()
        {
            string sVerifyMsgSig = WebHelper.GetQueryString("msg_signature");
            string sVerifyTimeStamp = WebHelper.GetQueryString("timestamp");
            string sVerifyNonce = WebHelper.GetQueryString("nonce");
            string sVerifyEchoStr = WebHelper.GetQueryString("echostr");

            return UserHelper.Validate(sVerifyMsgSig, sVerifyTimeStamp, sVerifyNonce, sVerifyEchoStr);
        }

        #endregion
    }
}