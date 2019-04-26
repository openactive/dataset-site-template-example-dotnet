using System;
using System.Collections.Generic;
using OpenActive.NET;
using Newtonsoft.Json;
using RestSharp;
using Stubble.Core.Builders;
using Stubble.Extensions.JsonNet;

namespace DatasetSiteTemplateExample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Platform-specific settings for dataset JSON
            var platform = new
            {
                platformName = "AcmeBooker",
                platformUrl = "https://acmebooker.example.com/"
            };

            // Customer-specific settings for dataset JSON (these should come from a database)
            var settings = new
            {
                organisationName = "Better",
                datasetSiteUrl = "https://halo-odi.legendonlineservices.co.uk/openactive/",
                datasetSiteDiscussionUrl = "https://github.com/gll-better/opendata",
                documentationUrl = "https://docs.acmebooker.example.com/",
                legalEntity = "GLL",
                plainTextDescription = "Established in 1993, GLL is the largest UK-based charitable social enterprise delivering leisure, health and community services. Under the consumer facing brand Better, we operate 258 public Sports and Leisure facilities, 88 libraries, 10 children’s centres and 5 adventure playgrounds in partnership with 50 local councils, public agencies and sporting organisations. Better leisure facilities enjoy 46 million visitors a year and have more than 650,000 members.",
                email = "info@better.org.uk",
                url = "https://www.better.org.uk/",
                logoUrl = "http://data.better.org.uk/images/logo.png",
                backgroundImageUrl = "https://data.better.org.uk/images/bg.jpg",
                baseUrl = "https://customer.example.com/feed/"
            };

            // Strongly typed JSON generation based on OpenActive.NET
            var data = new DatasetExtended
            {
                Id = Utils.SafeParseUrl(settings.datasetSiteUrl),
                Url = Utils.SafeParseUrl(settings.datasetSiteUrl),
                Name = settings.organisationName + " Sessions and Facilities",
                Description = $"Near real-time availability and rich descriptions relating to the sessions and facilities available from {settings.organisationName}, published using the OpenActive Modelling Specification 2.0.",
                Keywords = new string[6] {
                    "Sessions",
                    "Facilities",
                    "Activities",
                    "Sports",
                    "Physical Activity",
                    "OpenActive"
                },
                License = new Uri("https://creativecommons.org/licenses/by/4.0/"),
                DiscussionUrl = Utils.SafeParseUrl(settings.datasetSiteDiscussionUrl),
                Documentation = Utils.SafeParseUrl(settings.documentationUrl),
                InLanguage = "en-GB",
                SoftwareVersion = Utils.ApplicationVersion.GetVersion(),
                SchemaVersion = "https://www.openactive.io/modelling-opportunity-data/2.0/",
                Publisher = new OpenActive.NET.Organization
                {
                    Name = settings.organisationName,
                    LegalName = settings.legalEntity,
                    Description = settings.plainTextDescription,
                    Email = settings.email,
                    Url = Utils.SafeParseUrl(settings.url),
                    Logo = new OpenActive.NET.ImageObject
                    {
                        Url = Utils.SafeParseUrl(settings.logoUrl)
                    }
                },
                Distribution = new List<DataDownload>
                {
                    new DataDownload
                    {
                        Name = "SessionSeries",
                        AdditionalType = new Uri("https://openactive.io/SessionSeries"),
                        EncodingFormat = OpenActiveDiscovery.MediaTypes.Version1.RealtimePagedDataExchange.ToString(),
                        ContentUrl = Utils.SafeParseUrl(settings.baseUrl + "feeds/session-series")
                    },
                    new DataDownload
                    {
                        Name = "ScheduledSession",
                        AdditionalType = new Uri("https://openactive.io/ScheduledSession"),
                        EncodingFormat = OpenActiveDiscovery.MediaTypes.Version1.RealtimePagedDataExchange.ToString(),
                        ContentUrl = Utils.SafeParseUrl(settings.baseUrl + "feeds/scheduled-sessions")
                    },
                    new DataDownload
                    {
                        Name = "FacilityUse",
                        AdditionalType = new Uri("https://openactive.io/FacilityUse"),
                        EncodingFormat = OpenActiveDiscovery.MediaTypes.Version1.RealtimePagedDataExchange.ToString(),
                        ContentUrl = Utils.SafeParseUrl(settings.baseUrl + "feeds/facility-uses")
                    },
                    new DataDownload
                    {
                        Name = "Slot",
                        AdditionalType = new Uri("https://openactive.io/Slot"),
                        EncodingFormat = OpenActiveDiscovery.MediaTypes.Version1.RealtimePagedDataExchange.ToString(),
                        ContentUrl = Utils.SafeParseUrl(settings.baseUrl + "feeds/slots")
                    }
                },
                DatePublished = DateTimeOffset.UtcNow,
                BackgroundImage = Utils.SafeParseUrl(settings.backgroundImageUrl),
                PlatformName = platform.platformName,
                PlatformUrl = Utils.SafeParseUrl(platform.platformUrl)
            };

            // OpenActive.NET creates complete JSON from the strongly typed structure, complete with schema.org types.
            var jsonString = data.ToString(new JsonSerializerSettings { Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore });

            // Deserialize the completed JSON object to make it compatible with the mustache template
            dynamic jsonData = JsonConvert.DeserializeObject(jsonString);

            // Stringify the input JSON, and place the contents of the string
            // within the "json" property at the root of the JSON itself.
            jsonData.json = jsonString;

            // Download the mustache template
            // Note it is not recommended to download this live in production, this file should be copied locally and loaded into memory
            var client = new RestClient("https://www.openactive.io/");
            var request = new RestRequest("dataset-site-template/datasetsite.mustache", Method.GET);
            request.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
            var queryResult = client.Execute(request);
            var template = queryResult.Content;

            //Use the resulting JSON with the mustache template to render the dataset site.
            var stubble = new StubbleBuilder().Configure(s => s.AddJsonNet()).Build();
            var output = stubble.Render(template, jsonData);

            //Output HTML for the completed page
            // Note to test this simply add "> output.txt" to the command-line arguments in Visual Studio's debug properties.
            Console.WriteLine(output);
            
        }

        public class Utils
        {
            public static Uri SafeParseUrl(string str)
            {
                if (Uri.TryCreate(str, UriKind.Absolute, out Uri url))
                {
                    return url;
                }
                else
                {
                    return null;
                }
            }

            public class ApplicationVersion
            {
                public static string GetVersion()
                {
                    return typeof(ApplicationVersion).Assembly.GetName().Version.ToString().TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9').TrimEnd('.');
                }
            }
        }

        // Below are temporary classes which include existing properties from other types in schema.org,
        // pending the new Dataset API Discovery spec (https://www.openactive.io/dataset-api-discovery/EditorsDraft).

        // The key elements of the below defacto standard format are currently in use across all existing OpenActive data publishers,
        // so it is safe to use the below while we wait for the spec.

        // Once the new spec is released and the OpenActive machine readable data models are updated,
        // the below will then be included in OpenActive.NET, so can be removed from here.

        public class DatasetExtended : Schema.NET.Dataset
        {
            [JsonProperty("url")]
            public new Uri Url { get; set; }
            [JsonProperty("name")]
            public new string Name { get; set; }
            [JsonProperty("description")]
            public new string Description { get; set; }
            [JsonProperty("keywords")]
            public new string[] Keywords { get; set; }
            [JsonProperty("license")]
            public new Uri License { get; set; }
            [JsonProperty("distribution")]
            public new List<DataDownload> Distribution { get; set; }
            [JsonProperty("discussionUrl")]
            public new Uri DiscussionUrl { get; set; }
            [JsonProperty("documentation")]
            public Uri Documentation { get; set; }
            [JsonProperty("inLanguage")]
            public new string InLanguage { get; set; }
            [JsonProperty("publisher")]
            public new OpenActive.NET.Organization Publisher { get; set; }
            [JsonProperty("datePublished")]
            public new DateTimeOffset? DatePublished { get; set; }
            [JsonProperty("schemaVersion")]
            public new string SchemaVersion { get; set; }
            [JsonProperty("softwareVersion")]
            public string SoftwareVersion { get; set; }
            [JsonProperty("backgroundImage")]
            public Uri BackgroundImage { get; set; }
            [JsonProperty("platformName")]
            public string PlatformName { get; set; }
            [JsonProperty("platformUrl")]
            public Uri PlatformUrl { get; set; }
            [JsonProperty("json")]
            public string json { get; set; }
        }

        public class DataDownload : Schema.NET.DataDownload
        {
            [JsonProperty("name")]
            public new string Name { get; set; }
            [JsonProperty("additionalType")]
            public new Uri AdditionalType { get; set; }
            [JsonProperty("encodingFormat")]
            public new string EncodingFormat { get; set; }
            [JsonProperty("contentUrl")]
            public new Uri ContentUrl { get; set; }
        }
    }
}
