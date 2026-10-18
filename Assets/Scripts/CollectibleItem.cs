using UnityEngine;

[DisallowMultipleComponent]
public class CollectibleItem : MonoBehaviour
{
    [SerializeField] private string itemName;
    [SerializeField] private UnityEngine.Events.UnityEvent onCollected = new UnityEngine.Events.UnityEvent();

    public string ItemName => string.IsNullOrWhiteSpace(itemName) ? gameObject.name : itemName;
    public bool IsCollected { get; private set; }

    public void Collect()
    {
        if (IsCollected)
            return;

        IsCollected = true;
        onCollected.Invoke();
        // Keep the object alive so doors can still read its collected state.
        gameObject.SetActive(false);
    }
}
