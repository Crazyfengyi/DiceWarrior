using System;
using System.Collections;
using GameMain;
using UnityEngine;
using UnityEngine.UI;
using YangTools.Scripts.Core.ResourceManager;

/// <summary>
/// 足球视图类，负责处理单个足球的显示和交互
/// </summary>
public sealed class FootballView : MonoBehaviour
{
    private const float MoveTrailTime = 0.16f;
    private const float MoveTrailStartWidthRatio = 0.3f;
    private const float MoveTrailEndWidthRatio = 0.06f;
    private const float MoveTrailMinVertexDistance = 0.02f;

    private static Material moveTrailMaterial;

    public Image icon;
    // 按钮组件
    public UICustomButton btn;
    public Outline outline;
    public Canvas canvas;
    
    // 足球数据
    public FootballData data;
    // 点击事件回调
    private Action<FootballData> onClicked;
    // 足球的矩形变换组件
    public RectTransform RectTransform { get; private set; }
    private GameObject moveTrailObject;
    private TrailRenderer moveTrailRenderer;
    private Coroutine destroyMoveTrailCoroutine;

    /// <summary>
    /// 创建足球视图
    /// </summary>
    /// <param name="parent">父变换</param>
    /// <param name="ball">足球数据</param>
    /// <param name="callback">点击回调</param>
    public void Init(Transform parent, FootballData ball, Action<FootballData> callback)
    {
        // 设置足球图像
        ResourceManager.SetImageSprite(icon,ToSpriteName(ball.Type));
        // 设置足球轮廓
        outline.effectColor = new Color(1f, 1f, 1f, 0.75f);
        outline.effectDistance = new Vector2(2f, -2f);

        // 初始化足球视图
        data = ball;
        RectTransform = gameObject.GetComponent<RectTransform>();
        onClicked = callback;
        btn.AddListener(HandleClick);
        canvas.sortingOrder = GetComponentInParent<Canvas>().sortingOrder + 5;
    }

    /// <summary>
    /// 设置足球是否可交互
    /// </summary>
    /// <param name="value">是否可交互</param>
    public void SetInteractable(bool value)
    {
        if (btn != null)
        {
            btn.TargetButton.interactable = value;
        }
    }

    public void RefreshColor()
    {
        if (data != null && icon != null)
        {
            ResourceManager.SetImageSprite(icon,ToSpriteName(data.Type));
        }
    }

    public void PlayMoveTrail()
    {
        StopDestroyMoveTrailCoroutine();
        if (moveTrailObject == null)
        {
            CreateMoveTrail();
        }

        if (moveTrailRenderer == null)
        {
            return;
        }

        moveTrailRenderer.Clear();
        moveTrailRenderer.emitting = true;
    }

    public void StopMoveTrail()
    {
        if (moveTrailRenderer == null)
        {
            return;
        }

        moveTrailRenderer.emitting = false;
        StopDestroyMoveTrailCoroutine();
        destroyMoveTrailCoroutine = StartCoroutine(DestroyMoveTrailAfterDelay(moveTrailRenderer.time));
    }

    private void CreateMoveTrail()
    {
        moveTrailObject = new GameObject("MoveTrail", typeof(TrailRenderer));
        moveTrailObject.layer = gameObject.layer;
        Transform trailTransform = moveTrailObject.transform;
        trailTransform.SetParent(transform, false);
        trailTransform.SetAsFirstSibling();
        trailTransform.localPosition = Vector3.zero;
        trailTransform.localRotation = Quaternion.identity;
        trailTransform.localScale = Vector3.one;

        moveTrailRenderer = moveTrailObject.GetComponent<TrailRenderer>();
        moveTrailRenderer.time = MoveTrailTime;
        moveTrailRenderer.minVertexDistance = MoveTrailMinVertexDistance;
        moveTrailRenderer.autodestruct = false;
        moveTrailRenderer.emitting = false;
        moveTrailRenderer.material = GetMoveTrailMaterial();
        moveTrailRenderer.startWidth = GetMoveTrailWidth(MoveTrailStartWidthRatio);
        moveTrailRenderer.endWidth = GetMoveTrailWidth(MoveTrailEndWidthRatio);
        moveTrailRenderer.startColor = GetMoveTrailColor(0.55f);
        moveTrailRenderer.endColor = GetMoveTrailColor(0f);
        moveTrailRenderer.numCornerVertices = 4;
        moveTrailRenderer.numCapVertices = 4;
        moveTrailRenderer.sortingOrder = canvas.sortingOrder - 1;
    }

    private float GetMoveTrailWidth(float ratio)
    {
        if (RectTransform != null && RectTransform.rect.width > 0f && RectTransform.rect.height > 0f)
        {
            float worldWidth = RectTransform.TransformVector(Vector3.right * RectTransform.rect.width).magnitude;
            float worldHeight = RectTransform.TransformVector(Vector3.up * RectTransform.rect.height).magnitude;
            return Mathf.Min(worldWidth, worldHeight) * ratio;
        }

        return 20f * ratio;
    }

    private Color GetMoveTrailColor(float alpha)
    {
        Color color = icon != null ? icon.color : Color.white;
        color.a = alpha;
        return color;
    }

    private static Material GetMoveTrailMaterial()
    {
        if (moveTrailMaterial != null)
        {
            return moveTrailMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("UI/Default");
        }

        moveTrailMaterial = new Material(shader);
        return moveTrailMaterial;
    }

    private IEnumerator DestroyMoveTrailAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        DestroyMoveTrail();
    }

    private void StopDestroyMoveTrailCoroutine()
    {
        if (destroyMoveTrailCoroutine == null)
        {
            return;
        }

        StopCoroutine(destroyMoveTrailCoroutine);
        destroyMoveTrailCoroutine = null;
    }

    private void DestroyMoveTrail()
    {
        StopDestroyMoveTrailCoroutine();
        if (moveTrailObject != null)
        {
            Destroy(moveTrailObject);
        }

        moveTrailObject = null;
        moveTrailRenderer = null;
    }

    private void OnDestroy()
    {
        DestroyMoveTrail();
    }

    /// <summary>
    /// 处理足球点击
    /// </summary>
    private void HandleClick()
    {
        onClicked?.Invoke(data);
    }

    /// <summary>
    /// 根据足球类型返回对应的资源名称
    /// </summary>
    private static string ToSpriteName(FootballType type)
    {
        string enemName = Enum.GetName(typeof(FootballType),type);
        enemName = enemName.ToLower();
        return enemName;
    }
}
