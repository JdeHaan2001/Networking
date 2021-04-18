namespace shared
{
    public class RemoveAvatar : ISerializable
    {
        public int id { get; private set; }

        public RemoveAvatar() { }
        public RemoveAvatar(int pID)
        {
            id = pID;
        }

        public void Serialize(Packet pPacket)
        {
            pPacket.Write(id);
        }

        public void Deserialize(Packet pPacket)
        {
            id = pPacket.ReadInt();
        }
    }
}
