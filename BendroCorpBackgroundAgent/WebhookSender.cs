using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BendroCorpBackgroundAgent
{
    public static class WebhookSender
    {
        public static async Task<bool> Send (string uri, WebHookPayload payload){
            var client = new HttpClient();
            HttpResponseMessage res = await client.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
            if (res.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return true;
            }else{
                return false;
            }
        }
    }

    public class WebHookPayload
    {
        public string content { get; set; }
    }
}
