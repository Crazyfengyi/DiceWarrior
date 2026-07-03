using System.Collections.Generic;
using cfg;
using UnityEngine;

public sealed class DiceBattleModel
{
    public enum RoundWinnerType
    {
        Draw = 0,
        Player = 1,
        Enemy = 2
    }

    public sealed class PlayerDieState
    {
        private readonly List<int> faces = new List<int>();

        public PlayerDieState(EquippedDiceSlotData source)
        {
            DieName = source == null ? "空" : source.Name;
            if (source != null && source.Faces != null)
            {
                for (int i = 0; i < source.Faces.Count; i++)
                {
                    faces.Add(source.Faces[i]);
                }
            }

            ResetForNextRound();
        }

        public string DieName { get; }
        public IReadOnlyList<int> Faces => faces;
        public bool IsEmpty => faces.Count == 0;
        public bool HasThrown { get; private set; }
        public int CurrentRoll { get; private set; }
        public int CurrentFaceIndex { get; private set; }

        /// <summary>
        /// 获取骰子的极限区间文案。
        /// </summary>
        public string GetRangeText()
        {
            if (faces.Count == 0)
            {
                return "-";
            }

            int min = int.MaxValue;
            int max = int.MinValue;
            for (int i = 0; i < faces.Count; i++)
            {
                min = Mathf.Min(min, faces[i]);
                max = Mathf.Max(max, faces[i]);
            }

            return $"{min}~{max}";
        }

        /// <summary>
        /// 投出当前骰子。
        /// </summary>
        public bool Throw()
        {
            if (IsEmpty || HasThrown)
            {
                return false;
            }

            CurrentFaceIndex = Random.Range(0, faces.Count);
            CurrentRoll = faces[CurrentFaceIndex];
            HasThrown = true;
            return true;
        }

        /// <summary>
        /// 重投当前骰子。
        /// </summary>
        public bool Reroll()
        {
            if (IsEmpty || !HasThrown)
            {
                return false;
            }

            CurrentFaceIndex = Random.Range(0, faces.Count);
            CurrentRoll = faces[CurrentFaceIndex];
            return true;
        }

        /// <summary>
        /// 重置到下一回合的未投出状态。
        /// </summary>
        public void ResetForNextRound()
        {
            HasThrown = false;
            CurrentRoll = 0;
            CurrentFaceIndex = -1;
        }
    }

    public sealed class EnemyDieState
    {
        public EnemyDieState(int diceSides)
        {
            DiceSides = Mathf.Max(2, diceSides);
        }

        public int DiceSides { get; }
        public int CurrentRoll { get; private set; }

        /// <summary>
        /// 投出敌方骰子。
        /// </summary>
        public void Throw()
        {
            CurrentRoll = Random.Range(1, DiceSides + 1);
        }

        /// <summary>
        /// 清空当前回合结果。
        /// </summary>
        public void ResetForNextRound()
        {
            CurrentRoll = 0;
        }
    }

    private const int SingleDieRerollLimitPerRound = 1;
    private const int AllDiceRerollLimitPerRound = 1;

    private readonly List<PlayerDieState> playerDiceStates = new List<PlayerDieState>();
    private readonly List<EnemyDieState> enemyDiceStates = new List<EnemyDieState>();
    private readonly List<DiceBattleEnemyStatusConfig> enemyStatuses = new List<DiceBattleEnemyStatusConfig>();
    private readonly List<DiceBattleEnemySkillConfig> enemySkillSequence = new List<DiceBattleEnemySkillConfig>();

