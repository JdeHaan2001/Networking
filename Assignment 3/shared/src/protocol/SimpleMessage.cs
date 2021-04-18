namespace shared
{
    public class SimpleMessage : ISerializable
    {
        string Text;

        public void Serialize(Packet pPacket)
        {
            pPacket.Write(Text);
        }

        public void Deserialize(Packet pPacket)
        {
            Text = pPacket.ReadString();
        }

        public void SetText(string pText)
        {
            Text = pText;
        }

        public string GetText()
        {
            return Text;
        }
    }
}
