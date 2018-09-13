using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Pigeon.Utility;
using Pigeon.Sht.Models;
using System.Threading;
using System.Xml;
using System.Web;
using Pigeon.Sht.Config;
using System.IO;
namespace Pigeon.Sht.BLL
{
    public class Business : BasePost
    {
        public EBusiness model { get; set; }
        public EProduct Content { get; set; }        
        public static List<string> listDetails = new List<string>();
        public static void Send(EPublishData data, EProduct content)
        {
            //2014-05-09 设置勤加缘特殊字符不为空对象
            if (content.Subheading == null) content.Subheading = string.Empty;
            new Business().Submit(data, content);
        }
        public void Submit(EPublishData data, EProduct content)
        {
            this.Data = data;
            this.model = data.WebSite.Business;
            this.Content = content;
            this.Complete += new CompleteHandler(data.Complete);
            this.Message += new MessageHandler(data.Message);
            this.OnEntryVerifyCode += new EntryVerifyCodeHandler(Data.EntryVerifyCode);
            this.OnSelClassID += new SelClassIDHandler(Data.SelClassID);
            this.OnReturnCode2 += new ReturnVerifyCode2(Data.ReturnCode2);
            this.OnSelVerifyID += new SelVerifyIDHandler(Data.SelVerifyID);


            Thread m_thread = new Thread(new ThreadStart(Submit));
            m_thread.SetApartmentState(ApartmentState.STA);
            m_thread.Start();
            m_thread.Join();
        }
        private string GetKeyword()
        {
            string[] t1 = Text.RemoveDup(this.Content.Keywords).Split(',');
            this.Content.KeywordIndex++;
            if (this.Content.KeywordIndex >= t1.Length)
                this.Content.KeywordIndex = 0;
            BLL.Product.setKeywordIndex(this.Content);
            return t1[this.Content.KeywordIndex];
        }

        /// <summary>
        /// 随机获取一个关键词
        /// </summary>
        /// <returns></returns>
        private string GetRandKeyword()
        {
            int i = 0;
            Random rand = new Random();
            string[] t1 = Text.RemoveDup(this.Content.Keywords).Split(',');
            try
            {
                i = rand.Next(t1.Length);
                return t1[i];
            }
            catch
            {
                i = rand.Next(t1.Length);
            }

            return t1[i];
        }

        void SetEncoding()
        {
            try
            {
                this.Encoding = this.Data.WebSite.Encoding;
                if (!string.IsNullOrEmpty(model.Encoding))
                    this.Encoding = System.Text.Encoding.GetEncoding(model.Encoding);
            }
            catch { }
            if (model.Encoding == "0")
                this.Encoding = null;
        }
        private string GetKeyword(string Keywords)
        {
            string[] t1 = Keywords.Split(',');
            if (t1.Length != Helper.Random.MaxValue)
                Helper.Random.SetValue(0, t1.Length);
            int i = Helper.Random.Next();
            string v = string.Empty;
            if (i < 0 || i >= t1.Length) i = 0;
            return t1[i];
        }

