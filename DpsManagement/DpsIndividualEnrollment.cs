using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DpsManagement
{
    public class DpsIndividualEnrollment
    {
        private IConfiguration Configuration { get; set; }

        private readonly ILogger<DpsIndividualEnrollment> _logger;

        public DpsIndividualEnrollment(IConfiguration config, ILoggerFactory loggerFactory)
        {
            Configuration = config;
            _logger = loggerFactory.CreateLogger<DpsIndividualEnrollment>();
        }
    }
}
