using System;
using System.Collections.Generic;
using System.Text;

namespace CertifcateChecker
{
    public class Settings
    {
        public string SendGrid { get; set; }

        public string[] Sites { get; set; }

        public EmailInfo From { get; set; }

        public List<EmailInfo> To { get; set; }
    }
}
