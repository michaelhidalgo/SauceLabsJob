using System;
using System.Drawing.Imaging;
using System.IO;
using System.Drawing;
using System.Net;
using Newtonsoft.Json.Linq;

namespace SauceLabsJob
{
    internal class Program
    {
        private static string baseUrl = @"https://saucelabs.com/rest/v1/securityinnovation";
        private static string userName = "securityinnovation";
        private static string AccesKey = "";
        private static string Path = @"C:\temp\";

        static void Main(string[] args)
        {
            //The job will run every day at 12:00 AM. So the query will return all the test that run for the day.
            var fromTime = ToUnixTimestamp(DateTime.Now.Date);

            var jobsUrl = baseUrl + "/jobs?full=true&from=" + fromTime;
            var uri = new Uri(jobsUrl);
            var jobsList = ProcessHttpRequest(uri);
           

            if (jobsList != "[]")
            {
                dynamic jsonResponse = JArray.Parse(jobsList);
          
                foreach (dynamic response in jsonResponse)
                {
                    var testName= response.name;
                    if (testName != null )
                    {
                        if (testName.ToString().ToLower().Contains("browserresize"))
                        {
                           //Filtering complete jobs and
                            if (response.record_screenshots.Value && response.status == "complete")
                            {
                                var jobId = response.id;
                                DownloadScreenShots(jobId.Value, response);
                            }
                        }
                    }

                }
            }
          
        }

        private static string ProcessHttpRequest(Uri url)
        {
            var request = WebRequest.Create(url);
            request.Method = "Get";
            request.Headers.Add(HttpRequestHeader.Authorization, Base64EncodingCredentials());
            
            var response = request.GetResponse();

            using (var ms = new StreamReader (response.GetResponseStream()))
            {
                var returnValue = ms.ReadToEnd();
                return returnValue;
            }
        }

        private static Stream ImageStream(Uri url)
        {
            var request = WebRequest.Create(url);
            request.Method = "Get";
            request.Headers.Add(HttpRequestHeader.Authorization, Base64EncodingCredentials());

            var response = request.GetResponse();

            return response.GetResponseStream();
        }

        private static  string Base64EncodingCredentials()
        {
            var credentials = String.Format("{0}:{1}", userName, AccesKey);
            var byteArray = System.Text.Encoding.UTF8.GetBytes(credentials);
            var base64Encoding = Convert.ToBase64String(byteArray);

            return string.Format("Basic {0}", base64Encoding);
        }

        private static int ToUnixTimestamp( DateTime value)
        {
            return (int)Math.Truncate((value.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
        }

        private static void DownloadScreenShots(string jobId, dynamic response)
        {
            var tempUrl = String.Format("/jobs/{0}/assets", jobId);
            var url = String.Format("{0}{1}", baseUrl, tempUrl);
            var jsonResponse = ProcessHttpRequest(new Uri(url));

            dynamic assets = JObject.Parse(jsonResponse);
            if (assets.screenshots !=null)
            {
                var screenshots = ((JArray) assets.screenshots);
                if (screenshots.Count > 0)
                {
                    foreach (var screenshot in screenshots)
                    {
                        var fileName = String.Format("{0}_{1}_{2}_{3}.jpg", response.name, response.browser, response.os, screenshot);

                        var screenShotUrl = String.Format("{0}/{1}", url, screenshot);
                        var imageResponse = ImageStream(new Uri(screenShotUrl));
                        var image= Image.FromStream(imageResponse);
                        var currentDate = DateTime.Now.Date.ToString("dd-MM-yyyy");
                        var currentDirectory = String.Format("{0}{1}",Path, currentDate);
                        if (!Directory.Exists(currentDirectory))
                        {
                            Directory.CreateDirectory(currentDirectory);
                        }
                        image.Save(String.Format("{0}/{1}", currentDirectory, fileName), ImageFormat.Jpeg);
                        image.Dispose();
                    }
                }
            }
        }


        private static void DeleteFailedJobs(dynamic jsonResponse)
        {
            foreach (dynamic response in jsonResponse)
            {
               
                    var URL = String.Format("{0}/jobs/{1}", baseUrl, response.id);
                    HttpDelete(new Uri(URL));
               
            }
        
        }

        private static void HttpDelete(Uri url)
        {
            var request = WebRequest.Create(url);
            request.Method = "DELETE";
            request.Headers.Add(HttpRequestHeader.Authorization, Base64EncodingCredentials());

            var response = request.GetResponse();
        }
    }
}