    public DiceBattleModel(DiceBattle config, IReadOnlyList<EquippedDiceSlotData> playerDiceSlots)
    {
        EnemyName = string.IsNullOrEmpty(config?.EnemyName) ? "Enemy" : config.EnemyName;
        EnemySpriteName = config?.EnemySpriteName ?? string.Empty;
        PlayerMaxHp = Mathf.Max(0, config?.PlayerHp ?? 0);
        PlayerHp = PlayerMaxHp;
        PlayerAttack = Mathf.Max(0, config?.PlayerAttack ?? 0);
        EnemyMaxHp = Mathf.Max(0, config?.EnemyHp ?? 0);
        EnemyHp = EnemyMaxHp;
        EnemyAttack = Mathf.Max(0, config?.EnemyAttack ?? 0);
        CoinReward = Mathf.Max(0, config?.CoinReward ?? 0);

        BuildPlayerDiceStates(playerDiceSlots);
        BuildEnemyDiceStates(config);
        BuildEnemyDisplayConfigs(config);

        CurrentRound = 1;
        RemainingSingleDieRerolls = SingleDieRerollLimitPerRound;
        RemainingAllDiceRerolls = AllDiceRerollLimitPerRound;
        LastMessage = "点击投出开始行动";
    }

    public string EnemyName { get; }
    public string EnemySpriteName { get; }
    public int PlayerMaxHp { get; }
    public int PlayerHp { get; private set; }
    public int PlayerAttack { get; }
    public int EnemyMaxHp { get; }
    public int EnemyHp { get; private set; }
    public int EnemyAttack { get; }
    public int CoinReward { get; }
    public int CurrentRound { get; private set; }
    public int PlayerCurrentResult { get; private set; }
    public int EnemyCurrentResult { get; private set; }
    public int RemainingSingleDieRerolls { get; private set; }
    public int RemainingAllDiceRerolls { get; private set; }
    public string LastMessage { get; private set; }
    public string RoundResolvedMessage { get; private set; }
    public RoundWinnerType RoundWinner { get; private set; }
    public int RoundDamage { get; private set; }
    public int RoundPlayerTotal { get; private set; }
    public int RoundEnemyTotal { get; private set; }
    public IReadOnlyList<PlayerDieState> PlayerDiceStates => playerDiceStates;
    public IReadOnlyList<EnemyDieState> EnemyDiceStates => enemyDiceStates;
    public IReadOnlyList<DiceBattleEnemyStatusConfig> EnemyStatuses => enemyStatuses;
    public DiceBattleEnemySkillConfig CurrentSkill => GetCurrentSkill();
    public bool IsFinished => PlayerHp <= 0 || EnemyHp <= 0;
    public bool IsPlayerWin => EnemyHp <= 0 && PlayerHp > 0;
    public bool IsRoundResolved { get; private set; }
    public bool CanThrowAll => !IsFinished && HasAnyUnthrownPlayerDice();
    public bool CanRerollAll => !IsFinished && RemainingAllDiceRerolls > 0 && HasAnyThrownPlayerDice();
    public bool CanEndTurn => !IsFinished && !IsRoundResolved && HasAnyThrownPlayerDice();

    /// <summary>
    /// 投出单颗玩家骰子。
    /// </summary>
    public bool ThrowPlayerDie(int diceIndex)
    {
        if (!TryGetPlayerDie(diceIndex, out PlayerDieState die) || !die.Throw())
        {
            return false;
        }

        RecalculatePlayerCurrentResult();
        LastMessage = $"{die.DieName} 投出了 {die.CurrentRoll}";
        return true;
    }

    /// <summary>
    /// 投出全部未投出的玩家骰子。
    /// </summary>
    public bool ThrowAllPlayerDice()
    {
        if (!CanThrowAll)
        {
            return false;
        }

        bool changed = false;
        for (int i = 0; i < playerDiceStates.Count; i++)
        {
            changed |= playerDiceStates[i].Throw();
        }

        if (!changed)
        {
            return false;
        }

        RecalculatePlayerCurrentResult();
        LastMessage = $"已投出全部骰子，当前结果 {PlayerCurrentResult}";
        return true;
    }

    /// <summary>
    /// 重投单颗玩家骰子。
    /// </summary>
    public bool RerollPlayerDie(int diceIndex)
    {
        if (RemainingSingleDieRerolls <= 0 || !TryGetPlayerDie(diceIndex, out PlayerDieState die) || !die.Reroll())
        {
            return false;
        }

        RemainingSingleDieRerolls--;
        RecalculatePlayerCurrentResult();
        LastMessage = $"{die.DieName} 重投为 {die.CurrentRoll}";
        return true;
    }

