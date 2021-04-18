
namespace shared
{
    public class AvatarObject : ISerializable
    {
        public int id { get; private set; }
        public int skindID { get; private set; }
        public float xPos { get; private set; }
        public float yPos { get; private set; }
        public float zPos { get; private set; }

        public AvatarObject() { }
        public AvatarObject(int pID, int pSkinID, float pXPos, float pYPos, float pZPos)
        {
            id = pID;
            skindID = pSkinID;
            xPos = pXPos;
            yPos = pYPos;
            zPos = pZPos;
        }
        public void Serialize(Packet pPacket)
        {
            pPacket.Write(id);
            pPacket.Write(skindID);
            pPacket.Write(xPos);
            pPacket.Write(yPos);
            pPacket.Write(zPos);
        }

        public void Deserialize(Packet pPacket)
        {
            id = pPacket.ReadInt();
            skindID = pPacket.ReadInt();
            xPos = pPacket.ReadFloat();
            yPos = pPacket.ReadFloat();
            zPos = pPacket.ReadFloat();
        }
    }
}
