using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 用于根据目标分辨率比例自动选择横向拓展还是纵向拓展
/// 
/// 一般来说，比目标分辨率更宽的屏幕选择横向拓展（高度不变），更高的屏幕选择纵向拓展（宽度不变）
/// </summary>
public class AutoCanvasScaling : MonoBehaviour
{
    public CanvasScaler canvasScaler;

    private void OnEnable()
    {
        Debug.Log("AutoCanvasScaling Awake");
        float currentAspectRatio = (float)Screen.height / Screen.width;
        float aspectRatio = canvasScaler.referenceResolution.y / canvasScaler.referenceResolution.x;

        if (currentAspectRatio > aspectRatio)
        {
            canvasScaler.matchWidthOrHeight = 0f;
        }
        else
        {
            canvasScaler.matchWidthOrHeight = 1f;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
