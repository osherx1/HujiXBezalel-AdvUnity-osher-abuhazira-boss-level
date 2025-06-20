using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    public float lifetime = 1.0f;
    public float floatSpeed = 1.0f;
    public float fadeSpeed = 2.0f;
    public TextMeshPro textMesh; // Drag the child TextMeshPro here in Inspector

    private Color textColor;
    private float timer = 0f;

    private void Awake()
    {
        // If not set in Inspector, try to find in children
        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMeshPro>();
        textColor = textMesh.color;
    }

    // Call this after Instantiate to set the damage amount
    public void Setup(int damage)
    {
        textMesh.text = damage.ToString();
        timer = 0f;
        textMesh.color = textColor; // Reset alpha each time
    }

    private void Update()
    {
        // Move popup upwards over time
        transform.position += floatSpeed * Time.deltaTime * Vector3.up;
        
        Vector3 pos = transform.position;
        pos.z = Camera.main.transform.position.z + 1f;
        transform.position = pos;

        // Billboard
        transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
        

        timer += Time.deltaTime;

        // Fade out in the last 30% of lifetime
        if (timer > lifetime * 0.7f)
        {
            float fade = 1 - ((timer - lifetime * 0.7f) / (lifetime * 0.3f));
            textMesh.color = new Color(textColor.r, textColor.g, textColor.b, Mathf.Clamp01(fade));
        }

        // Destroy the parent popup object after lifetime expires
        if (timer >= lifetime)
            Destroy(gameObject);
    }
}