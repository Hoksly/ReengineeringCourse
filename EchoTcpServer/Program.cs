using EchoTcpServer;

EchoServer server = new EchoServer(5000);
_ = Task.Run(() => server.StartAsync());

using var sender = new UdpTimedSender("127.0.0.1", 60000);
Console.WriteLine("Press any key to stop sending...");
sender.StartSending(5000);

Console.WriteLine("Press 'q' to quit...");
while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q)
{
    // waiting for quit
}

sender.StopSending();
server.Stop();
