using UnityEngine;

public class HealthBarWorld : MonoBehaviour
{
    public SpriteRenderer barFill;
    [Header("Set this to the BarFill's X scale when full (from Inspector)")]
    public float initialFillScaleX = 1.08984f;

    public void Setup(int maxHealth)
    {
        SetHealth(maxHealth, maxHealth);
    }

    public void SetHealth(int current, int max)
    {
        float pct = Mathf.Clamp01((float)current / max);
        if (barFill != null)
        {
            barFill.transform.localScale = new Vector3(initialFillScaleX * pct, barFill.transform.localScale.y, barFill.transform.localScale.z);
        }
    }
}
