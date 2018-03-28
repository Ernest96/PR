using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace PR
{
    class Request
    {

        public static RequestResult DoGetRequest(string url)
        {
            RequestResult result = new RequestResult();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = 60 * 1000; // one minute
            request.Headers.Add("X-API-Key", "55193451-1409-4729-9cd4-7c65d63b8e76");
            request.Headers.Add("Accept", "text/csv"); 
           
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    result.responseCode = response.StatusCode;

                    result.data = new StreamReader(response.GetResponseStream()).ReadToEnd();

                    response.Close();
                }
            }
            catch (WebException webException)
            {
                Logger.Writeln($"Error at request: {url}", ConsoleColor.Red);
                Logger.Writeln($"Could not get response from endpoint: {webException.Message}", ConsoleColor.Red);

                using (HttpWebResponse response = (HttpWebResponse)webException.Response)
                {
                    if (response != null)
                    {
                        result.responseCode = response.StatusCode;
                        result.data = new StreamReader(response.GetResponseStream()).ReadToEnd();
                        response.Close();
                    }
                    else
                    {
                        result.responseCode = HttpStatusCode.BadRequest;
                    }
                }
            }

            if (result.responseCode != HttpStatusCode.OK)
            {
                Logger.Writeln($"Error at request: {url}", ConsoleColor.Red);
                Logger.Writeln($"StatusCode: {(int)result.responseCode}", ConsoleColor.Red);
            }

            return result;
        }
    }
}
