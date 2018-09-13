using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Pigeon.Utility;
using System.Threading;
using System.Collections;
using System.Net;
using Pigeon.Sht.Config;
namespace Pigeon.Sht.BLL
{
    public class Login : BasePost
    {
        public static string logindata = "";
        private bool Logined { get; set; }
        private Models.ELogin model { get; set; }
      
        public Login()
        {
            Times = 0;
            Logined = false;
            UserID = 0;
        }
        public bool Post(Models.EPublishData data, int UserID)
        {
            this.Data = data;
            Message += new MessageHandler(data.Message);
            Complete += new CompleteHandler(data.Complete);
            this.OnEntryVerifyCode += new EntryVerifyCodeHandler(Data.EntryVerifyCode);
            this.OnSelClassID += new SelClassIDHandler(Data.SelClassID);
            this.OnSelVerifyID += new SelVerifyIDHandler(Data.SelVerifyID);

            Logined = false;
            this.UserID = UserID;
            Data.UserID = UserID;
            model = Data.WebSite.Login;
            Thread m_thread = new Thread(new ThreadStart(Post));
            m_thread.SetApartmentState(ApartmentState.STA);
            m_thread.Start();
            m_thread.Join();
            return Logined;
        }

        private void Post()
        {
            model.RemoteData = GetRemote(model.Remotes);
            if (Data.WebSite.ID == 10)
            {
                if (model.RemoteData["vipNum"] == null) { ShowLog("网站出现问题，获取数据失败！"); return; }
                string vipNum = model.RemoteData["vipNum"].ToString();
                if (!string.IsNullOrEmpty(vipNum))
                {
                    Data.WebSite.tempValue = vipNum;
                }
            }
            else if (Data.WebSite.ID == 74) { //客集齐
                if (string.IsNullOrEmpty(model.RemoteData.ToString()))
                {
                    Logined = true;
                    return;
                }
            }
            Data.Company = BLL.Company.GetItem(this.UserID);
            if (null == Data.Company) return;
            NameValue _UserName = BLL.Company.GetAccount(Data.WebSite, Data.Company);
            //ShowLog(string.Format("正在登陆：{0}", _UserName.Name));
            if (model.Check != 0)
            {
                Debug("检查是否已登陆.");
                // 如果登陆了，就不能获取到某个值 zhengyue
                if (model.RemoteData[model.CheckKey] == null && model.Check == 99 && GetAllCookies())
                {
                    Logined = true;
                    Debug(string.Format("用户名：{0}；已登陆。", _UserName.Name));
                    return;
                }

                if (model.RemoteData[model.CheckKey] == _UserName.Name)
                {
                    Logined = true;
                    Debug(string.Format("用户名：{0}；已登陆。", _UserName.Name));
                    return;
                }
            }


            if (Data.WebSite.ID == 162)
            { //国际机械信息网
                Helper.GetCookieContainer(UserID).Add(new System.Net.Cookie("cf_clearance", model.Clearance, "/", "www.machineryinfo.net"));
                Http.isUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36 Edge/15.15063";
            }
            else if (Data.WebSite.ID == 164) { //万金网
                Helper.GetCookieContainer(UserID).Add(new System.Net.Cookie("cf_clearance", model.Clearance, "/", "www.b2bwj.com"));
                Http.isUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36 Edge/15.15063";
            }
            else if (Data.WebSite.ID == 3)
            { //马可波罗
                Helper.GetCookieContainer(UserID).Add(new System.Net.Cookie(model.Clearance.Split('=')[0], model.Clearance.Split('=')[1], "/", "my.b2b.makepolo.com"));
            }

            string data = model.Params;

            data = EnData(data);
            if (Data.WebSite.ID == 145)
            {
                data = Html.ReplaceN(data, "\r\n");
            }

            //慧聪网，登录前清除Cookie，防止登录失败
            if (Data.WebSite.ID == 124)
            {
                string[] arrUrl = { Data.WebSite.Login.Post, Data.WebSite.Login.Url };
                Http.ClearCookie(arrUrl);
            }

            data = DeRemote(data, model.RemoteData);
            VerifyUrl = DeRemote(model.Img, model.RemoteData);
            if (Data.WebSite.ID == 10) {
                VerifyUrl = model.Img.Replace("(#)", Data.WebSite.tempValue);
                model.Post = model.Post.Replace("(#)", Data.WebSite.tempValue);
            }
            PostData.ParseQueryString(data, Data.WebSite.Encoding);
            
            if (!string.IsNullOrEmpty(model.PicVerifty)){
                ShowLog("拖动图片后点击提交再确认");
                SelVerifyID(model.Post, Helper.GetCookieContainer(Data.UserID), Resume);
            }
            if (!string.IsNullOrEmpty(model.Code))
            {
                ShowLog("输入验证码");
                ShowVerifyCode(Resume);
            }
            else
                if (!string.IsNullOrEmpty(model.ClassFileName))
                {
                    SelClassID(string.Format("{0}WebSite\\{1}", Fetch.FilesRoot, model.ClassFileName), Resume);
                }
                else
                    Resume(this, new NameValuelist());
        }

        
        public void Resume(object sender, NameValuelist data)
        {
            SendMessage(200, string.Format("正在登陆 {0}", Data.Company.UserName));
            if (string.IsNullOrEmpty(model.Post))
            {
                ShowLog("登陆配置不正确", 3);
                return;
            }
            Regex r;
            #region 设置验证码
            if (!string.IsNullOrEmpty(model.Code) &&
                !string.IsNullOrEmpty(data["VerifyCode"]))
            {
                PostData[model.Code] = data["VerifyCode"];
            }
            if (!string.IsNullOrEmpty(model.Code2) &&
             !string.IsNullOrEmpty(data[model.Code2]))
            {
                PostData[model.Code2] = data[model.Code2];
            }
            if (!string.IsNullOrEmpty(model.PicVerifty))
            {
                Thread.Sleep(1000);
                if (data["cookie"] == null) { ShowLog("获取cookie为空！"); return; }
                string[] cookStr = data["cookie"].Replace(",", "%2c").Split(';');
                foreach (string str in cookStr)
                {
                    System.Net.Cookie ck = new System.Net.Cookie(str.Split('=')[0].Trim(), str.Split('=')[1].Trim());
                    ck.Domain = model.PicVerifty;
                    Helper.GetCookieContainer(Data.UserID).Add(ck);
                }
                Http.isUserAgent = data["useragent"];
            }
            #endregion
            ShowLog(model.Post, 5);
 
            model.Post = DeRemote(model.Post, model.RemoteData);

            if (Data.WebSite.ID == 136)
            {
                Site_136 site136 = new Site_136();
                PostData["pass"] = site136.GetPass(PostData, model.RemoteData["key"]);
                PostData["rand"] = Html.UrlEncode(PostData["rand"]);
            }
            string param = PostData.ToString().Replace("@@@", "+"); //转义 +号，给aspx页面用 2015-01-04
            if (Data.WebSite.Business.Addaspx == "true")
             {
                 param = PostData.ToString().Replace("+", "%2b");
             }
            if (PostData.ToString().Contains("+"))
            {
                param = PostData.ToString().Replace("+", "%2b");
            }
            
            
            ShowLog(param, 5);
            //提交数据
            try
            {
                Cookies = model.Cookies;

                ResponseData = GetData(model.Post, param, model.Url);

                if (Data.WebSite.ID == 7) {
                    Regex rs = new Regex("createHiddenIframe\\(\"([\\s\\S]+?)\",", RegexOptions.IgnoreCase);
                    if (rs.IsMatch(ResponseData)) {
                        GetData(rs.Matches(ResponseData)[0].Groups[1].Value + "&callback=focusSSOController.doCrossDomainCallBack&scriptId=ssoscript0", "");
                        if (rs.Matches(ResponseData).Count > 1)
                            GetData(rs.Matches(ResponseData)[2].Groups[1].Value + "&callback=focusSSOController.doCrossDomainCallBack&scriptId=ssoscript2", "");
                        GetData("http://membercenter.cn.made-in-china.com/sso/msgres/?retCode=200&retMsg=null&nv=0&crossDomain=null&gotoUrl=&callback=parent.focusSSOController.callFeedback", "");
                    }
                }
                if (Data.WebSite.ID == 22) {
                    //新品快播网
                    ResponseData = Regex.Unescape(ResponseData);
                }
            }
            catch (Exception ex)
            {
                ShowLog("远程服务器返回错误：" + ex.Message, 6);
                return;
            }
            #region 验证码错误
            r = getRegExp(model.CodeFail);
            if (null != r && r.IsMatch(ResponseData))
            {
                if (Times >= 3)
                {
                    ShowLog(string.Format("验证码错了{0}次；不再重试", Times));
                    return;
                }
                Times++;
                //ShowLog("验证码错误");
                SendMessage(100, "验证码错误");
                //ShowVerifyCode(Resume);
                return;
            }
            #endregion

            #region 登陆成功
            r = getRegExp(model.Success);
            if (null != r && r.IsMatch(ResponseData))
            {
                GetRemote(ResponseData,model.Remotes);
                if (Data.WebSite.ID == 81)
                {
                    Regex r1 = new Regex("index([\\s\\S].*?).html", RegexOptions.IgnoreCase);
                    if (r1.IsMatch(ResponseData))
                    {
                        logindata = r1.Match(ResponseData).Groups[1].Value;
                    }
                }
                //中国制造网 登录后跳转
                //if (Data.WebSite.ID == 7)
                //{
                //    //logindata = ResponseData;
                //    //Regex r1 = new Regex("'([\\s\\S]+?)',", RegexOptions.IgnoreCase);
                //    //foreach (Match match in r1.Matches(ResponseData))
                //    //{
                //    //    if (match.Value.IndexOf("made-in-china.com") > -1)
                //    //        GetData(match.Groups[1].Value + "&callback=focusSSOController.doCrossDomainCallBack&scriptId=ssoscript0", "");
                //    //}
                //    Regex r1 = new Regex("\"([\\s\\S].*?)\",");
                //    if (r1.IsMatch(ResponseData)) 
                //        GetData(r1.Match(ResponseData).Groups[1].Value + "&callback=focusSSOController.doCrossDomainCallBack&scriptId=ssoscript0","");
                //}

                ShowLog( "登陆成功");
                Logined = true;

                //wanghui 2013-03-19 添加激活步骤
                if (model.ActiceUrl != null && (!model.ActiceUrl.Equals("")))
                {
                    Regex active_r = new Regex(model.ActiceUrl, RegexOptions.Singleline);
                    if (active_r != null && active_r.IsMatch(ResponseData))
                    {
                        foreach (Match m in active_r.Matches(ResponseData))
                        {
                            string str = m.Value;
                            if (model.Actice_SubString_Qian != null)
                            {
                                if (str.IndexOf(model.Actice_SubString_Qian) != -1)
                                    str = str.Substring(str.IndexOf(model.Actice_SubString_Qian) + model.Actice_SubString_Qian.Length);
                            }
                            if (model.Actice_SubString_Hou != null)
                            {
                                if (str.LastIndexOf(model.Actice_SubString_Hou) != -1)
                                    str = str.Substring(0, (str.LastIndexOf(model.Actice_SubString_Hou)));
                            }
                            model.ActiceUrl = model.ActiceUrl_Site + str;
                            break;
                        }
                        Debug("激活操作：" + model.ActiceUrl);
                        GetData(model.ActiceUrl, "");
                    }
                }

                return;
            }
            #endregion


            #region 用户名不存在
            r = getRegExp(model.UserNameNotExist);
            if (null != r && r.IsMatch(ResponseData))
            {
                if (model.AutoReg)
                    Logined = Register.UserRegister(this.Data);
                return;
            }
            #endregion


            r = getRegExp(model.Fail);
            if (null != r && r.IsMatch(ResponseData))
            {
                ShowLog("登陆失败：" + Html.unescape(Html.RemoveHtml(r.Match(ResponseData).Value)).Trim(), 2);
            }
            else
                ShowLog("登陆返回错误");
            ShowLog(ResponseData, 6);           
        }

        public static bool UserLogin(Models.EPublishData data, int UserID)
        {
            return new Login().Post(data, UserID);
        }
    }
}