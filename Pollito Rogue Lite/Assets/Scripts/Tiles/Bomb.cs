using UnityEngine;
using System.Collections;
public class Bomb : MonoBehaviour
{
    [SerializeField] private float tiempoExplosion = 0.5f; // Segundos antes de explotar
    [SerializeField] private GameObject explosionPrefab; // Prefab de la explosión
    [SerializeField] private float _bombVolume = 1.0f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer radiusRenderer;
    private float radiusAlpha = 0.3f;
    public Color blinkColor = Color.red;
    private Color originalColor;

    private float explosionRadius = 1.5f;
    private float expansionTime = 0.25f;
    public Transform radiusVisualizer;
    private float timer = 0f;

    private void Start()
    {
        originalColor = spriteRenderer.color;
        StartCoroutine(BlinkRoutine());
        Explotar();
    }
    private void Update()
    {
        if (timer < expansionTime)
        {
            timer += Time.deltaTime;
            float t = timer / expansionTime;
            float scale = Mathf.Lerp(0f, explosionRadius * 2f, t);
            radiusVisualizer.localScale = new Vector3(scale, scale, 1f);
            Color c = radiusRenderer.color;
            c.a = radiusAlpha; // setear alpha
            radiusRenderer.color = c;
        }
    }

    // Ahora que busque el TilemapManager desde acá y llame esa funcion
    private void Explotar()
    {
        //Sonido de explosion
        StartCoroutine(EsperarYDestruir());
    }
    private IEnumerator EsperarYDestruir() 
    {
        yield return new WaitForSeconds(tiempoExplosion);
        TilemapManager tilemapManager = FindObjectOfType<TilemapManager>();
        if (tilemapManager != null)
        {
            Vector3Int tilePos = tilemapManager.WorldToCell(transform.position);
            tilemapManager.ExplodeBomb(tilePos);
        }
        // Instanciamos la explosión
        Vector3 spawnPos = transform.position + new Vector3(0f, -0.5f, 0f);
        Instantiate(explosionPrefab, spawnPos, Quaternion.identity);
        AkSoundEngine.PostEvent("Explosion", gameObject);
        // Destruimos la bomba
        Destroy(gameObject);
    }
    
    private IEnumerator BlinkRoutine() 
    {
        AkSoundEngine.PostEvent("Bomb", gameObject);
        float elapsed = 0;
        float blinkInterval = 0.5f; //Cada medio segundo
        while (elapsed < tiempoExplosion) 
        {
            spriteRenderer.color = blinkColor;
            yield return new WaitForSeconds(blinkInterval);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval * 2;
        }
    }
}