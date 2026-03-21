using Ascon.Plm.ServerApi;

namespace CSharpServerAPI
{
    /// <summary>
    /// Клиент для взаимодействия с Server API сервера приложений ЛОЦМАН:PLM.
    /// </summary>
    public sealed class LoodsmanServerApiClient
    {
        /// <summary>
        /// Соединение с сервером приложений ЛОЦМАН:PLM.
        /// </summary>
        private readonly IConnection _connection;

        public LoodsmanServerApiClient(string serverAddress, Guid sessionId)
        {
            var uriBuilder = new UriBuilder(serverAddress);

            var connectionFactory = new ConnectionFactory(null, sessionId.ToString());
            _connection = connectionFactory.CreateConnection(uriBuilder.Host, uriBuilder.Port);
        }
    }
}
