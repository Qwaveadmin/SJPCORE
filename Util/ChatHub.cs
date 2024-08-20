using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MQTTnet;
using MQTTnet.Server;


namespace SJPCORE.Util
{ 
    public class ChatHub : Hub
    {
        private readonly MqttServer _mqttserver;
        private readonly EMQXClientService _emqClientService;
        public ChatHub(MqttServer mqttserver, EMQXClientService emqClientService)
        {
            _mqttserver = mqttserver;
            _emqClientService = emqClientService;
        }
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task PublishEMQX( string payload,string topic)
        {
            await _emqClientService.PublishMessageAsync(topic, payload);
        }

        public async Task PublishLocal( string payload,string topic)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload("ควย")
                .WithRetainFlag()
                .Build();

            await _mqttserver.InjectApplicationMessage(
                new InjectedMqttApplicationMessage(message)
                {
                    SenderClientId = "commander"
                });
        }
    }
}
