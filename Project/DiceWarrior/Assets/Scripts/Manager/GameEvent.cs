using YangTools;

public class UseBagProp : EventMessageBase
{
    public int id;
    public int num;
}

public class BagPropChange : EventMessageBase
{
    public int propID;
    public float num;//+还是-
}

public class AccountIdChange : EventMessageBase
{
    public string accountId;
}

/// <summary>
/// 进度改变
/// </summary>
public class PressChange : EventMessageBase
{

}
/// <summary>
/// 游戏开始
/// </summary>
public class GameStart : EventMessageBase
{
    public int levelID;
    public string levelName;
}
