﻿using CommonJob;
using Microsoft.Extensions.Logging;
using Planar.Common;
using Planar.Common.Helpers;
using Quartz;
using RestSharp;
using RestSharp.Authenticators;
using System.Diagnostics;
using System.Net;

namespace Planar
{
    public class RestJob : BaseCommonJob<RestJob, RestJobProperties>
    {
        public RestJob(ILogger<RestJob> logger, IJobPropertyDataLayer dataLayer) : base(logger, dataLayer)
        {
        }

        public override async Task Execute(IJobExecutionContext context)
        {
            try
            {
                await Initialize(context);
                ValidateRestJob();
                var task = Task.Run(() => ExecuteRest(context));
                await WaitForJobTask(context, task);
            }
            catch (Exception ex)
            {
                HandleException(context, ex);
            }
            finally
            {
                FinalizeJob(context);
            }
        }

        private async Task ExecuteRest(IJobExecutionContext context)
        {
            var timeout = TriggerHelper.GetTimeout(context.Trigger) ?? TimeSpan.FromMinutes(30);

            var options = InitializeOptions(timeout);
            SetProxy(options);
            SetAuthentication(options);
            var client = new RestClient(options);
            RestRequest request = InitializeRequest();
            SetHeaders(request);
            SetFormData(request);
            SetBody(context, request);

            // Execute Rest
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await client.ExecuteAsync(request, context.CancellationToken);
            stopwatch.Stop();

            LogExecution(stopwatch, response);
            HandleFailure(response);
        }

        private void SetAuthentication(RestClientOptions options)
        {
            if (Properties.BasicAuthentication != null)
            {
                var authenticator = new HttpBasicAuthenticator(Properties.BasicAuthentication.Username, Properties.BasicAuthentication.Password);
                options.Authenticator = authenticator;
            }

            if (Properties.JwtAuthentication != null)
            {
                var authenticator = new JwtAuthenticator(Properties.JwtAuthentication.Token);
                options.Authenticator = authenticator;
            }
        }

        private void HandleFailure(RestResponse response)
        {
            if (!response.IsSuccessful)
            {
                MessageBroker.SafeAppendLog(LogLevel.Error, $"Response fail");
                MessageBroker.SafeAppendLog(LogLevel.Error, $"Error Message: {response.ErrorMessage}");

                if (response.ErrorException == null)
                {
                    throw new RestJobException($"rest job fail with response status code: {response.StatusCode} {response.StatusDescription}");
                }
                else
                {
                    throw new RestJobException("rest job fail", response.ErrorException);
                }
            }
        }

        private void LogExecution(Stopwatch stopwatch, RestResponse response)
        {
            MessageBroker.SafeAppendLog(LogLevel.Information, $"Status Code: {response.StatusCode}");
            MessageBroker.SafeAppendLog(LogLevel.Information, $"Status Description: {response.StatusDescription}");
            MessageBroker.SafeAppendLog(LogLevel.Information, $"Response Uri: {response.ResponseUri}");
            MessageBroker.SafeAppendLog(LogLevel.Information, $"Duration: {FormatTimeSpan(stopwatch.Elapsed)}");

            if (Properties.LogResponseContent)
            {
                MessageBroker.SafeAppendLog(LogLevel.Information, $"Response Content: {response.Content}");
            }
        }

        private void SetBody(IJobExecutionContext context, RestRequest request)
        {
            if (!string.IsNullOrEmpty(Properties.BodyFile))
            {
                var filename = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Jobs, Properties.Path, Properties.BodyFile);
                var body = File.ReadAllText(filename);

                foreach (var item in context.MergedJobDataMap)
                {
                    var key = $"{{{{{item.Key}}}}}";
                    var value = Convert.ToString(item.Value);
                    if (body.Contains(key))
                    {
                        body = body.Replace(key, value);
                        MessageBroker.AppendLog(LogLevel.Information, $"  - Placeholder '{key}' was replaced by value '{value}'");
                    }
                }

                request.AddJsonBody(body);
            }
        }

        private void SetFormData(RestRequest request)
        {
            if (Properties.FormData != null)
            {
                foreach (var h in Properties.FormData)
                {
                    request.AlwaysMultipartFormData = true;
                    request.AddParameter(h.Key, h.Value);
                }
            }
        }

        private void SetHeaders(RestRequest request)
        {
            if (Properties.Headers != null)
            {
                foreach (var h in Properties.Headers)
                {
                    request.AddHeader(h.Key, h.Value ?? string.Empty);
                }
            }
        }

        private RestRequest InitializeRequest()
        {
            var uri = new Uri(Properties.Url, UriKind.Absolute);
            var request = new RestRequest
            {
                Resource = uri.ToString(),
                Method = Enum.Parse<Method>(Properties.Method, ignoreCase: true)
            };
            return request;
        }

        private void SetProxy(RestClientOptions options)
        {
            if (Properties.Proxy != null)
            {
                var proxy = new WebProxy
                {
                    Address = string.IsNullOrEmpty(Properties.Proxy.Address) ? null : new Uri(Properties.Proxy.Address),
                    UseDefaultCredentials = Properties.Proxy.UseDefaultCredentials,
                    BypassProxyOnLocal = Properties.Proxy.BypassOnLocal
                };

                if (Properties.Proxy.Credentials != null)
                {
                    proxy.Credentials = new NetworkCredential
                    {
                        Domain = Properties.Proxy.Credentials.Domain,
                        Password = Properties.Proxy.Credentials.Password,
                        UserName = Properties.Proxy.Credentials.Username,
                    };
                }

                options.Proxy = proxy;
            }
        }

        private RestClientOptions InitializeOptions(TimeSpan timeout)
        {
            return new RestClientOptions
            {
                MaxTimeout = Convert.ToInt32(timeout.TotalMilliseconds),
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    return Properties.IgnoreSslErrors;
                },
                FollowRedirects = Properties.FollowRedirects,
                MaxRedirects = Properties.MaxRedirects,
                Expect100Continue = Properties.Expect100Continue,
                UserAgent = Properties.UserAgent,
            };
        }

        private void ValidateRestJob()
        {
            try
            {
                ValidateMandatoryString(Properties.Path, nameof(Properties.Path));
                var bodyFullname = FolderConsts.GetSpecialFilePath(
                    PlanarSpecialFolder.Jobs,
                    Properties.Path ?? string.Empty,
                    Properties.BodyFile ?? string.Empty);

                if (!string.IsNullOrEmpty(Properties.BodyFile) && !File.Exists(bodyFullname))
                {
                    throw new RestJobException($"body file '{bodyFullname}' could not be found");
                }
            }
            catch (Exception ex)
            {
                var source = nameof(ValidateRestJob);
                _logger.LogError(ex, "fail at {Source}", source);
                MessageBroker.AppendLog(LogLevel.Error, $"Fail at {source}. {ex.Message}");
                throw;
            }
        }

        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
            {
                return $"{timeSpan:\\(d\\)\\ hh\\:mm\\:ss\\.fffffff}";
            }

            return $"{timeSpan:hh\\:mm\\:ss\\.fffffff}";
        }
    }
}