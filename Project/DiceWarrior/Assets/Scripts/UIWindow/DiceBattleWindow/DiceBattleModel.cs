using cfg;
using UnityEngine;

public sealed class DiceBattleModel
{
    public DiceBattleModel(DiceBattle config)
    {
        EnemyName = string.IsNullOrEmpty(config.EnemyName) ? "Enemy" : config.EnemyName;
        PlayerMaxHp = Mathf.Max(0, config.PlayerHp);
        PlayerHp = PlayerMaxHp;
        PlayerAttack = Mathf.Max(0, config.PlayerAttack);
        EnemyMaxHp = Mathf.Max(0, config.EnemyHp);
        EnemyHp = EnemyMaxHp;
        EnemyAttack = Mathf.Max(0, config.EnemyAttack);
        DiceSides = config.DiceSides < 2 ? 6 : config.DiceSides;
        CoinReward = Mathf.Max(0, config.CoinReward);
    }

    public string EnemyName { get; }
    public int PlayerMaxHp { get; }
    public int PlayerHp { get; private set; }
    public int PlayerAttack { get; }
    public int EnemyMaxHp { get; }
    public int EnemyHp { get; private set; }
    public int EnemyAttack { get; }
    public int DiceSides { get; }
    public int CoinReward { get; }
    public int PlayerDice { get; private set; }
    public int EnemyDice { get; private set; }
    public int Round { get; private set; }
    public bool IsFinished => PlayerHp <= 0 || EnemyHp <= 0;
    public bool IsPlayerWin => EnemyHp <= 0 && PlayerHp > 0;

    public string RollRound()
    {
        if (IsFinished)
        {
            return "战斗已结束";
        }

        Round++;
        PlayerDice = Random.Range(1, DiceSides + 1);
        EnemyDice = Random.Range(1, DiceSides + 1);
        int diff = Mathf.Abs(PlayerDice - EnemyDice);

        if (PlayerDice > EnemyDice)
        {
            int damage = PlayerAttack + diff;
            EnemyHp = Mathf.Max(0, EnemyHp - damage);
            return $"第{Round}回合：你掷出{PlayerDice}，敌人掷出{EnemyDice}，造成{damage}伤害";
        }

        if (EnemyDice > PlayerDice)
        {
            int damage = EnemyAttack + diff;
            PlayerHp = Mathf.Max(0, PlayerHp - damage);
            return $"第{Round}回合：你掷出{PlayerDice}，敌人掷出{EnemyDice}，受到{damage}伤害";
        }

        return $"第{Round}回合：双方都掷出{PlayerDice}，平局无伤害";
    }
}
