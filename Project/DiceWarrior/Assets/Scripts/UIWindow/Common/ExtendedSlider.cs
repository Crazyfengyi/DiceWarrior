using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExtendedSlider  : Slider, IBeginDragHandler, IEndDragHandler
{
    public Action beiginDrag { get; set; }
    public Action endDrag { get; set; }
 
    public Action pointerUp { get; set; }
    public void OnBeginDrag(PointerEventData eventData)
    {
        beiginDrag?.Invoke();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        endDrag?.Invoke();
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        pointerUp?.Invoke();
        
    }
}
