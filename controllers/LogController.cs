using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using Aliyun.Api.LOG;
using Aliyun.Api.LOG.Request;
using Aliyun.Api.LOG.Response;
using Newtonsoft.Json;

namespace BrnMall.Web.controllers
{
    public class LogController : Controller
    {
        // GET: Log
        
        public JsonResult GetLogs(uint from,uint to,int lines,int offset)
        {
            if (lines < 1) lines = 1;
            if (lines > 100) lines = 100;
            if (offset < 0) offset = 0;
            LogClient client = new LogClient("cn-hangzhou.log.aliyuncs.com", "LTAIk45GEzpqUxg6", "jcLHPKpmTSU3yjh5quANAHMbqaW3xt");
            GetLogsRequest getLogReq = new GetLogsRequest("weapp-api",
                "cvs_log",
                from,
                to,
                "",
                "",
                lines,
                offset,
                false);
            GetLogsResponse getLogResp = client.GetLogs(getLogReq);
            IList<string> msgs= (from queriedLog in getLogResp.Logs from content in queriedLog.Contents where content.Key.Equals("message", StringComparison.InvariantCultureIgnoreCase) select content.Value).ToList();
            
            return Json(getLogResp.Count > 0 ? JsonConvert.SerializeObject(msgs) : "",JsonRequestBehavior.AllowGet); 
        }
    }
}