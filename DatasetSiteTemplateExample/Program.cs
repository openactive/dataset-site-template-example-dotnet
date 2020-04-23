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
                baseUrl = "https://halo-odi.legendonlineservices.co.uk/api/"
            };

            // Strongly typed JSON generation based on OpenActive.NET
            var data = new Dataset
            {
                Id = settings.datasetSiteUrl.ParseUrlOrNull(),
                Url = settings.datasetSiteUrl.ParseUrlOrNull(),
                Name = settings.organisationName + " Sessions and Facilities",
                Description = $"Near real-time availability and rich descriptions relating to the sessions and facilities available from {settings.organisationName}, published using the OpenActive Modelling Specification 2.0.",
                Keywords = new List<string> {
                    "Sessions",
                    "Facilities",
                    "Activities",
                    "Sports",
                    "Physical Activity",
                    "OpenActive"
                },
                License = new Uri("https://creativecommons.org/licenses/by/4.0/"),
                DiscussionUrl = settings.datasetSiteDiscussionUrl.ParseUrlOrNull(),
                Documentation = settings.documentationUrl.ParseUrlOrNull(),
                InLanguage = new List<string> { "en-GB" },
                BookingService = new BookingService
                {
                    Name = platform.platformName,
                    Url = platform.platformUrl.ParseUrlOrNull(),
                    SoftwareVersion = Utils.ApplicationVersion.GetVersion(),
                },
                SchemaVersion = "https://www.openactive.io/modelling-opportunity-data/2.0/".ParseUrlOrNull(),
                Publisher = new Organization
                {
                    Name = settings.organisationName,
                    LegalName = settings.legalEntity,
                    Description = settings.plainTextDescription,
                    Email = settings.email,
                    Url = settings.url.ParseUrlOrNull(),
                    Logo = new ImageObject
                    {
                        Url = settings.logoUrl.ParseUrlOrNull()
                    }
                },
                Distribution = new List<DataDownload>
                {
                    new DataDownload
                    {
                        Name = "SessionSeries",
                        AdditionalType = new Uri("https://openactive.io/SessionSeries"),
                        EncodingFormat = OpenActiveMediaTypes.RealtimePagedDataExchange.Version1,
                        ContentUrl = (settings.baseUrl + "feeds/session-series").ParseUrlOrNull(),
                        Identifier = "SessionSeries"
                    },
                    new DataDownload
                    {
                        Name = "ScheduledSession",
                        AdditionalType = new Uri("https://openactive.io/ScheduledSession"),
                        EncodingFormat = OpenActiveMediaTypes.RealtimePagedDataExchange.Version1,
                        ContentUrl = (settings.baseUrl + "feeds/scheduled-sessions").ParseUrlOrNull(),
                        Identifier = "ScheduledSession"
                    },
                    new DataDownload
                    {
                        Name = "FacilityUse",
                        AdditionalType = new Uri("https://openactive.io/FacilityUse"),
                        EncodingFormat = OpenActiveMediaTypes.RealtimePagedDataExchange.Version1,
                        ContentUrl = (settings.baseUrl + "feeds/facility-uses").ParseUrlOrNull(),
                        Identifier = "FacilityUse"
                    },
                    new DataDownload
                    {
                        Name = "Slot for FacilityUse",
                        AdditionalType = new Uri("https://openactive.io/Slot"),
                        EncodingFormat = OpenActiveMediaTypes.RealtimePagedDataExchange.Version1,
                        ContentUrl = (settings.baseUrl + "feeds/slots").ParseUrlOrNull(),
                        Identifier = "FacilityUseSlot"
                    }
                },
                DatePublished = DateTimeOffset.UtcNow,
                DateModified = DateTimeOffset.UtcNow,
                BackgroundImage = new ImageObject
                {
                    Url = settings.backgroundImageUrl.ParseUrlOrNull()
                }
            };

            // OpenActive.NET creates complete JSON from the strongly typed structure, complete with schema.org types.
            var jsonString = OpenActiveSerializer.SerializeToHtmlEmbeddableString(data);

            // Deserialize the completed JSON object to make it compatible with the mustache template
            dynamic jsonData = JsonConvert.DeserializeObject(jsonString);

            // Stringify the input JSON, and place the contents of the string
            // within the "json" property at the root of the JSON itself.
            jsonData.json = jsonString;

            // Download the mustache template
            // FOR PRODUCTION USE DO NOT DOWNLOAD THE MUSTACHE FILE LIVE, A COPY MUST BE STORED LOCALLY TO PREVENT XSS ATTACKS
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
            public class ApplicationVersion
            {
                public static string GetVersion()
                {
                    return typeof(ApplicationVersion).Assembly.GetName().Version.ToString().TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9').TrimEnd('.');
                }
            }
        }
    }
}
