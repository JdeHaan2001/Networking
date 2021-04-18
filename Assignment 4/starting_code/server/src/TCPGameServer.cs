using System;
using System.Net.Sockets;
using System.Net;
using shared;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace server {

	/**
	 * Basic TCPGameServer that runs our game.
	 * 
	 * Server is made up out of different rooms that can hold different members.
	 * Each member is identified by a TcpMessageChannel, which can also be used for communication.
	 * In this setup each client is only member of ONE room, but you could change that of course.
	 * 
	 * Each room is responsible for cleaning up faulty clients (since it might involve gameplay, status changes etc).
	 * 
	 * As you can see this setup is limited/lacking:
	 * - only 1 game can be played at a time
	 */
	class TCPGameServer
	{

		public static void Main(string[] args)
		{
			TCPGameServer tcpGameServer = new TCPGameServer();
			tcpGameServer.run();
		}

		//we have 3 different rooms at the moment (aka simple but limited)

		private LoginRoom _loginRoom;	//this is the room every new user joins
		private LobbyRoom _lobbyRoom;	//this is the room a user moves to after a successful 'login'
		private GameRoom _gameRoom;     //this is the room a user moves to when a game is succesfully started
		private List<GameRoom> gameRoomList = new List<GameRoom>();

		//stores additional info for a player
		private Dictionary<TcpMessageChannel, PlayerInfo> _playerInfo = new Dictionary<TcpMessageChannel, PlayerInfo>();

		private TCPGameServer()
		{
			//we have only one instance of each room, this is especially limiting for the game room (since this means you can only have one game at a time).
			_loginRoom = new LoginRoom(this);
			_lobbyRoom = new LobbyRoom(this);
			_gameRoom = new GameRoom(this);
			//gameRoomList.Add(_gameRoom);
		}

		private void run()
		{
			Log.LogInfo("Starting server on port 55555", this, ConsoleColor.Gray);

			//start listening for incoming connections (with max 50 in the queue)
			//we allow for a lot of incoming connections, so we can handle them
			//and tell them whether we will accept them or not instead of bluntly declining them
			TcpListener listener = new TcpListener(IPAddress.Any, 55555);
			listener.Start(50);

			while (true)
			{
				//check for new members	
				if (listener.Pending())
				{
					//get the waiting client
					Log.LogInfo("Accepting new client...", this, ConsoleColor.White);
					TcpClient client = listener.AcceptTcpClient();
					//and wrap the client in an easier to use communication channel
					TcpMessageChannel channel = new TcpMessageChannel(client);
					//and add it to the login room for further 'processing'
					_loginRoom.AddMember(channel);
				}

				//now update every single room
				_loginRoom.Update();
				_lobbyRoom.Update();
				//_gameRoom.Update();
				foreach (GameRoom gameRoom in gameRoomList)
				{
					gameRoom.Update();
				}

				Thread.Sleep(100);

				gameRoomManagement();
			}

		}
		
		//provide access to the different rooms on the server 
		public LoginRoom GetLoginRoom() { return _loginRoom; }
		public LobbyRoom GetLobbyRoom() { return _lobbyRoom; }
		public GameRoom GetGameRoom() { return _gameRoom; }
		public GameRoom StartGameRoom()
		{
			if (gameRoomList.Count > 0)
			{
				int roomIterationCount = 0;
				foreach (GameRoom room in gameRoomList)
				{
					if (room.IsGameInPlay)
					{
						roomIterationCount++;
						continue;
					}
					else
					{
						Log.LogInfo("Existing Game Room is used for new game", this, ConsoleColor.DarkYellow);
						return room;
					}
				}
				if (roomIterationCount == gameRoomList.Count)
				{
					GameRoom gameRoom = new GameRoom(this);
					gameRoomList.Add(gameRoom);
					Log.LogInfo("All Game Rooms are in use. New Game Room is created", this, ConsoleColor.Green);
					return gameRoom;
				}
			}
			else
			{
				GameRoom gameRoom = new GameRoom(this);
				gameRoomList.Add(gameRoom);
				Log.LogInfo("No existing Game Rooms were found. Created new Game Room", this, ConsoleColor.Green);
				return gameRoom;
			}
			Log.LogInfo("Something went wrong with the room creation", this, ConsoleColor.Red);
			return null;
		}

		/**
		 * Returns a handle to the player info for the given client 
		 * (will create new player info if there was no info for the given client yet)
		 */
		public PlayerInfo GetPlayerInfo (TcpMessageChannel pClient)
		{
			if (!_playerInfo.ContainsKey(pClient))
			{
				_playerInfo[pClient] = new PlayerInfo();
			}

			return _playerInfo[pClient];
		}

		/**
		 * Returns a list of all players that match the predicate, e.g. to get a list of 
		 * all players named bob, you would do:
		 *	GetPlayerInfo((playerInfo) => playerInfo.name == "bob");
		 */
		public List<PlayerInfo> GetPlayerInfo(Predicate<PlayerInfo> pPredicate)
		{
			return _playerInfo.Values.ToList<PlayerInfo>().FindAll(pPredicate);
		}

		/**
		 * Should be called by a room when a member is closed and removed.
		 */
		public void RemovePlayerInfo (TcpMessageChannel pClient)
		{
			_playerInfo.Remove(pClient);
		}

		public void AddPlayerInfo(TcpMessageChannel pChannel, PlayerInfo pPlayerInfo)
		{
			if (!_playerInfo.ContainsKey(pChannel))
			{
				_playerInfo.Add(pChannel, pPlayerInfo);
				Console.WriteLine($"{pChannel} added to _playerInfo dictionary");
			}
			else
			{
				Console.WriteLine($"Could not add {pChannel} to _playerInfo dictionary, because key already exists");
			}
		}
		/// <summary>
		/// Makes sure that there won't be too many game rooms that are not used
		/// </summary>
		private void gameRoomManagement()
		{
			int memberCount = _playerInfo.Count;
			int gameRoomCount = gameRoomList.Count;
			// Checks if there are more game rooms than that is needed.
			//if (((memberCount / 2) + 1) >= gameRoomCount) // +1 is because I want there to be at least 1 game room left
			if(gameRoomCount + 1 >= (memberCount / 2))
			{
				List<GameRoom> gameRooms = new List<GameRoom>(gameRoomList);
				foreach (GameRoom room in gameRooms)
				{
					if (!room.IsGameInPlay)
					{
						gameRoomList.Remove(room);
						Log.LogInfo($"Removed un-used Game Room {room} from Game Room list", this, ConsoleColor.Cyan);
					}
					else
					{
						continue;
					}
				}
			}
		}

	}

}


