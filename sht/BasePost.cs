using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Pigeon.Utility;
using System.Net;
using System.IO;
using System.Collections; 
namespace Pigeon.Sht.BLL
{
    public delegate void CompleteHandler(object sender, Models.EWebSite site);
    public class BasePost
    {
        public HttpHelper Http { get; set; }
        public int UserID { get; set; }
        public bool Enabled { get; set; }
        public string VerifyUrl { get; set; }
        public NameValuelist PostData { get; set; }
        public Models.EPublishData Data { get; set; }
        public bool isDebug { get; set; }
        public string ResponseData { get; set; }
        public int Times { get; set; }
        public Encoding Encoding { get; set; }
        public event MessageHandler Message;
        public event CompleteHandler Complete;
        public event EntryVerifyCodeHandler OnEntryVerifyCode;
        public event SelClassIDHandler OnSelClassID;
        public event SelVerifyIDHandler OnSelVerifyID;
        public CookieContainer CookieContainerimg { get; set; }


        /// <summary>
        /// 追加Cookies 2013-07-23
        /// </summary>
        public string Cookies { get; set; }

        public BasePost()
        {
            isDebug = Helper.Config.IsDebug;
            PostData = new NameValuelist();
            Http = new HttpHelper();
            Times = 0;

        }
        #region 事件
        public void OnComplete(object sender)
        {
            if (null != Complete)
                Complete(sender, Data.WebSite);
        }
        public void SendMessage(Utility.Models.EMessage msg)
        {
            if (null == Message) return;
            msg.Message = string.Format("[{0}]{1}", Data.WebSite.Name, msg.Message);
            Message(this, msg);
        }
        public void SendMessage(int code, string message)
        {
            if (null == Message) return;
            Utility.Models.EMessage msg = new Utility.Models.EMessage();
            msg.Code = code;
            msg.Message = string.Format("[{0}]{1}", Data.WebSite.Name, message);
            Message(this, msg);
        }
        #endregion

