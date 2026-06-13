using cfg;
using UnityEngine;

public static class MoneyRewardCalculator
{
    private const int RandomRangeFormulaType = 0;
    private const int ApproachCashLimitFormulaType = 1;
    private const int FixedFormulaType = 2;
    private const float CashLimit = 500f;

    public static bool TryCalculate(float currentCash, out float reward, out int decimalPlaces)
    {
        reward = 0f;
        decimalPlaces = 2;

        MoneyRewardAttenuation config = FindConfig(currentCash);
        if (config == null)
        {
            Debug.LogError($"未找到人民币奖励衰减配置, 当前人民币:{currentCash}");
            return false;
        }

        decimalPlaces = Mathf.Max(0, config.DecimalPlaces);
        reward = Round(CalculateRawReward(currentCash, config), decimalPlaces);
        return reward > 0f;
    }

    private static MoneyRewardAttenuation FindConfig(float currentCash)
    {
        MoneyRewardAttenuationCategory category =
            GameTableManager.Instance?.Tables?.MoneyRewardAttenuationCategory;
        if (category == null)
        {
            return null;
        }

        for (int i = 0; i < category.DataList.Count; i++)
        {
            MoneyRewardAttenuation config = category.DataList[i];
            bool hasNoMax = config.MaxCash <= 0f;
            if (currentCash >= config.MinCash && (hasNoMax || currentCash < config.MaxCash))
            {
                return config;
            }
        }

        return null;
    }

    private static float CalculateRawReward(float currentCash, MoneyRewardAttenuation config)
    {
        switch (config.FormulaType)
        {
            case RandomRangeFormulaType:
                return Random.Range(config.MinReward, config.MaxReward);
            case ApproachCashLimitFormulaType:
                return Mathf.Max(0f, CashLimit - currentCash) * 0.01f;
            case FixedFormulaType:
                return config.MinReward;
            default:
                Debug.LogError($"未知人民币奖励公式类型:{config.FormulaType}, 配置Id:{config.Id}");
                return 0f;
        }
    }

    private static float Round(float value, int decimalPlaces)
    {
        float multiplier = Mathf.Pow(10f, decimalPlaces);
        return Mathf.Round(value * multiplier) / multiplier;
    }
}
