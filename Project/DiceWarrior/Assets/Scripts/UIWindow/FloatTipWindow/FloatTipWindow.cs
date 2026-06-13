using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YangTools.Scripts.Core.YangUGUI;

public sealed class FloatTipWindow : UGUIPanelBase<DefaultUGUIDataBase>
{
    private static FloatTipWindow instance;
    private static bool isOpening;
    private static readonly Queue<FloatTipRequest> pendingRequests = new Queue<FloatTipRequest>();

    [SerializeField] 
    private RectTransform contentRoot;

    public FloatTipItem prefab;
    
    public static void Show(string text)
    {
        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 screenPosition = center + new Vector2(0, 200);
        Show(text, screenPosition, Color.white, 36);
    }

    public static async void Show(string text, Vector2 screenPosition, Color color, int fontSize = 36)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        FloatTipRequest request = new FloatTipRequest(text, screenPosition, color, Mathf.Max(1, fontSize));
        if (instance != null)
        {
            instance.Spawn(request);
            return;
        }

        pendingRequests.Enqueue(request);
        if (isOpening)
        {
            return;
        }

        isOpening = true;
        try
        {
            (int id, FloatTipWindow panel) result = await UIMonoInstance.OpenPanel<FloatTipWindow>(GroupType.Top);
            if (result.panel != null)
            {
                result.panel.FlushPendingRequests();
            }
        }
        finally
        {
            isOpening = false;
        }
    }

    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        instance = this;
        EnsureContentRoot();
        FlushPendingRequests();
    }

    public override void OnClose(bool isShutdown, object userData)
    {
        if (instance == this)
        {
            instance = null;
        }

        base.OnClose(isShutdown, userData);
    }

    private void FlushPendingRequests()
    {
        while (pendingRequests.Count > 0)
        {
            Spawn(pendingRequests.Dequeue());
        }
    }

    private void Spawn(FloatTipRequest request)
    {
        EnsureContentRoot();

        FloatTipItem itemObject = Instantiate(prefab,contentRoot);
        Vector2 anchoredPosition = ScreenToAnchoredPosition(request.ScreenPosition);
        itemObject.Play(request.Text, anchoredPosition, request.Color);
    }

    private void EnsureContentRoot()
    {
        if (contentRoot != null)
        {
            contentRoot.gameObject.SetActive(true);
            return;
        }

        GameObject rootObject = new GameObject("ContentRoot", typeof(RectTransform));
        contentRoot = rootObject.GetComponent<RectTransform>();
        contentRoot.SetParent(transform, false);
        contentRoot.anchorMin = Vector2.zero;
        contentRoot.anchorMax = Vector2.one;
        contentRoot.offsetMin = Vector2.zero;
        contentRoot.offsetMax = Vector2.zero;
    }

    private Vector2 ScreenToAnchoredPosition(Vector2 screenPosition)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera camera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            camera = canvas.worldCamera;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRoot, screenPosition, camera, out Vector2 localPoint))
        {
            return localPoint;
        }

        return screenPosition;
    }

    private readonly struct FloatTipRequest
    {
        public FloatTipRequest(string text, Vector2 screenPosition, Color color, int fontSize)
        {
            Text = text;
            ScreenPosition = screenPosition;
            Color = color;
        }

        public string Text { get; }
        public Vector2 ScreenPosition { get; }
        public Color Color { get; }
    }
}
