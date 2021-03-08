using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace ConsoleCxAPI
{
    class Program
    {
        static void Main(string[] args)
        {
            Program a = new Program();
            a.Test();
        }

        public void Test()
        {
            string uri = "http://localhost/";
            //zip包上传的本地路径
            string filePath = @"C:\Users\Kai\Desktop\CxAPICode\ConsoleCxAPI.zip";

            //获取tokne
            string access_token = GetAccessToken(uri);
            //Console.WriteLine(access_token);

            //创建分支项目
            string proid = ProjectBranch(uri,access_token);
            Thread.Sleep(20000);

            //上传zip源代码包
            string str = UploadZip(uri, proid, access_token, filePath);

            //创建扫描
            string scanId = postScans(uri,proid,access_token);
        }

        //获取assecc_token
        public string GetAccessToken(string uri)
        {
            //参数拼接
            string url = uri + "cxrestapi/auth/identity/connect/token";
            string param = "username=admin&password=Password01.&grant_type=password&scope=sast_rest_api&client_id=resource_owner_client&client_secret=014DF517-39D1-4453-B7B3-9930C563627C";

            //参数转化为ascii码
            Encoding encoding = Encoding.UTF8;
            byte[] bs = encoding.GetBytes(param);
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);  //创建request
            req.Method = "POST";    //确定传值的方式，此处为post方式传值
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = bs.Length;

            Stream reqStream = req.GetRequestStream();
            reqStream.Write(bs, 0, bs.Length);

            WebResponse wr = req.GetResponse();
            Stream myResponseStream = wr.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, encoding);
            string retString = myStreamReader.ReadToEnd();
            //myStreamReader.Close();
            //myResponseStream.Close();
            Console.WriteLine(retString);


            //字符串转json格式，获取accesstoken数据
            JObject token = (JObject)JsonConvert.DeserializeObject(retString);//或者JObject jo = JObject.Parse(jsonText);
            string token_access = token["access_token"].ToString();
            //Console.WriteLine(token);
            //Console.WriteLine(token_access);
            return "Bearer " + token_access;
        }

        //创建分支项目
        public string ProjectBranch(string uri,string accesstoken)
        {
            //参数拼接
            string proid = "2";   //用于创建分支项目的主项目id
            string url = uri+"cxrestapi/projects/"+proid+"/branch";
            string param = "name=项目分支名称";//test03分支项目名称

            ///参数转化为ascii码
            Encoding encoding = Encoding.UTF8;
            byte[] bs = encoding.GetBytes(param);

            //创建request
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);

            //确定传值的方式，此处为post方式传值
            req.Method = "POST"; 
            req.ContentType = "application/x-www-form-urlencoded";
            req.Headers.Add("Authorization", accesstoken);
            req.ContentLength = bs.Length;

            //发送请求
            Stream reqStream = req.GetRequestStream();
            reqStream.Write(bs, 0, bs.Length);
            
            //接受返回的数据
            WebResponse wr = req.GetResponse();
            Stream myResponseStream = wr.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, encoding);
            string retString = myStreamReader.ReadToEnd();
            //Console.WriteLine(retString);

            //字符串转json格式，获取accesstoken数据
            JObject proBranch = (JObject)JsonConvert.DeserializeObject(retString);//或者JObject jo = JObject.Parse(jsonText);
            string proId = proBranch["id"].ToString();
            //Console.WriteLine(token);
            //Console.WriteLine(token_access);
            return proId;
        }

        //本地上传zip格式源代码包
        public string UploadZip(string uri,string proid, string accesstoken,string filePath)
        {
            //参数拼接
            string url = uri+"cxrestapi/projects/"+ proid + "/sourceCode/attachments";
            //string filename = "ConsoleCxAPI.zip";
            //获取文件名称+后缀
            string filename = Path.GetFileName(filePath);

            Encoding encoding = Encoding.UTF8;
            MemoryStream memoryStream = new MemoryStream();

            //仿造postman中的from-data格式
            // 用于边界符
            string boundary = "----" + DateTime.Now.Ticks.ToString("x");     
           
            //数据模板
            string fileFormdataTemplate =
                    "\r\n--" + boundary +
                    "\r\nContent-Disposition: form-data; name=\"zippedSource\"; filename=\"{0}\"" +
                    "\r\nContent-Type: application/octet-stream" +
                    "\r\n\r\n";

            string formItem = string.Format(fileFormdataTemplate, filename);
            byte[] formItemBytes = Encoding.UTF8.GetBytes(formItem);
            memoryStream.Write(formItemBytes, 0, formItemBytes.Length);

            //文件数据
            Stream filedata =  File.OpenRead(filePath);
            var buffer = new byte[1024];
            int bytesRead; // =0
            while ((bytesRead = filedata.Read(buffer, 0, buffer.Length)) != 0)
            {
                memoryStream.Write(buffer, 0, bytesRead);
            }
            
            //模板结尾
            var footer = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
            memoryStream.Write(footer, 0, footer.Length);


            //创建webRequest并设置属性
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);  //创建request
            req.ContentType = string.Format("multipart/form-data; boundary={0}", boundary);
            req.Method = "POST";    //确定传值的方式，此处为post方式传值
            req.Headers.Add("Authorization", accesstoken);

            req.AllowWriteStreamBuffering = false;//对发送的数据不使用缓存
            req.Timeout = 300000;//设置获得响应的超时时间（半小时）
            req.KeepAlive = true;
            req.ContentLength = memoryStream.Length;

            //直接写入流
            memoryStream.Position = 0;
            Stream requestStream = req.GetRequestStream();
            byte[] buffer1 = new byte[1024];
            int bytesRead_L = 0;
            while ((bytesRead_L = memoryStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                requestStream.Write(buffer, 0, bytesRead_L);
            }

            //获取服务器端的响应
            WebResponse wr = req.GetResponse();
            Stream myResponseStream = wr.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, encoding);
            string retString = myStreamReader.ReadToEnd();
            return "true";
        }

        //创建扫描
        public string postScans(string uri,string proid,string accesstoken) 
        {
            //拼接数据
            string url = uri+"cxrestapi/sast/scans";
            string data = "projectId="+ proid + "&isIncremental=false&isPublic=true&forceScan=true&comment=VScodesacn";

            //参数转化为ascii码
            Encoding encoding = Encoding.UTF8;
            byte[] bs = encoding.GetBytes(data);
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);  //创建request
            req.Method = "POST";    //确定传值的方式，此处为post方式传值
            req.ContentType = "application/x-www-form-urlencoded";
            //req.ContentType = "application/json;v=1.0";
            req.Headers.Add("Authorization", accesstoken);
            req.ContentLength = bs.Length;

            //发送请求
            Stream reqStream = req.GetRequestStream();
            reqStream.Write(bs, 0, bs.Length);

            //接收请求数据
            WebResponse wr = req.GetResponse();
            Stream myResponseStream = wr.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, encoding);
            string retString = myStreamReader.ReadToEnd();
            //Console.WriteLine(retString);

            //字符串转json格式，获取accesstoken数据
            JObject scan = (JObject)JsonConvert.DeserializeObject(retString);//或者JObject jo = JObject.Parse(jsonText);
            string ScanId = scan["id"].ToString();
            return ScanId;
        }   
    }

}
