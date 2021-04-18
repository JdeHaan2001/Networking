using shared;
using System;
using System.Collections.Generic;

namespace server
{
	/**
	 * This room runs a single Game (at a time). 
	 * 
	 * The 'Game' is very simple at the moment:
	 *	- all client moves are broadcasted to all clients
	 *	
	 * The game has no end yet (that is up to you), in other words:
	 * all players that are added to this room, stay in here indefinitely.
	 */
	class GameRoom : Room
	{
		public bool IsGameInPlay { get; private set; }

		//wraps the board to play on...
		private TicTacToeBoard _board = new TicTacToeBoard();

		public GameRoom(TCPGameServer pOwner) : base(pOwner)
		{
		}

		public void StartGame (TcpMessageChannel pPlayer1, TcpMessageChannel pPlayer2)
		{
			if (IsGameInPlay) throw new Exception("Programmer error duuuude.");

			IsGameInPlay = true;
			_board.GetBoardData().ResetBoard();
			addMember(pPlayer1);
			addMember(pPlayer2);

            //Resets the board for the clients
            ResetBoardData boardData = new ResetBoardData();
            boardData.boardData = _board.GetBoardData();
            sendToAll(boardData);

            //Sets the playernames at the start of the game
            PlayerNameResponse nameResponse = new PlayerNameResponse();
			nameResponse.player1Name = _server.GetPlayerInfo(pPlayer1).name;
			nameResponse.player2Name = _server.GetPlayerInfo(pPlayer2).name;
			sendToAll(nameResponse);
		}

		protected override void addMember(TcpMessageChannel pMember)
		{
			base.addMember(pMember);

			//notify client he has joined a game room 
			RoomJoinedEvent roomJoinedEvent = new RoomJoinedEvent();
			roomJoinedEvent.room = RoomJoinedEvent.Room.GAME_ROOM;
			pMember.SendMessage(roomJoinedEvent);
		}

		public override void Update()
		{
			//demo of how we can tell people have left the game...
			int oldMemberCount = memberCount;
			base.Update();
			int newMemberCount = memberCount;

			if (oldMemberCount != newMemberCount)
			{
				Log.LogInfo("People left the game...", this);
			}
		}

		protected override void handleNetworkMessage(ASerializable pMessage, TcpMessageChannel pSender)
		{
			if (pMessage is MakeMoveRequest) handleMakeMoveRequest(pMessage as MakeMoveRequest, pSender);
		}

		private void handleMakeMoveRequest(MakeMoveRequest pMessage, TcpMessageChannel pSender)
		{
			//we have two players, so index of sender is 0 or 1, which means playerID becomes 1 or 2
			int playerID = indexOfMember(pSender) + 1;

			//make the requested move (0-8) on the board for the player
			_board.MakeMove(pMessage.move, playerID);

			//and send the result of the boardstate back to all clients
			MakeMoveResult makeMoveResult = new MakeMoveResult();
			makeMoveResult.whoMadeTheMove = playerID;
			makeMoveResult.boardData = _board.GetBoardData();
			sendToAll(makeMoveResult);
			handleWhoWon();
		}

		private void handleWhoWon()
		{
			List<TcpMessageChannel> members = getMembers();
			if (_board.GetBoardData().WhoHasWon() == 1)
			{
				returnToLobby(members[0]);
			}
			else if (_board.GetBoardData().WhoHasWon() == 2)
			{
				returnToLobby(members[1]);
			}
		}

		private void returnToLobby(TcpMessageChannel pWinner)
		{
			safeForEach(_server.GetLobbyRoom().AddMember);

			PlayerInfo playerInfo = _server.GetPlayerInfo(pWinner);
			ChatMessage message = new ChatMessage();
			message.message = $"{playerInfo.name} is the winner";
			sendToAll(message);

			safeForEach(removeMember);
			_board.GetBoardData().ResetBoard();
			IsGameInPlay = false;
		}
	}
}
