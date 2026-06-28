using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DiceBattleDieFaceCellUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI valueText;

    /// <summary>
    /// 刷新骰面格子显示。
    /// </summary>
    public void Refresh(int value, bool visible, bool highlighted)
    {
        ValidateBindings();
        gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        if (valueText != null)
        {
            valueText.text = value.ToString();
        }

        if (background != null)
        {
            background.color = highlighted
                ? new Color(0.95f, 0.62f, 0.72f, 1f)
                : new Color(1f, 1f, 1f, 0.9f);
        }
    }

    /// <summary>
    /// 刷新敌方骰子显示。
    /// </summary>
    public void RefreshEnemy(int value, bool visible)
    {
        ValidateBindings();
        gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        if (valueText != null)
        {
            valueText.text = value > 0 ? value.ToString() : "?";
        }

        if (background != null)
        {
            background.color = new Color(0.26f, 0.43f, 0.76f, 1f);
        }
    }

    /// <summary>
    /// 校验本格子的预制体引用是否完整。
    /// </summary>
    private void ValidateBindings()
    {
        if (background == null || valueText == null)
        {
            Debug.LogError($"DiceBattleDieFaceCellUI 引用未绑定完整：{name}", this);
        }
    }
}
