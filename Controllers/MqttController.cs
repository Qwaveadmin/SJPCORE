using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MQTTnet.Server;
using System.Threading.Tasks;

namespace MQTT_QWAVE.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class MqttController : ControllerBase
    {
        private readonly MqttServer mqttServer;

        public MqttController(MqttServer mqttServer)
        {
            this.mqttServer = mqttServer;
        }

        
        [HttpGet]
        [Route("clients")]
        public async Task<IActionResult> GetClientsAsync()
        {
            return Ok(await mqttServer.GetClientsAsync());
        }

        [HttpGet]
        [Route("session")]
        public async Task<IActionResult> GetSessionsAsync()
        {
            return Ok(await mqttServer.GetSessionsAsync());
        }

        [HttpGet]
        [Route("retain")]
        public async Task<IActionResult> GetRetainedMessagesAsync()
        {
            return Ok(await mqttServer.GetRetainedMessagesAsync());
        }

        [HttpDelete]
        [Route("retain")]
        public IActionResult DelRetainedMessagesAsync()
        {
            return Ok(mqttServer.DeleteRetainedMessagesAsync());
        }

        [HttpGet]
        [Route("status")]
        public IActionResult GetStatus()
        {
            return Ok(new { IsStarted = mqttServer.IsStarted });
        }
        
        [HttpGet]
        [Route("start")]
        public async Task<IActionResult> Start()
        {
            if (mqttServer.IsStarted) return Ok(new { IsStarted = mqttServer.IsStarted });
            return Ok( mqttServer.StartAsync());
        }

        [HttpGet]
        [Route("stop")]
        public async Task<IActionResult> Stop()
        {
            return Ok(mqttServer.StopAsync());
        }
    }
}
