using KafkaService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaService
{
    public abstract class KafkaClient
    {
        protected ILogger _logger;
        protected ConsumerModel? _consumerModel;
        protected ProducerModel? _producerModel;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="consumerModel"></param>
        /// <param name="logger"></param>
        public KafkaClient(ConsumerModel consumerModel, ILogger logger)
        {
            _consumerModel = consumerModel;
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="producerModel"></param>
        /// <param name="logger"></param>
        public KafkaClient(ProducerModel producerModel, ILogger logger)
        {
            _producerModel = producerModel;
            _logger = logger;
        }
    }
}
