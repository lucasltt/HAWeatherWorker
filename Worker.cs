using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using SharpKml.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HAWeatherWorker
{

    //Latitude - Eixo Y NS
    //Longitude - Eixo X OL
    //Casa - Latitude = -23.48164; Longitude = -46.61819
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ServiceConfigurations _serviceConfigurations;


        private List<Storm> Storms = new List<Storm>();
        private StormSummary stormSummary = new StormSummary();

        private MqttFactory factory = new MqttFactory();
        private IMqttClient mqttClient;
        private IMqttClientOptions mqttOptions;
        private MqttApplicationMessage mqttMessage;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;

            _serviceConfigurations = new ServiceConfigurations();

            new ConfigureFromConfigurationOptions<ServiceConfigurations>(configuration.GetSection("ServiceConfigurations")).Configure(_serviceConfigurations);


            mqttClient = factory.CreateMqttClient();
            mqttOptions = new MqttClientOptionsBuilder().WithTcpServer(_serviceConfigurations.MQTTServer).Build();



            mqttClient.UseDisconnectedHandler(async e =>
            {
                _logger.LogInformation("### DISCONNECTED FROM SERVER ###");
                await Task.Delay(System.TimeSpan.FromSeconds(5));

                try
                {
                    await mqttClient.ConnectAsync(mqttOptions, CancellationToken.None);
                }
                catch
                {
                    _logger.LogInformation("### RECONNECTING FAILED ###");
                }
            });


            mqttClient.UseConnectedHandler(async e =>
            {
                _logger.LogInformation("### CONNECTED WITH SERVER ###");

                // Subscribe to a topic
                await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("tele/SensorTempo/SENSOR").Build());

                _logger.LogInformation("### SUBSCRIBED ###");
            });


            mqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                _logger.LogInformation("### RECEIVED APPLICATION MESSAGE ###");
                string payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                int i0 = payload.IndexOf("\"A0\"");
                payload = payload.Substring(i0 + 5);
                int i1 = payload.IndexOf("}");
                payload = payload.Substring(0, i1);
                stormSummary.AddChuva(Convert.ToInt32(payload));
                _logger.LogInformation($"+ Payload = {payload}");


            });


        }


        public async Task<string> DownloadStormData()
        {
            using (var client = new HttpClient())
            {

                using (var result = await client.GetAsync(_serviceConfigurations.StormURL))
                {
                    if (result.IsSuccessStatusCode)
                    {
                        return await result.Content.ReadAsStringAsync();
                    }

                }
            }
            return null;
        }

        public async Task<List<Storm>> ParseStormData()
        {
            List<Storm> storms = new List<Storm>();


            string stormData = await DownloadStormData();

            SharpKml.Base.Parser kmlParser = new SharpKml.Base.Parser();
            kmlParser.ParseString(stormData, false);

            if (kmlParser.Root is Kml kml)
            {

                var placemarks = new List<Placemark>();
                ExtractPlacemarks(kml.Feature, placemarks);


                foreach (Placemark placemark in placemarks)
                    if (placemark.Geometry is SharpKml.Dom.Point point)
                        storms.Add(new Storm() { Latitude = point.Coordinate.Latitude, Longitude = point.Coordinate.Longitude, LatitudeOrigem = _serviceConfigurations.LatitudeCasa, LongitudeOrigem = _serviceConfigurations.LongitudeCasa });

            }

            return storms;


        }


        private static void ExtractPlacemarks(Feature feature, List<Placemark> placemarks)
        {
            if (feature is Placemark placemark)
            {
                placemarks.Add(placemark);
            }
            else
            {
                if (feature is Container container)
                {
                    foreach (Feature f in container.Features)
                    {
                        ExtractPlacemarks(f, placemarks);
                    }
                }
            }
        }




        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await mqttClient.ConnectAsync(mqttOptions, CancellationToken.None);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                Storms = await ParseStormData();

                stormSummary.Storms1KM = Storms.Where(d => d.Distance <= 1).Count();
                stormSummary.Storms2KM = Storms.Where(d => d.Distance > 1 && d.Distance <= 2).Count();
                stormSummary.Storms4KM = Storms.Where(d => d.Distance > 2 && d.Distance <= 4).Count();
                stormSummary.Storms8KM = Storms.Where(d => d.Distance > 4 && d.Distance <= 8).Count();

                if (Storms.Where(d => d.Distance <= 8).Count() > 0)
                {
                    stormSummary.NearestStormDistance = Storms.Where(d => d.Distance <= 8).Select(j => j.Distance).Min();
                    stormSummary.LastStormDate = DateTime.Now.ToString("dd/MM/yy HH:mm");
                }
                else
                {
                    stormSummary.NearestStormDistance = 0;
                }


                mqttMessage = new MqttApplicationMessageBuilder().WithTopic("Storm/1KM").WithPayload(stormSummary.Storms1KM.ToString()).Build();
                await mqttClient.PublishAsync(mqttMessage, CancellationToken.None);

                mqttMessage = new MqttApplicationMessageBuilder().WithTopic("Storm/2KM").WithPayload(stormSummary.Storms2KM.ToString()).Build();
                await mqttClient.PublishAsync(mqttMessage, CancellationToken.None);

                mqttMessage = new MqttApplicationMessageBuilder().WithTopic("Storm/4KM").WithPayload(stormSummary.Storms4KM.ToString()).Build();
                await mqttClient.PublishAsync(mqttMessage, CancellationToken.None);

                mqttMessage = new MqttApplicationMessageBuilder().WithTopic("Storm/8KM").WithPayload(stormSummary.Storms8KM.ToString()).Build();
                await mqttClient.PublishAsync(mqttMessage, CancellationToken.None);

                mqttMessage = new MqttApplicationMessageBuilder().WithTopic("Storm/NEAREST").WithPayload(stormSummary.NearestStormDistance.ToString()).Build();
                await mqttClient.PublishAsync(mqttMessage, CancellationToken.None);

                mqttMessage = new MqttApplicationMessageBuilder().WithTopic("Storm/LASTDATE").WithPayload(stormSummary.LastStormDate).Build();
                await mqttClient.PublishAsync(mqttMessage, CancellationToken.None);

                mqttMessage = new MqttApplicationMessageBuilder().WithTopic("Storm/CHUVA").WithPayload(stormSummary.StatusChuva).Build();
                await mqttClient.PublishAsync(mqttMessage, CancellationToken.None);


                //_logger.LogInformation("Quantidade de raios até 1km: {d}", stormSummary.Storms1KM);
                //_logger.LogInformation("Quantidade de raios entre 1km e 2km: {d}", stormSummary.Storms2KM);
                //_logger.LogInformation("Quantidade de raios entre 2km e 4km: {d}", stormSummary.Storms4KM);
                //_logger.LogInformation("Quantidade de raios entre 4km e 8km: {d}", stormSummary.Storms8KM);
                //_logger.LogInformation("Raio mais próximo: {d}", stormSummary.NearestStormDistance);
                //_logger.LogInformation("Ultima Chuva: {d}", stormSummary.LastStormDate);
                //_logger.LogInformation("Status Chuva: {d}", stormSummary.StatusChuva);
                _logger.LogInformation($"Task Executed at {System.DateTime.Now.ToShortDateString()} { System.DateTime.Now.ToShortTimeString()}");
                await Task.Delay(_serviceConfigurations.Intervalo, stoppingToken);
            }
        }
    }
}
