namespace DFLCommonNetwork.GameEngine
{
    public class GameClient
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Pass { get; set; }
        public string Mail { get; set; }
        public PlayerSave PlayerSave { get; set; }
        public PlayerState State { get; set; }
    }
}