        private string qResult;
        private string InfoTitle;
        private string picDataResult;
        public void Submit()
        {
            Debug("准备发布数据：" + model.Url);
            this.Cookies = Data.WebSite.Business.Cookies;

            //初始化每个网站的自定义配置(一步电子) 2017-07-10
            SiteBase.LoadBefore(Data.WebSite.ID, Data.WebSite.Encoding, Http, Data, PostData, RemoteData, Content);

            //机电之家-域名更改-2016-12-2
            if (Data.WebSite.ID == 10 && !string.IsNullOrEmpty(Data.WebSite.tempValue))
            {
                model.Url = model.Url.Replace("(#)", Data.WebSite.tempValue);
                model.Post = model.Post.Replace("(#)", Data.WebSite.tempValue);
                model.UpUrl = model.UpUrl.Replace("(#)", Data.WebSite.tempValue);
                for (int i = 0; i < model.Remotes.Count; i++)
                {
                    model.Remotes[i].Url = model.Remotes[i].Url.Replace("(#)", Data.WebSite.tempValue);
                }
            }
            //优先上传图片
            if (Data.WebSite.Business.uploadimg == "true")
            {
                string PicUrlimg = Content.PicUrl;
                if (model.PicUrl_Index == "1")
                    PicUrlimg = Content.PicUrls;
                if (Data.WebSite.ID == 16)
                {
                    model.RemoteData = GetRemote(model.Remotes);
                }
                string UpDatas = DeRemote(model.UpData, model.RemoteData);
                UpDatas = EnData(UpDatas);    //替换内置的变量(如用户名),商虎中国上传图片用到2015-01-12

                picDataResult = GetData(AnalyzeData(EnData(model.UpUrl), Content.PublishData), UpDatas, string.Empty,
                    new NameValue(model.UpField, string.Format("{0}Data/Pic/{1}", Fetch.SystemRoot, PicUrlimg)));

            }

            SetEncoding();
           
            if (Data.WebSite.ID == 81)
            {
                model.Remotes[0].Url = model.Remotes[0].Url + Login.logindata + ".html";
            }
            else if (Data.WebSite.ID == 162) {
                Http.isUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36 Edge/15.15063";
            }
           
            model.RemoteData = GetRemote(model.Remotes);

            if (Data.WebSite.ID == 16 & !string.IsNullOrEmpty(model.RemoteData["img_addr"]))
            {
                string[] temp = model.RemoteData["img_addr"].Split(',');
                if (temp.Length >= 2)
                {
                    model.RemoteData["img_addr"] = model.RemoteData["img_addr"].Split(',')[1];
                }
                else
                {
                    model.RemoteData["img_addr"] = model.RemoteData["img_addr"].Split(',')[0];
                }
            }

            #region 发产品前，选分类 2014-01-12
            if (model.Step != null)
            {
                for (int i = 0; i < model.Step.Count; i++)
                {
                    if (!string.IsNullOrEmpty(model.Step[i].ClassFields))
                    {
                        SendMessage(200, "选择分类");
                        try
                        {
                            if (SelClassID(string.Format("{0}WebSite\\{1}",
                                Fetch.FilesRoot,
                                model.Step[i].ClassFileName),
                                ClassIDCollback))
                            {
                                if (!isSelectId)
                                    return;
                            }
                        }
                        catch (Exception ex)
                        {
                            SendMessage(500, ex.Message);
                        }
                    }
                }
            }

            #endregion

            string data = AnalyzeData(model.Params, Content.PublishData);

            string Details = Content.Details;
       
            string tk = GetKeyword();
            string RandKeyWord = GetRandKeyword();

            List<string> _tmp = new List<string>();

            //设置每天推荐的关键词作为标题关键词 2014-04-30 
            //if (!Content.CopyFromUrl.Equals(""))
            //    tk = Content.CopyFromUrl;            //标题重复，先暂停功能 2017-04-10

            if (model.IsWebJie != "true")
            {
                //微街宝用户 详细内容在前面加2次 联系方式简介
                if (Data.Company.GroupID == "67" || Data.Company.GroupID == "68")
                {
                    if (Data.WebSite.ID == 143)
                    {
                        string tempStr = "---";
                        Random random = new Random();
                        for (int i = 0; i < 30; i++)
                        {
                            int num = random.Next(0, 20);
                            tempStr += num + "---";
                        }
                        Details = Data.Company.ShortName + "/" + "<div style=\" display: none;\">" + tk + tempStr + "</div>" + Data.Company.Tag + Details;
                        _tmp.Add(Details);
                        _tmp.Add("\r\n");
                        _tmp.Add(Content.Intro);
                        _tmp.Add("\r\n");
                        _tmp.Add(Content.Intro);
                        _tmp.Add("\r\n");
                    }
                    else
                    {
                        _tmp.Add(Data.Company.ShortName);
                        _tmp.Add("/");
                        _tmp.Add(Data.Company.Tag);
                        _tmp.Add("\r\n");
                        _tmp.Add(Content.Intro);
                        _tmp.Add("\r\n");
                        _tmp.Add(Content.Intro);
                        _tmp.Add("\r\n");
                        _tmp.Add(Details);
                    }
                    Details = String.Join("", _tmp.ToArray());
                }
                else
                {
                    _tmp.Add(tk);
                    _tmp.Add(Content.Intro);
                    _tmp.Add(tk);
                    _tmp.Add("\r\n");
                    _tmp.Add(Details);
                    _tmp.Add(tk);
                    Details = string.Join("", _tmp.ToArray());
                }
            }

            Details = ReplaseHTML(Data.WebSite.Business, Details);  //过滤HTML内容 2013-08-14 wanghui 

            Details = StringHelper.Replace(Details, model.Detail_Replase);  //删除敏感词 2017-07-10

            switch (model.HtmlMode)
            {
                case 1: break;
                default: Details = Html.TextEncode(Details); break;
            }

            Details = ReplaceStr(Details);
            Details = StringHelper.RemoveEmail(Details, Data.WebSite.Business.IsRemoveEmail);
            Details = StringHelper.RemovePhone(Details, Data.WebSite.Business.IsRemovePhone);   //移除联系方式 2017-01-05
            Details = StringHelper.RemoveUrl(Details, Data.WebSite.Business.IsRemoveUrl);       //移除URL 2017-01-05
            if (Data.WebSite.Business.issensite == "true")
            {
                Details = ChangleDetail(Details);
            }

            data = EnData(data, "Details", Html.FixTags(Text.SubString(Details, model.MaxDetails)), Encoding);
            data = EnData(data, "Intro", Content.Intro, Encoding);
            data = EnData(data, "ProductName", Content.ProductName, Encoding);
            data = EnData(data, "ProductModel", Content.ProductModel, Encoding);
            _tmp = new List<string>();
            _tmp.Add(tk);
            _tmp.Add(Data.Company.ShortName);
            string _t1 = string.IsNullOrEmpty(Content.ProductName) ? Content.Title : Content.ProductName;
            _tmp.Add(Text.SubString(_t1, 12));

            string Title = string.Join(Helper.Config.Separator, _tmp.ToArray());
            if (model.MaxTitle > 0) Title = Text.SubString(Title, model.MaxTitle);

            if (Data.WebSite.ID == 3)//马可波罗
            {
                //去除标题重复的字
                int index = 0, count = 0, tempindex = 0, tempindexs = 0;
                string keyword = "";
                for (int i = 0; i < Title.Length / 4; i++)
                {
                    keyword = Title.Substring(i, 4);
                    while ((tempindex = Title.IndexOf(keyword, index)) != -1)
                    {
                        count++;
                        if (count == 2)
                        {
                            //先删除后添加
                            Title = Title.Replace(keyword, "");
                            Title = Title.Insert(tempindexs, keyword);
                        }
                        tempindexs = tempindex;//保存上一次的位置
                        index += 4;
                    }
                    index = 0;
                    count = 0;
                }
                Title = ChangeTitle(model.Remotes, Title);//2016-9-18 马可波罗 去除重复
            }
            //else if (Data.WebSite.ID == 124) { //慧聪网
            //    //给指定的关键词加空格
            //    string[] keywss = model.Keywords_Replase.Split(',');
            //    int index = 0;
            //    string tempstr = "";
            //    for (int i = 0; i < keywss.Length; i++)
            //    {
            //        while ((index = Title.IndexOf(keywss[i],index)) != -1)
            //        {
            //            tempstr = keywss[i].Insert(1,"!");
            //            tempstr = tempstr.Insert(3, "!");
            //            Title = Title.Replace(keywss[i],"").Insert(index, tempstr);
            //            index++;
            //        }
            //        index = 0;
            //        while ((index = Content.Keywords.IndexOf(keywss[i], index)) != -1)
            //        {
            //            tempstr = keywss[i].Insert(1, "!"); 
            //            tempstr = tempstr.Insert(3, "!");
            //            Content.Keywords = Content.Keywords.Replace(keywss[i],"").Insert(index, tempstr);
            //            index++;
            //        }
            //        index = 0;
            //    }
            //}


            //替换标题里特殊字符 2013-07-17 
            Title = StringHelper.Replace(Title, model.Title_Replase);

            //关键词去掉敏感词 2017-04-23(慧聪网)
            Content.Keywords = StringHelper.Replace(Content.Keywords, model.Keywords_Replase, "!"); //默认是*号，但是慧聪网不能用*号


            #region 勤加缘网站标题 + 特殊字符 2014-04-28 wanghui
            if (Data.WebSite.ID == 2)
            {
                //特殊字符格式： 供1,供2,供3,供4,供5,供6,供7,供8,供9
                if (tk.Length < 9 && Content.Subheading.Length > 0)
                {
                    int subIndex = -1;
                    string[] arrSubHeading = Content.Subheading.Split(',');
                    string[] arrKey = Text.RemoveDup(this.Content.Keywords).Split(',');

                    for (int i = 0; i < arrKey.Length; i++)
                    {
                        if (arrKey[i] == tk)
                        {
                            subIndex = i;
                            break;
                        }
                    }

                    if (subIndex < 0 || subIndex > arrSubHeading.Length)
                        Title = tk + arrSubHeading[arrSubHeading.Length - 1];
                    else
                        Title = tk + arrSubHeading[subIndex];
                }
            }
            #endregion

            InfoTitle = Title;
            data = EnData(data, "Title", Title, Encoding);
            data = EnData(data, "Now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            //替换关键词 格式：{$Keyword[-1]}
            Regex r = new Regex(@"{\$Keyword\[([\s\S]?\d+)\]}", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            string[] t1 = Content.Keywords.Split(',');
            vRandom rand = new vRandom(0, t1.Length);
            foreach (Match m in r.Matches(data))
            {
                if (m.Success && !string.IsNullOrEmpty(m.Groups[1].ToString()))
                {
                    int i = vConvert.ToInt32(m.Groups[1].ToString(), 0);
                    string v = string.Empty;
                    if (i == -1) i = rand.Next();                    
                    if (i >= 0 && i < t1.Length) v = t1[i];
                    if (i == -2) v = tk;    //2017-03-10 用标题上的关键词,增加频率

                    //限制关键词字数 2013-07-31 wanghui 
                    if (model.MaxKeyword > 0 && v.Length > model.MaxKeyword)
                        v = v.Substring(0, model.MaxKeyword);

                    data = r.Replace(data, Html.UrlEncode(v, Encoding), 1);
                }
            }
            data = EnData(data, "Keyword", Content.Keywords, Encoding);
            data = EnData(data);
            data = DeRemote(data, model.RemoteData);

            PostData.ParseQueryString(data, Encoding);

            //根据索引来上传指定的图片 2015-03-09
            string PicUrl = Content.PicUrl;
            if (model.PicUrl_Index == "1")
                PicUrl = Content.PicUrls;

            //初始化每个网站的自定义配置(东方供应商,云同盟) 2017-04-12
            SiteBase.Load(Data.WebSite.ID, Data.WebSite.Encoding, Http, Data,PostData, RemoteData);    

            //通过第二地址上传图片
            if (model.UpType == 2 && !string.IsNullOrEmpty(PicUrl) && IO.FileExists(string.Format("{0}Data/Pic/{1}", Fetch.SystemRoot, PicUrl)))
            {
                if (Data.WebSite.ID == 6)//生意宝默认创建一个图片文件夹 zhengyue 2015-7-16 11:11:03
                {
                    GetData("http://my.album.toocle.com/index.php?_d=member&_a=pic_category&f=add&in_wp=0&name=1", "", "");
                }
                string UpData = DeRemote(model.UpData, model.RemoteData);
                if (UpData == null && Data.WebSite.ID == 40)
                {
                    UpData = "__VIEWSTATE=/wEPDwUJMjQ4ODE4ODM1D2QWAmYPFgIeB2VuY3R5cGUFE211bHRpcGFydC9mb3JtLWRhdGEWBAIBDxYCHgdWaXNpYmxlZ2QCAg8QDxYGHg1EYXRhVGV4dEZpZWxkBQtzdHJTb3J0TmFtZR4ORGF0YVZhbHVlRmllbGQFBWludElEHgtfIURhdGFCb3VuZGdkEBUBDOWFqOmDqOWIhuexuxUBAi0xFCsDAWdkZGQ=";
                }
                if (UpData == null && Data.WebSite.ID == 120)
                {
                    UpData = "__VIEWSTATE=/wEPDwUKLTY4MTgxNTkwNQ9kFgICAw8WAh4HZW5jdHlwZQUTbXVsdGlwYXJ0L2Zvcm0tZGF0YRYCAgEPFgIeA3NyYwU5aHR0cDovL3BpYzIucXUxMTQuY29tL3Y1L3Bvc3QyLzIwMTIwNjAxL2ltYWdlcy9ub3BpYzIuZ2lmZGR5WC2hdTBKiimSxzJHoF7cfQpWww==&__VIEWSTATEGENERATOR=218B696C";
                }

                UpData = EnData(UpData);    //替换内置的变量(如用户名),商虎中国上传图片用到2015-01-12

                //2013-07-31 wanghui 替换上传图片路径中的参数
                for (int i = 0; i < model.RemoteData.Count; i++)
                    model.UpUrl = model.UpUrl.Replace("{$remote[" + model.RemoteData.Keys[i] + "]}", model.RemoteData[i]);

                string s = "";
                if (Data.WebSite.Business.NewUpimg != null && Data.WebSite.Business.NewUpimg.ToLower() == "true")
                    s = UpLoad(AnalyzeData(EnData(model.UpUrl), Content.PublishData), string.Format("{0}Data/Pic/{1}", Fetch.SystemRoot, PicUrl), model.UpField, "", UpData, "");
                else if (Data.WebSite.ID == 124)
                { //慧聪的上传图片方式
                    HCUpload hc = new HCUpload();
                    string filepath = string.Format("{0}Data/Pic/{1}", Fetch.SystemRoot, PicUrl);
                    FileStream fs = new FileStream(filepath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    byte[] fileBytes = new byte[fs.Length];
                    fs.Read(fileBytes, 0, fileBytes.Length);
                    fs.Close();
                    fs.Dispose();
                    string[] updatas = UpData.Split('&');
                    for (int i = 0; i < updatas.Length; i++)
                        hc.SetFieldValue(updatas[i].Split('=')[0], updatas[i].Split('=')[1]);
                    hc.SetFieldValue("name", PicUrl);
                    hc.SetFieldValue("type", IO.GetContentType(filepath));
                    hc.SetFieldValue("upFile", Path.GetFileName(filepath), "image/jpeg", fileBytes);
                    hc.Upload(model.UpUrl, out s);
                    Regex hcr = new Regex("result\":([\\s\\S].*?),\"error");
                    HttpGet.Get(string.Format("http://imgup.b2b.hc360.com/imgup/turbine/action/imgup.businchance.BusinChaceImgSaveAction/eventsubmit_dosavepic/doSavepic?callback=jQuery17106462454474941199_1514197297474&picstr={0}&piclist=[{1}]&_=1514197618710", updatas[0].Split('=')[1], hcr.Match(s).Groups[1].Value));//发送图片请求
                }
                else if (Data.WebSite.ID == 10) { 
                    //机电之家的图片处理方式
                    string filepath = string.Format("{0}Data/Pic/{1}", Fetch.SystemRoot, PicUrl);
                    SpecialPicture sp = new SpecialPicture();
                    sp.Loading();
                    FileStream fs = new FileStream(filepath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    byte[] fileBytes = new byte[fs.Length];
                    fs.Read(fileBytes, 0, fileBytes.Length);
                    fs.Close();
                    fs.Dispose();
                    sp.SetFieldValue(model.UpField, Path.GetFileName(filepath), "image/jpg", fileBytes);
                    sp.Upload(model.UpUrl, Helper.GetCookieContainer(Data.UserID).GetCookies(new Uri(model.Url))[0].Name + "=" + Helper.GetCookieContainer(Data.UserID).GetCookies(new Uri(model.Url))[0].Value, out s);
                }
                else if (Data.WebSite.ID == 47)
                {
                    //黄页88的图片处理方式
                    string filepath = string.Format("{0}Data/Pic/{1}", Fetch.SystemRoot, PicUrl);
                    SpecialPicture sp = new SpecialPicture();
                    sp.Loading();
                    FileStream fs = new FileStream(filepath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    byte[] fileBytes = new byte[fs.Length];
                    fs.Read(fileBytes, 0, fileBytes.Length);
                    fs.Close();
                    fs.Dispose();

                    string html = HttpGet.Get("http://upimg.huangye88.com/upload/aliyunossupload.do");
                    string filecatid = "";
                    if (Regex.IsMatch(html, "OSSAccessKeyId\" value=\"([\\s\\S].*?)\"", RegexOptions.IgnoreCase) && Regex.IsMatch(html, "policy\" value=\"([\\s\\S].*?)\"", RegexOptions.IgnoreCase)
                        && Regex.IsMatch(html, "signature\" value=\"([\\s\\S].*?)\"", RegexOptions.IgnoreCase) && Regex.IsMatch(html, "realpath = \"([\\s\\S].*?)\"", RegexOptions.IgnoreCase))
                    {
                        sp.SetFieldValue("OSSAccessKeyId", Regex.Match(html, "OSSAccessKeyId\" value=\"([\\s\\S].*?)\"", RegexOptions.IgnoreCase).Groups[1].Value);
                        sp.SetFieldValue("policy", Regex.Match(html, "policy\" value=\"([\\s\\S].*?)\"", RegexOptions.IgnoreCase).Groups[1].Value);
                        sp.SetFieldValue("signature", Regex.Match(html, "signature\" value=\"([\\s\\S].*?)\"", RegexOptions.IgnoreCase).Groups[1].Value);
                        filecatid = Regex.Match(html, "realpath = \"([\\s\\S].*?)\"", RegexOptions.IgnoreCase).Groups[1].Value + "0.jpg";
                        sp.SetFieldValue("key", filecatid);
                    }
                    sp.SetFieldValue(model.UpField, Path.GetFileName(filepath), "image/jpg", fileBytes);
                    sp.Upload(model.UpUrl, "", out s);
                    if (!string.IsNullOrEmpty(s))
                        SendMessage(201, "上传图片失败！");
                    //live/user/0/1521788476079685100-0.jpg|oss|live/user/0/1521788476079685100-0.jpg|a:1:{s:1:\"s\";s:65:\"live/user/0/1521788476079685100-0.jpg@1e_1c_80w_80h_90Q.jpg\";}|image/jpeg|a|undefined|1.12
                    PostData["info[files][0][id]"] = filecatid + "|oss|" + filecatid + "|a:1:{s:1:\"s\";s:65:\"" + filecatid + "@1e_1c_80w_80h_90Q.jpg\";}|image/jpeg|a|undefined|1.12";
                }
                else if (Data.WebSite.ID == 176 || Data.WebSite.ID == 177)
                {
                    string filepath = string.Format("{0}Data/Pic/{1}", Fetch.SystemRoot, PicUrl);
                    SpecialPicture sp = new SpecialPicture();
                    sp.Loading();
                    FileStream fs = new FileStream(filepath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    byte[] fileBytes = new byte[fs.Length];
                    fs.Read(fileBytes, 0, fileBytes.Length);
                    fs.Close();
                    fs.Dispose();
                    sp.SetFieldValue("sort", "0");
                    sp.SetFieldValue("yysize", fileBytes.Length.ToString());
                    sp.SetFieldValue("filenames", PicUrl);
                    sp.SetFieldValue(model.UpField, Path.GetFileName(filepath), "image/jpg", fileBytes);
                    sp.Upload(model.UpUrl, sp.GetAllCookies(Helper.GetCookieContainer(Data.UserID)), out s);
                    if (!Regex.IsMatch(s, "成功", RegexOptions.IgnoreCase))
                    {
                        SendMessage(201, "上传图片失败！");
                        return;
                    }

                    string html = GetData("http://www.echuchen.net/user/my_picture_view.asp?action=1", "");
                    if (Regex.IsMatch(html, "select\\('([\\s\\S].*?)'", RegexOptions.IgnoreCase))
                        PostData[model.ImgField] = Regex.Match(html, "select\\('([\\s\\S].*?)'", RegexOptions.IgnoreCase).Groups[1].Value;
                }
                else if (Data.WebSite.Business.uploadimg != "true")
                {
                    s = GetData(AnalyzeData(EnData(model.UpUrl), Content.PublishData), UpData, string.Empty, new NameValue(model.UpField, string.Format("{0}Data/Pic/{1}", Fetch.SystemRoot, PicUrl)));
                }
                //图片获取适用两个itme 数据
                Upmatch(model.UpMatch, s);
                for (int i = 0; i < UpmatchData.Count; i++)
                    PostData[UpmatchData.Keys[i]] = UpmatchData[i];


                Utility.Models.EMessage msg = MatchReplace(model.UpMatch, s);
                if (msg.Code == 200)
                {
                    if (Data.WebSite.ID == 6)
                    {
                        PostData[model.ImgField] = EdDateURL("http://my.album.toocle.com/?_d=member&_a=album&f=select&pic=pic_name1&photo=photo1&px=140", msg.Data);
                    }
                    else if (Data.WebSite.ID == 136)
                    {
                        PostData["content"] = "<p><img src=\"" + msg.Data.Replace("'", "") + "\" alt=\"\" /><br /></p>" + PostData["content"].ToString();
                    }
                    else if (Data.WebSite.ID == 139)
                    {
                        PostData["img_info"] = "{\"0\":{\"file_name\":\"" + msg.Data + "\",\"isup\":\"1\",\"original\":\"\"},\"1\":{\"file_name\":\"\",\"isup\":\"0\",\"original\":\"\"},\"2\":{\"file_name\":\"\",\"isup\":\"0\",\"original\":\"\"},\"3\":{\"file_name\":\"\",\"isup\":\"0\",\"original\":\"\"},\"4\":{\"file_name\":\"\",\"isup\":\"0\",\"original\":\"\"}}";
                        PostData[model.ImgField] += msg.Data;
                    }else if (Data.WebSite.ID == 155 || Data.WebSite.ID == 157) {
                        //中国制药网-供应 和 中国制药网
                        //2/20170831/636397880100997123103.jpg,jpg,53043;img65
                        PostData.Set("code", PostData[0] + "pic=" + msg.Data.Substring(0, msg.Data.IndexOf(",")) + "%0Apicdomain=" + msg.Data.Substring(msg.Data.IndexOf(";"), msg.Data.Length - msg.Data.IndexOf(";")).Replace(";", ""));
                    }
                    else
                    {
                        PostData[model.ImgField] += msg.Data;
                    }
                }
                
                else if (msg.Code == 500) { 
                    
                    if (Data.WebSite.ID == 153) {
                        string htmlresult = GetData("http://www.zyzhan.com/ajax/MTnets.UserManage.Stand.StandManage,UserManage2013_Stand.ashx?_method=AddGallery&_session=rw","name="+DateTime.Now.ToShortDateString()+"\nvisitType=1\nvisitValue=","");//Create Picture Files
                        Match m = Regex.Match(htmlresult, "ess\":\"([\\s\\S].*?)\"");
                        if (m.ToString().Length < 1) { 
                            SendMessage(201, "上传图片失败！");
                            PostData.Set("code", PostData[0] + "%0APictureID=");
                        }
                        else
                        {
                            string PictureID = m.ToString().Substring(m.ToString().IndexOf(':') + 2, m.ToString().Length - m.ToString().IndexOf(':') - 3);//图库编号
                            string savePicture = msg.Data.Substring(msg.Data.IndexOf(",") + 1, msg.Data.IndexOf(";") - msg.Data.IndexOf(","));
                            string htmlresult2params = string.Format("Name="+DateTime.Now.ToString("yyyyMMddHHmmss.fff")+"\nGalleryID={0}\nPictureDomain={1}\nPicture={2}\nFileSize=0\nWidth=0\nHeight=0",
                                PictureID, msg.Data.Substring(msg.Data.IndexOf(';') + 1, msg.Data.Length - msg.Data.IndexOf(';') - 1), msg.Data.Substring(0, msg.Data.IndexOf(",")));
                            string htmlresult2 = GetData("http://www.zyzhan.com/ajax/MTnets.UserManage.Stand.StandManage,UserManage2013_Stand.ashx?_method=AddPicture&_session=rw", htmlresult2params, "");
                            m = Regex.Match(htmlresult2, "ess\":\"([\\s\\S].*?)\"");
                            string SuccessPictureID = m.ToString().Substring(m.ToString().IndexOf(':') + 2, m.ToString().Length - m.ToString().IndexOf(':') - 3);//保存成功编号
                            PostData.Set("code", PostData[0] + "%0APictureID=" + SuccessPictureID);
                        }
                    }
                    else if (Data.WebSite.ID == 156) {
                        string htmlresult = GetData("http://www.hbzhan.com/ajax/MTnets.UserManage.Stand.StandManage,UserManage2013_Stand.ashx?_method=AddGallery&_session=rw", "name=" + DateTime.Now.ToShortDateString() + "\nvisitType=1\nvisitValue=", "");//Create Picture Files
                        Match m = Regex.Match(htmlresult, "ess\":\"([\\s\\S].*?)\"");
                        if (m.ToString().Length < 1)
                        {
                            SendMessage(201, "上传图片失败！");
                            PostData.Set("code", PostData[0] + "%0APictureID=");
                        }
                        else
                        {
                            string PictureID = m.ToString().Substring(m.ToString().IndexOf(':') + 2, m.ToString().Length - m.ToString().IndexOf(':') - 3);//图库编号
                            string savePicture = msg.Data.Substring(msg.Data.IndexOf(",") + 1, msg.Data.IndexOf(";") - msg.Data.IndexOf(","));
                            string htmlresult2params = string.Format("Name=" + DateTime.Now.ToString("yyyyMMddHHmmss.fff") + "\nGalleryID={0}\nPictureDomain={1}\nPicture={2}\nFileSize=0\nWidth=0\nHeight=0",
                                PictureID, msg.Data.Substring(msg.Data.IndexOf(';') + 1, msg.Data.Length - msg.Data.IndexOf(';') - 1), msg.Data.Substring(0, msg.Data.IndexOf(",jpg")));
                            string htmlresult2 = GetData("http://www.hbzhan.com/ajax/MTnets.UserManage.Stand.StandManage,UserManage2013_Stand.ashx?_method=AddPicture&_session=rw", htmlresult2params, "");
                            m = Regex.Match(htmlresult2, "ess\":\"([\\s\\S].*?)\"");
                            string SuccessPictureID = m.ToString().Substring(m.ToString().IndexOf(':') + 2, m.ToString().Length - m.ToString().IndexOf(':') - 3);//保存成功编号
                            PostData.Set("code", PostData[0] + "%0APictureID=" + SuccessPictureID);
                        }
                    }
                    else if (Data.WebSite.ID == 174) {
                        PostData.Set("title", PostData[0] + "%0Apic=" + msg.Data.Substring(0, msg.Data.IndexOf(",")) + "%0Apicdomain=" + msg.Data.Substring(msg.Data.IndexOf(";"), msg.Data.Length - msg.Data.IndexOf(";")).Replace(";", ""));
                    }
                    else if (Data.WebSite.ID == 175) {
                        string htmlresult = GetData("http://www.jc35.com/ajax/MTnets.UserManage.Stand.StandManage,UserManage2013_Stand.ashx?_method=AddGallery&_session=rw", "name=" + DateTime.Now.ToShortDateString() + "\nvisitType=1\nvisitValue=", "");//Create Picture Files
                        Match m = Regex.Match(htmlresult, "ess\":\"([\\s\\S].*?)\"");
                        if (m.ToString().Length < 1)
                        {
                            SendMessage(201, "上传图片失败！");
                            PostData.Set("ProName", PostData[0] + "%0APictureID=");
                        }
                        else
                        {
                            string PictureID = m.ToString().Substring(m.ToString().IndexOf(':') + 2, m.ToString().Length - m.ToString().IndexOf(':') - 3);//图库编号
                            string savePicture = msg.Data.Substring(msg.Data.IndexOf(",") + 1, msg.Data.IndexOf(";") - msg.Data.IndexOf(","));
                            string htmlresult2params = string.Format("Name=" + DateTime.Now.ToString("yyyyMMddHHmmss.fff") + "\nGalleryID={0}\nPictureDomain={1}\nPicture={2}\nFileSize=0\nWidth=0\nHeight=0",
                                PictureID, msg.Data.Substring(msg.Data.IndexOf(';') + 1, msg.Data.Length - msg.Data.IndexOf(';') - 1), msg.Data.Substring(0, msg.Data.IndexOf(",")));
                            string htmlresult2 = GetData("http://www.jc35.com/ajax/MTnets.UserManage.Stand.StandManage,UserManage2013_Stand.ashx?_method=AddPicture&_session=rw", htmlresult2params, "");
                            m = Regex.Match(htmlresult2, "ess\":\"([\\s\\S].*?)\"");
                            string SuccessPictureID = m.ToString().Substring(m.ToString().IndexOf(':') + 2, m.ToString().Length - m.ToString().IndexOf(':') - 3);//保存成功编号
                            PostData.Set("ProName", PostData[0] + "%0APictureID=" + SuccessPictureID);
                        }
                    }
                }
                SendMessage(0, "上传图片中....");
                //Debug("上传图片中....");
            }
            ///特殊处理
            SetData(picDataResult, Data.WebSite.ID);
            
            Times = 0;

            //为007商务网定制的GeeTest验证
            if (!string.IsNullOrEmpty(model.GeeTest)) {
                try
                {
                    if (SelVerifyID(model.GeeTest, Helper.GetCookieContainer(Data.UserID), Resume))
                        return;
                    else
                        Resume(this, new NameValuelist());
                }
                catch(Exception ex) {
                    SendMessage(500, ex.Message);
                }
            }

            //清除之前的产品分类 2014-02-21
            if (model.IsRemoveSelectId)
            {
                PostData.Remove("sortid");
                PostData.Remove("typeid");
                PostData.Remove("typeid2");
            }
            if (!string.IsNullOrEmpty(model.ClassFields) && !CheckField(model.ClassFields, PostData))
            {
                SendMessage(200, "选择分类");
                try
                {
                    //2013-10-22 wanghui 环球厨卫订制
                    if (Data.WebSite.ID == 8)
                        model.ClassFileName = model.ClassFileName.Replace("{$id}",site8.gsid);


                    if (SelClassID(string.Format("{0}WebSite\\{1}",
                        Fetch.FilesRoot,
                        model.ClassFileName),
                        Resume))
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    SendMessage(500, ex.Message);
                }
            }
            else
                Resume(this, new NameValuelist());
        }
        public static string qStr = string.Empty;
        public void Resume(object sender, NameValuelist data)
        {
            if (string.IsNullOrEmpty(model.Post))
            {
                SendMessage(500, "供求信息配置不正确");
                return;
            }
            Product.UserID = Content.UserID;
            ShowLog(data.ToString(), 5);
            if (!string.IsNullOrEmpty(model.ClassFields))
            {
                //重设分类
                SendMessage(0, "重设分类");
                foreach (string _key in model.ClassFields.Split(','))
                {
                    if (string.IsNullOrEmpty(_key)) continue;
                    string[] s1 = _key.Split('=');

                    if (s1.Length != 2) continue;
                    string v = GetField(data, s1[1]);
                    if (!string.IsNullOrEmpty(v))
                    {
                        if (Data.WebSite.ID == 153 || Data.WebSite.ID == 155 || Data.WebSite.ID == 156 || Data.WebSite.ID == 157)
                            PostData.Set("code", PostData[0] + "%0A" +s1[0] + "=" + v);
                        else if (Data.WebSite.ID == 174)
                            PostData.Set("title", PostData[0] + "%0A" + s1[0] + "=" + v);
                        else if (Data.WebSite.ID == 175)
                            PostData.Set("ProName", PostData[0] + "%0A" + s1[0] + "=" + v);
                        else 
                            PostData[s1[0]] = v;// data[s1[1]];
                        BLL.Product.SetFields(Content, Data.WebSite.ID, s1[0], v);
                    }
                }
                BLL.Product.SaveFields(Content);
            }
            
            if (string.IsNullOrEmpty(data["VerifyCode"]) &&
                !string.IsNullOrEmpty(model.Code))
            {
                SendMessage(201, "输入验证码");
                VerifyUrl = model.Img;
                ShowVerifyCode(Resume);
                return;
            }

            //wanghui 2014-03-20 添加第二个验证码
            if (string.IsNullOrEmpty(data["VerifyCode2"]) && !string.IsNullOrEmpty(model.Font_Code))
            {
                SendMessage(201, "输入文字验证码");
                ShowReturnCode2(data, Resume);
                return;
            }
            //图形验证码
            if (string.IsNullOrEmpty(data["url"]) && !string.IsNullOrEmpty(model.GeeTest))
            {
                SendMessage(0, "获取地址失败");
                return;
            }

            #region 设置验证码

            if (!string.IsNullOrEmpty(model.Code) &&
                !string.IsNullOrEmpty(data["VerifyCode"]))
            {
                if (Data.WebSite.ID == 174)
                    PostData.Set("title", PostData[0] + "%0Acode=" + data["VerifyCode"]);
                else if (Data.WebSite.ID == 175)
                    PostData.Set("ProName", PostData[0] + "%0Acode=" + data["VerifyCode"]);
                else
                    PostData[model.Code] = data["VerifyCode"];
            }
            #endregion

            #region 设置文字验证码 2014-03-20 wanghui
            if (!string.IsNullOrEmpty(model.Font_Code) && !string.IsNullOrEmpty(data["VerifyCode2"]))
            {
                PostData[model.Font_Code] = data["VerifyCode2"];
            }
            #endregion

            #region 拖图验证码 2018-03-07
            if (!string.IsNullOrEmpty(data["url"]) && !string.IsNullOrEmpty(model.GeeTest))
            {
                model.Post = data["url"];
            }
            #endregion

            #region 环球经贸网
            if (Data.WebSite.ID == 1)
                model.Post += data["VerifyCode"];
            #endregion

            #region 提交数据
            ShowLog("提交供求数据:" + PostData.ToString(), 1);
            //ShowLog("信息标题：" + PostData[string.IsNullOrEmpty(model.TitleField) ? "Title" : model.TitleField]);
            ShowLog("信息标题：" + InfoTitle);

            NameValue file = (NameValue)null;

            //根据索引来上传指定的图片 2015-03-09
            string PicUrl = Content.PicUrl;
            if (model.PicUrl_Index == "1")
                PicUrl = Content.PicUrls;

            //上传图片
            if (model.UpType == 1 && !string.IsNullOrEmpty(PicUrl) && IO.FileExists(string.Format("{0}Data/Pic/{1}", Fetch.SystemRoot, PicUrl)))
            {
                file = new NameValue(model.UpField, IO.Combine(Fetch.SystemRoot, "Data", "Pic", PicUrl));
                ShowLog("直接上传图片" + file.Value.ToString(), 1);
            }
            //未实现的功能
            if (model.getremote == "1")
            {
                model.RemoteData = GetRemote(model.Remotes);
            }
            try
            {
                string PostUrl = DeRemote(EnData(model.Post), model.RemoteData);
                if (Data.WebSite.ID == 137)
                {
                    PostUrl = string.Format("{0}{1}", PostUrl, PostData["tempV"]);
                }
                if (isDebug)
                    ShowLog(string.Format("正在提交数据：{0}", PostUrl), 5);
                else
                    ShowLog("正在提交数据...", 5);


                if (Data.WebSite.ID == 81)
                {
                    PostUrl = PostUrl + Login.logindata + ".html?id=" + Login.logindata;
                    model.Url = model.Url + Login.logindata + ".html";
                }



                if (Data.WebSite.ID == 23)
                {
                    if (string.IsNullOrEmpty(qStr) || qStr.Equals("0")) qStr = model.RemoteData["q"];
                    else PostData["q"] = qStr;
                }



                if (null == file)
                {
                    if (Data.WebSite.ID == 8)
                    {
                        #region 环球厨卫发布 单独定制 2013-10-22 wanghui

                        //账号格式： login_name=bxjpjc001&pass=b123456
                        string loginData = EnData(Data.WebSite.Login.Params);
                        string[] split = { "&" };
                        string[] arr = loginData.Split(split, StringSplitOptions.None);
                        if (arr == null || arr.Length < 2)
                        {
                            Debug("账号密码设置错误");
                            return;
                        }
                        int nameIndex = arr[0].IndexOf("=");
                        int passIndex = arr[1].IndexOf("=");
                        string name = arr[0].Substring(nameIndex + 1);
                        string pass = arr[1].Substring(passIndex + 1);
                        
                        site8.SOAP_Login(name, pass);
                        
                        
                        #endregion
                        
                        string msg = string.Empty;
                        ResponseData = site8.SOAP_Post(out msg);
                        if (msg.Length > 0)
                            ResponseData = msg;
                    }                   
                    else if (Data.WebSite.ID == 143)
                    {
                        string jsonStr = string.Format("{0}\"Infor\":[\"{1}\",\"{2}\",\"{3}\",\"{4}\"]{5}", "{", PostData["title"], PostData["content"].Replace("\"", "'"), PostData["imageid"], PostData["type"], "}");
                        ResponseData = GetData(PostUrl, jsonStr, model.Url);
                    }

                    else if (Data.WebSite.ID == 145)
                    {
                        ResponseData = GetData(PostUrl, PostData.ToString("\r\n"), model.Url);
                        ResponseData = ChangeResponeData(PostUrl, ResponseData);
                    }
                    else if (Data.WebSite.ID == 147)
                    {
                        var dat = GetData(PostData, model.Coder) + "&docs=http://www.docin.com/p-1196925886.html&docs=http://www.docin.com/p-941484592.html&docs=http://www.docin.com/p-236510723.html&docs=http://&docs=http://";
                        ResponseData = GetData(PostUrl, dat, model.Url);
                        ResponseData = ChangeResponeData(PostUrl, ResponseData);
                    }
                    else if (Data.WebSite.ID == 158) {
                        PostData["content"] = PostData["content"].Replace("&lt;", "<");
                        PostData["content"] = PostData["content"].Replace("&gt;", ">");
                        ResponseData = GetData(PostUrl, GetData(PostData, model.Coder), EnData(model.Url));
                        ResponseData = ChangeResponeData(PostUrl, ResponseData);
                    }
                    else
                    {
                        //AddCookie(PostUrl);

                        //string param = "__VIEWSTATE=6X6qG%2FaNjncr6zyYLHTMjJ9xU%2BrcVjeHI%2Bh8Oph0rA73%2F1N3jT81H3NPu5xP7Kh%2FApjjANeCUpECE2M1aCdbcMXzGVoGrmk2FsnhQV1nlWwKlLNhRRf1X%2FFmZhPiRBgx%2Fa0ZNA4dnAuuk6RlAQ1FJ8IB7HVJuSRp%2FvNyni1VM0sN4Zwbx25kX4r4kQqJzQqO%2F0L52dT4%2BLC8ZVo%2FUv%2F5jtR79h7Q%2B4MM8S8E0aRkER23wbT%2F0BBguDzej49rtbXIJGmUsbEyZSk5gqBgbabs8q%2FfrLOjt5LwqfCwmzq65r71B1v9j%2BTgy13DW3LUUSt7KmBbZ6JTz%2FRtPUf%2BOfLfp4gHCyi6HdQ4Rp4ENy5CWiM%2BCr2k1bpiFEYLyxEVp5QWSwYf5BSad75zRAXCd7N%2BUySrSyJYv3hwXei4qstsh2%2Bz4JmPNSt0J2TpShYRntJYK3hebGsILZyUKzU9%2BSs8qoxqDMPQU2Odtgpr7Ub%2FSlExBM7hiQNxduPfgGx83nrN1D76jQg%2BLJ5rEAvpYwPz1DgmIf31v%2Fy%2FxQ37jrgj3oq4rJaqpKVbGgW8wDwDpee3BefWFqGznfr779l%2FnnSlkdwUX%2BWMDlGILOnR52jqv1HqE%2Bz07Sj14PkvwxqKIMiltSZa52rEjMacJ1GblbQdnifCpg2E9prsst1bfDiwwWjnUs98Qx1XJVezGgIwTokT8%2B4SYeVdV9t9DsomBKrPl9xJ3jiQbs8wQODV4KX%2FbcaZtiQk9vWrh8nvCnUTsMwCLnG0k8NFn7z1cNwp10IX7iqgz2Dntj4wN9zOq8R2Jk%2F0hywr4EJXNWH2vL6N4CKSEyIOSo741fMXIxREk%2BsAeSmSX303XD4xPWdRXU69O356xBLBv5UhrinqjNqM0U9BsxOICJmNnzOFI0S5kz1gV4chVPpDKp%2Ft8lhxOBS1pGoDq0g%2FpldxSU0euwTXy%2FTtmFizi8hETrFh9VWErX04WW1r6RunDH0lRwt0dVFMeateBpiiisO2ht03m9No%2BNNVuP6ual%2BJK2r7XtGiUiadqKj%2B54JxmmRl7bEcZOF2j8yOADVDWfIF4B3%2FyifUVfqR4q%2BcZDxpleMZ49lz4uHrA5gKBYa75a%2F2B2D85rROC52zO%2FRoVYKCNU6YWXANXY2piDMkF6Li2dJatvttyQp4JPJkBuVoCiZleE6aenDX6hHaFpRzOYxDsnr3boHKXnTLo7dLi76okxbHzR11zDOBMt5ss776AvAUOYkn30HSEObQj7sCGZ1DF8%2BQ6ISrfOzCdF43tdPsDM6nFDnWTh5yPb2Z290kjxCeil6k8poxB1RqdwPNGlqGI3nBpU1CtyB8KK%2BNPb%2BG7Wz8I4radsKyG3k9A4ozGOJgTCEfxKebTDQ0TEWJLS5oBOVmFANtvEf33kigLNDdLPRcM2zxvsayD7LzqxUC9acCDLRFiGFnJ%2FvC79AIzY2NfT47AKXMUFcIqkv1Din1%2BHQFzxEi2qhL1VuNL9SxI%2Bo9bww%2BDTcDTZYkcCUnklFqSbaIqIxEBqarYD9PIi2dCEl8U1832DFcM5LYVZxLA8EP4jk%2FmZ1MmLsnE0hOQACMSSXBlofCj0hVt27LjShMWO1GM9Dp%2FbJb8W41RidU91XH%2FCqKMwRaKSNN37LzYJoYoWMsVYv57XdbAZ7laA1rcIfqnwT5CTgiRv%2B2ebmiL1thnDFKqKphS3pC6o1BOgAM0BhXWq9UHA1Z64ExVMFec33iiMDkUtPN7dUznJHb9mg1JPqfpUIHR6OPeU3fZro6m4%2BXz231SNxlp5056SqHf6ABkGfTJj7gFJMgSNw5aBivasRkM4CfStQxws7yhkmUFbzEPoAN0UlC%2BqyN28ISyZrl7l%2BHA6KJMguLfDwpJsOljJDvl%2FssggyILLKwYNAh1hNu6RxKmbtUW0FqJpE8qc85Oc4LiU%2F0xGsMAls9DgiGdSY0CcQbUnQ1glgBcY0qLaPjgAj0b5vi96%2FzmutZWn3vssJnKdfOFS0LxslaKJGzPtMQYYYI7vytf82meJH7%2BvMluhAe2SxYkgnF8cWY5fTYrFRvqgVQlJaATVpWcxjnZU%2BeVSY65iJ%2BQzOEcJhEM2rh7zmZJJTIyEXB1%2B7jCnESEaleyO7fmfVOXtgzCCMg5Dxj9EzgklLBGQim18ON0Zy9VafGSBR8ZrUKiZZZ33YwcFshDhGiMOhn7dnJ4G1H2pZy8T3ycuCgmbF5648NF3Cc0dkBluyUu3XUbDLdO%2Fcjfp9uHUV9YlCfKWU5tgErp6aKN1oJ1bthClVSfb%2Bj9jq9FSaevqJRI6IKFF4qURjm7A4Y9jxuBeteZ21x1IQfNNPRpLKrBuhYG1X9JL0KPY3dh8JDkRwTQJtKsn3nmMC%2FUnVBv6%2BddC47TVMxo5NA0IP6adtWfOZdr9oP8M3j9o7LJE9nkbRB8fRw9whEaMynEaOFTBxBT9yrnuxj6xT5ZQoJqx536h6RESMJs3biRVuSGgxlZMQLRKpBlEa84IdOB9Qdjc%2B95RQZS3UY1YK4S2d0Z%2Bls2tTNlN4BTQ9hDIRQzKNOLd%2BiPwg4qYUE51A1cyGhagbnd7mOAzbB79remfgZ4DHqi1nP8Mj0mFDFKPK8VgxVj%2F99ylKV8BrYlZB0YI6JIAy%2FrcwNqQvnV58yS6nbD1y9%2B0hmtSeiZ3zlRUTOzusIIIePjHWuCJJxaab3gyv0ydpY9Mt7hJl87Rsel8H6vI1rgDUHzps3n8C4CjuDqH2qj4j5VaVMIc77uQkwaygoYmvLCXKUHnrvyYRWq41mmLsqtt4V5pZlSpuU77S1S4mFpNsN3aNcJ%2BDQ03I5bT8SHpp54H4hVNTQN4ZqVVY9Uw5lO1SQrZTkYP%2B41usYivAbuLyRdJ4cY5MZ%2BjOtKKgOFmnJ5jLWi3uHDEfRteCWCWXgHJN4NfDPdjsx3eqG6qywQV9w6X1e8QJev3R9XY9yQiS1hmMyjQNhrF7%2FXyGsxpiv3zAFdYtz71bbnHWmKjMCmJfNSNK6NriasAiGYd0560W37V%2Bnu8cJ6bSP5PY7XfMEF9%2BnC9o667RtW8aLZQMY7gMQAsv2h68hf8bU9hcFsI1g3Q5vxzxAnGenJyPThtpEZcykugo6y%2BcGUBBiGGDX00TNmNCaQYYV5VFD6bi8sW%2BEK772nkcWpbl1zDV0L5C7uGr9aoqxSO6ZABTioWF2f3ggsMakh0h3oNwGkiTWjg6S9%2FQYiVm8AKD6LrM8O1GusIH8rg2o%2BHSTuj9QqaGYWMkURCzAv61qZiCE9aGu6F1SE69h2Zd2jHnbHT0iy%2BigvsZlYydYIq1QPW2itfeIb0upEUfJPhD%2Bdhl3EvEiRXZ03q9ZZECMql%2BIfb77Ph3tcZsYMEK%2BfsNGuXr0QwtMGTjNgAtLdHIiCE1zL0PYQ7naVXPz2EjmWt%2FgfB1iJdHylYqfD3rMQGuun4XEgMBWNluivwHTvW23kBHfovlR6sTLG5agPHT6dFa3mB0VMBy12yEjEcmyrk%2F8NkL3C27no5%2BA591V8UW47bIFTzr1BtMn7sBmPwOAowW4RKFs5z4dvKVJhW78%2BrHOIi9F7e8%2BI8kmiTOEsqr58imk3SPmDESmsLnxXMcpl0m0fIp0Bq9BbqZowQPeOXD9h7V2oMc%2BQ6e0YIruDx%2F2a7wl%2BON86UY1KciYCogVfm42zOVO9s0rekZdvHfquTDfd9UboICj1TkT2A5ZQeSewo4z%2FvWdloQopLxs3bFJ7b6t5O0I5izv9fedXFjO55zuN3rjN4njaopTvByNKxlRjfSeGRWCf5IkpBDaycvZC3uCZJJpWTFGcV%2BvmuvDrp%2FX6T4fk88JRRG2bnt46IjJpz4fUIdezykmKDYZ1FElhVDY5J0AaM0USZd3LDtmlEcC1mP1MxqHWjLRYZNvrWyhPsZTnxr2i%2FJ1pqRAyMYHkMGO1zf9%2BDsKMVcJ%2Be%2FTG3R4y%2FnWSwzlSL5DFCDWJJaz1pyQ66ljMuP5qYx6Z2vmSlaf%2B%2Bf56haUmSE3t0S7EqEf7%2FkqqYncrJqGct0h4cX1k%2F7iolVJmjIOeNRZlDFjJZlQCgAxbVx%2BdpeXUZAtVCV7XRf%2F5oE8i7SEJHbEr4JhGtjfu%2FBYjrkrWd3%2BytYcDRvHbL%2B8fM%2BPYOL1kb%2B1aks78T8vsg2tUNYiv5nI4DneCrIh9L2spYYXRpVKJsXoTP6nt6nVILKo3R3Q%2BYckcc3Xp0KSBE14mo3BCKe%2B7wJA5oHN1dYMasvN969BajY8a72ZEgwt9K7XwqVBpcS8E7wsxO26M9fHk%2FlSwd6vBTjCv3tSsgw%2BZA8V%2F8TBZ76T1s7CzZ4EIlRk5yANWWAfMmM4bp2wPxigcA1tKbRxTGl0hsNfp1ktaCQPIx0aa8DQ6ieSl5nr%2Bymo58aUL4v9DpfOFVz78TysWjI1ouhU5Pfm80jTAj65VE9KwPdmGJF5W3OfR7PMehCWaBcUkAXXoj3aIUr8jC48p3aBHQFk1%2FINaTI6en1QAX3q6se2%2Bz4C1ECLD463XXq4aN9zajxswmrTrH7Bi3JXHzNm9gHd8%2BMo2yvFfSPzd%2BFmFRFJTBqoUKdL4%2B7wGjQKOOumjdOEAe75eDozmGJiNpfsyAQnsFSPp8v0wlK5Wb4pIuksDeaMoH5yl%2BY9n1G6QK4QHZrvJ7%2Ftq5EG1ucYeGLk8cxU%2FwxdxCYQ1tukYdPnIpvbF1lG9wIkv7Of0uQMTuFWqjaUW27ovK39DP4SXuuLsGeNd0M2q1Lmo9h0J6OO5gnOPcblctO5ofKyLUZjsRtisoy1QKK7LwKZL5ftnXLkFZIjxMMtWYtszew6MeUANt%2FHON4hboUIlcXfrlsF5OVBsqIGuwEm%2F485Becn1uMAiGIEwRnvJjJszdrmaUMhxt%2BnxtSIYBPeivJUKD%2B%2BHZuyMzHEm932f%2BXilMVZI8bGDOpzNoPFn3rI0BpVXwiftHPapXv%2F74kYh89MzQpedJxvwy7kVFisndTmOclg%2B7oJAfcf7y1JNzE5EFKlP5fiIHZkpaSQV5zsCgoYYQ%3D%3D&txtseachkey=&selectIndustry1=25_1&selectIndustry2=1673_0&txtKeyword1143=%E7%83%9F%E5%9B%B1%E7%BB%B4%E4%BF%AE%E5%8E%82%E5%AE%B6&txtKeyword2=&txtKeyword3=&txtKeyword4=&txtCommerceTitle=%E7%83%9F%E5%9B%B1%E7%BB%B4%E4%BF%AE%E5%8E%82%E5%AE%B6-%E9%94%A6%E5%B3%B0%E9%AB%98%E7%A9%BA&rbtnlsCommerceType=2&rbtnlsCommerceIndustry=360&txt_195130=%E9%94%A6%E5%B3%B0%E9%AB%98%E7%A9%BA&txt_195132=&txt_195133=&txt_195135=&txt_195138=&txt_195139=&hidOldColumnIds=&OldParamNames=&ParamNames=txt_195130%2Ctxt_195132%2Ctxt_195133%2Ctxt_195135%2Ctxt_195138%2Ctxt_195139%2C&file_upload=&hidImg=&hidpicpreview=http%3A%2F%2Fuserimages16.51sole.com%2F20170505%2Fb_3985554_201705050958058834.jpg&txtProductQuantity=999&txtBrand113=%E9%94%A6%E5%B3%B0%E9%AB%98%E7%A9%BA&txtCaseDetail=%E7%83%9F%E5%9B%B1%E7%BB%B4%E4%BF%AE%E5%8E%82%E5%AE%B6&txtPrice=&chkPrice=on&txtProductSpec=%E7%83%9F%E5%9B%B1%E7%BB%B4%E4%BF%AE%E5%8E%82%E5%AE%B6&txtTrafficDesc=&txtConsignment=&txtCommerceContent=%E7%83%9F%E5%9B%B1%E4%BF%AE%E8%A1%A5%E6%96%BD%E5%B7%A5%E6%97%B6%EF%BC%8C%E9%A1%B6%E9%83%A8%E9%80%89%E7%94%A8%CE%A614%E7%9A%84%E9%92%A2%E4%B8%9D%E7%BB%B3%E5%8F%8C%E8%82%A1%E6%82%AC%E6%8C%82%E6%BB%91%E8%BD%AE%EF%BC%8C%E4%BF%AE%E8%A1%A5%E7%83%9F%E5%9B%B1%E6%97%B6%E7%83%9F%E5%9B%B1%E9%A1%B6%E9%83%A8%E5%B7%A5%E4%BA%BA%E4%B8%8D%E8%83%BD%E4%B8%80%E5%90%8C%E4%B8%8A%E5%8E%BB%EF%BC%8C%E6%9C%80%E4%B8%8A%E8%BE%B9%E6%9C%80%E5%A4%9A%E5%8F%AF%E4%BB%A52%E5%88%B03%E4%BA%BA%E3%80%82&ckAgreeRules=on&btn_newCommerce=%E6%8F%90+%E4%BA%A4&mobile=&scode=%E8%AF%B7%E5%A1%AB%E5%86%99%E7%9F%AD%E4%BF%A1%E4%B8%AD%E7%9A%84%E5%85%AD%E4%BD%8D%E9%AA%8C%E8%AF%81%E7%A0%81";
                        ResponseData = GetData(PostUrl, GetData(PostData, model.Coder), EnData(model.Url));

                        //ResponseData = GetData(PostUrl, GetData(PostData, model.Coder), model.Url);   //注释,136云同盟的referer含变量,必须要设置
                        ResponseData = ChangeResponeData(PostUrl, ResponseData);

                        if (Data.WebSite.ID == 22)//新品快播网需要转换
                            ResponseData = Regex.Unescape(ResponseData);
                    }
                }
                else
                {
                    ResponseData = GetData(PostUrl, GetData(PostData, model.Coder), model.Url, file);
                }

                //清空全局远程数据
                RemoteDatas = null;
            }
            catch (Exception ex)
            {
                ShowLog(ex.Message, 4);
                return;
            }
            #endregion
            ShowLog(ResponseData, 6);
            Regex r = null;
            #region 验证码错误
            r = getRegExp(model.CodeFail);
            if (null != r && r.IsMatch(ResponseData))
            {
                if (Times >= 3)
                {
                    SendMessage(201, string.Format("验证码错了{0}次；不再重试", Times));
                    return;
                }
                Times++;
                SendMessage(201, "验证码错误");
                ShowVerifyCode(Resume);
                return;
            }
            #endregion


            #region 发布成功
            r = getRegExp(model.Success);
            if (null != r && r.IsMatch(ResponseData))
            {


                //通过第二地址上传图片 
                if (model.UpType == 3 && !string.IsNullOrEmpty(PicUrl) && IO.FileExists(string.Format("{0}Data/Pic/{1}", Fetch.SystemRoot, PicUrl)))
                {
                    try
                    {
                        SendMessage(0, "通过第二地址上传图片");
                        Utility.Models.EMessage msg = MatchReplace(model.Match, ResponseData);
                        if (msg.Code == 200)
                        {
                            model.RemoteData.Add("succ1", msg.Data);
                        }
                        string UpUrl = DeRemote(EnData(AnalyzeData(model.UpUrl, Content.PublishData)), model.RemoteData);
                        SendMessage(0, "UpUrl=" + UpUrl);
                        string tmp = Http.GetString(UpUrl, "", model.Post, "GET", null, null);
                        SendMessage(0, tmp);
                        Regex r1 = new Regex(@"<input.*?name=""?(\w+)""?[^>]*>", RegexOptions.IgnoreCase);
                        foreach (Match m in r1.Matches(tmp))
                        {
                            if (!m.Success) continue;
                            Regex r2 = new Regex(@"value=(""[^""]*""|'[^']*'|[^""'\s>]+)", RegexOptions.IgnoreCase);
                            if (r2.IsMatch(m.Groups[0].ToString()))
                            {
                                string _val = Regex.Replace(r2.Match(m.Groups[0].ToString()).Groups[1].ToString(), @"^""|""$|^'|'$", "");
                                model.RemoteData.Add(m.Groups[1].Value, _val);
                                //SendMessage(0, string.Format("添加数据，{0}={1}", m.Groups[1].Value, _val));
                            }
                        }
                        string UpData = EnData(DeRemote(model.UpData, model.RemoteData));
                        Regex r3 = new Regex(model.uppattern, RegexOptions.IgnoreCase);
                        if (r3.IsMatch(ResponseData))
                        {
                            UpData = UpData.Replace("$returnid", r3.Match(ResponseData).Groups[1].Value);
                        }

                        SendMessage(0, "UpData=" + UpData);

                        string s = GetData(UpUrl, UpData, string.Empty, new NameValue(model.UpField, string.Format("{0}Data/Pic/{1}", Fetch.SystemRoot, PicUrl)));
                        msg = MatchReplace(model.UpMatch, s);
                        if (msg.Code == 200)
                        {
                            PostData[model.ImgField] = msg.Data;
                        }
                        Debug("上传图片：" + s);

                    }
                    catch (Exception ex)
                    {
                        SendMessage(500, ex.Message);
                    }
                }

                SendMessage(200, "发布成功");

                Debug("提交发布报告：" + Content.ProductID);
                NameValuelist nv = new NameValuelist();
                nv.Add("ProductID", Content.ProductID);

                //nv.Add("Subject", PostData[string.IsNullOrEmpty(model.TitleField) ? "Title" : model.TitleField]);
                nv.Add("Subject", InfoTitle);
                nv.Add("ClientID", Content.UserID);
                nv.Add("SiteID", this.Data.WebSite.ID);
                nv.Add("SiteName", this.Data.WebSite.Name);
                nv.Add("SiteUrl", this.Data.WebSite.Url);
                NameValue acc = Company.GetAccount(Data.WebSite, Data.Company);
                nv.Add("Account", acc.Name);
                nv.Add("Password", acc.Value);
                nv.Add("PublishMode", 1);

                //2013-12-11 添加产品的商家信息
                nv.Add("Company", this.Data.Company.SiteName);
                nv.Add("AdminName", Pigeon.Sht.Models.EAdminConfig.GetConfig().LoginName);

                BLL.Product.SetPublishLog(nv);
                SendMessage(0, "发布报告已提交");
                return;
            }
            #endregion
            r = getRegExp(model.ClassFail);
            if (null != r && r.IsMatch(ResponseData))
            {
                ShowLog("分类错误重新选择");
                SendMessage(100, InfoTitle + " 分类错误重新选择");
                try
                {
                    SelClassID(string.Format("{0}WebSite\\{1}",
                         Fetch.FilesRoot,
                         model.ClassFileName),
                         Resume);
                }
                catch (Exception ex)
                {
                    ShowLog(ex.Message);
                }
                return;
            }

            r = getRegExp(model.Fail);
            if (null != r && r.IsMatch(ResponseData))
            {
                if (Data.WebSite.ID == 23) qStr = model.RemoteData["q"];
                string s = "发布失败：" + Html.unescape(Html.RemoveHtml(r.Match(ResponseData).Value)).Trim() + " \r\n -> " + InfoTitle;
                //ShowLog(s);
                SendMessage(201, s);
                #region 去掉到拦截的关键字，重新发送 --zhengyue 2015-7-17 11:59:37
                if (Data.WebSite.Business.Detelekeyword == "1")
                {
                    ShowLog("去掉不健康字符，尝试重新发送！");
                    string padata = PostData.ToString();
                    int itrue = 0;
                    while (itrue == 0)
                    {
                        string keywordsss = r.Match(ResponseData).Groups[1].Value;
                        padata = padata.Replace(keywordsss, " ");
                        if (Data.WebSite.Business.DeleteWordstext != null)
                        {
                            string[] strsqils = Data.WebSite.Business.DeleteWordstext.Split('#');
                            for (int t = 0; t < strsqils.Length; t++)
                            {
                                padata = padata.Replace(strsqils[t].ToString(), " ");
                            }
                        }
                        PostData.ParseQueryString(padata, Encoding);
                        #region 设置验证码
                        if (!string.IsNullOrEmpty(model.Img))
                        {
                            SendMessage(201, "输入验证码");
                            VerifyUrl = model.Img;
                            ShowVerifyCode(Resume);
                            return;
                        }
                        #endregion

                        ResponseData = GetData(DeRemote(EnData(model.Post), model.RemoteData), GetData(PostData, model.Coder), model.Url);

                        r = getRegExp(model.Success);
                        if (null != r && r.IsMatch(ResponseData))
                        {
                            SendMessage(200, "发布成功");
                            itrue = 1;
                        }
                        else
                        {
                            r = getRegExp(model.Fail);
                            if (null != r && r.IsMatch(ResponseData))
                            {
                                if (r.Match(ResponseData).Groups[1].Value == keywordsss)
                                {
                                    BLL.Product.SetPublishLog(Content.ProductID, string.Format("[{0}]{1}", Data.WebSite.Name, "发布失败！！" + r.Match(ResponseData).Groups[1].Value + "无法识别该关键字,请手动去除"));
                                    SendMessage(201, "发布失败！！" + r.Match(ResponseData).Groups[1].Value + "无法识别该关键字,请手动去除");
                                    return;
                                }
                                SendMessage(201, "发布失败！！" + Html.unescape(Html.RemoveHtml(r.Match(ResponseData).Value)).Trim());
                                ShowLog("再次去掉不健康字符，尝试重新发送！");

                            }
                            else
                            {
                                SendMessage(201, "发布失败！！检查发布条数是否超限！");
                                itrue = 1;
                                BLL.Product.SetPublishLog(Content.ProductID, string.Format("[{0}]{1}", Data.WebSite.Name, "发布失败！！检查发布条数是否超限！"));
                                return;
                            }
                        }
                    }
                    NameValuelist nv = new NameValuelist();
                    nv.Add("ProductID", Content.ProductID);
                    //nv.Add("Subject", PostData[string.IsNullOrEmpty(model.TitleField) ? "Title" : model.TitleField]);
                    nv.Add("Subject", InfoTitle);
                    nv.Add("ClientID", Content.UserID);
                    nv.Add("SiteID", this.Data.WebSite.ID);
                    nv.Add("SiteName", this.Data.WebSite.Name);
                    nv.Add("SiteUrl", this.Data.WebSite.Url);
                    NameValue acc = Company.GetAccount(Data.WebSite, Data.Company);
                    nv.Add("Account", acc.Name);
                    nv.Add("Password", acc.Value);
                    nv.Add("PublishMode", 1);

                    //2013-12-11 添加产品的商家信息
                    nv.Add("Company", this.Data.Company.SiteName);
                    nv.Add("AdminName", Pigeon.Sht.Models.EAdminConfig.GetConfig().LoginName);
                    BLL.Product.SetPublishLog(nv);
                    SendMessage(0, "发布报告已提交");
                    return;
                }
                #endregion
                BLL.Product.SetPublishLog(Content.ProductID, string.Format("[{0}]{1}", Data.WebSite.Name, s));
                return;
            }

            //ShowLog("发布返回错误");
            SendMessage(201, "发布返回错误 \r\n -> " + InfoTitle);
            BLL.Product.SetPublishLog(Content.ProductID, string.Format("[{0}]{1}", Data.WebSite.Name, "发布失败错误 \r\n -> " + InfoTitle));
        }

        #region 发产品前，选分类的回调函数
        /// <summary>
        /// 发布前，是否选择了分类
        /// </summary>
        public bool isSelectId = false;
        public void ClassIDCollback(object sender, NameValuelist data)
        {
            Business bus = (Business)sender;
            EBusiness busModel = bus.model;
            bus.isSelectId = true;  //设置为已经选择分类状态
            string stepParam = busModel.Step[0].Param + "&" + data.ToString();

            //POST请求方式， 发布前，需要先从A页面POST到B页面，不能直接在浏览器打开B页面
            if (busModel.Step[0].Url_Method != null && busModel.Step[0].Url_Method.ToLower().Equals("post"))
                busModel.Step[0].Param += ("&" + data.ToString());
            else
            {
                //GET请求方式，发布前，需要从A页面GET到B页面
                if (busModel.Post.IndexOf("?") == -1)
                    busModel.Post += ("?" + stepParam);
                else if (model.Post.IndexOf("&") == -1)
                    busModel.Post += stepParam;
                else
                    busModel.Post += ("&" + stepParam);
            }
            busModel.Params += ("&" + stepParam);   //给发布页面的参数，追加step的所有参数

            SendMessage(200, "启用分类参数：" + stepParam);
        }
        #endregion

        #region 2017-04-24 wanghui 环球厨卫，POST采用json格式,手动指定发布
        Site_8 site8 = new Site_8();
        #endregion


        /// <summary>
        /// 过滤HTML格式
        /// </summary>
        /// <param name="bis"></param>
        /// <param name="details"></param>
        string ReplaseHTML(EBusiness bis, string details)
        {
            //wanghui 2013-06-24
            if (bis.Fillter_Html == 1)
            {
                details = details.Replace("&amp; ", "&");
                details = details.Replace("&amp;", "&");
                details = details.Replace("<br />", "\n");
                details = details.Replace("<BR />", "\n");
                details = details.Replace("<br />", "\n");
                details = details.Replace("<BR  />", "\n");
                details = details.Replace("<br>", "\n");
                details = details.Replace("<br/>", "\n");     //换行
                details = details.Replace("&nbsp;", "\r");    //空格

                details = details.Replace("<p>", "\n");
                details = details.Replace("</p>", "");
                details = details.Replace("<P>", "\n");
                details = details.Replace("</P>", "");

                details = details.Replace("<div>", "\n");//div标签换行
                details = details.Replace("</div>", "");
                details = details.Replace("<DIV>", "\n");
                details = details.Replace("</DIV>", "");

                for (int i = 1; i < 7; i++)
                {
                    details = details.Replace("<h" + i + ">", "\r\n\n");
                    details = details.Replace("</h" + i + ">", "");
                }
                //忽略img标签 
                if (bis.ShowImg == "true")
                {
                    details = Html.RemoveHtml(details, new string[] { "img" });
                }
                else
                {
                    details = Html.RemoveHtml(details);   //过滤其他格式
                }        
            }
            else if (bis.Fillter_Html == 2)   //只支持<br>标签
            {
                details = details.Replace("<br />", "{br}{br}");
                details = details.Replace("<BR />", "{br}{br}");
                details = details.Replace("<br>", "{br}{br}");
                details = details.Replace("<br/>", "{br}{br}");     //换行
                details = details.Replace("\r\n", "{br}{br}");     //换行
                details = details.Replace("\n", "{br}{br}");     //换行
                details = details.Replace("&nbsp;", " ");    //空格

                details = details.Replace("<p>", "{br}{br}");
                details = details.Replace("</p>", "");
                details = details.Replace("<P>", "{br}{br}");
                details = details.Replace("</P>", "");

                details = details.Replace("<div>", "{br}{br}");//div标签换行
                details = details.Replace("</div>", "");
                details = details.Replace("<DIV>", "{br}{br}");
                details = details.Replace("</DIV>", "");


                for (int i = 1; i < 7; i++)
                {
                    details = details.Replace("<h" + i + ">", "{br}{br}");//h标签换行
                    details = details.Replace("</h" + i + ">", "");
                }
                //忽略img标签
                if (bis.ShowImg == "true")
                {
                    details = Html.RemoveHtml(details, new string[] { "img" });
                }
                else
                {
                    details = Html.RemoveHtml(details);   //过滤其他格式
                }
                details = details.Replace("{br}", "<br />");     //换行
            }
            else if (bis.Fillter_Html == -1)
            {
                //忽略img标签
                if (bis.ShowImg == "true")
                {
                    details = Html.RemoveHtml(details, new string[] { "img" });
                }
                else
                    details = Html.RemoveHtml(details); //过滤其他格式
            }

            else if (bis.Fillter_Html == 3)
                details = StringHelper.RemoveHtml(details);   //过滤其他格式
            else if (bis.Fillter_Html == 4)
                details = HtmlDecode(details);  //html解译
            else if (bis.Fillter_Html == 5)
                details = HtmlEncode(details);   //html转义 


            if (bis.ShowImg != "true")
            {
                details = Html.RemoveStr(details, "img");
            }
            details = SpecialDealHtml(details);//特殊处理
            return details;
        }
        
        string HtmlEncode(string HTML)
        {
            return HttpUtility.HtmlEncode(HTML);
        }

        string HtmlDecode(string HTML)
        {
            return HttpUtility.HtmlDecode(HTML);
        }
            
        public ReturnVerifyCode2 OnReturnCode2;

        public void ShowReturnCode2(NameValuelist oldList, ResumeHandler resume)
        {
            if (string.IsNullOrEmpty(model.Font_Img))
            {
                SendMessage(0, "文字验证码配置错误");
                return;
            }
            Random rand = new Random();
            model.Font_Img = model.Font_Img.Replace("{$random}", rand.Next().ToString());
            ShowLog("输入验证码：" + model.Font_Img);
            if (OnReturnCode2 != null)
            {
                string responseHtml = string.Empty;
                try
                {

                    responseHtml = GetData(model.Font_Img, "");

                    if (model.Font_Regex != null && (!model.Font_Regex.Equals("")))
                    {
                        Regex r1 = new Regex(model.Font_Regex, RegexOptions.IgnoreCase);
                        responseHtml = r1.Match(responseHtml).Groups[model.Font_Regex_Index].Value;
                    }
                    OnReturnCode2(this, oldList, responseHtml, resume);
                }
                catch (Exception ex)
                {
                    ShowLog(ex.Message, 2);
                    SendMessage(500, ex.Message);
                }
            }
        }

        /// <summary>
        /// 针对需要特殊处理的网站
        /// </summary>
        /// <returns></returns>
        string SpecialDealHtml(string details)
        {
            string patt = "<img(.*?)>";
            Regex reg = new Regex(patt, RegexOptions.IgnoreCase);
            if (reg.IsMatch(details))
            {
                string tempImg = reg.Match(details).Groups[0].Value;
                string patt1 = "src=\"([\\s\\S]+?)\"";
                Regex reg1 = new Regex(patt1, RegexOptions.IgnoreCase);
                if (reg1.IsMatch(tempImg))
                {
                    details = details.Replace(reg.Match(details).Groups[0].Value, string.Format("<img {0} />", reg1.Match(tempImg).Groups[0].Value));
                }
            }
            if (Data.WebSite.ID == 6)
            {
                details = details.Replace("首发", "").Replace("丙烯", "");
            }
            if (Data.WebSite.ID == 95)
            {
                details = details.Replace("&quot;", "");
                string pattern = "<img(.*?)>";
                Regex re = new Regex(pattern, RegexOptions.IgnoreCase);
                if (re.IsMatch(details))
                {
                    details = details.Replace(re.Match(details).Groups[0].Value, re.Match(details).Groups[0].Value.Replace(" ", "+"));
                }
            }
            if (Data.WebSite.ID == 143)
            {
                details = Html.RemoveStr(details, new string[] { "a", "span" });
            }
            if (Data.WebSite.ID == 144)
            {
                string pattern = "<img(.*?)>";
                Regex re = new Regex(pattern, RegexOptions.IgnoreCase);
                if (re.IsMatch(details))
                {
                    string str1 = re.Match(details).Groups[0].Value;
                    string pattern2 = "src=\"([\\s\\S]+?)\"";
                    Regex re2 = new Regex(pattern2, RegexOptions.IgnoreCase);
                    if (re2.IsMatch(str1))
                    {
                        details = details.Replace(str1, string.Format("[img]{0}[/img]", re2.Match(str1).Groups[1].Value));
                    }
                }
            }
            if (Data.WebSite.ID == 137)
            {
                string pattern = "<img(.*?)>";
                Regex re = new Regex(pattern, RegexOptions.IgnoreCase);
                if (re.IsMatch(details))
                {
                    var temp = re.Match(details).Groups[1].Value.Remove(re.Match(details).Groups[1].Value.LastIndexOf('/'));
                    temp = "<img" + temp + " " + "data-ke-src=" + temp.Replace("src=", "") + ">";

                    details = details.Replace(re.Match(details).Groups[0].Value, temp);
                }
            }
            return details;
        }

        /// <summary>
        /// 特殊问题
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="webId"></param>
        void SetData(string datas, int webId)
        {
            try
            {
                if (Data.WebSite.ID == 1)
                {
                    if (!string.IsNullOrEmpty(PostData["keywords_1"]))
                    {
                        PostData["keywords_1"] = Html.unescape(PostData["keywords_1"]);
                    }
                    if (!string.IsNullOrEmpty(PostData["keywords_0"]))
                    {
                        PostData["keywords_0"] = Html.unescape(PostData["keywords_0"]);
                    }
                    if (!string.IsNullOrEmpty(PostData["keywords_2"]))
                    {
                        PostData["keywords_2"] = Html.unescape(PostData["keywords_2"]);
                    }
                }
                ////针对特殊问题
                //if (Data.WebSite.ID == 12)
                //{
                //    if (!string.IsNullOrEmpty(PostData["mypicone"]))
                //    {
                //        int length = (PostData["mypicone"].LastIndexOf(".") - PostData["mypicone"].LastIndexOf("/"));
                //        PostData["mypicone"] = PostData["mypicone"].Substring(PostData["mypicone"].LastIndexOf("/") + 1, length - 1);
                //    }
                //}
                if (Data.WebSite.ID == 44)
                {
                    string imgUrl = PostData["HidInputImageList"];
                    PostData["HidInputImageList"] = string.Format("{0},{0},{0}", imgUrl);
                }
                if (Data.WebSite.ID == 126)
                {
                    string url = "http://member.kuyibu.com/PicManage/UserPhotoList.ashx?albId=1";
                    string html = GetData(url, null, null);
                    string pattern1 = "photoSrc\":\"([\\s\\S].*?)\"";
                    Regex reg = new Regex(pattern1, RegexOptions.IgnoreCase);
                    if (reg.IsMatch(html))
                    {
                        MatchCollection mc = reg.Matches(html);
                        Match ma = mc[mc.Count - 1];
                        if (ma != null)
                        {
                            string val = reg.Match(ma.Value).Groups[1].Value;
                            PostData["picsample_0"] = val;
                        }
                    }
                }
                if (Data.WebSite.ID == 144)
                {
                    string url = "http://www.95gq.com/ajax/ajax_write_img_data.php";
                    Upmatch(model.UpMatch, datas);
                    string data = string.Format("albumid={0}&imgurl={1}&sourcename={2}&imgsize=1000&type=1", model.RemoteData["albumid"], UpmatchData["imgurl"], UpmatchData["sourcename"]);
                    PostData["productimgurl"] = GetData(url, data, null);
                }

                if (Data.WebSite.ID == 145)
                {
                    string pattern1 = "([\\s\\S]+?),";

                    string pictureDomain = "";
                    string picture = "";
                    Regex re1 = new Regex(pattern1, RegexOptions.IgnoreCase);
                    if (re1.IsMatch(pattern1))
                    {
                        picture = re1.Match(datas).Groups[1].Value;
                    }
                    pictureDomain = datas.Substring(datas.LastIndexOf(';'), datas.Length - datas.LastIndexOf(';')).Replace(";", "");

                    model.RemoteData.Add("nPictureDomain", pictureDomain);
                    model.RemoteData.Add("nPicture", picture);
                    model.Remotes.ForEach(x => { x.IsNOIn = false; });
                    model.RemoteData = GetRemote(model.Remotes);
                    model.Remotes.ForEach(x => { x.IsNOIn = true; });
                    PostData["PictureID"] = model.RemoteData["PictureID"];
                }
                if (Data.WebSite.ID == 148)
                {
                    string url = "http://manage.ic98.com/supply.asp?act=new";
                    string html = GetData(url, null, null);
                    string pattern1 = "name=\"([\\s\\S]+?)\"";
                    Regex reg = new Regex(pattern1, RegexOptions.IgnoreCase);
                    if (reg.IsMatch(html))
                    {
                        MatchCollection mc = reg.Matches(html);
                        foreach (Match ma in mc)
                        {
                            string val = reg.Match(ma.Value).Groups[1].Value;
                            if (val.Contains("kaia"))
                            {
                                PostData.Add(val, val);
                            }
                        }
                    }
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// 去掉详细内容中的特殊字符(最后拼接好的内容)
        /// </summary>
        /// <param name="details"></param>
        /// <returns></returns>
        public string ReplaceStr(string details)

        {
            if (Data.WebSite.ID == 1)
            {
                details = details.Replace("\"", "''");
            }
            if (Data.WebSite.ID == 45)
            {
                details = details.Replace("&#39;", "'");
                details = details.Replace("&quot;", "''");
                details = details.Replace("&amp;", "");
            }
            if (Data.WebSite.ID == 143)
            {
                int times = Html.StrTimes(details, "www.", 4);
                if (times > 2)
                {
                    string pattern = @"((http|https)://)?(www.)?[a-z0-9\.]+(\.(com|net|cn|com\.cn|com\.net|net\.cn))(/[^\s\n]*)?";
                    // string pattern = @"[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?";
                    Regex re = new Regex(pattern, RegexOptions.IgnoreCase);
                    MatchCollection mc = re.Matches(details);
                    bool firstOut = true;
                    foreach (Match ma in mc)
                    {
                        if (!string.IsNullOrEmpty(ma.Groups[0].Value) && ma.Groups[0].Value.ToLower().Contains("www"))
                        {
                            if (firstOut) { firstOut = false; continue; }
                            details = details.Replace(ma.Groups[0].Value, "");
                        }
                    }
                    details = details.Replace("网址", "").Replace("网 址", "");
                }
            }
            //去掉正文中的敏感词-2016-11-4
            if (model.checkDirtyWords == "true")
            {
                details = DetailHelper.Instance.ReplaceKeywords(details);                
            }
            return details;
        }

        /// <summary>
        /// 改变标题2016-9-18(马可菠萝标题重复)
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public string ChangeTitle(List<ERemote> remote, string title)
        {
            try
            {
                if (remote.Count > 0)
                {
                    if (Data.WebSite.ID == 3)
                    {
                        model.RemoteData.Remove("CheckTitle");
                        remote.ForEach(x =>
                        {
                            if (!x.checkTitle) { remote.Remove(x); }
                        });
                        remote[0].Form_Params = string.Format("title={0}&ts={1}", title, DateTime.Now.Ticks);
                        GetRemote(remote);
                        string result = model.RemoteData["CheckTitle"];
                        if ((!string.IsNullOrEmpty(result) && result == "1") || title.EndsWith("/"))
                        {
                            if (!string.IsNullOrEmpty(remote[0].title_params))
                            {
                                var s = remote[0].title_params.Split(',');
                                string str = Html.GetRandom(s);
                                title = title.Substring(0, title.Length - str.Length).Insert(title.Length - 1, str);
                            }
                        }
                    }
                }
            }
            catch
            {
                SendMessage(0, "标题重复-替换出错");
            }
            return title;
        }

        /// <summary>
        /// 更改发布返回信息(2016-9-19)
        /// </summary>
        /// <param name="responeData"></param>
        /// <returns></returns>
        public string ChangeResponeData(string url, string responeData)
        {
            if (string.IsNullOrEmpty(responeData)) return responeData;


            if (Data.WebSite.ID == 145)
            {
                string pattern = "Success\":\"([\\s\\S]+?)\"";
                Regex re3 = new Regex(pattern, RegexOptions.IgnoreCase);
                if (re3.IsMatch(pattern))
                {
                    int res = -1;
                    string data = re3.Match(responeData).Groups[1].Value;
                    if (int.TryParse(data, out res))
                    {
                        if (res >= 0)
                        {
                            responeData = "发布成功";
                        }
                        else
                        {
                            responeData = responeData.Replace("DirtyWords", "敏感词");
                            SendMessage(0, responeData);
                        }
                    }

                }
            }
            else if (Data.WebSite.ID == 1)
            {
                Regex r = getRegExp(model.Fail);
                if (null != r && r.IsMatch(responeData))
                {
                    string s = Html.unescape(Html.RemoveHtml(r.Match(responeData).Value)).Trim();
                    if (s.Contains(model.filedcontent))
                    {
                        var arr = model.titleparms.Split(',');
                        string str = Html.GetRandom(arr);
                        PostData["title"] = PostData["title"].Substring(0, PostData["title"].Length - str.Length).Insert(PostData["title"].Length - str.Length, str);
                        ShowLog("尝试重新发送,标题:" + PostData["title"]);
                        responeData = GetData(url, GetData(PostData, model.Coder), model.Url);
                    }
                }
            }
            else if (Data.WebSite.ID == 44) //搜了网标题重复 2017-07-27
            {
                Regex r = getRegExp(model.Success);
                if (!r.IsMatch(responeData))
                {
                    if (responeData.Contains("标题不能重复"))
                    {
                        int secends = 1;
                        var arr = model.titleparms.Split(',');
                        string str = Html.GetRandom(arr);
                        ShowLog(string.Format("标题重复,暂停{0}秒后尝试重新发送,标题:{1}", secends, PostData["txtProductName"]));
                        PostData["txtProductName"] = PostData["txtProductName"].Substring(0, PostData["txtProductName"].Length - str.Length).Insert(PostData["txtProductName"].Length - str.Length, str);
                        Thread.Sleep(secends * 1000);
                        responeData = GetData(url, GetData(PostData, model.Coder), model.Url);
                    }
                }
            }
            else if (Data.WebSite.ID == 45) //东方供应商标题重复 2017-07-27
            {
                Regex r = getRegExp(model.Success);
                if (!r.IsMatch(responeData))
                {
                    if (responeData.Contains("不能重复"))
                    {
                        int secends = 1;
                        var arr = model.titleparms.Split(',');
                        string str = Html.GetRandom(arr);
                        ShowLog(string.Format("标题重复,暂停{0}秒后尝试重新发送,标题:{1}", secends, PostData["title"]));
                        PostData["title"] = PostData["title"].Substring(0, PostData["title"].Length - str.Length).Insert(PostData["title"].Length - str.Length, str);
                        Thread.Sleep(secends * 1000);
                        responeData = GetData(url, GetData(PostData, model.Coder), model.Url);
                    }
                }
            }
            else if (Data.WebSite.ID == 147)
            {
                model.Success = PostData["title"].Replace("/", "&#47;");
            }
            else if (Data.WebSite.ID == 136 && !responeData.Contains("不允许使用的词汇"))
            { 
                //不允许使用的词汇：缩阴
                try
                {
                    NameValue nv = Company.GetAccount(Data.WebSite, Data.Company);
                    string url1 = "http://" + nv.Name + ".skxox.com/manage/write3.ashx?ex=next&key=" + responeData + "&r=0.9034425607049628";
                    responeData = GetData(url1, "", url1);
                    string url2 = "http://" + nv.Name + ".skxox.com/p/" + responeData.Substring(0, 8) + "/" + responeData.Substring(8, 6) + ".html";

                    responeData = GetData(url2, "", url2);
                    model.Success = PostData["title"];
                }
                catch (Exception ex) { }
            }
            else if (Data.WebSite.ID == 158) { 
                
            }

            return responeData;
        }    
    }
}