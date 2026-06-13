using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YangTools.Scripts.Core.YangUGUI;

public sealed class ComboWindow : UGUIPanelBase<DefaultUGUIDataBase>
{
    private const int ComboDisplayStartCount = 2;
    private static ComboWindow instance;
    private static bool isOpening;
    private static int pendingComboCount;

    public RectTransform comboNode;
    public CanvasGroup comboCanvasGroup;
    public Image comboImageBg;
    public Image comboImage;
    public TextMeshProUGUI comboText;

    public List<Sprite> comboIconBg;
    public List<Sprite> comboIcon;
    public List<Material> comboTextMaterial;
    public List<Color> comboTextColor;
    
    private Sequence comboSequence;

    public static async void ShowCombo(int comboCount)
    {
        if (comboCount < ComboDisplayStartCount)
        {
            return;
        }

        if (instance != null)
        {
            instance.Play(comboCount);
            return;
        }

        pendingComboCount = Mathf.Max(pendingComboCount, comboCount);
        if (isOpening)
        {
            return;
        }

        isOpening = true;
        try
        {
            (int id, ComboWindow panel) result = await UIMonoInstance.OpenPanel<ComboWindow>(GroupType.Top);
            result.panel?.FlushPendingCombo();
        }
        finally
        {
            isOpening = false;
        }
    }

    public static void Hide()
    {
        pendingComboCount = 0;
        instance?.HideInternal();
    }

    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        instance = this;
        FlushPendingCombo();
    }

    public override void OnClose(bool isShutdown, object userData)
    {
        if (instance == this)
        {
            instance = null;
        }

        comboSequence?.Kill();
        comboSequence = null;
        DOTween.Kill(this);
        base.OnClose(isShutdown, userData);
    }

    /// <summary>
    /// 处理待处理的连击数
    /// 当连击数达到显示阈值时，触发连击效果
    /// </summary>
    private void FlushPendingCombo()
    {
        // 检查待处理的连击数是否达到显示起始阈值
        if (pendingComboCount < ComboDisplayStartCount)
        {
            // 如果未达到阈值，直接返回，不做处理
            return;
        }

        int comboCount = pendingComboCount;
        pendingComboCount = 0;

        // 执行连击效果，传入当前的连击数
        Play(comboCount);
    }

    private void Play(int comboCount)
    {
        int showIndex = 0;
        if (comboCount == 2)
        {
            showIndex = 0;
        }
        else if (2 < comboCount && comboCount <= 4)
        {
            showIndex = Random.Range(0, 1) > 0.5 ? 1 : 2;
        }
        else if (4 < comboCount)
        {
            showIndex = 2;
        }

        comboImageBg.sprite = comboIconBg[Mathf.Clamp(showIndex, 0, comboIconBg.Count - 1)];
        comboImage.sprite = comboIcon[Mathf.Clamp(showIndex, 0, comboIcon.Count - 1)];
        comboText.text = $"X{comboCount}";
        comboText.fontMaterial = comboTextMaterial[Mathf.Clamp(showIndex, 0, comboTextMaterial.Count - 1)];
        comboText.color = comboTextColor[Mathf.Clamp(showIndex, 0, comboTextColor.Count - 1)];

        comboSequence?.Kill();
        comboCanvasGroup.alpha = 0f;
        comboNode.localScale = Vector3.one * 0.72f;
        comboNode.gameObject.SetActive(true);

        comboSequence = DOTween.Sequence().SetTarget(this);
        comboSequence
            .Append(comboCanvasGroup.DOFade(1f, 0.08f))
            .Join(comboNode.DOScale(Vector3.one * 1.18f, 0.16f).SetEase(Ease.OutBack))
            .Append(comboNode.DOScale(Vector3.one, 0.12f).SetEase(Ease.OutCubic))
            .AppendInterval(0.42f)
            .Append(comboCanvasGroup.DOFade(0f, 0.22f).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                comboCanvasGroup.alpha = 0f;
                comboNode.gameObject.SetActive(false);
            });
    }

    private void HideInternal()
    {
        comboSequence?.Kill();
        comboSequence = null;
        comboCanvasGroup.alpha = 0f;
        comboNode.gameObject.SetActive(false);
    }
}