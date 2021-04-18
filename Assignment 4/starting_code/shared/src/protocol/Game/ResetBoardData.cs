namespace shared
{
    public class ResetBoardData : ASerializable
    {
        public TicTacToeBoardData boardData;

        public override void Serialize(Packet pPacket)
        {
            pPacket.Write(boardData);
        }
        public override void Deserialize(Packet pPacket)
        {
            boardData = pPacket.Read<TicTacToeBoardData>();
        }
    }
}
