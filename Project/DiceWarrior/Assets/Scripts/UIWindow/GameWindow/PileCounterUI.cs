using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PileCounterUI : MonoBehaviour
{
    [SerializeField] private UICustomButton button;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image background;

    public RectTransform RectTransform => transform as RectTransform;

    /// <summary>
    /// 绑定牌堆计数器的界面引用。
    /// </summary>
    public void Bind(UICustomButton bindButton, TextMeshProUGUI bindTitleText, TextMeshProUGUI bindCountText,
        Image bindBackground)
    {
        button = bindButton;
        titleText = bindTitleText;
        countText = bindCountText;
        background = bindBackground;
    }

    /// <summary>
    /// 刷新牌堆标题和数量。
    /// </summary>
    public void Refresh(string title, int count, string countHoverText = null)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (countText != null)
        {
            countText.text = count.ToString();
        }

        if (background != null)
        {
            background.color = new Color(0.27f, 0.45f, 0.78f, 1f);
        }
    }

}
