namespace shared
{
    public class GetClientName : ASerializable
    {
        public string ClientName;
        public override void Serialize(Packet pPacket)
        {
            pPacket.Write(ClientName);
        }

        public override void Deserialize(Packet pPacket)
        {
            pPacket.ReadString();
        }
    }
}
