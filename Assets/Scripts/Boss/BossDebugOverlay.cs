using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class BossDebugOverlay : MonoBehaviour
{
    public BossCore boss;
    private Text debugText;
    private StringBuilder builder;
    public Font debugFont;

    void Start()
    {
        GameObject canvasGO = new GameObject("BossDebugCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject textGO = new GameObject("BossDebugText");
        textGO.transform.SetParent(canvasGO.transform);
        debugText = textGO.AddComponent<Text>();
        debugText.font = debugFont;
        debugText.fontSize = 16;
        debugText.alignment = TextAnchor.UpperLeft;
        debugText.color = Color.red;

        RectTransform rt = debugText.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(10, -50);
        rt.sizeDelta = new Vector2(500, 200);

        builder = new StringBuilder();
    }

    void Update()
    {
        if (boss == null) return;

        builder.Clear();
        builder.AppendLine("<b>Boss Debug</b>");
        builder.AppendLine($"State: {boss.CurrentState}");
        //builder.AppendLine($"Health: {boss.CurrentHealth}");
        builder.AppendLine($"canAttack: {boss.CanAttack}");
        builder.AppendLine($"flyTimer: {boss.FlyTimer:0.00}");
        builder.AppendLine($"glideRoutine == null: {boss.IsGlideRoutineNull}");
        builder.AppendLine($"slamRoutine == null: {boss.IsSlamRoutineNull}");
        builder.AppendLine($"fireBallRoutine == null: {boss.IsFireRoutineNull}");

        debugText.text = builder.ToString();
    }
}