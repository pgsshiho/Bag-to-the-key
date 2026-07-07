using UnityEngine;

public class Itemgetbase : MonoBehaviour
{
    public ItemData item;
    private GetItem _itemGetter;

    void Awake()
    {
        // 인터페이스로 GetComponent 접근 (유니티에서 인터페이스로 직접 제네릭 호출를 안전하게 지원하지 않을 수 있으므로 typeof 사용)
        _itemGetter = GetComponent(typeof(GetItem)) as GetItem;
        if (_itemGetter == null)
        {
            Debug.LogError($"IItemGetter 구현체가 필요합니다. GameObject '{gameObject.name}'에 IItemGetter를 구현한 컴포넌트를 추가하세요.");
        }
    }

    public void GetItem()
    {
        if (_itemGetter == null) return;
        _itemGetter.GetItems(item);
    }
}
