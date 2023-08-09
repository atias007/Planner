﻿using CloudNative.CloudEvents;
using CommonJob;
using CommonJob.MessageBrokerEntities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planar.Common;
using Planar.Common.Exceptions;
using Planar.Common.Helpers;
using Quartz;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Planar
{
    public abstract class PlanarJob : BaseProcessJob<PlanarJob, PlanarJobProperties>
    {
        private readonly IMonitorUtil _monitorUtil;
        private readonly object ConsoleLocker = new();
        private readonly bool _isDevelopment;
        private bool _isHealthCheck;
        private string? _executionException;

        protected PlanarJob(
            ILogger<PlanarJob> logger,
            IJobPropertyDataLayer dataLayer,
            IMonitorUtil monitorUtil) : base(logger, dataLayer)
        {
            _monitorUtil = monitorUtil;

            MqttBrokerService.InterceptingPublishAsync += InterceptingPublishAsync;
            _isDevelopment = string.Equals(AppSettings.Environment, "development", StringComparison.OrdinalIgnoreCase);
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            try
            {
                await Initialize(context, _monitorUtil);
                ValidateProcessJob();
                ValidateExeFile();
                context.CancellationToken.Register(OnCancel);

                var timeout = TriggerHelper.GetTimeoutWithDefault(context.Trigger);
                var startInfo = GetProcessStartInfo();
                var success = StartProcess(startInfo, timeout);
                if (!success)
                {
                    OnTimeout();
                }

                ValidateHealthCheck();
                LogProcessInformation();
                CheckProcessCancel();
                CheckJobErrorReport();
            }
            catch (Exception ex)
            {
                HandleException(context, ex);
            }
            finally
            {
                FinalizeJob(context);
                FinalizeProcess();
                UnregisterMqttBrokerService();
            }
        }

        private void CheckJobErrorReport()
        {
            if (string.IsNullOrWhiteSpace(_executionException)) { return; }
            throw new PlanarJobExecutionException(_executionException);
        }

        private void ValidateExeFile()
        {
            try
            {
                if (!FileExtentionIsExe(Filename))
                {
                    throw new PlanarException($"process filename '{Filename}' must have 'exe' extention");
                }
            }
            catch (Exception ex)
            {
                var source = nameof(ValidateExeFile);
                _logger.LogError(ex, "Fail at {Source}. Message: {Message}", source, ex.Message);
                MessageBroker.AppendLog(LogLevel.Error, $"Fail at {source}. {ex.Message}");
                throw;
            }
        }

        private static bool FileExtentionIsExe(string filename)
        {
            const string exe = ".exe";
            var fi = new FileInfo(filename);
            return string.Equals(fi.Extension, exe);
        }

        private static byte GetCloudEventByteValue(CloudEvent cloudEvent)
        {
            var data = ValidateCloudEventData(cloudEvent.Data);

            if (byte.TryParse(data, out var value))
            {
                return value;
            }
            else
            {
                throw new PlanarJobException($"Message broker channels '{cloudEvent.Type}' has invalid byte value '{cloudEvent.Data}'");
            }
        }

        private static string GetCloudEventStringValue(CloudEvent cloudEvent)
        {
            var data = ValidateCloudEventData(cloudEvent.Data);
            return data;
        }

        private static T GetCloudEventEntityValue<T>(CloudEvent cloudEvent)
            where T : class, new()
        {
            var data = ValidateCloudEventData(cloudEvent.Data);
            var log = JsonConvert.DeserializeObject<T>(data);
            return log
                ?? throw new PlanarJobException($"Message broker channels '{cloudEvent.Type}' has invalid '{typeof(T).Name}' value '{data}'");
        }

        private static int GetCloudEventInt32Value(CloudEvent cloudEvent)
        {
            var data = ValidateCloudEventData(cloudEvent.Data);

            if (int.TryParse(data, out var value))
            {
                return value;
            }
            else
            {
                throw new PlanarJobException($"Message broker channels '{cloudEvent.Type}' has invalid integer value '{cloudEvent.Data}'");
            }
        }

        private static string ValidateCloudEventData(object? data)
        {
            if (data == null)
            {
                throw new PlanarJobException("message broker has empty data");
            }

            var result = data.ToString();
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new PlanarJobException("message broker has empty data");
            }

            return result;
        }

        private void CheckProcessCancel()
        {
            if (_processKilled)
            {
                throw new PlanarJobException($"process '{Filename}' was stopped at {DateTimeOffset.Now}");
            }
        }

        private void ValidateHealthCheck()
        {
            try
            {
                if (_isHealthCheck) { return; }

                if (_output.Length > 0)
                {
                    MessageBroker.AppendLogRaw(_output.ToString());
                }
                else
                {
                    var log = new LogEntity
                    {
                        Level = LogLevel.Warning,
                        Message = "No health check signal from job. Check if the following code: \"PlanarJob.Start<TJob>();\" exists in startup of your console project"
                    };

                    MessageBroker.AppendLog(log);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"fail to handle health chek at {nameof(FinalizeProcess)}");
            }

            if (!_isHealthCheck)
            {
                throw new PlanarJobException("no health check signal from job. See job log for more information");
            }
        }

        private void UnregisterMqttBrokerService()
        {
            try { MqttBrokerService.InterceptingPublishAsync -= InterceptingPublishAsync; } catch { DoNothingMethod(); }
        }

        protected override ProcessStartInfo GetProcessStartInfo()
        {
            var bytes = Encoding.UTF8.GetBytes(MessageBroker.Details);
            var base64String = Convert.ToBase64String(bytes);
            var startInfo = base.GetProcessStartInfo();
            startInfo.Arguments = $"--planar-service-mode --context {base64String}";
            startInfo.StandardErrorEncoding = Encoding.UTF8;
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            return startInfo;
        }

        private void InterceptingPublishAsync(object? sender, CloudEventArgs e)
        {
            try
            {
                if (e.ClientId == FireInstanceId)
                {
                    InterceptingPublishAsyncInner(e);
                }
            }
            catch (PlanarJobException ex)
            {
                _logger.LogCritical("Fail intercepting published message on MQTT broker event. {Error}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fail intercepting published message on MQTT broker event");
            }
        }

        private void WriteToConsole(LogEntity logEntity)
        {
            if (_isDevelopment)
            {
                lock (ConsoleLocker)
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.Out.WriteLine($" - {logEntity.Message}");
                    Console.ResetColor();
                }
            }
        }

        private void InterceptingPublishAsyncInner(CloudEventArgs e)
        {
            if (!Enum.TryParse<MessageBrokerChannels>(e.CloudEvent.Type, ignoreCase: true, out var channel))
            {
                _logger.LogError("Message broker channels '{Type}' is not valid", e.CloudEvent.Type);
                return;
            }

            int iValue;
            KeyValueObject kv;
            switch (channel)
            {
                case MessageBrokerChannels.AddAggregateException:
                    var ex = GetCloudEventEntityValue<ExceptionDto>(e.CloudEvent);
                    MessageBroker.AddAggregateException(ex);
                    break;

                case MessageBrokerChannels.AppendLog:
                    var log = GetCloudEventEntityValue<LogEntity>(e.CloudEvent);
                    MessageBroker.AppendLog(log);
                    WriteToConsole(log);
                    break;

                case MessageBrokerChannels.IncreaseEffectedRows:
                    iValue = GetCloudEventInt32Value(e.CloudEvent);
                    MessageBroker.IncreaseEffectedRows(iValue);
                    break;

                case MessageBrokerChannels.SetEffectedRows:
                    iValue = GetCloudEventInt32Value(e.CloudEvent);
                    MessageBroker.SetEffectedRows(iValue);
                    break;

                case MessageBrokerChannels.PutJobData:
                    kv = GetCloudEventEntityValue<KeyValueObject>(e.CloudEvent);
                    MessageBroker.PutJobDataAction(kv);
                    break;

                case MessageBrokerChannels.PutTriggerData:
                    kv = GetCloudEventEntityValue<KeyValueObject>(e.CloudEvent);
                    MessageBroker.PutTriggerData(kv);
                    break;

                case MessageBrokerChannels.UpdateProgress:
                    var progress = GetCloudEventByteValue(e.CloudEvent);
                    MessageBroker.UpdateProgress(progress);
                    break;

                case MessageBrokerChannels.ReportException:
                    _executionException = GetCloudEventStringValue(e.CloudEvent);
                    break;

                case MessageBrokerChannels.HealthCheck:
                    _isHealthCheck = true;
                    UnsubscribeOutput();
                    break;

                default:
                    _logger.LogWarning("PlanarJob intercepting published message with unsupported channel {Channel}", channel);
                    break;
            }
        }
    }
}