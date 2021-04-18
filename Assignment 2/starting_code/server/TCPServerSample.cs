using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using shared;
using System.Threading;
using System.Linq;

class TCPServerSample
{
	/**
	 * This class implements a simple concurrent TCP Echo server.
	 * Read carefully through the comments below.
	 */
	private static Dictionary<TcpClient, string> tcpDict = new Dictionary<TcpClient, string>();

	private static int clientAmount = 0;
	private static TcpListener listener = null;

	public static void Main (string[] args)
	{
		tcpDict.Clear(); // Makes sure that dictionary is empty before using it
		Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Server started on port 55555");

		listener = new TcpListener (IPAddress.Any, 55555);
		listener.Start ();

		while (true)
		{
            processNewClients();
			processExistingClients();
			cleanupFaultyClients();

			//Although technically not required, now that we are no longer blocking, 
			//it is good to cut your CPU some slack
			Thread.Sleep(200);
		}
	}

	private static void processNewClients()
	{
		string clientStartName = "guest";
		while (listener.Pending())
		{
			clientAmount++;

			//tcpDict.Count + 1, the + 1 is because I want the guest count to start from 1 and not 0
            tcpDict.Add(listener.AcceptTcpClient(), $"{clientStartName}{clientAmount}");
			Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Accepted {clientStartName}{clientAmount}");
            try
            {
                foreach (KeyValuePair<TcpClient, string> tcpClient in tcpDict)
                {
                    string outString = string.Empty;
                    if (tcpClient.Value == $"{clientStartName}{clientAmount}")
                    {
                        outString = $"You joined the server as {tcpClient.Value}";

                        byte[] outBytes = Encoding.UTF8.GetBytes(outString);
                        StreamUtil.Write(tcpClient.Key.GetStream(), outBytes);
                        continue;
                    }

                    outString = $"{clientStartName}{clientAmount} has joined the server";
                    byte[] outGoingBytes = Encoding.UTF8.GetBytes(outString);
                    StreamUtil.Write(tcpClient.Key.GetStream(), outGoingBytes);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
		}
		
		#region Given Code
			//First big change with respect to example 001
			//We no longer block waiting for a client to connect, but we only block if we know
			//a client is actually waiting (in other words, we will not block)
			//In order to serve multiple clients, we add that client to a list
			//while (listener.Pending())
			//{
			//    clients.Add(listener.AcceptTcpClient());
			//    Console.WriteLine("Accepted new client.");
			//}
			#endregion
	}

    private static void processExistingClients()
	{
        List<TcpClient> keyCopy = tcpDict.Keys.ToList();
        foreach (TcpClient tcpClient in keyCopy)
        {
            try
            {
                if (tcpClient.Available == 0) continue;
                NetworkStream stream = tcpClient.GetStream();

                byte[] inBytes = StreamUtil.Read(stream);
                string inString = $"{Encoding.UTF8.GetString(inBytes)}";
                Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Received: {inString} from {tcpDict[tcpClient]}");

                if (inString.StartsWith("/"))
                {
                    handleCommands(keyCopy, tcpClient, inString);
                }
                else
                {
                    string newString = $"[{tcpDict[tcpClient]}]: {inString}";
                    Console.WriteLine($"{ DateTime.Now.ToString("hh:mm:ss")} Added username to message. New message is {newString}");
                    sendToAll(tcpClient, newString);
                }
            }
            catch (Exception e)
            {
                Console.Write(e);
            }
        }
    }

    private static void handleCommands(List<TcpClient> pKeyCopy, TcpClient pTcpClient, string pInString)
    {
        string[] words = pInString.Split(' ');
        string command = words[0].ToLower();
        int clientCount = tcpDict.Count;
        if (command == "/setname" || words[0] == "/sn") handleSetNameCommand(pKeyCopy, pTcpClient, words, clientCount);
        else if (command == "/list") handleListCommand(pTcpClient);
        else if (command == "/help") handleHelpCommand(pTcpClient);
    }

    private static void handleHelpCommand(TcpClient pTcpClient)
    {
        string outString = "List of commands:\n";
        outString += "/setname [name] With this command you can change your name. Name cannot be empty and must be unique\n";
        outString += "/list With this command you can get a list of all names of connected clients\n";
        outString += "/help With this command you can get a list of all possible chat commands\n";
        try
        {
            byte[] outBytes = Encoding.UTF8.GetBytes(outString);
            StreamUtil.Write(pTcpClient.GetStream(), outBytes);
        }
        catch (Exception e)
        {
            Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Could not send message to {tcpDict[pTcpClient]}");
            Console.WriteLine(e);
        }
    }

    private static void handleListCommand(TcpClient pTcpClient)
    {
        string outString = $"There are {tcpDict.Count} clients in the server\n";
        foreach (KeyValuePair<TcpClient, string> client in tcpDict)
        {
            outString += $"{client.Value}\n";
        }
        try
        {
            byte[] outBytes = Encoding.UTF8.GetBytes(outString);
            StreamUtil.Write(pTcpClient.GetStream(), outBytes);
            Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Send client list to {tcpDict[pTcpClient]}");
        }
        catch(Exception e)
        {
            Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Could not send message to {tcpDict[pTcpClient]}");
            Console.WriteLine(e);
        }
    }

    private static void handleSetNameCommand(List<TcpClient> pKeyCopy, TcpClient pTcpClient, string[] pWords, int pClientCount)
    {
        Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} {tcpDict[pTcpClient]} requests a namechange");
        if (pWords.Length > 1)
        {
            string name = pWords[1].ToLower();
            int clientIterationCount = 0;
            foreach (TcpClient client in pKeyCopy)
            {
                try
                {
                    if (tcpDict[client] != name)
                    {
                        clientIterationCount++;
                    }
                    else if (tcpDict[client] == name)
                    {
                        Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Name {name} is already taken.");
                        string outString = $"The name {name} is already taken, please choose another name";
                        byte[] outBytes = Encoding.UTF8.GetBytes(outString);
                        StreamUtil.Write(pTcpClient.GetStream(), outBytes);
                    }

                    if (clientIterationCount >= pClientCount)
                    {
                        string tempName = tcpDict[pTcpClient];
                        tcpDict[pTcpClient] = name;
                        Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} {tempName} name changed to {name}");
                        string outString = $"You have changed your name to {name}";
                        byte[] outBytes = Encoding.UTF8.GetBytes(outString);
                        StreamUtil.Write(pTcpClient.GetStream(), outBytes);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        else
        {
            string outString = $"Your name can not be empty. Try /help for all server commands";
            byte[] outBytes = Encoding.UTF8.GetBytes(outString);
            StreamUtil.Write(pTcpClient.GetStream(), outBytes);
        }
    }

    private static void sendToAll(TcpClient tcpClient, string inString)
    {
        try
        {
            byte[] outBytes = Encoding.UTF8.GetBytes(inString);

            foreach (KeyValuePair<TcpClient, string> targetClient in tcpDict)
            {
                StreamUtil.Write(targetClient.Key.GetStream(), outBytes);
                Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} {tcpDict[tcpClient]} sent: {inString} to {targetClient.Value}");
            }
        }
        catch (Exception e)
        {
            Console.Write(e);
        }
    }

    private static void cleanupFaultyClients()
    {
        try
        {
            //Sends an empty message to make sure client is still connected to the server
            foreach (KeyValuePair<TcpClient, string> tcpClient in tcpDict)
            {
                string heartBeat = "~Hearbeat~";
                byte[] outBytes = Encoding.UTF8.GetBytes(heartBeat);
                StreamUtil.Write(tcpClient.Key.GetStream(), outBytes);
            }
        }
        catch {}
        if (tcpDict.Count > 0)
        {
            var keysToRemove = tcpDict.Keys.ToList();
            foreach (TcpClient tcpClient in keysToRemove)
            {
                if (!tcpClient.Connected)
                {
                    tcpClient.Close();
                    Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Removed {tcpDict[tcpClient]} from server");
                    tcpDict.Remove(tcpClient);
                }
            }
        }
    }
}