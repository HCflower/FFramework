```事件系统使用:
using UnityEngine;
using SmallFramework;

public class BasicEventExample : MonoBehaviour
{
    void Start()
    {
        var eventSystem = EventSystem.Instance;

        // 注册无参数事件
        eventSystem.RegisterEvent("GameStart", OnGameStart);

        // 注册有参数事件
        eventSystem.RegisterEvent<int>("ScoreChanged", OnScoreChanged);
        eventSystem.RegisterEvent<string>("PlayerNameChanged", OnPlayerNameChanged);
        eventSystem.RegisterEvent<Vector3>("PlayerPositionChanged", OnPlayerPositionChanged);

        // 触发事件
        eventSystem.TriggerEvent("GameStart");
        eventSystem.TriggerEvent<int>("ScoreChanged", 100);
        eventSystem.TriggerEvent<string>("PlayerNameChanged", "Player1");
        eventSystem.TriggerEvent<Vector3>("PlayerPositionChanged", new Vector3(1, 2, 3));
    }

    private void OnGameStart()
    {
        Debug.Log("游戏开始！");
    }

    private void OnScoreChanged(int newScore)
    {
        Debug.Log($"分数变化: {newScore}");
    }

    private void OnPlayerNameChanged(string playerName)
    {
        Debug.Log($"玩家名称: {playerName}");
    }

    private void OnPlayerPositionChanged(Vector3 position)
    {
        Debug.Log($"玩家位置: {position}");
    }

    void OnDestroy()
    {
        // 手动注销事件
        var eventSystem = EventSystem.Instance;
        if (eventSystem != null)
        {
            eventSystem.UnregisterEvent("GameStart", OnGameStart);
            eventSystem.UnregisterEvent<int>("ScoreChanged", OnScoreChanged);
            eventSystem.UnregisterEvent<string>("PlayerNameChanged", OnPlayerNameChanged);
            eventSystem.UnregisterEvent<Vector3>("PlayerPositionChanged", OnPlayerPositionChanged);
        }
    }
}
```
