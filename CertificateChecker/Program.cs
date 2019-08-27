using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CertifcateChecker
{
    class Program
    {
        static async Task Main()
        {
            string settingsFile = "settings.json";
            if (!File.Exists(settingsFile))
            {
                Console.WriteLine($"Missing settings file {settingsFile}.");

                Terminate(1);
            }

            Settings settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsFile));

            if (settings == null)
            {
                Console.WriteLine("Missing settings.");

                Terminate(1);
            }

            if (settings.Sites == null || settings.Sites.Length == 0)
            {
                Console.WriteLine("Missing setting for sites.");

                Terminate(1);
            }

            List<Site> siteList = new List<Site>(settings.Sites.Length);
            foreach (var url in settings.Sites)
            {
                siteList.Add(new Site()
                {
                    Url = url,
                });
            }

            var handler = new HttpClientHandler
            {
                UseDefaultCredentials = true,

                ServerCertificateCustomValidationCallback = (sender, cert, chain, error) =>
                {
                    // Using StartsWith because sender.RequestUri contains trailing /
                    string requestUrl = sender.RequestUri.ToString();
                    var site = siteList.Find(s => requestUrl.StartsWith(s.Url));

                    if (site == null)
                    {
                        Console.WriteLine("No data found");
                        return true;
                    }

                    site.Issuer = cert.Issuer;
                    site.ValidFrom = cert.NotBefore;
                    site.ValidTo = cert.NotAfter;
                    site.Subject = cert.Subject;

                    return true;
                }
            };

            using (HttpClient client = new HttpClient(handler))
            {
                foreach (var site in siteList)
                {
                    using (HttpResponseMessage response = await client.GetAsync(site.Url))
                    {
                        using (HttpContent content = response.Content)
                        {
                            Console.WriteLine($"Fetch data for site {site.Url}");
                        }
                    }
                }
            }

            await SendEmail(siteList, settings);

            Console.WriteLine("Done");
            Terminate();
        }

        private static async Task SendEmail(List<Site> siteList, Settings settings)
        {
            if (siteList is null)
            {
                throw new ArgumentNullException(nameof(siteList));
            }

            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (siteList.Count == 0)
            {
                return;
            }

            StringBuilder plainTextContent = new StringBuilder();
            StringBuilder htmlContent = new StringBuilder();

            foreach (var site in siteList)
            {
                var diff = site.ValidTo - DateTime.UtcNow;
                string errorText = string.Empty;

                if (diff.TotalDays < 0)
                {
                    errorText = $"ERROR: Certificate expired {diff.TotalDays.ToString("0")} days ago";
                }
                else if (diff.TotalDays < 7)
                {
                    errorText = $"WARNING: Certificate expires in {diff.TotalDays.ToString("0")} days";
                }
                else if (diff.TotalDays < 30)
                {
                    errorText = $"INFO: Certificate expires in {diff.TotalDays.ToString("0")} days";
                }

                htmlContent.Append($"<h1>{site.Url}</h1>");
                plainTextContent.Append($"{site.Url}{Environment.NewLine}");

                if (!string.IsNullOrEmpty(errorText))
                {
                    htmlContent.Append($"<h1 style=\"background-color: black;color:Red;\">{errorText}</h1>");

                    plainTextContent.Append($"{errorText}");
                }

                htmlContent.Append($"<p>Valid from: {site.ValidFrom}");
                htmlContent.Append("<br />");
                htmlContent.Append($"Valid to: {site.ValidTo}");
                htmlContent.Append("<br />");
                htmlContent.Append($"Expires in: {diff.TotalDays.ToString("0")} days</p>");
                htmlContent.Append($"<hr /><br />");

                plainTextContent.Append($"Valid from: {site.ValidFrom}{Environment.NewLine}");
                plainTextContent.Append($"Valid to: {site.ValidTo}{Environment.NewLine}");
                plainTextContent.Append($"Expires in: {diff.TotalDays.ToString("0")} days{Environment.NewLine}");
                plainTextContent.Append($"{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}");
            }

            var apiKey = settings.SendGrid;
            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(settings.From.Email, settings.From.DisplayName);
            var subject = "Status for certificates";

            var to = new List<EmailAddress>(settings.To.Count);
            foreach (var t in settings.To)
            {
                to.Add(new EmailAddress(t.Email, t.DisplayName));
            }

            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from, to, subject, plainTextContent.ToString(), htmlContent.ToString());
            await client.SendEmailAsync(msg);
        }

        private static void Terminate(int exitCode = 0)
        {
            if (Debugger.IsAttached)
            {
                Console.ReadKey();
            }

            Environment.Exit(exitCode);
        }
    }
}
