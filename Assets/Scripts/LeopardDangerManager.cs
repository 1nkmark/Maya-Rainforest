using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(AudioSource))]
public class LeopardDangerManager : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;
    public Transform leopardGroup;
    public Transform leopard;
    public Transform[] leopardTargets;
    public string leopardObjectName = "leopard";

    [Header("Distance Settings (Meters)")]
    public float warningDistance = 5f;
    public float gameOverDistance = 1.2f;

    [Header("Near Scene Trigger")]
    public bool loadNearScene = true;
    public float nearSceneDistance = 3.5f;
    public string nearSceneName = "SecondScene";
    public bool useWhiteoutTransition = true;
    public float nearSceneWhiteoutSeconds = 0.8f;
    public float nearSceneWhiteHoldSeconds = 0.1f;

    [Header("Before Second Scene Dialogue")]
    public DialogueSystem dialogueSystem;

    public bool playDialogueBeforeNearScene = true;

    public List<TemporaryDialogueLine> beforeSecondSceneLines = new List<TemporaryDialogueLine>();

    [Tooltip("没有音频时，每句台词显示多久")]
    public float beforeSecondSceneTextDelay = 2.5f;

    [Tooltip("有音频时，音频结束后再等多久进入下一句")]
    public float beforeSecondSceneAudioDelay = 0.5f;

    [Header("Screen Effects")]
    [Range(0f, 1f)] public float maxRedEdgeAlpha = 0.75f;
    public float visualSmoothSpeed = 6f;
    public float gameOverFadeSeconds = 1f;
    public float failSceneDelaySeconds = 0.4f;
    public string failSceneName = "FailCanvas";

    [Header("Heartbeat Audio")]
    public AudioClip heartbeatClip;
    [Range(0f, 1f)] public float maxHeartbeatVolume = 0.9f;
    public float minHeartbeatPitch = 0.85f;
    public float maxHeartbeatPitch = 1.35f;

    private AudioSource heartbeatSource;
    private Canvas dangerCanvas;
    private Image redEdgeImage;
    private Image blackScreenImage;
    private Image whiteScreenImage;
    private Camera playerCamera;
    private bool gameOverStarted;
    private float currentDanger;

    private void Reset()
    {
        AutoAssignHeartbeatClip();
    }

    private void OnValidate()
    {
        warningDistance = Mathf.Max(0.1f, warningDistance);
        gameOverDistance = Mathf.Clamp(gameOverDistance, 0.05f, warningDistance - 0.01f);

        if (loadNearScene)
        {
            nearSceneDistance = Mathf.Clamp(nearSceneDistance, gameOverDistance + 0.01f, warningDistance - 0.01f);
        }

        nearSceneWhiteoutSeconds = Mathf.Max(0f, nearSceneWhiteoutSeconds);
        nearSceneWhiteHoldSeconds = Mathf.Max(0f, nearSceneWhiteHoldSeconds);
        maxHeartbeatPitch = Mathf.Max(minHeartbeatPitch, maxHeartbeatPitch);
        AutoAssignHeartbeatClip();
    }

    private void Awake()
    {
        heartbeatSource = GetComponent<AudioSource>();
        heartbeatSource.playOnAwake = false;
        heartbeatSource.loop = true;
        heartbeatSource.spatialBlend = 0f;

        if (heartbeatClip != null)
        {
            heartbeatSource.clip = heartbeatClip;
        }

        if (dialogueSystem == null)
        {
            dialogueSystem = FindObjectOfType<DialogueSystem>();
        }

        FindSceneTargets();
        CreateDangerCanvas();
    }

    private void Update()
    {
        if (gameOverStarted)
        {
            return;
        }

        FindSceneTargets();

        if (player == null || !TryGetNearestLeopardDistance(out float distance))
        {
            SetDanger(0f);
            return;
        }

        if (loadNearScene && distance <= nearSceneDistance && distance > gameOverDistance)
        {
            LoadNearScene();
            return;
        }

        if (distance <= gameOverDistance)
        {
            StartCoroutine(GameOverAndLoadFailScene());
            return;
        }

        float danger = Mathf.InverseLerp(warningDistance, gameOverDistance, distance);
        SetDanger(danger);
    }

    private void FindSceneTargets()
    {
        if (player == null)
        {
            playerCamera = Camera.main;
            if (playerCamera != null)
            {
                player = playerCamera.transform;
                AssignCanvasCamera();
            }
        }

        bool hasManualTargets = leopardTargets != null && leopardTargets.Length > 0;
        if (leopard == null && leopardGroup == null && !hasManualTargets && !string.IsNullOrEmpty(leopardObjectName))
        {
            GameObject leopardObject = GameObject.Find(leopardObjectName);
            if (leopardObject != null)
            {
                if (leopardObject.transform.childCount > 0)
                {
                    leopardGroup = leopardObject.transform;
                }
                else
                {
                    leopard = leopardObject.transform;
                }
            }
        }
    }

    private bool TryGetNearestLeopardDistance(out float nearestDistance)
    {
        nearestDistance = float.PositiveInfinity;

        if (player == null)
        {
            return false;
        }

        bool foundTarget = false;

        EvaluateLeopardDistance(leopard, ref nearestDistance, ref foundTarget);

        if (leopardGroup != null)
        {
            if (leopardGroup.childCount > 0)
            {
                for (int i = 0; i < leopardGroup.childCount; i++)
                {
                    EvaluateLeopardDistance(leopardGroup.GetChild(i), ref nearestDistance, ref foundTarget);
                }
            }
            else
            {
                EvaluateLeopardDistance(leopardGroup, ref nearestDistance, ref foundTarget);
            }
        }

        if (leopardTargets != null)
        {
            for (int i = 0; i < leopardTargets.Length; i++)
            {
                EvaluateLeopardDistance(leopardTargets[i], ref nearestDistance, ref foundTarget);
            }
        }

        return foundTarget;
    }

    private void EvaluateLeopardDistance(Transform target, ref float nearestDistance, ref bool foundTarget)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            return;
        }

        float distance = Vector3.Distance(player.position, target.position);
        if (distance < nearestDistance)
        {
            nearestDistance = distance;
            foundTarget = true;
        }
    }

    private void SetDanger(float danger)
    {
        danger = Mathf.Clamp01(danger);
        currentDanger = Mathf.MoveTowards(currentDanger, danger, visualSmoothSpeed * Time.deltaTime);

        if (redEdgeImage != null)
        {
            Color color = redEdgeImage.color;
            color.a = currentDanger * maxRedEdgeAlpha;
            redEdgeImage.color = color;
        }

        UpdateHeartbeat(currentDanger);
    }

    private void UpdateHeartbeat(float danger)
    {
        if (heartbeatSource == null || heartbeatSource.clip == null)
        {
            return;
        }

        heartbeatSource.volume = danger * maxHeartbeatVolume;
        heartbeatSource.pitch = Mathf.Lerp(minHeartbeatPitch, maxHeartbeatPitch, danger);

        if (danger > 0.01f)
        {
            if (!heartbeatSource.isPlaying)
            {
                heartbeatSource.Play();
            }
        }
        else if (heartbeatSource.isPlaying)
        {
            heartbeatSource.Stop();
        }
    }

    private void CreateDangerCanvas()
    {
        GameObject canvasObject = new GameObject("Leopard Danger Canvas");
        canvasObject.transform.SetParent(transform, false);

        dangerCanvas = canvasObject.AddComponent<Canvas>();
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        AssignCanvasCamera();
        dangerCanvas.sortingOrder = 500;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        redEdgeImage = CreateFullScreenImage(canvasObject.transform, "Red Edge");
        redEdgeImage.sprite = CreateVignetteSprite();
        redEdgeImage.color = new Color(1f, 0f, 0f, 0f);
        redEdgeImage.raycastTarget = false;

        blackScreenImage = CreateFullScreenImage(canvasObject.transform, "Game Over Black Screen");
        blackScreenImage.color = new Color(0f, 0f, 0f, 0f);
        blackScreenImage.raycastTarget = false;

        whiteScreenImage = CreateFullScreenImage(canvasObject.transform, "Near Scene Whiteout");
        whiteScreenImage.color = new Color(1f, 1f, 1f, 0f);
        whiteScreenImage.raycastTarget = false;
    }

    private void AssignCanvasCamera()
    {
        if (dangerCanvas == null)
        {
            return;
        }

        playerCamera = playerCamera != null ? playerCamera : Camera.main;
        if (playerCamera != null)
        {
            dangerCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            dangerCanvas.worldCamera = playerCamera;
            dangerCanvas.planeDistance = 0.2f;
        }
        else
        {
            dangerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
    }

    private Image CreateFullScreenImage(Transform parent, string objectName)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.AddComponent<Image>();
        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        return image;
    }

    private Sprite CreateVignetteSprite()
    {
        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = Mathf.Abs((x / (float)(size - 1)) * 2f - 1f);
                float v = Mathf.Abs((y / (float)(size - 1)) * 2f - 1f);
                float edge = Mathf.Max(u, v);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, edge));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private IEnumerator GameOverAndLoadFailScene()
    {
        gameOverStarted = true;
        SetDanger(1f);

        if (heartbeatSource != null && heartbeatSource.clip != null && !heartbeatSource.isPlaying)
        {
            heartbeatSource.Play();
        }

        float elapsed = 0f;
        while (elapsed < gameOverFadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / gameOverFadeSeconds);

            if (blackScreenImage != null)
            {
                blackScreenImage.color = new Color(0f, 0f, 0f, alpha);
            }

            yield return null;
        }

        if (heartbeatSource != null)
        {
            heartbeatSource.Stop();
        }

        if (failSceneDelaySeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(failSceneDelaySeconds);
        }

        if (!string.IsNullOrEmpty(failSceneName))
        {
            SceneManager.LoadScene(GetSceneName(failSceneName));
        }
    }

    private void LoadNearScene()
    {
        string sceneName = GetSceneName(nearSceneName);
        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        gameOverStarted = true;

        if (heartbeatSource != null && heartbeatSource.isPlaying)
        {
            heartbeatSource.Stop();
        }

        StartCoroutine(DialogueAndWhiteoutThenLoad(sceneName));
    }
    private IEnumerator DialogueAndWhiteoutThenLoad(string sceneName)
    {
        bool dialogueFinished = true;

        // 1. 对话开始
        if (
            playDialogueBeforeNearScene &&
            dialogueSystem != null &&
            beforeSecondSceneLines != null &&
            beforeSecondSceneLines.Count > 0
        )
        {
            dialogueFinished = false;

            dialogueSystem.PlayTemporaryDialogueSequence(
                beforeSecondSceneLines,
                () =>
                {
                    dialogueFinished = true;
                },
                beforeSecondSceneTextDelay,
                beforeSecondSceneAudioDelay
            );
        }

        // 2. 白光也同时开始
        if (useWhiteoutTransition && whiteScreenImage != null && nearSceneWhiteoutSeconds > 0f)
        {
            float elapsed = 0f;

            while (elapsed < nearSceneWhiteoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(elapsed / nearSceneWhiteoutSeconds);

                whiteScreenImage.color = new Color(1f, 1f, 1f, alpha);

                yield return null;
            }

            whiteScreenImage.color = Color.white;
        }

        // 3. 如果白光已经满了，但对话还没结束，就继续等对话结束
        while (!dialogueFinished)
        {
            yield return null;
        }

        // 4. 对话结束后，再保持一下全白
        if (nearSceneWhiteHoldSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(nearSceneWhiteHoldSeconds);
        }

        // 5. 跳转第二幕
        SceneManager.LoadScene(sceneName);
    }
    private IEnumerator ContinueLoadNearScene(string sceneName)
    {
        if (useWhiteoutTransition)
        {
            yield return StartCoroutine(WhiteoutAndLoadScene(sceneName));
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private IEnumerator WhiteoutAndLoadScene(string sceneName)
    {
        if (heartbeatSource != null && heartbeatSource.isPlaying)
        {
            heartbeatSource.Stop();
        }

        if (whiteScreenImage == null || nearSceneWhiteoutSeconds <= 0f)
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < nearSceneWhiteoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / nearSceneWhiteoutSeconds);
            whiteScreenImage.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        whiteScreenImage.color = Color.white;

        if (nearSceneWhiteHoldSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(nearSceneWhiteHoldSeconds);
        }

        SceneManager.LoadScene(sceneName);
    }

    private string GetSceneName(string configuredSceneName)
    {
        if (string.IsNullOrWhiteSpace(configuredSceneName))
        {
            return string.Empty;
        }

        return configuredSceneName.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileNameWithoutExtension(configuredSceneName)
            : configuredSceneName;
    }

    private void AutoAssignHeartbeatClip()
    {
#if UNITY_EDITOR
        if (heartbeatClip != null)
        {
            return;
        }

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Scripts/real heartbeat.mp3");
        if (clip != null)
        {
            heartbeatClip = clip;
        }
#endif
    }
}
