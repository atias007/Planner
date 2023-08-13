﻿using CloudNative.CloudEvents;
using CloudNative.CloudEvents.NewtonsoftJson;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Server;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar
{
    public class MqttBrokerService : IHostedService
    {
        private const int _port = 206;
        private MqttServer _mqttServer = null!;
        private static readonly JsonEventFormatter _formatter = new();
        private readonly ILogger<MqttBrokerService> _logger;

        public static event EventHandler<CloudEventArgs>? InterceptingPublishAsync;

        public MqttBrokerService(ILogger<MqttBrokerService> logger)
        {
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var options = new MqttServerOptionsBuilder()
                        .WithDefaultEndpoint()
                        .WithDefaultEndpointPort(_port)
                        .WithDefaultCommunicationTimeout(TimeSpan.FromSeconds(5))
                        .WithKeepAlive()
                        .WithPersistentSessions()
                        .WithTcpKeepAliveInterval(3)
                        .WithTcpKeepAliveRetryCount(3)
                        .Build();

                _mqttServer = new MqttFactory().CreateMqttServer(options);
                _mqttServer.ClientConnectedAsync += ClientConnected;
                _mqttServer.InterceptingPublishAsync += InterceptingPublish;
                _mqttServer.StartedAsync += StartedAsync;
                _mqttServer.StoppedAsync += StoppedAsync;
                await _mqttServer.StartAsync();
                _logger.LogInformation("Initialize: {Operation}", "Starting MQTT Service...");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Initialize: Fail To {Operation}", "Start MQTT Service");
            }
        }

        private async Task StoppedAsync(EventArgs arg)
        {
            _logger.LogWarning("Stopped MQTT Service");
            await Task.CompletedTask;
        }

        private async Task StartedAsync(EventArgs arg)
        {
            _logger.LogInformation("Initialize: {Operation}", "MQTT Service started");
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_mqttServer == null) { return; }
            SafeHandle(() => _mqttServer.ClientConnectedAsync -= ClientConnected);
            SafeHandle(() => _mqttServer.InterceptingPublishAsync -= InterceptingPublish);
            SafeHandle(() => _mqttServer.StartedAsync -= StartedAsync);
            await SafeHandleAsync(_mqttServer.StopAsync);
            SafeHandle(_mqttServer.Dispose);
        }

        private static void SafeHandle(Action action)
        {
            try
            {
                action.Invoke();
            }
            catch
            {
                // *** DO NOTHING ***
            }
        }

        private static async Task SafeHandleAsync(Func<Task> func)
        {
            try
            {
                await func.Invoke();
            }
            catch
            {
                // *** DO NOTHING ***
            }
        }

        private static void OnInterceptingPublishAsync(CloudEvent cloudEvent, InterceptingPublishEventArgs arg)
        {
            if (InterceptingPublishAsync != null)
            {
                var cloudEventArgs = new CloudEventArgs(cloudEvent, arg.ClientId);
                InterceptingPublishAsync(null, cloudEventArgs);
            }
        }

        private async Task InterceptingPublish(InterceptingPublishEventArgs arg)
        {
            try
            {
                var cloudEvent = arg.ApplicationMessage.ToCloudEvent(_formatter);
                OnInterceptingPublishAsync(cloudEvent, arg);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "fail to handle MQTT published message");
            }

            await Task.CompletedTask;
        }

        private async Task ClientConnected(ClientConnectedEventArgs arg)
        {
            SafeHandle(() =>
            _logger.LogDebug("New MQTT connection: ClientId = {ClientId}, Endpoint = {Cndpoint}", arg.ClientId, arg.Endpoint));

            await Task.CompletedTask;
        }
    }
}