    /// <summary>
    /// 重投全部已投出的玩家骰子。
    /// </summary>
    public bool RerollAllPlayerDice()
    {
        if (!CanRerollAll)
        {
            return false;
        }

        bool changed = false;
        for (int i = 0; i < playerDiceStates.Count; i++)
        {
            if (playerDiceStates[i].HasThrown)
            {
                changed |= playerDiceStates[i].Reroll();
            }
        }

        if (!changed)
        {
            return false;
        }

        RemainingAllDiceRerolls--;
        RecalculatePlayerCurrentResult();
        LastMessage = $"已全部重投，当前结果 {PlayerCurrentResult}";
        return true;
    }

    /// <summary>
    /// 结束玩家行动并结算本回合。
    /// </summary>
    public bool EndPlayerTurn()
    {
        if (!CanEndTurn)
        {
            return false;
        }

        RollEnemyDice();
        ResolveRoundDamage();
        return true;
    }

    /// <summary>
    /// 完成当前回合结算后的推进处理。
    /// </summary>
    public void AdvanceAfterRoundResolution()
    {
        if (!IsRoundResolved || IsFinished)
        {
            return;
        }

        string resolvedMessage = RoundResolvedMessage;
        CurrentRound++;
        ResetRoundForNextTurn();
        LastMessage = $"{resolvedMessage}，进入下一回合";
    }

    /// <summary>
    /// 判断单颗骰子当前是否可重投。
    /// </summary>
    public bool CanRerollSingleDie(int diceIndex)
    {
        return RemainingSingleDieRerolls > 0 &&
            TryGetPlayerDie(diceIndex, out PlayerDieState die) &&
            die.HasThrown &&
            !die.IsEmpty &&
            !IsFinished;
    }

    private void BuildPlayerDiceStates(IReadOnlyList<EquippedDiceSlotData> playerDiceSlots)
    {
        playerDiceStates.Clear();
        if (playerDiceSlots == null)
        {
            return;
        }

        for (int i = 0; i < playerDiceSlots.Count; i++)
        {
            playerDiceStates.Add(new PlayerDieState(playerDiceSlots[i]));
        }
    }

    private void BuildEnemyDiceStates(DiceBattle config)
    {
        enemyDiceStates.Clear();
        if (config?.EnemyDiceFaces == null || config.EnemyDiceFaces.Count == 0)
        {
            enemyDiceStates.Add(new EnemyDieState(config?.DiceSides ?? 6));
            return;
        }

        for (int i = 0; i < config.EnemyDiceFaces.Count; i++)
        {
            enemyDiceStates.Add(new EnemyDieState(config.EnemyDiceFaces[i]));
        }
    }

    private void BuildEnemyDisplayConfigs(DiceBattle config)
    {
        enemyStatuses.Clear();
        enemySkillSequence.Clear();

        Tables tables = GameTableManager.Instance?.Tables;
        if (tables == null || config == null)
        {
            return;
        }

        if (config.EnemyStatusIds != null && tables.DiceBattleEnemyStatusConfigCategory != null)
        {
            for (int i = 0; i < config.EnemyStatusIds.Count; i++)
            {
                DiceBattleEnemyStatusConfig status =
                    tables.DiceBattleEnemyStatusConfigCategory.GetOrDefault(config.EnemyStatusIds[i]);
                if (status != null && status.Enabled)
                {
                    enemyStatuses.Add(status);
                }
            }
        }

        if (config.EnemySkillSequenceIds != null && tables.DiceBattleEnemySkillConfigCategory != null)
        {
            for (int i = 0; i < config.EnemySkillSequenceIds.Count; i++)
            {
                DiceBattleEnemySkillConfig skill =
                    tables.DiceBattleEnemySkillConfigCategory.GetOrDefault(config.EnemySkillSequenceIds[i]);
                if (skill != null && skill.Enabled)
                {
                    enemySkillSequence.Add(skill);
                }
            }
        }
    }

    private bool TryGetPlayerDie(int diceIndex, out PlayerDieState die)
    {
        if (diceIndex >= 0 && diceIndex < playerDiceStates.Count)
        {
            die = playerDiceStates[diceIndex];
            return die != null && !die.IsEmpty;
        }

        die = null;
        return false;
    }

