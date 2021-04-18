using shared;
using System;
using UnityEngine;

/**
 * 'Chat' state while you are waiting to start a game where you can signal that you are ready or not.
 */
public class LobbyState : ApplicationStateWithView<LobbyView>
{
    [Tooltip("Should we enter the lobby in a ready state or not?")]
    [SerializeField] private bool autoQueueForGame = false;
    private string clientName = null;

    public override void EnterState()
    {
        base.EnterState();

        view.SetLobbyHeading($"Welcome to the Lobby... {clientName}");
        view.ClearOutput();
        view.AddOutput($"Server settings:"+fsm.channel.GetRemoteEndPoint());
        view.SetReadyToggle(autoQueueForGame);

        view.OnChatTextEntered += onTextEntered;
        view.OnReadyToggleClicked += onReadyToggleClicked;

        if (autoQueueForGame)
        {
            onReadyToggleClicked(true);
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        
        view.OnChatTextEntered -= onTextEntered;
        view.OnReadyToggleClicked -= onReadyToggleClicked;
    }

    /**
     * Called when you enter text and press enter.
     */
    private void onTextEntered(string pText)
    {
        view.ClearInput();

        ChatMessage message = new ChatMessage();
        message.message = pText;
        fsm.channel.SendMessage(message); 
    }

    /**
     * Called when you click on the ready checkbox
     */
    private void onReadyToggleClicked(bool pNewValue)
    {
        ChangeReadyStatusRequest msg = new ChangeReadyStatusRequest();
        msg.ready = pNewValue;
        fsm.channel.SendMessage(msg);
    }

    private void addOutput(string pInfo)
    {
        view.AddOutput(pInfo);
    }

    private void sendObject(ASerializable pOutObj)
    {
        try
        {
            Debug.Log($"Sending: {pOutObj}");
            Packet outPacket = new Packet();
            outPacket.Write(pOutObj);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }
    /// //////////////////////////////////////////////////////////////////
    ///                     NETWORK MESSAGE PROCESSING
    /// //////////////////////////////////////////////////////////////////

    private void Update()
    {
        receiveAndProcessNetworkMessages();
    }
    
    protected override void handleNetworkMessage(ASerializable pMessage)
    {
        if (pMessage is ChatMessage) handleChatMessage(pMessage as ChatMessage);
        else if (pMessage is GetClientName) handleGetClientName(pMessage as GetClientName);
        else if (pMessage is RoomJoinedEvent) handleRoomJoinedEvent(pMessage as RoomJoinedEvent);
        else if (pMessage is LobbyInfoUpdate) handleLobbyInfoUpdate(pMessage as LobbyInfoUpdate);
    }

    private void handleGetClientName(GetClientName pMessage)
    {
        clientName = pMessage.ClientName;
    }

    private void handleChatMessage(ChatMessage pMessage)
    {
        //just show the message
        addOutput(pMessage.message);
    }

    private void handleRoomJoinedEvent(RoomJoinedEvent pMessage)
    {
        //did we move to the game room?
        if (pMessage.room == RoomJoinedEvent.Room.GAME_ROOM)
        {
            fsm.ChangeState<GameState>();
        }
    }

    private void handleLobbyInfoUpdate(LobbyInfoUpdate pMessage)
    {
        //update the lobby heading
        view.SetLobbyHeading($"Welcome to the Lobby {clientName} ({pMessage.memberCount} people, {pMessage.readyCount} ready)");
    }

    

}
