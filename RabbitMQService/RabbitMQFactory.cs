using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQService.Model;
using System.Net.Sockets;
using System.Security.Authentication;

namespace RabbitMQService
{
    /// <summary>
    /// Rabbit MQ Client Abstract Class
    /// </summary>
    public abstract class RabbitMQFactory
    {
        #region Public static Method
  
        public static ConnectionFactory ConnectionFactory(ClientModel clientModel)
        {
            
                ConnectionFactory connectionFactory = new()
                {
                    UserName = clientModel.Username,
                    Password = clientModel.Password,
                    VirtualHost = clientModel.VirtualHostName,
                    HostName = clientModel.HostName
                };
                string fClientProvidedName = clientModel.ClientProvidedName;
                if (!string.IsNullOrEmpty(fClientProvidedName))
                {
                    connectionFactory.ClientProvidedName = fClientProvidedName;
                }
                connectionFactory.AutomaticRecoveryEnabled = clientModel.AutomaticRecoveryEnabled;
                connectionFactory.TopologyRecoveryEnabled = clientModel.TopologyRecoveryEnabled;
                if (clientModel.Ssl.Enabled)
                {
                    connectionFactory.AuthMechanisms = new IAuthMechanismFactory[] { new ExternalMechanismFactory() };
                    connectionFactory.Ssl.Enabled = true;
                    connectionFactory.Ssl.ServerName = clientModel.Ssl.ServerName;
                    connectionFactory.Ssl.CertPath = clientModel.Ssl.CertPath;
                    connectionFactory.Ssl.CertPassphrase = clientModel.Ssl.CertPassphrase;
                    connectionFactory.Ssl.Version = SslProtocols.Tls12;
                }
                return connectionFactory;
            
        }
        #endregion
    }
}