        public string EnData(string data)
        {
            if (string.IsNullOrEmpty(data)) return data;
            //获取省 市
            string Province = "";
            string city = "";
            if (Data.Company.AreaName != "" && Data.Company.AreaName != null)
            {
                string[] array = Data.Company.AreaName.Split(',');
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].Contains("省"))
                    {
                        Province = array[i];
                    }
                    else if (array[i].Contains("市") || i == array.Length - 1)
                    {
                        city = array[i];
                    }

                }
            }

            NameValue nv = Company.GetAccount(Data.WebSite, Data.Company);
            data = EnData(data, "UserName", nv.Name);
            data = EnData(data, "MD5UserName", Encrypt.MD5(nv.Name, Encoding.UTF8));
            data = EnData(data, "Password", nv.Value);
            data = EnData(data, "MD5Password", Encrypt.MD5(nv.Value.ToString(), Encoding.UTF8));
            data = EnData(data, "Rand", Text.GetRandomStr(6));
            data = EnData(data, "Randc", Text.GetRandomChineseStr(10));
            data = EnData(data, "getTime", Math.Floor(DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds));
            data = EnData(data, "getTimeCH",  Math.Floor((DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1))).TotalMilliseconds));
            data = EnData(data, "getnowtime", DateTime.Now.ToString());

            data = EnData(data, "UserValiDate", DateTime.Now.AddMonths(6).ToString("yyyy-MM-dd"));
            data = EnData(data, "ValiDate", DateTime.Now.AddMonths(6).AddDays(-4).ToString("yyyy-MM-dd"));

            data = EnData(data, "TimeStamp", Text.TimeStamp);
            data = EnData(data, "Ticks", DateTime.Now.Ticks);
            //随机MD5 2017-05-04
            data = EnData(data, "MD5Rand", Encrypt.MD5(new Random().Next().ToString(), Encoding.UTF8));
            //公司简称
            data = EnData(data, "ShortName", Data.Company.ShortName);

            Regex r = new Regex(@"{\$Time\[([\s\S]?\d+)\]}", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match m in r.Matches(data))
            {
                if (m.Success && !string.IsNullOrEmpty(m.Groups[1].ToString()))
                {
                    data = r.Replace(data, DateTime.Now.AddDays(vConvert.ToDouble(r.Match(data).Groups[1].Value, 0)).ToString(), 1);
                }
            }
            r = new Regex(@"{\$Date\[([\s\S]?\d+)\]}", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match m in r.Matches(data))
            {
                if (m.Success && !string.IsNullOrEmpty(m.Groups[1].ToString()))
                {
                    data = r.Replace(data, DateTime.Now.AddDays(vConvert.ToDouble(r.Match(data).Groups[1].Value, 0)).ToString("yyyy-MM-dd"), 1);
                }
            }
            r = new Regex(@"{\$TimeStamp\[([\s\S]?\d+)\]}", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match m in r.Matches(data))
            {
                if (m.Success && !string.IsNullOrEmpty(m.Groups[1].ToString()))
                {
                    data = r.Replace(data, vConvert.ToTimeStamp(DateTime.Now, false, vConvert.ToInt32(m.Groups[1].Value, 0)).ToString(), 1);
                }
            }

            data = EnData(data, "Fax", Data.Company.Fax);
            data = EnData(data, "ZipCode", Data.Company.ZipCode);
            data = EnData(data, "Address", Data.Company.Address);
            data = EnData(data, "AreaID", Data.Company.AreaID);
            data = EnData(data, "AreaName", Data.Company.AreaName);
            data = EnData(data, "Province", Province);
            data = EnData(data, "city", city);
            data = EnData(data, "CellPhone", Data.Company.CellPhone);
            data = EnData(data, "Email", Data.Company.Email);
            data = EnData(data, "Keywords", Data.Company.Keywords);
            data = EnData(data, "Linkman", Data.Company.linkman);
            data = EnData(data, "MateDescription", Data.Company.MateDescription);
            data = EnData(data, "MateKeywords", Data.Company.MateKeywords);
            data = EnData(data, "OnElite", Data.Company.OnElite);
            data = EnData(data, "Phone", Data.Company.Phone);
            data = EnData(data, "PinyinName", Data.Company.PinyinName);
            data = EnData(data, "PinyinShortName", Data.Company.PinyinShortName);
            data = EnData(data, "SiteName", Data.Company.SiteName);
            data = EnData(data, "SiteUrl", Data.Company.SiteUrl);
            data = EnData(data, "QQ", "");
            data = EnData(data, "MSN", "");
            data = EnData(data, "CellPhone", Data.Company.CellPhone);
            data = EnData(data, "newguid", System.Guid.NewGuid().ToString());

           
            return data;
        }

        #region 获取远程数据
        public  NameValuelist RemoteData { get; set; }
        public NameValuelist GetRemote(List<Models.ERemote> remotes)
        {
            if (null == RemoteData) RemoteData = new NameValuelist();
            List<string> url = new List<string>();
            string _data = string.Empty;

            foreach (Models.ERemote item in remotes)
            {
                
                if (string.IsNullOrEmpty(item.Url)) continue;
                if (item.IsNOIn) continue;
                if (null != RemoteData)
                item.Url= DeRemote(EnData(item.Url), RemoteData);

                ShowLog(string.Format("获取远程数据：{0};方法：{1}", item.Url, item.Method), 5);

                if (!url.Contains(item.Url.ToLower().Trim()))
                {
                    try
                    {
                        if ((!string.IsNullOrEmpty(item.Form_Method)) && item.Form_Method.ToUpper() == "POST")
                        {
                            Http.Method = "POST";
                            if (item.Form_Params == null)
                                item.Form_Params = "";
                            string tempData = EnData(DeRemote(item.Form_Params, RemoteData));
                            if (!string.IsNullOrEmpty(item.strlink))
                            {
                                tempData = Html.ReplaceN(tempData, item.strlink);
                            }
                            if (item.IsPostUrl != "true")
                            {
                                _data = GetData(EnData(item.Url), tempData, item.Referer);
                            }
                            else 
                            {
                                _data = GetResponeUrl(EnData(item.Url), tempData);
                            }
                        }
                        else
                        {
                            Http.Method = "GET";
                            _data = GetData(EnData(item.Url), string.Empty, item.Referer);
                        }
                        ShowLog(_data, 6);
                        url.Add(item.Url.ToLower().Trim());
                    }
                    catch (Exception ex)
                    {
                        ShowLog(ex.Message, 4);
                        _data = string.Empty;
                        continue;
                    }
                }
                else
                {
                    ShowLog(string.Format("再次分析：{0}；方法：{1}", item.Url, item.Method), 5);
                }
                try
                {
                    switch (item.Method)
                    {
                        case 1:
                            {
                                if (string.IsNullOrEmpty(item.Pattern)) break;
                                Regex r1 = new Regex(item.Pattern);
                                int i = 0;
                                foreach (string _v in r1.Split(_data))
                                {
                                    RemoteData.Add("remote" + i, _v.Trim());
                                    i++;
                                }
                            }
                            break;
                        case 2:
                            {
                                ShowLog(string.Format("方法：2;正则：{0}", item.Pattern), 5);
                                if (string.IsNullOrEmpty(item.Pattern)) break;
                                Regex r1 = new Regex(item.Pattern);
                                foreach (Match m in r1.Matches(_data))
                                {
                                    if (!m.Success || m.Groups.Count < 3) continue;
                                    string _key = m.Groups[1].Value.ToLower();
                                    string _val = m.Groups[2].Value;
                                    RemoteData.Add(_key, _val);
                                }
                            }
                            break;
                        case 3:
                            foreach (Models.ERemoteItem _item in item.Items)
                            {
                                string _val = Cookie(_item.Key);
                                RemoteData.Add(_item.Key, _val);
                            }
                            //foreach (string _key in item.Keys)
                            //{
                            //    string _val = Cookie(_key);
                            //    if (!string.IsNullOrEmpty(_val))
                            //    {
                            //        RemoteData.Add(_key.ToLower().Trim(), _val);
                            //    }
                            //}
                            break;
                        case 4:
                            foreach (Models.ERemoteItem _item in item.Items)
                            {
                                if (string.IsNullOrEmpty(_item.Pattern)) continue;
                                try
                                {
                                    Regex r1 = new Regex(_item.Pattern.Trim(), RegexOptions.IgnoreCase);
                                    if (r1.IsMatch(_data))
                                    {

                                        if (Data.WebSite.Business.Addaspx == "true")
                                        {
                                            string value = r1.Match(_data).Groups[_item.Index].Value;
                                            value = value.Replace("+", "%2b");
                                            value = value.Replace("/", "%2f");
                                            value = value.Replace("=", "%3d");

                                            //if (_item.Key == "viewstate")
                                            //    value = "6X6qG%2FaNjncr6zyYLHTMjJ9xU%2BrcVjeHI%2Bh8Oph0rA73%2F1N3jT81H3NPu5xP7Kh%2FApjjANeCUpECE2M1aCdbcMXzGVoGrmk2FsnhQV1nlWwKlLNhRRf1X%2FFmZhPiRBgx%2Fa0ZNA4dnAuuk6RlAQ1FJ8IB7HVJuSRp%2FvNyni1VM0sN4Zwbx25kX4r4kQqJzQqO%2F0L52dT4%2BLC8ZVo%2FUv%2F5jtR79h7Q%2B4MM8S8E0aRkER23wbT%2F0BBguDzej49rtbXIJGmUsbEyZSk5gqBgbabs8q%2FfrLOjt5LwqfCwmzq65r71B1v9j%2BTgy13DW3LUUSt7KmBbZ6JTz%2FRtPUf%2BOfLfp4gHCyi6HdQ4Rp4ENy5CWiM%2BCr2k1bpiFEYLyxEVp5QWSwYf5BSad75zRAXCd7N%2BUySrSyJYv3hwXei4qstsh2%2Bz4JmPNSt0J2TpShYRntJYK3hebGsILZyUKzU9%2BSs8qoxqDMPQU2Odtgpr7Ub%2FSlExBM7hiQNxduPfgGx83nrN1D76jQg%2BLJ5rEAvpYwPz1DgmIf31v%2Fy%2FxQ37jrgj3oq4rJaqpKVbGgW8wDwDpee3BefWFqGznfr779l%2FnnSlkdwUX%2BWMDlGILOnR52jqv1HqE%2Bz07Sj14PkvwxqKIMiltSZa52rEjMacJ1GblbQdnifCpg2E9prsst1bfDiwwWjnUs98Qx1XJVezGgIwTokT8%2B4SYeVdV9t9DsomBKrPl9xJ3jiQbs8wQODV4KX%2FbcaZtiQk9vWrh8nvCnUTsMwCLnG0k8NFn7z1cNwp10IX7iqgz2Dntj4wN9zOq8R2Jk%2F0hywr4EJXNWH2vL6N4CKSEyIOSo741fMXIxREk%2BsAeSmSX303XD4xPWdRXU69O356xBLBv5UhrinqjNqM0U9BsxOICJmNnzOFI0S5kz1gV4chVPpDKp%2Ft8lhxOBS1pGoDq0g%2FpldxSU0euwTXy%2FTtmFizi8hETrFh9VWErX04WW1r6RunDH0lRwt0dVFMeateBpiiisO2ht03m9No%2BNNVuP6ual%2BJK2r7XtGiUiadqKj%2B54JxmmRl7bEcZOF2j8yOADVDWfIF4B3%2FyifUVfqR4q%2BcZDxpleMZ49lz4uHrA5gKBYa75a%2F2B2D85rROC52zO%2FRoVYKCNU6YWXANXY2piDMkF6Li2dJatvttyQp4JPJkBuVoCiZleE6aenDX6hHaFpRzOYxDsnr3boHKXnTLo7dLi76okxbHzR11zDOBMt5ss776AvAUOYkn30HSEObQj7sCGZ1DF8%2BQ6ISrfOzCdF43tdPsDM6nFDnWTh5yPb2Z290kjxCeil6k8poxB1RqdwPNGlqGI3nBpU1CtyB8KK%2BNPb%2BG7Wz8I4radsKyG3k9A4ozGOJgTCEfxKebTDQ0TEWJLS5oBOVmFANtvEf33kigLNDdLPRcM2zxvsayD7LzqxUC9acCDLRFiGFnJ%2FvC79AIzY2NfT47AKXMUFcIqkv1Din1%2BHQFzxEi2qhL1VuNL9SxI%2Bo9bww%2BDTcDTZYkcCUnklFqSbaIqIxEBqarYD9PIi2dCEl8U1832DFcM5LYVZxLA8EP4jk%2FmZ1MmLsnE0hOQACMSSXBlofCj0hVt27LjShMWO1GM9Dp%2FbJb8W41RidU91XH%2FCqKMwRaKSNN37LzYJoYoWMsVYv57XdbAZ7laA1rcIfqnwT5CTgiRv%2B2ebmiL1thnDFKqKphS3pC6o1BOgAM0BhXWq9UHA1Z64ExVMFec33iiMDkUtPN7dUznJHb9mg1JPqfpUIHR6OPeU3fZro6m4%2BXz231SNxlp5056SqHf6ABkGfTJj7gFJMgSNw5aBivasRkM4CfStQxws7yhkmUFbzEPoAN0UlC%2BqyN28ISyZrl7l%2BHA6KJMguLfDwpJsOljJDvl%2FssggyILLKwYNAh1hNu6RxKmbtUW0FqJpE8qc85Oc4LiU%2F0xGsMAls9DgiGdSY0CcQbUnQ1glgBcY0qLaPjgAj0b5vi96%2FzmutZWn3vssJnKdfOFS0LxslaKJGzPtMQYYYI7vytf82meJH7%2BvMluhAe2SxYkgnF8cWY5fTYrFRvqgVQlJaATVpWcxjnZU%2BeVSY65iJ%2BQzOEcJhEM2rh7zmZJJTIyEXB1%2B7jCnESEaleyO7fmfVOXtgzCCMg5Dxj9EzgklLBGQim18ON0Zy9VafGSBR8ZrUKiZZZ33YwcFshDhGiMOhn7dnJ4G1H2pZy8T3ycuCgmbF5648NF3Cc0dkBluyUu3XUbDLdO%2Fcjfp9uHUV9YlCfKWU5tgErp6aKN1oJ1bthClVSfb%2Bj9jq9FSaevqJRI6IKFF4qURjm7A4Y9jxuBeteZ21x1IQfNNPRpLKrBuhYG1X9JL0KPY3dh8JDkRwTQJtKsn3nmMC%2FUnVBv6%2BddC47TVMxo5NA0IP6adtWfOZdr9oP8M3j9o7LJE9nkbRB8fRw9whEaMynEaOFTBxBT9yrnuxj6xT5ZQoJqx536h6RESMJs3biRVuSGgxlZMQLRKpBlEa84IdOB9Qdjc%2B95RQZS3UY1YK4S2d0Z%2Bls2tTNlN4BTQ9hDIRQzKNOLd%2BiPwg4qYUE51A1cyGhagbnd7mOAzbB79remfgZ4DHqi1nP8Mj0mFDFKPK8VgxVj%2F99ylKV8BrYlZB0YI6JIAy%2FrcwNqQvnV58yS6nbD1y9%2B0hmtSeiZ3zlRUTOzusIIIePjHWuCJJxaab3gyv0ydpY9Mt7hJl87Rsel8H6vI1rgDUHzps3n8C4CjuDqH2qj4j5VaVMIc77uQkwaygoYmvLCXKUHnrvyYRWq41mmLsqtt4V5pZlSpuU77S1S4mFpNsN3aNcJ%2BDQ03I5bT8SHpp54H4hVNTQN4ZqVVY9Uw5lO1SQrZTkYP%2B41usYivAbuLyRdJ4cY5MZ%2BjOtKKgOFmnJ5jLWi3uHDEfRteCWCWXgHJN4NfDPdjsx3eqG6qywQV9w6X1e8QJev3R9XY9yQiS1hmMyjQNhrF7%2FXyGsxpiv3zAFdYtz71bbnHWmKjMCmJfNSNK6NriasAiGYd0560W37V%2Bnu8cJ6bSP5PY7XfMEF9%2BnC9o667RtW8aLZQMY7gMQAsv2h68hf8bU9hcFsI1g3Q5vxzxAnGenJyPThtpEZcykugo6y%2BcGUBBiGGDX00TNmNCaQYYV5VFD6bi8sW%2BEK772nkcWpbl1zDV0L5C7uGr9aoqxSO6ZABTioWF2f3ggsMakh0h3oNwGkiTWjg6S9%2FQYiVm8AKD6LrM8O1GusIH8rg2o%2BHSTuj9QqaGYWMkURCzAv61qZiCE9aGu6F1SE69h2Zd2jHnbHT0iy%2BigvsZlYydYIq1QPW2itfeIb0upEUfJPhD%2Bdhl3EvEiRXZ03q9ZZECMql%2BIfb77Ph3tcZsYMEK%2BfsNGuXr0QwtMGTjNgAtLdHIiCE1zL0PYQ7naVXPz2EjmWt%2FgfB1iJdHylYqfD3rMQGuun4XEgMBWNluivwHTvW23kBHfovlR6sTLG5agPHT6dFa3mB0VMBy12yEjEcmyrk%2F8NkL3C27no5%2BA591V8UW47bIFTzr1BtMn7sBmPwOAowW4RKFs5z4dvKVJhW78%2BrHOIi9F7e8%2BI8kmiTOEsqr58imk3SPmDESmsLnxXMcpl0m0fIp0Bq9BbqZowQPeOXD9h7V2oMc%2BQ6e0YIruDx%2F2a7wl%2BON86UY1KciYCogVfm42zOVO9s0rekZdvHfquTDfd9UboICj1TkT2A5ZQeSewo4z%2FvWdloQopLxs3bFJ7b6t5O0I5izv9fedXFjO55zuN3rjN4njaopTvByNKxlRjfSeGRWCf5IkpBDaycvZC3uCZJJpWTFGcV%2BvmuvDrp%2FX6T4fk88JRRG2bnt46IjJpz4fUIdezykmKDYZ1FElhVDY5J0AaM0USZd3LDtmlEcC1mP1MxqHWjLRYZNvrWyhPsZTnxr2i%2FJ1pqRAyMYHkMGO1zf9%2BDsKMVcJ%2Be%2FTG3R4y%2FnWSwzlSL5DFCDWJJaz1pyQ66ljMuP5qYx6Z2vmSlaf%2B%2Bf56haUmSE3t0S7EqEf7%2FkqqYncrJqGct0h4cX1k%2F7iolVJmjIOeNRZlDFjJZlQCgAxbVx%2BdpeXUZAtVCV7XRf%2F5oE8i7SEJHbEr4JhGtjfu%2FBYjrkrWd3%2BytYcDRvHbL%2B8fM%2BPYOL1kb%2B1aks78T8vsg2tUNYiv5nI4DneCrIh9L2spYYXRpVKJsXoTP6nt6nVILKo3R3Q%2BYckcc3Xp0KSBE14mo3BCKe%2B7wJA5oHN1dYMasvN969BajY8a72ZEgwt9K7XwqVBpcS8E7wsxO26M9fHk%2FlSwd6vBTjCv3tSsgw%2BZA8V%2F8TBZ76T1s7CzZ4EIlRk5yANWWAfMmM4bp2wPxigcA1tKbRxTGl0hsNfp1ktaCQPIx0aa8DQ6ieSl5nr%2Bymo58aUL4v9DpfOFVz78TysWjI1ouhU5Pfm80jTAj65VE9KwPdmGJF5W3OfR7PMehCWaBcUkAXXoj3aIUr8jC48p3aBHQFk1%2FINaTI6en1QAX3q6se2%2Bz4C1ECLD463XXq4aN9zajxswmrTrH7Bi3JXHzNm9gHd8%2BMo2yvFfSPzd%2BFmFRFJTBqoUKdL4%2B7wGjQKOOumjdOEAe75eDozmGJiNpfsyAQnsFSPp8v0wlK5Wb4pIuksDeaMoH5yl%2BY9n1G6QK4QHZrvJ7%2Ftq5EG1ucYeGLk8cxU%2FwxdxCYQ1tukYdPnIpvbF1lG9wIkv7Of0uQMTuFWqjaUW27ovK39DP4SXuuLsGeNd0M2q1Lmo9h0J6OO5gnOPcblctO5ofKyLUZjsRtisoy1QKK7LwKZL5ftnXLkFZIjxMMtWYtszew6MeUANt%2FHON4hboUIlcXfrlsF5OVBsqIGuwEm%2F485Becn1uMAiGIEwRnvJjJszdrmaUMhxt%2BnxtSIYBPeivJUKD%2B%2BHZuyMzHEm932f%2BXilMVZI8bGDOpzNoPFn3rI0BpVXwiftHPapXv%2F74kYh89MzQpedJxvwy7kVFisndTmOclg%2B7oJAfcf7y1JNzE5EFKlP5fiIHZkpaSQV5zsCgoYYQ%3D%3D";
                                            RemoteData.Add(_item.Key, value);
                                        }
                                        else
                                        {
                                            RemoteData.Add(_item.Key, r1.Match(_data).Groups[_item.Index].Value);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ShowLog(string.Format("Method:{0}; Pattern:{1};Message:{2}", item.Method,
                                        _item.Pattern, ex.Message), 3);
                                }
                            }
                            break;
                        case 5:
                            foreach (Models.ERemoteItem _item in item.Items)
                            {
                                if (string.IsNullOrEmpty(_item.Pattern)) continue;
                                try
                                {
                                    Regex r1 = new Regex(_item.Pattern.Trim(), RegexOptions.IgnoreCase);
                                    if (r1.IsMatch(_data))
                                        RemoteData.Add(_item.Key, r1.Matches(_data)[_item.RowIndex].Groups[_item.Index].Value);
                                }
                                catch (Exception ex)
                                {
                                    ShowLog(string.Format("Method:{0}; Pattern:{1};Message:{2}", item.Method,
                                        _item.Pattern, ex.Message), 3);
                                }
                            }
                            break;
                        default:
                            foreach (Models.ERemoteItem _item in item.Items)
                            {
                                if (string.IsNullOrEmpty(_item.Key)) continue;
                                Regex r = new Regex(string.Format(@"<input.*?name=""?{0}""?[^>]*>", _item.Key));
                                foreach (Match m in r.Matches(_data))
                                {
                                    if (m.Success)
                                    {
                                        Regex r2 = new Regex(@"value=(""[^""]*""|'[^']*'|[^""'\s>]+)", RegexOptions.IgnoreCase);
                                        if (r2.IsMatch(m.Groups[0].ToString()))
                                        {
                                            string _val = Regex.Replace(r2.Match(m.Groups[0].ToString()).Groups[1].ToString(), @"^""|""$|^'|'$", "");
                                            RemoteData.Add(_item.Key.ToLower().Trim(), _val);
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug(ex.Message);
                    ErrorLog(ex);
                }
            }
            foreach (string _key in RemoteData.Keys)
            {
                ShowLog(string.Format("成功远程数据：{0}={1}", _key, RemoteData[_key]), 6);
            }
            return RemoteData;
        }

        //全局捕获远程数据
        public static NameValuelist RemoteDatas { get; set; }
        public  void GetRemote(string _data, List<Models.ERemote> remotes)
        {
            if (null == RemoteDatas) RemoteDatas = new NameValuelist();         

            foreach (Models.ERemote item in remotes)
            {
                if (item.global != "true")
                    continue;
                try
                {
                    switch (item.Method)
                    {
                        case 1:
                            {
                                if (string.IsNullOrEmpty(item.Pattern)) break;
                                Regex r1 = new Regex(item.Pattern);
                                int i = 0;
                                foreach (string _v in r1.Split(_data))
                                {
                                    RemoteDatas.Add("remote" + i, _v.Trim());
                                    i++;
                                }
                            }
                            break;
                        case 2:
                            {
                                ShowLog(string.Format("方法：2;正则：{0}", item.Pattern), 5);
                                if (string.IsNullOrEmpty(item.Pattern)) break;
                                Regex r1 = new Regex(item.Pattern);
                                foreach (Match m in r1.Matches(_data))
                                {
                                    if (!m.Success || m.Groups.Count < 3) continue;
                                    string _key = m.Groups[1].Value.ToLower();
                                    string _val = m.Groups[2].Value;
                                    RemoteDatas.Add(_key, _val);
                                }
                            }
                            break;
                        case 3:
                            foreach (Models.ERemoteItem _item in item.Items)
                            {
                                string _val = Cookie(_item.Key);
                                RemoteDatas.Add(_item.Key, _val);
                            }
                            //foreach (string _key in item.Keys)
                            //{
                            //    string _val = Cookie(_key);
                            //    if (!string.IsNullOrEmpty(_val))
                            //    {
                            //        RemoteData.Add(_key.ToLower().Trim(), _val);
                            //    }
                            //}
                            break;
                        case 4:
                            foreach (Models.ERemoteItem _item in item.Items)
                            {
                                if (string.IsNullOrEmpty(_item.Pattern)) continue;
                                try
                                {
                                    Regex r1 = new Regex(_item.Pattern.Trim(), RegexOptions.IgnoreCase);
                                    if (r1.IsMatch(_data))
                                    {

                                        if (Data.WebSite.Business.Addaspx == "true")
                                        {
                                            RemoteDatas.Add(_item.Key, r1.Match(_data).Groups[_item.Index].Value.ToString().Replace("+", "%2b"));
                                        }
                                        else
                                        {
                                            RemoteDatas.Add(_item.Key, r1.Match(_data).Groups[_item.Index].Value);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    ShowLog(string.Format("Method:{0}; Pattern:{1};Message:{2}", item.Method,
                                        _item.Pattern, ex.Message), 3);
                                }
                            }
                            break;
                        case 5:
                            foreach (Models.ERemoteItem _item in item.Items)
                            {
                                if (string.IsNullOrEmpty(_item.Pattern)) continue;
                                try
                                {
                                    Regex r1 = new Regex(_item.Pattern.Trim(), RegexOptions.IgnoreCase);
                                    if (r1.IsMatch(_data))
                                        RemoteDatas.Add(_item.Key, r1.Matches(_data)[_item.RowIndex].Groups[_item.Index].Value);
                                }
                                catch (Exception ex)
                                {
                                    ShowLog(string.Format("Method:{0}; Pattern:{1};Message:{2}", item.Method,
                                        _item.Pattern, ex.Message), 3);
                                }
                            }
                            break;
                        default:
                            foreach (Models.ERemoteItem _item in item.Items)
                            {
                                if (string.IsNullOrEmpty(_item.Key)) continue;
                                Regex r = new Regex(string.Format(@"<input.*?name=""?{0}""?[^>]*>", _item.Key));
                                foreach (Match m in r.Matches(_data))
                                {
                                    if (m.Success)
                                    {
                                        Regex r2 = new Regex(@"value=(""[^""]*""|'[^']*'|[^""'\s>]+)", RegexOptions.IgnoreCase);
                                        if (r2.IsMatch(m.Groups[0].ToString()))
                                        {
                                            string _val = Regex.Replace(r2.Match(m.Groups[0].ToString()).Groups[1].ToString(), @"^""|""$|^'|'$", "");
                                            RemoteDatas.Add(_item.Key.ToLower().Trim(), _val);
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug(ex.Message);
                    ErrorLog(ex);
                }
            }
            foreach (string _key in RemoteDatas.Keys)
            {
                ShowLog(string.Format("成功远程数据：{0}={1}", _key, RemoteData[_key]), 6);
            }
        
        }

       

        #endregion

        //生意宝专属方法
        public string EdDateURL(string url, string pate)
        {
            
            string imgurl = "";
            string dtalis = GetData("http://my.album.toocle.com/?_d=member&_a=album&f=select&pic=pic_name1&photo=photo1&px=140", string.Empty, string.Empty);
               Regex r1 = new Regex(pate+"\" id=\"([\\s\\S].*?)\"", RegexOptions.IgnoreCase);
               if (r1.IsMatch(dtalis))
               {
                   imgurl = r1.Match(dtalis).Groups[1].Value;
               }
            return imgurl;
        }


        public string DeRemote(string data, NameValuelist field)
        {
            //全局捕获远程数据替换
            data = DeRemotes(data);
            if (string.IsNullOrEmpty(data) || null == field) return data;
            Regex r = new Regex(@"\{\$remote\[(\w+)\]\}", RegexOptions.IgnoreCase);
            foreach (Match m in r.Matches(data))
            {
                if (m.Success)
                {
                    string _key = m.Groups[1].ToString().ToLower();
                    string _val = field[_key];
                    if (string.IsNullOrEmpty(_val)) _val = "";
                    data = r.Replace(data, _val, 1);
                    ShowLog(string.Format("转换数据：{0}={1}；", _key, _val), 5);
                }
            }
            return data;
        }

        //全局捕获远程数据替换 格式{$remotes[xx]}  xx是对应的全局捕获的远程数据
        public string DeRemotes(string data)
        {
            if (string.IsNullOrEmpty(data) || null == RemoteDatas) return data;
            Regex r = new Regex(@"\{\$remotes\[(\w+)\]\}", RegexOptions.IgnoreCase);
            foreach (Match m in r.Matches(data))
            {
                if (m.Success)
                {
                    string _key = m.Groups[1].ToString().ToLower();
                    string _val = RemoteDatas[_key];
                    if (string.IsNullOrEmpty(_val)) _val = "";
                    data = r.Replace(data, _val, 1);
                    ShowLog(string.Format("转换数据：{0}={1}；", _key, _val), 5);
                }
            }
            return data;
        }


        public string MatchValue(string data, string key, int type, object value)
        {
            data = EnData(data, key, BLL.WebSite.getValue(this.Data.WebSite, type, value.ToString(), 0));
            Regex r = new Regex(@"{\$" + key + @"\[(\d+)\]}", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match m in r.Matches(data))
            {
                if (m.Success)
                {
                    data = r.Replace(data, BLL.WebSite.getValue(this.Data.WebSite, type, value.ToString(), vConvert.ToInt32(m.Groups[1].Value, 0)), 1);
                }
            }
            return data;
        }
        public string EnData(string data, string key, object value)
        {
            return EnData(data, key, value, Data.WebSite.Encoding);
        }
        public string EnData(string data, string key, object value, Encoding enc)
        {
            string v = null == value ? "" : value.ToString();
            Regex r1 = new Regex(@"\{\$(" + key + @")(\[([\s\S]+)\])?\}", RegexOptions.IgnoreCase);
            foreach (Match m in r1.Matches(data))
            {
                if (!m.Success) continue;
                string _p = m.Groups[3].Value;
                if (RegExp.IsNumeric(_p))
                {
                    v = Text.SubString(v, vConvert.ToInt32(_p, 0));
                }
                data = r1.Replace(data, Html.UrlEncode(v, enc), 1);
                //data = data.Replace(m.Value, Html.UrlEncode(v, Data.WebSite.Encoding));
            }
            return data;
        }
        public string GetField(NameValuelist data, string key)
        {
            string s = string.Empty;
            string tmp = key;
            Regex r = new Regex(@"Class\d+|area\d+", RegexOptions.IgnoreCase);
            foreach (string _key in key.Split('|'))
            {
                //tmp = _key;
                //foreach (Match m in r.Matches(_key))
                //{
                //    if (m.Success && !string.IsNullOrEmpty(data[m.Value]))
                //    {
                //        tmp = tmp.Replace(m.Value, string.IsNullOrEmpty(data[m.Value]) ? "0" : data[m.Value]);
                //    }
                //}
                //if (!string.IsNullOrEmpty(tmp) && tmp != _key)
                //{
                //    s = tmp;
                //    break;
                //}
                if (string.IsNullOrEmpty(_key) ||
                    string.IsNullOrEmpty(data[_key]) ||
                    data[_key] == "0") continue;
                s = data[_key];
                if (!string.IsNullOrEmpty(s))
                    break;
            }
            return s.Trim();
        }
        public System.IO.Stream GetStream(string Url)
        {
     
            Http.Clear();
            Http.Encoding = Data.WebSite.Encoding;
            Http.CookieContainer = Helper.GetCookieContainer(Data.UserID);
            Http.Url = new Uri(Url);
            System.Net.WebResponse response = Http.GetResponse();
            
            if (null == response) return null;
           
            return response.GetResponseStream();
        }
        public string GetData(NameValuelist data, int Coder)
        {
            switch (Coder)
            {
                case 1: return data.ToString(null, true);
                case 2: return data.ToString();
                case 3: return data.ToString(ASCIIEncoding.ASCII);
                case 4: return data.ToString(ASCIIEncoding.UTF8);
                case 5: return data.ToString(ASCIIEncoding.UTF32);
                case 6: return data.ToString(ASCIIEncoding.UTF7);
                case 7: return data.ToString(ASCIIEncoding.GetEncoding("UTF-8"));
                case 8: return data.ToString(ASCIIEncoding.GetEncoding("GB2312"));
                case 9: return data.ToString().Replace("%0A", "\r\n");
                default: return data.ToString(this.Data.WebSite.Encoding);
            }
        }

        /// <summary>
        ///  new上传图片
        /// </summary>
        /// <param name="url"></param>
        /// <param name="file"></param>
        /// <param name="paramName"></param>
        /// <param name="contentType"></param>
        /// <param name="nvc"></param>
        /// <param name="cook"></param>
        /// <param name="Referers"></param>
        /// <returns></returns>
        public  string UpLoad(string url, string file, string paramName, string contentType, string  data, string Referers)
        {
            string result = string.Empty;
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.KeepAlive = true;
            wr.Credentials = System.Net.CredentialCache.DefaultCredentials;
            wr.CookieContainer = Helper.GetCookieContainer(Data.UserID);    
            wr.Referer = Referers;
            Stream rs = wr.GetRequestStream();

            //分解post数据
            if (data != "")
            {
                string[] arrydata = data.Split('&');
                string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
                foreach (string key in arrydata)
                {
                    rs.Write(boundarybytes, 0, boundarybytes.Length);
                    string formitem = string.Format(formdataTemplate, key.Split('=')[0].ToString(), key.Split('=')[1].ToString());
                    byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                    rs.Write(formitembytes, 0, formitembytes.Length);
                }
            }
            rs.Write(boundarybytes, 0, boundarybytes.Length);

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, paramName, file, IO.GetContentType(file));
            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            rs.Write(headerbytes, 0, headerbytes.Length);

            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[4096];
            int bytesRead = 0;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                rs.Write(buffer, 0, bytesRead);
            }
            fileStream.Close();

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();

            WebResponse wresp = null;

            try
            {
                wresp = wr.GetResponse();
                Stream stream2 = wresp.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
                result = reader2.ReadToEnd();
            }
            catch (Exception ex)
            {
                result = string.Empty;
            }
            finally
            {
                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
                wr = null;
            }
            return result;
        }

        public string GetData(string url, string data, string referer, params NameValue[] files)
        {
            Http.Clear();
            Http.Referer = referer;
            Http.PostData = data; 
            Http.AutoEncoding = false;
            Http.Encoding =Data.WebSite.Encoding;

            if (Data.WebSite.ID == 22)
            {
                List<string> hdList = new List<string>();
                //新品快播特殊处理
                hdList.Add("Access-Control-Allow-Origin=http://my.npicp.com");
                hdList.Add("X-Requested-With=XMLHttpRequest");
                Http.Headers = hdList;
            }
            //Http.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.65 Safari/537.36";
          
            Http.CookieContainer = Helper.GetCookieContainer(Data.UserID);
            if (null != Data.WebSite.Business.headers&&Data.WebSite.Business.headers_url==url) {
                List<String> list = new List<string>();
                for (int i = 0; i < Data.WebSite.Business.headers.Split(',').Length; i++)
                {
                    list.Add(Data.WebSite.Business.headers.Split(',')[i]);
                }
               Http.Headers = list;
            }
            if (Data.WebSite.ID == 40 && url.IndexOf("PicUpload") > -1)
            {
                Http.ContentType = "Content-Type: multipart/form-data; boundary=----WebKitFormBoundaryLqBMMvMwMCujQ9um";
            }     
            if (Data.WebSite.ID == 120 && url.IndexOf("FileProductPhoto") > -1)
            { 
                Http.ContentType = "Content-Type: multipart/form-data; boundary=----WebKitFormBoundaryJAN9xYnHXa2gWsAs";
            }
            if (Data.WebSite.ID == 13)
            {
                Http.ContentType = "Content-Type: multipart/form-data; boundary=------WebKitFormBoundary1nqbFfeVeUdJ4jmJ";
            }
            if (Data.WebSite.ID == 124)
                Http.ContentType = "content-type: multipart/form-data; boundary=---------------------------7e1f324120960";
            if (Data.WebSite.ID == 158){
                if (Http.CookieContainer.Count <= 0)
                {
                    //钱前网的cookie需要保存
                    CookieContainer cc = new CookieContainer();
                    byte[] byteArray = Encoding.UTF8.GetBytes(data); //转化
                    HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(url);
                    webReq.Method = "POST";
                    webReq.ContentType = "application/x-www-form-urlencoded";
                    webReq.CookieContainer = cc;
                    webReq.ContentLength = byteArray.Length;
                    webReq.KeepAlive = true;
                    webReq.UserAgent = "User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/47.0.2526.80 Safari/537.36 QQBrowser/9.3.6872.400";
                    webReq.Accept = "Accept: image/jpeg, image/gif, image/pjpeg, application/x-ms-application, application/xaml+xml, application/x-ms-xbap, application/x-shockwave-flash, */*";
                    webReq.ServicePoint.Expect100Continue = false;
                    using (Stream newStream = webReq.GetRequestStream())
                    {
                        newStream.Write(byteArray, 0, byteArray.Length);//写入参数
                    }
                    HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();
                    //Http.CookieContainer.Add(cc.GetCookies(new Uri(url))["users0726%5Fext200"]);
                    //Http.CookieContainer.Add(cc.GetCookies(new Uri(url))["s641sohuuser07263"]);
                    Hashtable table = (Hashtable)cc.GetType().InvokeMember("m_domainTable",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField |
                    System.Reflection.BindingFlags.Instance, null, cc, new object[] { });
                    foreach (object pathList in table.Values)
                    {
                        SortedList lstCookieCol = (SortedList)pathList.GetType().InvokeMember("m_list",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField
                            | System.Reflection.BindingFlags.Instance, null, pathList, new object[] { });
                        foreach (CookieCollection colCookies in lstCookieCol.Values)
                            foreach (System.Net.Cookie c in colCookies) { 
                                if (c.Name == "users0726%5Fext200")
                                    Http.CookieContainer.Add(new System.Net.Cookie(c.Name, c.Value, c.Path, c.Domain));
                                if (c.Name == "s641sohuuser07263")
                                    Http.CookieContainer.Add(new System.Net.Cookie(c.Name, c.Value, c.Path, c.Domain));
                            }
                    }
                }
            }



            #region 追加Cookie 2013-07-23 wanghui
            NameValuelist nvCookie = null;
            if (this.Cookies != null && this.Cookies.Length > 0)
            {
                NameValuelist nvTemp = new NameValuelist();
                nvCookie = nvTemp.ParseQueryString(this.Cookies, Data.WebSite.Encoding);            
            }
            #endregion

            if (null != files)
            {
                foreach (NameValue file in files)
                {
                    Http.AddFile(file.Name, file.Value.ToString());
                }
            }
            if (null != Data.WebSite.Expect100Continue)
            {
                Http.Expect100Continue = Data.WebSite.Expect100Continue.Value;
            }
            return Http.GetString(url, nvCookie);
        }
        public string GetData(string url, string data, string referer)
        {
            return GetData(url, data, referer, null);
        }
        public string GetData(string url, string data)
        {
            return GetData(url, data, string.Empty);
        }

       
        #region 2016/8/12 dengfuping 获取请求返回的url
        public string GetResponeUrl( string url,string data)
        {
            Http.Clear(); 
            Http.PostData = data;
            Http.AutoEncoding = false;
            Http.Encoding = Data.WebSite.Encoding;
            //Http.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.65 Safari/537.36";
            Http.CookieContainer = Helper.GetCookieContainer(Data.UserID);
            if (Data.WebSite.ID == 40 && url.IndexOf("PicUpload") > -1)
            {
                Http.ContentType = "Content-Type: multipart/form-data; boundary=----WebKitFormBoundaryLqBMMvMwMCujQ9um";
            }
            if (Data.WebSite.ID == 120 && url.IndexOf("FileProductPhoto") > -1)
            {
                Http.ContentType = "Content-Type: multipart/form-data; boundary=----WebKitFormBoundaryJAN9xYnHXa2gWsAs";
            }
            return Http.GetResponeUrl(url);
        }
        #endregion
        public Regex getRegExp(string p)
        {
            if (string.IsNullOrEmpty(p)) return null;
            return new Regex(p, RegexOptions.IgnoreCase);
        }
        public void ShowVerifyCode(ResumeHandler resume)
        {
            if (string.IsNullOrEmpty(VerifyUrl))
            {
                SendMessage(0, "验证码程序配置错误");
                return;
            }
            VerifyUrl = EnData(VerifyUrl);
            VerifyUrl = DeRemote(VerifyUrl, RemoteData);
            ShowLog("输入验证码：" + VerifyUrl);
            if (null != OnEntryVerifyCode)
            {
                try
                {
                    #region//易展网验证获取增加cookies  zhengyue 2015-6-18 16:36
                    if (Data.WebSite.ID == 123)
                    {
                        string url = "http://sso.yi-z.com/";
                        string cookiess = "";
                        string newguid = "";
                        System.Net.HttpWebRequest webrequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(url);
                        HttpWebResponse webresponse = (HttpWebResponse)webrequest.GetResponse();//请求连接,并反回数据
                        Stream stream = webresponse.GetResponseStream();//把返回数据转换成流文件

                        byte[] rsByte = new Byte[webresponse.ContentLength];  //把流文件转换为字节数组
                        stream.Read(rsByte, 0, (int)webresponse.ContentLength);
                        string guid = System.Text.Encoding.Default.GetString(rsByte, 0, rsByte.Length).ToString();
                        string cookiesstr = webresponse.Headers.ToString();
                        Regex r1 = new Regex("Set-Cookie:([\\s\\S]+?);", RegexOptions.IgnoreCase);
                        if (r1.IsMatch(cookiesstr))
                            cookiess = r1.Match(cookiesstr).Groups[1].Value;
                        Data.WebSite.Login.Cookies = cookiess.Trim();
                        Regex r2 = new Regex("CaptchaImageGuid='([\\s\\S]+?)'", RegexOptions.IgnoreCase);
                        if (r2.IsMatch(guid))
                            newguid = r2.Match(guid).Groups[1].Value;
                        VerifyUrl = VerifyUrl.Replace("newguid", newguid);
                        NameValuelist nvCookie = null;
                        if (cookiess != "" && cookiess.Length > 0)
                        {
                            NameValuelist nvTemp = new NameValuelist();
                            nvCookie = nvTemp.ParseQueryString(cookiess.Trim(), Data.WebSite.Encoding);

                        }
                        string cook = Http.GetString(VerifyUrl, nvCookie);
                    }
                    #endregion

                    if (Data.WebSite.Business.CodeTxt == "true")
                    {
                        #region 环球贸易 文字验证码转图片
                        if (Data.WebSite.ID == 15)
                        {
                            string temp = DateTime.Now.Ticks.ToString() + ".js"; 
                            string text = Http.GetString(VerifyUrl.Replace("(#)",temp));
                            string pattern1 = "innerHTML([\\s\\S]+?);";

                            Regex re1 = new Regex(pattern1, RegexOptions.IgnoreCase);
                            if (re1.IsMatch(pattern1))
                            {
                                text = re1.Match(text).Groups[1].Value;
                            }
                            System.Drawing.Bitmap Image = new System.Drawing.Bitmap(200, 30);
                            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(Image);
                            System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(100, 100, 100));
                            drawBrush.Color = System.Drawing.Color.FromArgb(26, 22, 156);
                            g.DrawString(text, new System.Drawing.Font("宋体", 10), drawBrush, new System.Drawing.PointF(0, Image.Height / 2 + 3));
                            g.Dispose();
                            
                            OnEntryVerifyCode(this, Image, resume);
                            Image.Dispose();
                        }
                        #endregion
                    }else
                    {
                        using (System.IO.Stream stream = GetStream(VerifyUrl))
                        {
                            OnEntryVerifyCode(this, new System.Drawing.Bitmap(stream), resume);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowLog(ex.Message, 2);
                    SendMessage(500, ex.Message);
                }
            }
        }

        //拖图验证码
        public bool SelVerifyID(string Url,CookieContainer cookie, ResumeHandler resume)
        {
            if (null != OnSelVerifyID)
            {
                OnSelVerifyID(this, Url,cookie, resume);
                return true;
            }
            else return false;
        }

        public bool SelClassID(string Url, ResumeHandler resume)
        {
            if (null != OnSelClassID)
            {
                OnSelClassID(this, Url, resume);
                return true;
            }
            else return false;
        }
        public bool CheckField(string key, NameValuelist data)
        {
            bool ret = true;
            foreach (string s in key.Split(','))
            {
                string[] s1 = s.Split('=');
                if (s1.Length != 2) continue;
                if (string.IsNullOrEmpty(GetField(data, s1[0])))
                {
                    ret = false;
                    break;
                }
            }
            return ret;
        }
        public bool CheckField(string key)
        {
            return CheckField(key, PostData);
        }
        public string Cookie(string name)
        {
            return Http.Cookies[name].Value;
        }
        public string AnalyzeData1(string data)
        {
            return AnalyzeData(data, string.Empty);
        }
        public string AnalyzeData(string data, string PublishData)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;
            Regex r = new Regex(@"{\$MyField\[([\s\S]*?)\]}", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match m in r.Matches(data))
            {
                if (m.Success && !string.IsNullOrEmpty(m.Groups[1].ToString()))
                {
                    string v = Html.UrlEncode(BLL.Product.GetFields(PublishData, Data.WebSite.ID, m.Groups[1].ToString()), Data.WebSite.Encoding);
                    if (string.IsNullOrEmpty(v)) v = string.Empty;
                    data = r.Replace(data, v, 1);
                    //data = data.Replace(m.Groups[0].ToString(), Html.UrlEncode(v, Data.WebSite.Encoding));
                }
            }
            r = new Regex(@"{\$CField\[([\s\S]*?)\]}", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match m in r.Matches(data))
            {
                if (m.Success && !string.IsNullOrEmpty(m.Groups[1].ToString()))
                {
                    string v = BLL.Company.GetMyFields(Data.Company, Data.WebSite.ID, m.Groups[1].ToString());
                    if (string.IsNullOrEmpty(v)) v = string.Empty;
                    data = r.Replace(data, v, 1);
                }
            }
            r = new Regex(@"{\$Cookie\[([\s\S]*?)\]}", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match m in r.Matches(data))
            {
                if (m.Success && !string.IsNullOrEmpty(m.Groups[1].ToString()))
                {
                    string v = Cookie(m.Groups[1].Value);
                    data = r.Replace(data, Html.UrlEncode(v, Data.WebSite.Encoding), 1);
                    //data = data.Replace(m.Groups[0].ToString(), Html.UrlEncode(v, Data.WebSite.Encoding));
                }
            }
            return data;
        }
        public Utility.Models.EMessage MatchReplace(List<Models.EReplace> list, string s)
        {
            Utility.Models.EMessage msg = new Utility.Models.EMessage();
            msg.Data = s;
            if (string.IsNullOrEmpty(s)) return msg;
            bool isMatch = true;

            foreach (Models.EReplace item in list)
            {
                if (string.IsNullOrEmpty(item.Pattern)) continue;
                Regex r = new Regex(item.Pattern, RegexOptions.IgnoreCase);
                if (!r.IsMatch(msg.Data))
                    isMatch = false;
                else
                {
                    switch (item.Mode)
                    {
                        case 1:
                            msg.Data = r.Match(msg.Data).Value; break;
                        case 2:
                            msg.Data = r.Match(msg.Data).Groups[item.Index].Value; break;
                        default: msg.Data = r.Replace(msg.Data, item.Evaluator); break;
                    }
                }
                //if (item.Mode == 1)
                //{
                //    if (r.IsMatch(msg.Data))
                //        msg.Data = r.Match(msg.Data).Value;
                //}
                //else
                //    msg.Data = r.Replace(msg.Data, item.Evaluator);
            }
            msg.Code = isMatch ? 200 : 500;
            return msg;
        }

        public NameValuelist UpmatchData { get; set; }
        //上传图片返回数据解析
        public NameValuelist Upmatch(List<Models.EReplace> list, string s)
        {
            if (null == UpmatchData) UpmatchData = new NameValuelist();

            #region
            foreach (Models.EReplace item in list)
            {


                try
                {
                    switch (item.Mode)
                    {

                        case 4:

                            if (string.IsNullOrEmpty(item.Pattern)) continue;
                            try
                            {
                                Regex r1 = new Regex(item.Pattern.Trim(), RegexOptions.IgnoreCase);
                                if (r1.IsMatch(s))
                                {

                                    if (Data.WebSite.Business.Addaspx == "true")
                                    {
                                        UpmatchData.Add(item.Key, r1.Match(s).Groups[item.Index].Value.ToString().Replace("+", "%2b"));
                                    }
                                    else
                                    {
                                        UpmatchData.Add(item.Key, r1.Match(s).Groups[item.Index].Value);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                ShowLog(string.Format("Method:{0}; Pattern:{1};Message:{2}", item.Mode,
                                    item.Pattern, ex.Message), 3);
                            }

                            break;
                        case 5:

                            if (string.IsNullOrEmpty(item.Pattern)) continue;
                            try
                            {
                                Regex r1 = new Regex(item.Pattern.Trim(), RegexOptions.IgnoreCase);
                                if (r1.IsMatch(s))
                                    UpmatchData.Add(item.Key, r1.Matches(s)[item.RowIndex].Groups[item.Index].Value);
                            }
                            catch (Exception ex)
                            {
                                ShowLog(string.Format("Method:{0}; Pattern:{1};Message:{2}", item.Mode,
                                    item.Pattern, ex.Message), 3);
                            }

                            break;
                        default:

                            if (string.IsNullOrEmpty(item.Key)) continue;
                            Regex r = new Regex(string.Format(@"<input.*?name=""?{0}""?[^>]*>", item.Key));
                            foreach (Match m in r.Matches(s))
                            {
                                if (m.Success)
                                {
                                    Regex r2 = new Regex(@"value=(""[^""]*""|'[^']*'|[^""'\s>]+)", RegexOptions.IgnoreCase);
                                    if (r2.IsMatch(m.Groups[0].ToString()))
                                    {
                                        string _val = Regex.Replace(r2.Match(m.Groups[0].ToString()).Groups[1].ToString(), @"^""|""$|^'|'$", "");
                                        UpmatchData.Add(item.Key.ToLower().Trim(), _val);
                                    }
                                }
                            }

                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug(ex.Message);
                    ErrorLog(ex);
                }
            #endregion

              
            }
            return UpmatchData;
        }

        public void Debug(string s)
        {
            if (isDebug)
                SendMessage(1, s);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="Level">
        /// 0=基本信息
        /// 1=提交信息
        /// 2=返回信息
        /// 3=本地错误
        /// 4=远程错误      
        /// 5=提交调试数据
        /// 6=返回调试数据
        /// </param>
        public void ShowLog(string Message, int Level)
        {
            if (Authority2.ValidateRight(Helper.Config.LogLevel,
                Authority2.MakeRightValue(Level.ToString())))
            {
                Log.Write(Message);
                Utility.Models.EMessage msg = new Utility.Models.EMessage();
                msg.Level = Level;
                msg.Message = Message;
                msg.Code = -1;
                SendMessage(msg);
            }
        }
        public void ShowLog(string Message)
        {
            ShowLog(Message, 0);
        }
        public void ErrorLog(Exception ex)
        {
            Helper.Error(ex);
        }



        /// <summary>
        /// 遍历CookieContainer
        /// </summary>
        /// <param name="cc"></param>
        /// <returns></returns>
        public bool GetAllCookies()
        {
            bool bol = false;
            CookieContainer cc = Helper.GetCookieContainer(Data.UserID);
            List<System.Net.Cookie> lstCookies = new List<System.Net.Cookie>();

            Hashtable table = (Hashtable)cc.GetType().InvokeMember("m_domainTable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField |
                System.Reflection.BindingFlags.Instance, null, cc, new object[] { });

            foreach (object pathList in table.Values)
            {
                SortedList lstCookieCol = (SortedList)pathList.GetType().InvokeMember("m_list",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField
                    | System.Reflection.BindingFlags.Instance, null, pathList, new object[] { });
                foreach (CookieCollection colCookies in lstCookieCol.Values)
                    foreach (System.Net.Cookie c in colCookies) lstCookies.Add(c);
            }
            for (int i = 0; i < lstCookies.Count; i++)
            {
                if (lstCookies[i].Name == Data.WebSite.Login.Checkname)
                {
                    bol = true;
                    break;
                }
            }
            return bol;
        }

        /// <summary>
        /// 模拟发布网址替换敏感关键词 
        /// </summary>
        /// <param name="details">详细内容</param>
        /// <returns></returns>
        public string ChangleDetail(string details)
        {
            try
            { 
                string url = Data.WebSite.Business.sensite;
                string urlpattern = Data.WebSite.Business.sensitepattern;
                string param = Data.WebSite.Business.sensiteparam.Replace("{$Details}", details);
                param = param.Replace("&nbsp;", "\r");
                param = param.Replace("&", "\r");
                string details1 = GetData(url, param, url);
                Regex re = new Regex(urlpattern, RegexOptions.IgnoreCase);
                if (re.IsMatch(details1))
                {
                    details = re.Match(details1).Groups[1].Value;
                }
                if (Data.WebSite.ID == 125)
                {
                    if (details.Contains("--------------------------------------------------------------------"))
                    {
                        details = details.Substring(details.IndexOf("--------------------------------------------------------------------"));
                        SendMessage(0, "文章内容中有关键请到http://fabu.zhaoshang100.com/user/ 进行验证并去掉,否则将去掉所有格式及符号");
                    }
                } 
            }
            catch (Exception ex)
            { }
           
            return details;
        }


        public void AddCookie( string url)
        {
            if (Data.WebSite.ID == 7)
            {  
                System.Net.HttpWebRequest webrequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(url);
                HttpWebResponse webresponse = (HttpWebResponse)webrequest.GetResponse();//请求连接,并反回数据
                Stream stream = webresponse.GetResponseStream();//把返回数据转换成流文件

                byte[] rsByte = new Byte[webresponse.ContentLength];  //把流文件转换为字节数组
                stream.Read(rsByte, 0, (int)webresponse.ContentLength);
                string guid = System.Text.Encoding.Default.GetString(rsByte, 0, rsByte.Length).ToString();
                string[] cookiesstr = webresponse.Headers["Set-Cookie"].Split(';');

                for (int i = 0; i < cookiesstr.Length; i++)
                { 
                    string[] str = cookiesstr[i].Split('=');
                    if (str.Length == 2)
                    {
                        Http.CookieContainer.Add(new Uri(url), new System.Net.Cookie(str[0].Trim(), str[1].Trim()));
                    }
                } 
            } 
        }
    }
}