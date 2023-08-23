﻿using Azure.Messaging.ServiceBus;
using Mango.Services.EmailAPI.Models.Dto;
using Mango.Services.EmailAPI.Services;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace Mango.Services.EmailAPI.Messaging
{
    public class AzureServiceBusConsumer : IAzureServiceBusConsumer
    {
        private readonly string serviceBusConnectionString;
        private readonly string emailCartQueue;
        private readonly IConfiguration _configuration;
        private readonly string registerUserQueue;

        private ServiceBusProcessor _emailCartProcessor;
        private ServiceBusProcessor _registerUserProcessor;

        private readonly EmailService _emailService;

        public AzureServiceBusConsumer(IConfiguration configuration, EmailService emailService)
        {
            _configuration = configuration;
            serviceBusConnectionString = _configuration.GetValue<string>("ServiceBusConnectionString");
            emailCartQueue = _configuration.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue");
            registerUserQueue = _configuration.GetValue<string>("TopicAndQueueNames:RegisterUserQueue");

            var client = new ServiceBusClient(serviceBusConnectionString);
            _emailCartProcessor = client.CreateProcessor(emailCartQueue);
            _registerUserProcessor = client.CreateProcessor(registerUserQueue);
            _emailService = emailService;
        }

        public async Task Start()
        {
            _emailCartProcessor.ProcessMessageAsync += OnEmailCartRequestReceiver;
            _emailCartProcessor.ProcessErrorAsync += ErrorHandler;
            await _emailCartProcessor.StartProcessingAsync();

            _registerUserProcessor.ProcessMessageAsync += OnUserRegisterRequestReceiver;
            _registerUserProcessor.ProcessErrorAsync += ErrorHandler;
            await _registerUserProcessor.StartProcessingAsync();
        }
              
        public async Task Stop()
        {
            await _emailCartProcessor.StopProcessingAsync();
            await _emailCartProcessor.DisposeAsync();
            
            await _registerUserProcessor.StopProcessingAsync();
            await _registerUserProcessor.DisposeAsync();
        }

        private async Task OnEmailCartRequestReceiver(ProcessMessageEventArgs args)
        {
            // this is where we will receive message
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            CartDto objMessage = JsonConvert.DeserializeObject<CartDto>(body);
            try
            {
                //TODO - try to log email
                await _emailService.EmailCartAndLog(objMessage);
                await args.CompleteMessageAsync(args.Message);
            }
            catch
            {
                throw;
            }
        }

        private async Task OnUserRegisterRequestReceiver(ProcessMessageEventArgs args)
        {
            // this is where we will receive message
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            var email = JsonConvert.DeserializeObject<string>(body);
            try
            {
                //TODO - try to log email
                await _emailService.RegisterUserEmailAndLog(email);
                await args.CompleteMessageAsync(args.Message);
            }
            catch
            {
                throw;
            }
        }

        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }

    }
}