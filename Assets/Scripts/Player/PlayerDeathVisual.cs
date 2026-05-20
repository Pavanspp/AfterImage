using UnityEngine;
using System.Collections;

// Plays a shatter effect on player death.
// Lives on the Player GameObject.
// Listens to PlayerStateHub.OnDeath and handles its own timing.
public class PlayerDeathVisual : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer playerRenderer;
    public PlayerStateHub playerState;

    [Header("Shatter Settings")]
    public int fragmentCount = 12;
    public float fragmentSpeed = 6f;
    public float fragmentDuration = 0.55f;
    public float fragmentSize = 0.12f;
    public Color fragmentColor = Color.white;

    void OnEnable()
    {
        playerState.OnDeath += HandleDeath;
    }

    void OnDisable()
    {
        playerState.OnDeath -= HandleDeath;
    }

    void HandleDeath()
    {
        StartCoroutine(ShatterRoutine());
    }

    IEnumerator ShatterRoutine()
    {
        playerRenderer.enabled = false;

        for (int i = 0; i < fragmentCount; i++)
            SpawnFragment(i);

        yield return new WaitForSeconds(fragmentDuration);
    }

    void SpawnFragment(int index)
    {
        GameObject frag = new GameObject("DeathFrag");
        frag.transform.position = transform.position;

        // random size variation
        float size = fragmentSize * Random.Range(0.6f, 1.4f);
        frag.transform.localScale = Vector3.one * size;

        SpriteRenderer sr = frag.AddComponent<SpriteRenderer>();
        sr.sprite = playerRenderer.sprite;
        sr.color = fragmentColor;
        sr.sortingLayerName = playerRenderer.sortingLayerName;
        sr.sortingOrder = playerRenderer.sortingOrder + 1;

        // spread evenly in a circle with random variation
        float angle = (360f / fragmentCount) * index + Random.Range(-15f, 15f);
        float rad = angle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        float speed = fragmentSpeed * Random.Range(0.6f, 1.4f);

        StartCoroutine(AnimateFragment(frag, sr, dir * speed));
    }

    IEnumerator AnimateFragment(GameObject frag, SpriteRenderer sr, Vector2 velocity)
    {
        float elapsed = 0f;
        Vector3 startScale = frag.transform.localScale;
        Color startColor = sr.color;

        while (elapsed < fragmentDuration)
        {
            if (frag == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / fragmentDuration;

            frag.transform.position += (Vector3)(velocity * Time.deltaTime * (1f - t));
            frag.transform.Rotate(0f, 0f, velocity.magnitude * Time.deltaTime * 80f);

            // Clamp to tiny positive value — never let scale reach zero
            float shrinkT    = Mathf.Clamp01((t - 0.4f) / 0.6f);
            float sizeScalar = Mathf.Max(0.001f, 1f - shrinkT);
            frag.transform.localScale = startScale * sizeScalar;

            Color c = startColor;
            c.a      = Mathf.Lerp(1f, 0f, Mathf.Pow(t, 2f));
            sr.color = c;

            yield return null;
        }

        Destroy(frag);
    }
}