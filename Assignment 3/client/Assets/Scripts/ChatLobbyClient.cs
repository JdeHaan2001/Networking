using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/**
 * The main ChatLobbyClient where you will have to do most of your work.
 * 
 * @author J.C. Wichman
 */
public class ChatLobbyClient : MonoBehaviour
{
    //reference to the helper class that hides all the avatar management behind a blackbox
    private AvatarAreaManager _avatarAreaManager;
    //reference to the helper class that wraps the chat interface
    private PanelWrapper _panelWrapper;

    [SerializeField] private string _server = "localhost";
    [SerializeField] private int _port = 55555;

    [Tooltip("How far away from the center of the scene will we spawn avatars?")]
    public float spawnRange = 10;
    [Tooltip("What is the minimum angle from the center we are spawning the avatar at?")]
    public float spawnMinAngle = 0;
    [Tooltip("What is the maximum angle from the center we are spawning the avatar at?")]
    public float spawnMaxAngle = 180;

    //keep track of the last avatar id to make sure these id's are unique
    private int _lastAvatarId = 0;

    private TcpClient _client;

    private List<AvatarObject> avatarList = null;

    private void Start()
    {
        connectToServer();
        
        //register for the important events
        _avatarAreaManager = FindObjectOfType<AvatarAreaManager>();
        _avatarAreaManager.OnAvatarAreaClicked += onAvatarAreaClicked;

        _panelWrapper = FindObjectOfType<PanelWrapper>();
        _panelWrapper.OnChatTextEntered += onChatTextEntered;
    }


    private void connectToServer()
    {
        try
        {
            _client = new TcpClient();
            _client.Connect(_server, _port);
            Debug.Log("Connected to server.");
        }
        catch (Exception e)
        {
            Debug.Log("Could not connect to server:");
            Debug.Log(e.Message);
        }
    }

    //private void makeNewAvatar()
    //{
    //    try
    //    {
    //        if (_client.Available > 0)
    //        {
    //            byte[] inBytes = StreamUtil.Read(_client.GetStream());
    //            Packet inPacket = new Packet(inBytes);
    //            ISerializable inObj = inPacket.ReadObject();

    //            if (inObj is shared.Avatar) showNewAvatar(inObj as shared.Avatar);
    //        }
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.Log("Could not connect to server:");
    //        Debug.Log(e.Message);
    //    }
    //}

    private void onAvatarAreaClicked(Vector3 pClickPosition)
    {
        Debug.Log("ChatLobbyClient: you clicked on " + pClickPosition);
        //TODO pass data to the server so that the server can send a position update to all clients (if the position is valid!!)
    }

    private void onChatTextEntered(string pText)
    {
        _panelWrapper.ClearInput();
        sendString(pText);
    }

    private void sendString(string pOutString)
    {
        try
        {
            //we are still communicating with strings at this point, this has to be replaced with either packet or object communication
            Debug.Log("Sending:" + pOutString);
            SimpleMessage simpleMessage = new SimpleMessage();
            simpleMessage.SetText(pOutString);
            sendObject(simpleMessage);
        }
        catch (Exception e)
        {
            //for quicker testing, we reconnect if something goes wrong.
            Debug.Log(e.Message);
            _client.Close();
            connectToServer();
        }
    }

    private void sendObject(ISerializable pOutObject)
    {
        try
        {
            Debug.Log($"Sending: {pOutObject}");

            Packet outPacket = new Packet();
            outPacket.Write(pOutObject);

            StreamUtil.Write(_client.GetStream(), outPacket.GetBytes());
        }
        catch (Exception e)
        {
            //for quicker testing, we reconnect if something goes wrong.
            Debug.Log(e.Message);
            _client.Close();
            connectToServer();
        }
    }

    // RECEIVING CODE

    private void Update()
    {
        try
        {
            if (_client.Available > 0)
            {
                byte[] inBytes = StreamUtil.Read(_client.GetStream());
                Packet inPacket = new Packet(inBytes);
                ISerializable inObj = inPacket.ReadObject();

                if (inObj is GetMessage) handleMessage(inObj as GetMessage);
                else if (inObj is GetAvatars) handleNewAvatar(inObj as GetAvatars);
                else if (inObj is HeartBeat) { }
                else if (inObj is RemoveAvatar) removeAvatar(inObj as RemoveAvatar);
            }
        }
        catch (Exception e)
        {
            //for quicker testing, we reconnect if something goes wrong.
            Debug.Log(e.Message);
            _client.Close();
            connectToServer();
        }
    }

    private void handleNewAvatar(GetAvatars pGetAvatars)
    {
        foreach (AvatarObject avatar in pGetAvatars.AvatarList)
        {
            if (_avatarAreaManager.HasAvatarView(avatar.id))
                continue;
            else
            {
                AvatarView avatarView = _avatarAreaManager.AddAvatarView(avatar.id);
                avatarView.transform.localPosition = new Vector3(avatar.xPos, avatar.yPos, avatar.zPos);
                avatarView.SetSkin(avatar.skindID);
                continue;
            }
        }
    }

    private void handleMessage(GetMessage pGetMessage)
    {
        showMessage(pGetMessage.text, pGetMessage.id);
    }

    private void showMessage(string pText, int pID)
    {

        AvatarView avatarView = _avatarAreaManager.GetAvatarView(pID);
        avatarView.Say(pText);

        #region Given Code
        //List<int> allAvatarIds = _avatarAreaManager.GetAllAvatarIds();

        //if (allAvatarIds.Count == 0)
        //{
        //    Debug.Log("No avatars available to show text through:" + pText);
        //    return;
        //}

        //int randomAvatarId = allAvatarIds[UnityEngine.Random.Range(0, allAvatarIds.Count)];
        //AvatarView avatarView = _avatarAreaManager.GetAvatarView(randomAvatarId);
        //avatarView.Say(pText);
        #endregion
    }

    private void removeAvatar(RemoveAvatar pRemoveAvatar)
    {
        _avatarAreaManager.RemoveAvatarView(pRemoveAvatar.id);
    }

}
