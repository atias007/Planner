﻿// See https://aka.ms/new-console-template for more information
using RabbitMQCheck;
using RestSharp;
using RestSharp.Authenticators;

Console.WriteLine("Hello, World!");

var options = new RestClientOptions
{
    BaseUrl = new Uri("http://localhost:15672"),
    Authenticator = new HttpBasicAuthenticator("guest", "guest")
};
var client = new RestClient(options);
var request = new RestRequest("/api/queues", Method.Get);
var response = await client.ExecuteAsync<QueueDetails[]>(request);
if (response.IsSuccessful && response.Data != null)
{
    var queues = response.Data;
    foreach (var queue in queues)
    {
        Console.WriteLine($"Queue: {queue.Name}");
        Console.WriteLine($"Messages: {queue.Messages}");
        Console.WriteLine($"Memory: {queue.Memory}");
        Console.WriteLine($"MessageBytes: {queue.MessageBytes}");
        Console.WriteLine($"MessagesReady: {queue.MessagesReady}");
        Console.WriteLine($"MessagesUnacknowledged: {queue.MessagesUnacknowledged}");
        Console.WriteLine($"MessagesDlx: {queue.MessagesDlx}");
        Console.WriteLine($"Consumers: {queue.Consumers}");
        Console.WriteLine();
    }
}
else
{
    Console.WriteLine($"Error: {response.ErrorMessage}");
    Console.WriteLine($"Error: {response.ErrorException}");
}