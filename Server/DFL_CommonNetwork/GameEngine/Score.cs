namespace DFLCommonNetwork.GameEngine
{
    public class Score
    {
        public int ID { get; set; }
        public string UserName { get; set; }
        public string MapID { get; set; }
        public int UserID { get; set; }
        public long Time { get; set; }
        public int Turns { get; set; }
    }
}