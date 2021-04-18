using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using shared;
using System.Threading;
using System.Linq;

/**
 * This class implements a simple tcp echo server.
 * Read carefully through the comments below.
 * Note that the server does not contain any sort of error handling.
 */
class TCPServerSample
{
	public static void Main(string[] args)
	{
		TCPServerSample server = new TCPServerSample();
		server.run();
	}

	private TcpListener _listener;
	private Dictionary<TcpClient, AvatarObject> clientDict = new Dictionary<TcpClient, AvatarObject>();
	private List<AvatarObject> allAvatars = new List<AvatarObject>();
	private Random rand = new Random();

	private int lastAvatarID = 0;
	private int minSpawnAngle = 0;
	private int maxSpawnAngle = 360;

	private float spawnDistance = 15;

	private void run()
	{
		Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Server started on port 55555");
		_listener = new TcpListener(IPAddress.Any, 55555);
		_listener.Start();

		while (true)
		{
			processNewClients();
			processExistingClients();
			processFaultyClients();

			//Although technically not required, now that we are no longer blocking, 
			//it is good to cut your CPU some slack
			Thread.Sleep(100);
		}
	}

	private void processNewClients()
	{
		while (_listener.Pending())
		{
			TcpClient client = _listener.AcceptTcpClient();
			//Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Accepted new client with id: {avatarID}.");
			makeNewAvatar(client);
		}
	}

	private void processExistingClients()
	{
		foreach (KeyValuePair<TcpClient, AvatarObject> client in clientDict)
		{
			try
			{
				if (client.Key.Available == 0) continue;

				byte[] inBytes = StreamUtil.Read(client.Key.GetStream());
				Packet inPacket = new Packet(inBytes);
				ISerializable inObj = inPacket.ReadObject();
				Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Received: {inObj} from client id: {client.Value}");

				if (inObj is SimpleMessage) handleMessage(client.Key, inObj as SimpleMessage);
			}
			catch (Exception e)
			{
				Console.WriteLine($"{DateTime.Now.ToString("hh: mm:ss")} {e.Message}");
			}
		}
	}

	private void handleMessage(TcpClient pClient, SimpleMessage pOutObj)
	{
		AvatarObject clientAvatar;
		clientDict.TryGetValue(pClient, out clientAvatar);
		GetMessage getMessage = new GetMessage(pOutObj.GetText(), clientAvatar.id);
		foreach (KeyValuePair<TcpClient, AvatarObject> client in clientDict)
			{
				sendObject(client.Key, getMessage, client.Value.id);
			}
	}

	private void sendObject(TcpClient pClient, ISerializable pOutObj, int pClientID)
	{
		try
		{
			if (pOutObj is HeartBeat) { }
			else Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Sending: {pOutObj} to client id {pClientID}");
			Packet outPacket = new Packet();
			outPacket.Write(pOutObj);
			StreamUtil.Write(pClient.GetStream(), outPacket.GetBytes());
		}
		catch (Exception e)
		{
			Console.WriteLine($"{DateTime.Now.ToString("hh: mm:ss")} Could not send object to client");
			Console.WriteLine(e.Message);
		}
	}

	private void makeNewAvatar(TcpClient pClient)
	{
		int avatarID = lastAvatarID++;

		float randomAngle = rand.Next(minSpawnAngle, maxSpawnAngle) * (float)(Math.PI / 180);
		float randomDistance = rand.Next(0, (int)spawnDistance);

		AvatarObject avatar = new AvatarObject(avatarID, rand.Next(0, 1000), (float)Math.Cos(randomAngle) * randomDistance, 0f, (float)Math.Sin(randomAngle) * randomDistance);
		clientDict.Add(pClient, avatar); 
		Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Added new client to dictionary");

		allAvatars.Add(avatar);

		GetAvatars getAvatars = new GetAvatars();
		getAvatars.AvatarList = allAvatars;

		Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Generated values for new avatar");
		Console.WriteLine($"	ID: {avatar.id}, skinID: {avatar.skindID}, xPos: {avatar.xPos}, yPos: {avatar.yPos}, zPos: {avatar.zPos}");

		foreach (KeyValuePair<TcpClient, AvatarObject> targetClient in clientDict)
		{
			sendObject(targetClient.Key, getAvatars, targetClient.Value.id);
		}
		Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Send new avatar to all clients");
	}

	private void processFaultyClients()
	{
		try
		{
			foreach (KeyValuePair<TcpClient, AvatarObject> client in clientDict)
			{
				HeartBeat heartBeat = new HeartBeat();

				sendObject(client.Key, heartBeat, client.Value.id);
			}
		}
        catch { }

		if (clientDict.Count > 0)
        {
            var keysToRemove = clientDict.Keys.ToList();

            foreach (TcpClient tcpClient in keysToRemove)
            {
                if (!tcpClient.Connected)
                {
                    handleAvatarRemoval(tcpClient);
					continue;
                }
            }
        }
		#region testCode
		//foreach (KeyValuePair<TcpClient, int> client in clientDict)
		//{
		//	try
		//	{
		//		//HeartBeat heartBeat = new HeartBeat("~Heartbeat~", client.Value);
		//		HeartBeat heartBeat = new HeartBeat();

		//		sendObject(client.Key, heartBeat, client.Value);

		//		//Packet outPacket = new Packet();
		//		//outPacket.Write(heartBeat);

		//		//StreamUtil.Write(client.Key.GetStream(), outPacket.GetBytes());
		//	}
		//	catch { }

		//	if (clientDict.Count > 0)
		//	{
		//		var keysToRemove = clientDict.Keys.ToList();

		//		foreach (TcpClient tcpClient in keysToRemove)
		//		{
		//			if (!tcpClient.Connected)
		//			{
		//				int clientID;
		//				clientDict.TryGetValue(tcpClient, out clientID);

		//				handleAvatarRemoval(tcpClient, clientID);
		//				continue;
		//			}
		//		}
		//	}
		//}
		#endregion
	}

    private void handleAvatarRemoval(TcpClient pTcpClient)
	{
		AvatarObject clientAvatar;
		clientDict.TryGetValue(pTcpClient, out clientAvatar);

		RemoveAvatar removeAvatar = new RemoveAvatar(clientAvatar.id);

		foreach (KeyValuePair<TcpClient, AvatarObject> client in clientDict)
		{
			sendObject(client.Key, removeAvatar, client.Value.id);
		}

		allAvatars.Remove(clientAvatar);
		clientDict.Remove(pTcpClient);
		//avatarDict.Remove(clientAvatar.id);

		pTcpClient.Close();
		Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Removed {pTcpClient} with ID: {clientAvatar.id} from server");
		clientDict.Remove(pTcpClient);
	}
}

