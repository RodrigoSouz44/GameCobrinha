using UnityEngine;

public class FoodBlink : MonoBehaviour
{
    private SpriteRenderer sr;
    private float speed = 3f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        float alpha = (Mathf.Sin(Time.time * speed) + 1f) / 2f; // varia entre 0 e 1
        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha * 0.8f + 0.2f);
    }
}
