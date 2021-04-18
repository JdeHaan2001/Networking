namespace shared
{
    public class GetMessage : ISerializable
    {
        public string text { get; private set; }
        public int id { get; private set; }

        public GetMessage() { }
        public GetMessage(string pText, int pID)
        {
            text = pText;
            id = pID;
        }

        public void Serialize(Packet pPacket)
        {
            pPacket.Write(text);
            pPacket.Write(id);
        }

        public void Deserialize(Packet pPacket)
        {
            text = pPacket.ReadString();
            id = pPacket.ReadInt();
        }
    }
}
