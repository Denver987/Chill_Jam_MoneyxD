using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ScreanSizeChangeNotifier : UIBehaviour
{
    [SerializeField] private UnityEvent notifyScreenSizeChange;

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        notifyScreenSizeChange.Invoke();
    }    
   
}
