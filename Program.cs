using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Inetlab.SMPP;
using Inetlab.SMPP.Common;

namespace MgSmppClientCsSample
{
    class Program
    {
        private static Stopwatch appSw;
        private static SmppClient smppClient;
        private static string serverIp = "162.253.16.28";
        private static int port = 18910;
        private static string systemId, password;
        private static string from, to, textMessage;

        static async Task Main(string[] args)
        {
            appSw = Stopwatch.StartNew();
            Console.WriteLine("#############################");
            Console.WriteLine("SMPP Client demo");
            Console.WriteLine("To exit, press CTRL+C");
            Console.WriteLine("#############################");

            Console.WriteLine("Initializing SMPP client..");
            smppClient = new SmppClient();
            InitializeEvents();
            Console.WriteLine("--SMPP client initialized");

            await ConnectAsync();
            GetAuthInput();
            await BindAsync();
        }

        private static void InitializeEvents()
        {
            smppClient.evDeliverSm += (a, data) =>
            {
                Console.WriteLine($"----Delivery report received: {data.Receipt.ToString()}");
            };
        }

        private static async Task ConnectAsync()
        {
            Stopwatch sw = Stopwatch.StartNew();
            Console.WriteLine("Connecting to MiniGateway SMPP server..");
            var connected = await smppClient.Connect(serverIp, port);
            if (!connected)
            {
                Console.WriteLine("--Fail to connect, press any key to try again. Press CTRL+C to exit");
                Console.ReadKey(true);
                await ConnectAsync();
            }
            else
            {
                Console.WriteLine($"--SMPP server connected [{sw.Elapsed}]");
            }
        }


        private static void GetAuthInput()
        {
            Console.WriteLine("Please enter your credential");
            Console.Write("--System ID: ");
            ReadInput(InputType.Username);
            Console.Write("--Password: ");
            ReadInput(InputType.Password);
        }

        private static async Task BindAsync()
        {
            Console.WriteLine($"Authenticating ({systemId}:{password})");
            var resp = await smppClient.Bind(systemId, password);

            if (resp.Header.Status == CommandStatus.ESME_ROK)
            {
                Console.WriteLine($"--Account authenticated. Welcome '{systemId}'!");
                await SendAsync();
            }
            else
            {
                Console.WriteLine($"--Authentication failed. BindResp: { resp.Header.Status }");
                Console.WriteLine($"--Press any key to re-enter your credential, or CTRL+C to exit");
                Console.ReadKey(true);
                GetAuthInput();
                await ConnectAsync();
                await BindAsync();
            }
        }


        private static async Task SendAsync()
        {
            Console.WriteLine($"INFO: After sending message, please wait for the delivery report to arrived before exiting app. Then, you may unbind & exit by pressing any key.");
            Console.WriteLine("To send a message, enter all required information below");

            GetMsgInput();

            var builder = SMS.ForSubmit().From(from).To(to).Text(textMessage).DeliveryReceipt();

            Console.WriteLine($"Submitting..");
            Stopwatch sw = Stopwatch.StartNew();
            var resp = await smppClient.Submit(builder);
            sw.Stop();

            Console.WriteLine($"--Submit finished [Takes {sw.Elapsed}]");
            Console.WriteLine($"----SubmitSmResp: {resp[0]}");

            if (resp[0].Header.Status != CommandStatus.ESME_ROK)
                Console.WriteLine("----Sorry, there are no delivery report for this message. You may press any key now to unbind & exit");

            Console.ReadKey();
            await UnbindAsync();
            appSw.Stop();
            Console.WriteLine($"Runtime: {appSw.Elapsed}");
            Console.WriteLine("#############################");
        }

        private static void GetMsgInput()
        {
            //Console.WriteLine("Please enter your credential");
            Console.Write("--Source Address: ");
            ReadInput(InputType.From);
            Console.Write("--Destination Address: ");
            ReadInput(InputType.To);
            Console.Write("--Text Message: ");
            ReadInput(InputType.TextMessage);
        }


        private static async Task UnbindAsync()
        {
            Console.WriteLine("Unbinding client from MiniGateway..");
            var resp = await smppClient.UnBind();
            Console.WriteLine($"--UnbindResp: {resp}");
        }

        #region Helper

        private static void ReadInput(InputType inputType)
        {
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                switch (inputType)
                {
                    case InputType.Username: Console.Write("--System ID: "); break;
                    case InputType.Password: Console.Write("--Password: "); break;
                    case InputType.From: Console.Write("--Source Address: "); break;
                    case InputType.To: Console.Write("--Destination Address: "); break;
                    case InputType.TextMessage: Console.Write("--Message Text: "); break;
                }
                ReadInput(inputType);
            }
            else
            {
                switch (inputType)
                {
                    case InputType.Username: systemId = input; break;
                    case InputType.Password: password = input; break;
                    case InputType.From: from = input; break;
                    case InputType.To: to = input; break;
                    case InputType.TextMessage: textMessage = input; break;
                }
            }
        }

        #endregion

    }
}
