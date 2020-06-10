using System;
using System.Collections.Generic;
using System.Text;

namespace CertifcateChecker
{
    public class Site
    {
        public string Url { get; set; }

        public DateTime ValidFrom { get; set; }

        public DateTime ValidTo { get; set; }

        public string Issuer { get; set; }

        public string Subject { get; set; }

        public int ValidDays
        {
            get
            {
                var diff = ValidTo.Date - DateTime.UtcNow.Date;

                return (int)diff.TotalDays;
            }
        }
    }
}
