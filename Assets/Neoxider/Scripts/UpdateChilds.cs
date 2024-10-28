using UnityEngine;
using UnityEngine.Events;

public class UpdateChilds : MonoBehaviour
{
    public UnityEvent OnChangeChildsCount;

    private void OnTransformChildrenChanged()
    {
        OnChangeChildsCount?.Invoke();
    }
}
