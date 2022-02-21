﻿using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using ProcessSharePoint.Entities;
using IdentityModel.Client;
using System.Linq;

namespace ProcessSharePoint
{
    public class Program
    {
        private static readonly IConfiguration _configuration;

        static Program()
        {
            var builder = new ConfigurationBuilder().AddNewtonsoftJsonFile($"appsettings.json", true, true);
            _configuration = builder.Build();
        }
        static void Main(string[] args)
        {
            var TokenEndpoint = _configuration["SharePointOnline:TokenEndpoint"];
            var ClientID = _configuration["SharePointOnline:client_id"];
            var ClientSecret = _configuration["SharePointOnline:client_secret"];
            var resource = _configuration["SharePointOnline:resource"];
            var GrantType = _configuration["SharePointOnline:grant_type"];
            var Tenant = _configuration["SharePointOnline:tenant"];
            TokenEndpoint = string.Format(TokenEndpoint, Tenant);



            var keyValues = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", GrantType),
                new KeyValuePair<string, string>("client_id", ClientID),
                new KeyValuePair<string, string>("client_secret", ClientSecret),
                new KeyValuePair<string, string>("resource", resource),
                new KeyValuePair<string, string>("tenant", Tenant),
            };

            HttpContent content = new FormUrlEncodedContent(keyValues);

            var httpClient = new HttpClient();
            var response = httpClient.PostAsync(TokenEndpoint, content).Result;
            var token = response.Content.ReadAsStringAsync().Result;
            var accessToken= (JsonConvert.DeserializeObject<AccessToken>(token)).access_token;


            var SiteDataEndPoint = _configuration["SharePointOnline:SiteDataEndPoint"];
            
            httpClient.SetBearerToken(accessToken);
            response = httpClient.GetAsync(SiteDataEndPoint).Result;
            var siteData = response.Content.ReadAsStringAsync().Result;
            var sharepointSite = JsonConvert.DeserializeObject<SharePointSite>(siteData);
            ;

            var ListsEndPoint = _configuration["SharePointOnline:ListsEndPoint"];
            ListsEndPoint = string.Format(ListsEndPoint, sharepointSite.id);

            ;
            httpClient.SetBearerToken(accessToken);
            response = httpClient.GetAsync(ListsEndPoint).Result;
            var listData = response.Content.ReadAsStringAsync().Result;
            var sharePointList = JsonConvert.DeserializeObject<SharePointList>(listData);
            var listid = sharePointList.value.FirstOrDefault(obj => obj.displayName == "Tasks").id;
            

            var ListDataEndPoint = _configuration["SharePointOnline:ListDataEndPoint"];
            ListDataEndPoint = string.Format(ListDataEndPoint, sharepointSite.id, listid);
            httpClient.SetBearerToken(accessToken);
            response = httpClient.GetAsync(ListDataEndPoint).Result;
            var ListData = response.Content.ReadAsStringAsync().Result;



            //Updating List fields
            var ListFieldsUpdateEndPoint = _configuration["SharePointOnline:ListFieldsUpdateEndPoint"];
            ListFieldsUpdateEndPoint = string.Format(ListFieldsUpdateEndPoint, sharepointSite.id, listid, "ItemId");
            
            httpClient.SetBearerToken(accessToken);
            var sharePointObject = new
            {
                field1 = "value1",
                field2 = "value2"
            };

            string strSharePointObject = JsonConvert.SerializeObject(sharePointObject, Newtonsoft.Json.Formatting.Indented);
            var httpContent = new StringContent(strSharePointObject);
            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var updateResponse = httpClient.PatchAsync(ListFieldsUpdateEndPoint, httpContent);

        }

    }
}
