public enum Orientation
{
    None,
    Up,
    Right,
    Down,
    Left
}

public enum VoteType
{
    Like = 0,
    Dislike = 1,
    GoldenLike = 2
}

public enum ChatCanal
{
    General = 0,
    PrivateMessage = 1,
    System = 2
}

public enum EmoteType
{
    General = 0,
    Explosive = 1,
    Faces = 2,
    DFL = 3
}

public enum AttitudeType
{
    Celebrate = 0,
    Attack = 1,
    Sword = 2,
    Shield = 3
}

public enum PlayerState
{
    NotConnected = 0,
    InHub = 1,
    InLobby = 2,
    InMapCreator = 3,
    InGame = 4,
    Playing = 5
}