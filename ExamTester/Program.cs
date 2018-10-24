using inRiver.Remoting;
using inRiver.Remoting.Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ImportExam
{
    class Program
    {
        static void Main(string[] args)
        {
            DebugExam();
            //TestExamUploadFile1();
            Console.ReadKey();
        }

        static void DebugExam()
        {
            ImportExam exam = new ImportExam();
            RemoteManager manager = RemoteManager.CreateInstance(
                "https://demo.remoting.productmarketingcloud.com",
                "academy62@inriver.com",
                "inRiverBest4Ever!"
                );
            exam.Context = new inRiverContext(manager, new ConsoleLogger());

            exam.Context.Log(inRiver.Remoting.Log.LogLevel.Information, "Debugging of Exam starting");

            string file1 = File.ReadAllText(@"..\..\XMLFile1.xml");
            string file2 = File.ReadAllText(@"..\..\XMLFile2.xml");

            exam.Context.Log(inRiver.Remoting.Log.LogLevel.Information, "Sending XMLFile1");
            exam.Add(file1);

            exam.Context.Log(inRiver.Remoting.Log.LogLevel.Information, "Sending XMLFile2");
            exam.Add(file2);

            exam.Context.Log(inRiver.Remoting.Log.LogLevel.Information, "Both files done");

        }

        static void TestExamUploadFile1()
        {
            string extensionApiKey = "noapikey";

            string customerSafename = "inriveracademyXX {XX = Your Academy number}";
            string environmentSafename = "test";
            string controllerName = "inbounddata";
            string extensionId = "{YourExtensionId}";

            string inboundEndpoint = $"https://demo.inbound.productmarketingcloud.com/api/{controllerName}/{customerSafename}/{environmentSafename}/{extensionId}";

            string filename = "XMLFile1.xml";
            string fileContent = File.ReadAllText(@"C:\temp\" + filename);


            HttpClient httpClient = new HttpClient();

            httpClient.BaseAddress = new Uri(inboundEndpoint);

            var byteArray = Encoding.ASCII.GetBytes("apikey:" + extensionApiKey);
            httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));


            var content = new StringContent(fileContent, Encoding.UTF8, "application/json");

            HttpResponseMessage response = httpClient.PostAsync(inboundEndpoint, content).Result;

            if (response.Content != null)
            {
                var responseContent = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Response: " + responseContent);
            }
            else
            {
                Console.WriteLine("No content");
            }
        }
    }
}