    private bool HasAnyUnthrownPlayerDice()
    {
        for (int i = 0; i < playerDiceStates.Count; i++)
        {
            if (!playerDiceStates[i].IsEmpty && !playerDiceStates[i].HasThrown)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasAnyThrownPlayerDice()
    {
        for (int i = 0; i < playerDiceStates.Count; i++)
        {
            if (!playerDiceStates[i].IsEmpty && playerDiceStates[i].HasThrown)
            {
                return true;
            }
        }

        return false;
    }

    private void RollEnemyDice()
    {
        EnemyCurrentResult = 0;
        for (int i = 0; i < enemyDiceStates.Count; i++)
        {
            enemyDiceStates[i].Throw();
            EnemyCurrentResult += enemyDiceStates[i].CurrentRoll;
        }
    }

    private void ResolveRoundDamage()
    {
        IsRoundResolved = true;
        RoundPlayerTotal = PlayerCurrentResult;
        RoundEnemyTotal = EnemyCurrentResult;
        int diff = Mathf.Abs(PlayerCurrentResult - EnemyCurrentResult);
        if (PlayerCurrentResult > EnemyCurrentResult)
        {
            int damage = PlayerAttack + diff;
            EnemyHp = Mathf.Max(0, EnemyHp - damage);
            RoundWinner = RoundWinnerType.Player;
            RoundDamage = damage;
            RoundResolvedMessage =
                $"第{CurrentRound}回合：玩家 {PlayerCurrentResult} 比 敌人 {EnemyCurrentResult} 高，造成 {damage} 点伤害";
            LastMessage = RoundResolvedMessage;
            return;
        }

        if (EnemyCurrentResult > PlayerCurrentResult)
        {
            int damage = EnemyAttack + diff;
            PlayerHp = Mathf.Max(0, PlayerHp - damage);
            RoundWinner = RoundWinnerType.Enemy;
            RoundDamage = damage;
            RoundResolvedMessage =
                $"第{CurrentRound}回合：敌人 {EnemyCurrentResult} 比 玩家 {PlayerCurrentResult} 高，造成 {damage} 点伤害";
            LastMessage = RoundResolvedMessage;
            return;
        }

        RoundWinner = RoundWinnerType.Draw;
        RoundDamage = 0;
        RoundResolvedMessage = $"第{CurrentRound}回合：双方同为 {PlayerCurrentResult}，平局无伤害";
        LastMessage = RoundResolvedMessage;
    }

    /// <summary>
    /// 重置到下一回合的内部状态。
    /// </summary>
    private void ResetRoundForNextTurn()
    {
        for (int i = 0; i < playerDiceStates.Count; i++)
        {
            playerDiceStates[i].ResetForNextRound();
        }

        for (int i = 0; i < enemyDiceStates.Count; i++)
        {
            enemyDiceStates[i].ResetForNextRound();
        }

        PlayerCurrentResult = 0;
        EnemyCurrentResult = 0;
        RemainingSingleDieRerolls = SingleDieRerollLimitPerRound;
        RemainingAllDiceRerolls = AllDiceRerollLimitPerRound;
        RoundResolvedMessage = string.Empty;
        RoundWinner = RoundWinnerType.Draw;
        RoundDamage = 0;
        RoundPlayerTotal = 0;
        RoundEnemyTotal = 0;
        IsRoundResolved = false;
    }

    private void RecalculatePlayerCurrentResult()
    {
        PlayerCurrentResult = 0;
        for (int i = 0; i < playerDiceStates.Count; i++)
        {
            if (playerDiceStates[i].HasThrown)
            {
                PlayerCurrentResult += playerDiceStates[i].CurrentRoll;
            }
        }
    }

    private DiceBattleEnemySkillConfig GetCurrentSkill()
    {
        if (enemySkillSequence.Count == 0)
        {
            return null;
        }

        int index = (CurrentRound - 1) % enemySkillSequence.Count;
        return enemySkillSequence[index];
    }
}
