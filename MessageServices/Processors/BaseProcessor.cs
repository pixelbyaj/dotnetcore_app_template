using StartupServices.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StartupServices.Processors
{
    public abstract class BaseProcessor
    {
        protected readonly IConfiguration _configuration;
        protected readonly ILogger _logger;
        protected bool _enabled;

        protected BaseProcessor(IConfiguration configuration, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        public bool Enabled { get { return _enabled; } }
    }
}
