using RabbitMQService;
using RabbitMQService.Abstraction;
using StartupServices.Interface;
using StartupServices.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StartupServices
{
    public static class CommonExtenstion
    {
        private const string RABBIT_MQ_SECTION = "Configuration:Services:RabbitMQ";
        private const string KAFKA_SECTION = "Configuration:Services:Kafka";
        public static IServiceCollection AddRabbitMQ(this IServiceCollection services,IConfiguration configuration)
        {
            var rabbitMQSection = configuration.GetSection(RABBIT_MQ_SECTION);

            if (rabbitMQSection.Exists())
            {
                return services;
            }

            bool.TryParse(rabbitMQSection.GetSection("Enabled").Value, out bool isRabbitMQEnabled);

            if (isRabbitMQEnabled)
            {
                services.AddSingleton<IRabbitMQProcessor>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<RabbitMQProcessor>>();
                    return new RabbitMQProcessor(rabbitMQSection, logger, sp);
                });
            }

            services.AddSingleton<ISubscriptionsManager, SubscriptionsManager>();
            return services;

        }

        public static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
        {
            var kafkaSection = configuration.GetSection(KAFKA_SECTION);

            if (kafkaSection.Exists())
            {
                return services;
            }

            bool.TryParse(kafkaSection.GetSection("Enabled").Value, out bool iskafkaEnabled);

            if (iskafkaEnabled)
            {
                services.AddSingleton<IKafkaProcessor>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<KafkaProcessor>>();
                    return new KafkaProcessor(kafkaSection, logger);
                });
            }

            services.AddSingleton<ISubscriptionsManager, SubscriptionsManager>();
            return services;

        }

    }
}
