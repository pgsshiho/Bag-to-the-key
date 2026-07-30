using UnityEngine;
using UnityEngine.UI;

public class MainmenuManager : MonoBehaviour
{
    public static System.Action OnAnyButtonClicked;
    // 여러 UI를 관리할 경우 Inspector에서 등록해 둘 수 있습니다 (선택 사항)
    public GameObject[] managedUIs;
    [SerializeField] private string firstGameplaySceneName = "FirstMap";
    [SerializeField] private string firstChapterTitle = "1 Chapter : 빈손";

    private void Start()
    {
        foreach (Button button in FindObjectsByType<Button>(
                     FindObjectsInactive.Include))
        {
            if (button.name == "Load")
            {
                button.onClick.AddListener(OpenLoadMenu);
                continue;
            }

            if (button.name == "NewStart")
                button.onClick.AddListener(StartNewGame);
        }
    }

    public void StartNewGame()
    {
        OnAnyButtonClicked?.Invoke();
        SceneTransitionService.GetOrCreate().StartNewGame(
            firstGameplaySceneName,
            firstChapterTitle);
    }

    public void OpenLoadMenu()
    {
        OnAnyButtonClicked?.Invoke();
        SaveLoadManager.GetOrCreate().OpenLoadMenu();
    }

    // 버튼에서 호출: 대상 UI GameObject를 인자로 넘기면 토글합니다.
    public void ToggleUI(GameObject ui)
    {
        if (ui == null) return;
        ui.SetActive(!ui.activeSelf);
    }

    // 선택적으로, 전달한 UI만 켜고 나머지는 끄고 싶을 때 사용
    public void ShowOnly(GameObject ui)
    {
        OnAnyButtonClicked?.Invoke();
        if (managedUIs == null || managedUIs.Length == 0)
        {
            if (ui != null) ui.SetActive(true);
            return;
        }

        foreach (var g in managedUIs)
        {
            if (g == null) continue;
            g.SetActive(g == ui);
        }
    }
}
