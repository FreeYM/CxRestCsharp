public void Test()
{
	//获取token
                string url = uri + "cxrestapi/auth/identity/connect/token";
                string postdata = "username=" + cxUserName + "&password=" + cxPassword + "&grant_type=password&scope=sast_rest_api&client_id=resource_owner_client&client_secret=014DF517-39D1-4453-B7B3-9930C563627C";
                string token = PostResponse(url, postdata, "", "");

                //创建分支
                url = uri + "cxrestapi/projects/11235/branch";
                postdata = "name="+companyName+ DateTime.Now.ToString("yyyyMMddHHmmss");
                string str = PostResponse(url, postdata, "branch", GetJsonValueByKey(token, "access_token"));
                Thread.Sleep(5000);//线程休眠1秒

                //上传本地源代码 zip 压缩包
                url = uri + "cxrestapi/projects/"+ GetJsonValueByKey(str, "id") + "/sourceCode/attachments";
                var formDatas = new List<FormItemModel>();
                //添加文件
                formDatas.Add(new FormItemModel()
                {
                    Key = "zippedSource",
                    Value = "",
                    FileName = "upload.zip",
                    FileContent = File.OpenRead(sFileName)
                });
                //提交表单
                string result = PostForm(url, GetJsonValueByKey(token, "access_token"), formDatas);
                //string result = PostResponse(url, postdata, "attachments", GetJsonValueByKey(token, "access_token"));

                //创建新的扫描
                url = uri + "/cxrestapi/sast/scans";
                postdata = "projectId="+ GetJsonValueByKey(str, "id") + "&isIncremental=false&isPublic=true&forceScan=true";
                string rstr= PostResponse(url, postdata, "scans", GetJsonValueByKey(token, "access_token"));
                System.IO.FileInfo fileInfo = fileInfo = new System.IO.FileInfo(sFileName);
                string scanId = GetJsonValueByKey(rstr, "id");
                if (scanId!="")
                {
                    try
                    {
                        string sSuccess = TestBussi.md5RelTest(Request.QueryString["tId"], Request.QueryString["cId"], sCode, "", "", scanId, "11235", System.Math.Ceiling(fileInfo.Length / 10240.0) + "KB");
                        if (sSuccess == "1")
                        {
                            Response.Write("<script Language='JavaScript'>alert('上传并成功创建扫描任务，扫描完成后系统会自动发送短信，近期请留意手机短信！'); top.window.getSafeList();top.Dialog.close()</script>");
                            File.Delete(sFileName);//上传成功后删除源文件
                        }
                    }
                    catch (Exception ex)
                    {
                        LogUtil.logDebug("CheckMarx保存异常:" + ex.Message);
                    }
                   
                }
                else
                {
                    Response.Write("<script Language='JavaScript'>alert('上传失败,请稍后再试！')</script>");
                }
}



/// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="fileNamePath"></param>
    /// <returns></returns>
    public ArrayList UpLoadFile(string fileNamePath)
    {
        string relativePath = "";
        string sResult = "";
        ArrayList arr = new ArrayList();
        try
        {
            string serverPath = System.Web.HttpContext.Current.Server.MapPath("~");
            string toFilePath = serverPath + "/ServicePlatform/test/sourceCode/";

            //获取要保存的文件信息
            System.IO.FileInfo file = new System.IO.FileInfo(fileNamePath);
            //获得文件扩展名
            string fileNameExt = file.Extension;

            //验证合法的文件
            if (CheckFileExt(fileNameExt))
            {
                //生成将要保存的随机文件名
                Random Rdm = new Random();
                string iRdm = Rdm.Next(0, 1000000).ToString();
                //MD5码
                sResult = FormsAuthentication.HashPasswordForStoringInConfigFile(iRdm, "md5");
                string fileName = sResult + fileNameExt;
                //获得要保存的文件路径
                string serverFileName = toFilePath + fileName;
                //物理完整路径                    
                string toFileFullPath = serverFileName; //HttpContext.Current.Server.MapPath(toFilePath);

                //将要保存的完整文件名                
                string toFile = toFileFullPath;//+ fileName;
                ///创建WebClient实例       
                System.Net.WebClient myWebClient = new System.Net.WebClient();
                //设定windows网络安全认证   方法1
                myWebClient.Credentials = System.Net.CredentialCache.DefaultCredentials;
                ////设定windows网络安全认证   方法2
                Request.Files[0].SaveAs(toFile);
                relativePath = serverPath + string.Format("/ServicePlatform/test/sourceCode/{0}", fileName);
            }
            else
            {
                Response.Write("<script Language='JavaScript'>alert('文件格式非法，只能上传zip格式的压缩文件！')</script>");
            }
        }
        catch (Exception ex)
        {

        }
        Hashtable hs = new Hashtable();
        hs.Add("sPath", relativePath);
        hs.Add("sCode", sResult);
        arr.Add(hs);
        return arr;
    }

    /// <summary>
    /// 检查是否为合法的上传文件
    /// </summary>
    /// <param name="_fileExt"></param>
    /// <returns></returns>
    private bool CheckFileExt(string fileType)
    {
        string[] allowExt = new string[] { ".zip" };
        for (int i = 0; i < allowExt.Length; i++)
        {
            if (allowExt[i] == fileType) { return true; }
        }
        return false;

    }

    /// <summary>
    /// post请求，并获取返回值
    /// </summary>
    /// <param name="url"></param>
    /// <param name="postData"></param>
    /// <param name="type"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private string PostResponse(string url, string postData, string type, string token)
    {
        string result = "";
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "Post";
        request.ContentType = "application/x-www-form-urlencoded";
        if (type == "branch" || type == "scans" || type == "sastScan")
        {
            request.Headers.Add("Authorization", "Bearer " + token);
        }
        else if (type == "attachments")
        {
            request.ContentType = "multipart/form-data";
            request.Headers.Add("Authorization", "Bearer " + token);
            request.Headers.Add("", "");
        }
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(postData);
            Stream newStream = request.GetRequestStream();
            newStream.Write(data, 0, data.Length);
            newStream.Close();
            HttpWebResponse respone = request.GetResponse() as HttpWebResponse;

            Stream netStream = respone.GetResponseStream();
            StreamReader respStreamReader = new StreamReader(netStream, Encoding.GetEncoding("utf-8"));
            result = respStreamReader.ReadToEnd();
        }
        catch (Exception ex)
        {

        }
        return result;
    }

    /// <summary>
    /// 根据json键获取对应的值
    /// </summary>
    /// <param name="jsonstr"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    private string GetJsonValueByKey(string jsonstr, string key)
    {
        try
        {
            JObject jo = (JObject)JsonConvert.DeserializeObject(jsonstr);
            return jo[key].ToString();
        }
        catch (Exception ex)
        {
            return "";
        }
    }

    /// <summary>
    /// 使用Post方法Foma-data格式化上传文件
    /// </summary>
    /// <param name="url"></param>
    /// <param name="formItems">Post表单内容</param>
    /// <param name="cookieContainer"></param>
    /// <param name="timeOut">默认20秒</param>
    /// <param name="encoding">响应内容的编码类型（默认utf-8）</param>
    /// <returns></returns>
    public static string PostForm(string url, string token, List<FormItemModel> formItems, CookieContainer cookieContainer = null, string refererUrl = null, Encoding encoding = null, int timeOut = 20000)
    {
        try
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            #region 初始化请求对象
            request.Method = "POST";
            request.Headers.Add("Authorization", "Bearer " + token);
            request.Timeout = timeOut;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.KeepAlive = true;
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.57 Safari/537.36";
            if (!string.IsNullOrEmpty(refererUrl))
                request.Referer = refererUrl;
            if (cookieContainer != null)
                request.CookieContainer = cookieContainer;
            #endregion

            string boundary = "----" + DateTime.Now.Ticks.ToString("x");//分隔符
            request.ContentType = string.Format("multipart/form-data; boundary={0}", boundary);
            //请求流
            var postStream = new MemoryStream();
            #region 处理Form表单请求内容
            //是否用Form上传文件
            var formUploadFile = formItems != null && formItems.Count > 0;
            if (formUploadFile)
            {
                //文件数据模板
                string fileFormdataTemplate =
                    "\r\n--" + boundary +
                    "\r\nContent-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"" +
                    "\r\nContent-Type: application/octet-stream" +
                    "\r\n\r\n";
                //文本数据模板
                string dataFormdataTemplate =
                    "\r\n--" + boundary +
                    "\r\nContent-Disposition: form-data; name=\"{0}\"" +
                    "\r\n\r\n{1}";
                foreach (var item in formItems)
                {
                    string formdata = null;
                    if (item.IsFile)
                    {
                        //上传文件
                        formdata = string.Format(
                            fileFormdataTemplate,
                            item.Key, //表单键
                            item.FileName);
                    }
                    else
                    {
                        //上传文本
                        formdata = string.Format(
                            dataFormdataTemplate,
                            item.Key,
                            item.Value);
                    }

                    //统一处理
                    byte[] formdataBytes = null;
                    //第一行不需要换行
                    if (postStream.Length == 0)
                        formdataBytes = Encoding.UTF8.GetBytes(formdata.Substring(2, formdata.Length - 2));
                    else
                        formdataBytes = Encoding.UTF8.GetBytes(formdata);
                    postStream.Write(formdataBytes, 0, formdataBytes.Length);

                    //写入文件内容
                    if (item.FileContent != null && item.FileContent.Length > 0)
                    {
                        using (var stream = item.FileContent)
                        {
                            byte[] buffer = new byte[1024];
                            int bytesRead = 0;
                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                            {
                                postStream.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                }
                //结尾
                var footer = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
                postStream.Write(footer, 0, footer.Length);

            }
            else
            {
                request.ContentType = "application/x-www-form-urlencoded";
            }
            #endregion

            request.ContentLength = postStream.Length;

            #region 输入二进制流
            if (postStream != null)
            {
                postStream.Position = 0;
                //直接写入流
                Stream requestStream = request.GetRequestStream();

                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                while ((bytesRead = postStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    requestStream.Write(buffer, 0, bytesRead);
                }
                postStream.Close();//关闭文件访问
            }
            #endregion

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (cookieContainer != null)
            {
                response.Cookies = cookieContainer.GetCookies(response.ResponseUri);
            }

            using (Stream responseStream = response.GetResponseStream())
            {
                using (StreamReader myStreamReader = new StreamReader(responseStream, encoding ?? Encoding.UTF8))
                {
                    string retString = myStreamReader.ReadToEnd();
                    return "1";
                }
            }
        }
        catch (Exception ce)
        {
            return "0";
        }
    }
}

/// <summary>
/// 表单数据项
/// </summary>
public class FormItemModel
{
    /// <summary>
    /// 表单键，request["key"]
    /// </summary>
    public string Key { set; get; }
    /// <summary>
    /// 表单值,上传文件时忽略，request["key"].value
    /// </summary>
    public string Value { set; get; }
    /// <summary>
    /// 是否是文件
    /// </summary>
    public bool IsFile
    {
        get
        {
            if (FileContent == null || FileContent.Length == 0)
                return false;

            if (FileContent != null && FileContent.Length > 0 && string.IsNullOrWhiteSpace(FileName))
                throw new Exception("上传文件时 FileName 属性值不能为空");
            return true;
        }
    }
    /// <summary>
    /// 上传的文件名
    /// </summary>
    public string FileName { set; get; }
    /// <summary>
    /// 上传的文件内容
    /// </summary>
    public Stream FileContent { set; get; }
}