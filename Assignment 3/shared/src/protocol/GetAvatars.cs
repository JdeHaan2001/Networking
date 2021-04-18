using System.Collections.Generic;
namespace shared
{
    public class GetAvatars : ISerializable
    {
        public List<AvatarObject> AvatarList { get; set; }
        public void Serialize(Packet pPacket)
        {
            int count = (AvatarList == null ? 0 : AvatarList.Count);

            pPacket.Write(count);

            for (int i = 0; i < count; i++)
            {
                pPacket.Write(AvatarList[i]);
            }

        }

        public void Deserialize(Packet pPacket)
        {
            AvatarList = new List<AvatarObject>();

            int count = pPacket.ReadInt();

            for (int i = 0; i < count; i++)
            {
                AvatarList.Add(pPacket.Read<AvatarObject>());
            }
        }

    }
}
