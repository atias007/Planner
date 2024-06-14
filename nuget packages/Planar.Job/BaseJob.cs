﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Job.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Planar.Job
{
    public abstract class BaseJob
    {
        private BaseJobFactory _baseJobFactory = null!;
        private IConfiguration _configuration = null!;
        private Version? _version;
        private bool _inConfiguration;
        private JobExecutionContext _context = new JobExecutionContext();
        private Timer? _timer;

        private ILogger _logger = null!;
        private IServiceProvider _provider = null!;

        protected IConfiguration Configuration
        {
            get
            {
                return _configuration;
            }
            private set
            {
                _configuration = value;
            }
        }

        protected TimeSpan JobRunTime => _baseJobFactory.JobRunTime;

        protected ILogger Logger
        {
            get
            {
                return _logger;
            }
            private set
            {
                _logger = value;
            }
        }

        protected IServiceProvider ServiceProvider => _provider;

        protected Version? Version
        {
            get { return _version; }
            set
            {
                if (!_inConfiguration)
                {
                    throw new PlanarJobException("Version can be set only in Configure method");
                }

                _version = value;
            }
        }

        public abstract void Configure(IConfigurationBuilder configurationBuilder, IJobExecutionContext context);

        public abstract Task ExecuteJob(IJobExecutionContext context);

        public abstract void RegisterServices(IConfiguration configuration, IServiceCollection services, IJobExecutionContext context);

        internal void Execute(string json)
        {
            Action<IConfigurationBuilder, IJobExecutionContext> configureAction = Configure;
            Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction = RegisterServices;

            InitializeBaseJobFactory(json);
            InitializeConfiguration(_context, configureAction);
            InitializeDepedencyInjection(_context, _baseJobFactory, registerServicesAction);

            Logger = ServiceProvider.GetRequiredService<ILogger>();

            if (PlanarJob.Mode == RunningMode.Release)
            {
                MqttClient.Start(_context.FireInstanceId, _context.JobPort).Wait();
                SpinWait.SpinUntil(() => MqttClient.IsConnected, TimeSpan.FromSeconds(5));
                if (MqttClient.IsConnected)
                {
                    MqttClient.Ping().Wait();
                    MqttClient.Publish(MessageBrokerChannels.HealthCheck).Wait();
                }
                else
                {
                    throw new PlanarJobException("Fail to initialize message broker");
                }
            }

            var mapper = new JobMapper(_logger);
            mapper.MapJobInstanceProperties(_context, this);
            LogVersion();

            try
            {
                var timeout = _context.TriggerDetails.Timeout;
                if (timeout == null || timeout.Value.TotalSeconds < 1) { timeout = TimeSpan.FromHours(2); }
                var timeoutms = timeout.Value.Add(TimeSpan.FromMinutes(3)).TotalMilliseconds;

                _timer = new Timer(timeoutms);
                _timer.Elapsed += TimerElapsed;
                _timer.Start();

                var task = ExecuteJob(_context);
                Task.WhenAll(task).Wait();
                _timer?.Stop();
                var mapperBack = new JobBackMapper(_logger, _baseJobFactory);
                mapperBack.MapJobInstancePropertiesBack(_context, this);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                MqttClient.Stop().Wait();
                _timer?.Dispose();
            }
        }

        private void LogVersion()
        {
            if (Version == null) { return; }
            Logger.LogInformation("job version: {Version}", Version);
        }

        private void HandleException(Exception ex)
        {
            if (ex is AggregateException aggregateException && aggregateException.InnerExceptions.Count > 0)
            {
                HandleException(aggregateException.InnerExceptions[0]);
                return;
            }

            var text = _baseJobFactory.ReportException(ex);
            if (PlanarJob.Mode == RunningMode.Debug)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(text);
                Console.ResetColor();
            }
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var ex = new PlanarJobException("Execution timeout. Terminate application");
                HandleException(ex);
            }
            catch
            {
                // *** DO NOTHING ***
            }

            try
            {
                MqttClient.Stop().Wait();
                _timer?.Dispose();
            }
            catch
            {
                // *** DO NOTHING ***
            }

            Environment.Exit(-1);
        }

        internal UnitTestResult ExecuteUnitTest(
            string json,
            Action<IConfigurationBuilder, IJobExecutionContext> configureAction,
            Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction)

        {
            InitializeBaseJobFactory(json);
            InitializeConfiguration(_context, configureAction);
            InitializeDepedencyInjection(_context, _baseJobFactory, registerServicesAction);

            Logger = ServiceProvider.GetRequiredService<ILogger>();
            LogVersion();
            ExecuteJob(_context).Wait();

            var result = new UnitTestResult
            {
                EffectedRows = EffectedRows,
                Log = BaseLogger.LogText
            };

            return result;
        }

        protected void AddAggregateException(Exception ex, int maxItems = 25)
        {
            _baseJobFactory.AddAggregateException(ex, maxItems);
        }

        protected void CheckAggragateException()
        {
            _baseJobFactory.CheckAggragateException();
        }

        protected int ExceptionCount => _baseJobFactory.ExceptionCount;

        protected int? EffectedRows
        {
            get { return _baseJobFactory.EffectedRows; }
            set { _baseJobFactory.EffectedRows = value; }
        }

        protected DateTime Now()
        {
            return _baseJobFactory.Now();
        }

        protected void PutJobData(string key, object? value)
        {
            var data = _context.JobDetails.JobDataMap;
            if (data.Count >= Consts.MaximumJobDataItems && !ContainsKey(key, data))
            {
                throw new PlanarJobException($"Job data items exceeded maximum limit of {Consts.MaximumJobDataItems}");
            }

            _baseJobFactory.PutJobData(key, value);
        }

        protected void PutTriggerData(string key, object? value)
        {
            var data = _context.TriggerDetails.TriggerDataMap;
            if (data.Count >= Consts.MaximumJobDataItems && !ContainsKey(key, data))
            {
                throw new PlanarJobException($"Trigger data items exceeded maximum limit of {Consts.MaximumJobDataItems}");
            }

            _baseJobFactory.PutTriggerData(key, value);
        }

        private bool ContainsKey(string key, IDataMap data)
        {
            return data.Any(k => string.Equals(k.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        protected void UpdateProgress(byte value)
        {
            _baseJobFactory.UpdateProgress(value);
        }

        protected void UpdateProgress(int current, int total)
        {
            _baseJobFactory.UpdateProgress(current, total);
        }

        private static void FilterJobData(IDataMap dictionary)
        {
            var toRemove = dictionary
                .Where(k => !Consts.IsDataKeyValid(k.Key))
                .Select(pair => pair.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                ((DataMap)dictionary).Remove(key);
            }
        }

        private void InitializeBaseJobFactory(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    Converters =
                    {
                        new TypeMappingConverter<IJobDetail, JobDetail>(),
                        new TypeMappingConverter<ITriggerDetail, TriggerDetail>(),
                        new TypeMappingConverter<IKey, Key>(),
                        new DataMapConvertor()
                    }
                };

                var ctx = JsonSerializer.Deserialize<JobExecutionContext>(json, options) ??
                    throw new PlanarJobException("Fail to initialize JobExecutionContext from json (error 7379)");

                _baseJobFactory = new BaseJobFactory(ctx);

                FilterJobData(ctx.MergedJobDataMap);
                FilterJobData(ctx.JobDetails.JobDataMap);
                FilterJobData(ctx.TriggerDetails.TriggerDataMap);

                // Debug mode need to manually read settings file/s
                if (PlanarJob.Mode == RunningMode.Debug)
                {
                    var settings = JobSettingsLoader.LoadJobSettings(null, ctx.JobSettings);
                    ctx.JobSettings = new Dictionary<string, string?>(settings);
                }

                _context = ctx;
            }
            catch (Exception ex)
            {
                throw new PlanarJobException("Fail to deserialize job execution context at BaseJob.InitializeBaseJobFactory(string)", ex);
            }
        }

        private void InitializeConfiguration(JobExecutionContext context, Action<IConfigurationBuilder, IJobExecutionContext> configureAction)
        {
            _inConfiguration = true;
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(context.JobSettings);
            configureAction.Invoke(builder, context);
            Configuration = builder.Build();
            _inConfiguration = false;
        }

        private void InitializeDepedencyInjection(JobExecutionContext context, BaseJobFactory baseJobFactory, Action<IConfiguration, IServiceCollection, IJobExecutionContext> registerServicesAction)
        {
            var services = new ServiceCollection();
            services.AddSingleton(Configuration);
            services.AddSingleton<IJobExecutionContext>(context);
            services.AddSingleton<IBaseJob>(baseJobFactory);
            services.AddSingleton<ILogger, PlanarLogger>();
            services.AddSingleton(typeof(ILogger<>), typeof(PlanarLogger<>));
            registerServicesAction.Invoke(Configuration, services, context);
            _provider = services.BuildServiceProvider();
        }
    }
}