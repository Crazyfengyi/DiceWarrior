using System;
using System.Collections;
using System.Collections.Generic;
using GameMain;
using Manager;
using UnityEngine;
using UnityEngine.UI;
using YangTools;

public class LoadScript : MonoBehaviour
{
    public Slider slider;
    public RectTransform barEffectParent;
    public RectTransform barEffect;

    private float targetValue;
    public static bool isLoadAniOver;

    void Awake()
    {
        gameObject.AddEventListener<UserResourcesReadyPress>(OnHandleEventMessage);
    }

    void OnDestroy()
    {
        Extend.RemoveEventListener(gameObject);
    }

    public void Update()
    {
        if (!Mathf.Approximately(slider.value, targetValue))
        {
            slider.value = Mathf.Lerp(slider.value, targetValue, 0.2f);
            barEffect.anchoredPosition = new Vector2(slider.value * barEffectParent.rect.width - 10, barEffect.anchoredPosition.y);
            barEffect.gameObject.SetActive(0.02f < slider.value && slider.value < 0.98f);
            if (slider.value >= 1)
            {
                isLoadAniOver = true;
                Destroy(gameObject);
            }
        }
    }

    private void OnHandleEventMessage(EventData obj)
    {
        targetValue = ((UserResourcesReadyPress) obj.Args).press;
    }
}