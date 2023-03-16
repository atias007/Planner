﻿using RestSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.CLI
{
    internal static class RestProxy
    {
        public static string Schema { get; set; } = "http";
        public static string Host { get; set; } = "localhost";
        public static int Port { get; set; } = 2306;

        private static RestClient? _client;
        private static readonly object _lock = new();

        public static void Flush()
        {
            lock (_lock)
            {
                _client = null;
            }
        }

        private static RestClient Proxy
        {
            get
            {
                lock (_lock)
                {
                    if (_client == null)
                    {
                        var options = new RestClientOptions
                        {
                            BaseUrl = BaseUri,
                            MaxTimeout = 10000
                        };

                        _client = new RestClient(options);
                    }

                    return _client;
                }
            }
        }

        private static Uri BaseUri
        {
            get
            {
                return new UriBuilder(Schema, Host, Port).Uri;
            }
        }

        public static async Task<RestResponse<TResponse>> Invoke<TResponse>(RestRequest request, CancellationToken cancellationToken)
        {
            var response = await Proxy.ExecuteAsync<TResponse>(request, cancellationToken);
            return response;
        }

        public static async Task<RestResponse> Invoke(RestRequest request, CancellationToken cancellationToken)
        {
            var response = await Proxy.ExecuteAsync(request, cancellationToken);
            return response;
        }
    }
}