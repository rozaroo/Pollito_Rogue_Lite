using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

namespace Managers
{
    public class LoadSceneManager : MonoBehaviour
    {
        public static LoadSceneManager Instance { get; private set; }

        [Header("Transition Settings")]
        [SerializeField] private bool _useTransitionAnimations = true;
        [SerializeField] private float _transitionDuration = 0.5f;

        [Header("Transition UI References")]
        [SerializeField] private Canvas _transitionCanvas;
        [SerializeField] private Image _transitionImage;
        private bool _isTransitioning = false;

        // Propiedades publicas para acceder al canvas e imagen si es necesario
        public Canvas TransitionCanvas => _transitionCanvas;
        public Image TransitionImage => _transitionImage;

        public bool GetUseTransitionAnimations()
        {
            return _useTransitionAnimations;
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SetupTransitionCanvas();
            }
            else Destroy(gameObject);
        }

        private void SetupTransitionCanvas()
        {
            // Validamos que las referencias estén asignadas
            if (_transitionCanvas == null || _transitionImage == null)
            {
                Debug.LogError("Transition canvas or image not assigned. Scene transitions will not be animated.");
                return;
            }

            // Activamos el canvas e imagen, y arrancamos con pantalla negra
            _transitionCanvas.gameObject.SetActive(true);
            _transitionImage.gameObject.SetActive(true);
            _transitionImage.color = new Color(0, 0, 0, 1);

            // Hacemos un fade de negro a transparente al inicio del juego
            _transitionImage.DOFade(0f, _transitionDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() => {
                    // Al terminar, ocultamos el canvas
                    _transitionCanvas.gameObject.SetActive(false);
                });
        }

        public void SetUseTransitionAnimations(bool useAnimations)
        {
            _useTransitionAnimations = useAnimations;
        }
        // Recarga la escena actual
        public void ReloadCurrentScene(float delay = 0f) => LoadScene(SceneManager.GetActiveScene().name, delay);
        // Carga una escena por nombre
        public bool LoadScene(string sceneName, float delay = 0f)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("Scene name is empty or null");
                return false;
            }

            if (!SceneExists(sceneName))
            {
                Debug.LogError($"Scene '{sceneName}' does not exist!");
                return false;
            }

            if (_isTransitioning)
            {
                Debug.LogWarning("Scene transition already in progress");
                return false;
            }
            // Si no hay delay, cargamos directo (con o sin animacion)
            if (delay <= 0)
            {
                if (_useTransitionAnimations && _transitionImage != null) StartCoroutine(AnimatedSceneLoad(sceneName));
                else SceneManager.LoadScene(sceneName);
                    
            }
            else StartCoroutine(DelayedLoad(delay, sceneName)); //Si hay delay, esperamos
            return true;
        }
        //Carga una escena por indice de build
        public bool LoadScene(int buildIndex, float delay = 0f)
        {
            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"Invalid scene index: {buildIndex}");
                return false;
            }

            if (_isTransitioning)
            {
                Debug.LogWarning("Scene transition already in progress");
                return false;
            }

            if (delay <= 0)
            {
                if (_useTransitionAnimations && _transitionImage != null) StartCoroutine(AnimatedSceneLoad(buildIndex: buildIndex));
                else SceneManager.LoadScene(buildIndex);
                    
            }
            else StartCoroutine(DelayedLoad(delay, buildIndex: buildIndex));
            return true;
        }
        // Corrutina: carga de escena con animacion de fade
        private IEnumerator AnimatedSceneLoad(string sceneName = null, int? buildIndex = null)
        {
            _isTransitioning = true;

            // Activamos canvas e imagen de transicion
            _transitionCanvas.gameObject.SetActive(true);
            _transitionImage.gameObject.SetActive(true);
            // Transparente
            _transitionImage.color = new Color(0, 0, 0, 0);

            // Fade in a negro
            yield return _transitionImage.DOFade(1f, _transitionDuration).SetEase(Ease.InOutQuad).WaitForCompletion();

            // Cargar la escena (por nombre o indice)
            if (buildIndex.HasValue) SceneManager.LoadScene(buildIndex.Value);
            else if (!string.IsNullOrWhiteSpace(sceneName)) SceneManager.LoadScene(sceneName);
            // pequeño delay para asegurar carga completa
            yield return new WaitForSeconds(0.1f);
            // Fade out (de negro a transparente)
            yield return _transitionImage.DOFade(0f, _transitionDuration).SetEase(Ease.InOutQuad).WaitForCompletion();
            // Ocultamos canvas al terminar
            _transitionCanvas.gameObject.SetActive(false);
            _isTransitioning = false;
        }
        // Corrutina: carga de escena con retraso
        private IEnumerator DelayedLoad(float delay, string sceneName = null, int? buildIndex = null)
        {
            yield return new WaitForSeconds(delay);

            if (_useTransitionAnimations && _transitionImage != null)
            {
                if (buildIndex.HasValue) StartCoroutine(AnimatedSceneLoad(buildIndex: buildIndex));
                else if (!string.IsNullOrWhiteSpace(sceneName)) StartCoroutine(AnimatedSceneLoad(sceneName: sceneName));
            }
            else
            {
                if (buildIndex.HasValue) SceneManager.LoadScene(buildIndex.Value);
                else if (!string.IsNullOrWhiteSpace(sceneName)) SceneManager.LoadScene(sceneName);
            }
        }
        // Verifica si una escena existe en los Build Settings
        private bool SceneExists(string sceneName)
        {
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                if (System.IO.Path.GetFileNameWithoutExtension(scenePath) == sceneName) return true;
            }
            return false;
        }
    }
}

