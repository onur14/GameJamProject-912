using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CinematicDeskIntro : MonoBehaviour
{
    [Header("UI Image References")]
    [Tooltip("Mavi-gri sevk ekranında boş panel")]
    [SerializeField] private Image dispatchScreenImage;

    [Tooltip("Gece vardiyası acil durum kontrol odası")]
    [SerializeField] private Image nightShiftDeskImage;

    [Tooltip("Acil merkezde ışıklar kapalı")]
    [SerializeField] private Image lightsOffImage;

    [Tooltip("Ekran kararması için siyah overlay")]
    [SerializeField] private Image blackoutOverlay;

    [Tooltip("Kamera sarsıntısı ve zoom için içerik kökü")]
    [SerializeField] private RectTransform contentRoot;

    [Header("Cinematic Letterbox (Sinematik Siyah Bantlar)")]
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform bottomBar;
    [SerializeField] private float letterboxHeight = 65f;

    [Header("Timing / Süre Ayarları")]
    [Tooltip("Başlangıçtaki terminal ekranında bekleme süresi")]
    [SerializeField] private float initialHoldDuration = 1.8f;

    [Tooltip("Geri zoom yapma süresi")]
    [SerializeField] private float zoomOutDuration = 2.4f;

    [Tooltip("Gece vardiyası odasında bekleme süresi")]
    [SerializeField] private float roomHoldDuration = 1.4f;

    [Tooltip("Işıkların cızırdama ve sönme süresi")]
    [SerializeField] private float blackoutFlickerDuration = 1.1f;

    [Tooltip("Karanlık odada bekleme süresi")]
    [SerializeField] private float darkHoldDuration = 1.2f;

    [Tooltip("Ekranın yavaşça tamamen kararma süresi")]
    [SerializeField] private float finalFadeDuration = 2.6f;

    [Header("Zoom & Camera Dynamics")]
    [Tooltip("Terminal ekranındayken başlangıç yakınlaşma oranı")]
    [SerializeField] private float initialZoomScale = 1.85f;

    [Tooltip("Gözün monitöre odaklanması için başlangıç ofseti (X, Y)")]
    [SerializeField] private Vector2 initialOffset = new Vector2(0f, -25f);

    [Tooltip("Organik kamera el titremesi gücü")]
    [SerializeField] private float handheldDriftAmount = 3.5f;

    [Header("Ses Efektleri (Procedural Audio)")]
    [Tooltip("Dosya gerektirmeyen gerçek zamanlı sinematik ses üretimi")]
    [SerializeField] private bool enableProceduralAudio = true;
    [Range(0f, 1f)]
    [SerializeField] private float audioVolume = 0.55f;

    [Header("Oynatma Ayarları")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool allowReplayWithR = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onAnimationComplete;

    private Coroutine sequenceCoroutine;
    private AudioSource audioSource;
    private Vector3 originalRootPos;
    private bool isPlaying = false;

    private void Awake()
    {
        if (contentRoot != null)
            originalRootPos = contentRoot.anchoredPosition;

        EnsureAudioSource();
        SetupLetterbox();
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
        else
        {
            ResetVisualsToStart();
        }
    }

    private void Update()
    {
        if (allowReplayWithR && Input.GetKeyDown(KeyCode.R))
        {
            Play();
        }
    }

    [ContextMenu("Play Cinematic Intro")]
    public void Play()
    {
        if (sequenceCoroutine != null)
            StopCoroutine(sequenceCoroutine);

        sequenceCoroutine = StartCoroutine(RunCinematicSequence());
    }

    private void ResetVisualsToStart()
    {
        SetAlpha(dispatchScreenImage, 1f);
        SetAlpha(nightShiftDeskImage, 0f);
        SetAlpha(lightsOffImage, 0f);
        SetAlpha(blackoutOverlay, 0f);

        if (contentRoot != null)
        {
            contentRoot.localScale = Vector3.one * initialZoomScale;
            contentRoot.anchoredPosition = originalRootPos + (Vector3)initialOffset;
        }
    }

    private IEnumerator RunCinematicSequence()
    {
        isPlaying = true;
        ResetVisualsToStart();

        // 1. SES: Düşük frekanslı jeneratör / monitör uğultusu
        if (enableProceduralAudio)
            PlayProceduralHum();

        // -------------------------------------------------------------
        // PERDE 1: Mavi-gri sevk ekranında boş panel (Monitöre Yakın Çekim)
        // -------------------------------------------------------------
        float elapsed = 0f;
        while (elapsed < initialHoldDuration)
        {
            elapsed += Time.deltaTime;

            // Hafif monitör titremesi ve el kamerası süzülmesi
            float swayX = Mathf.Sin(elapsed * 2.1f) * handheldDriftAmount;
            float swayY = Mathf.Cos(elapsed * 1.7f) * (handheldDriftAmount * 0.7f);
            if (contentRoot != null)
                contentRoot.anchoredPosition = originalRootPos + (Vector3)initialOffset + new Vector3(swayX, swayY, 0f);

            // CRT monitör tarama ışıltısı
            float crtPulse = 0.96f + Mathf.PingPong(elapsed * 4.5f, 0.04f);
            if (dispatchScreenImage != null)
                dispatchScreenImage.color = new Color(crtPulse, crtPulse, crtPulse, 1f);

            yield return null;
        }

        // -------------------------------------------------------------
        // PERDE 2: Geri Zoomla Gece Vardiyası Kontrol Odasına Geçiş
        // -------------------------------------------------------------
        elapsed = 0f;
        Vector3 startScale = Vector3.one * initialZoomScale;
        Vector3 endScale = Vector3.one;
        Vector3 startOffset = originalRootPos + (Vector3)initialOffset;
        Vector3 endOffset = originalRootPos;

        // Gece vardiyası resmini hafifçe görünür yapmaya başla
        SetAlpha(nightShiftDeskImage, 0f);

        while (elapsed < zoomOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / zoomOutDuration);

            // Yumuşak sinematik EaseInOutCubic eğrisi
            float easeT = (t < 0.5f) ? (4f * t * t * t) : (1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f);

            if (contentRoot != null)
            {
                // Hafif lens yaylanması (Settle)
                float scaleT = easeT;
                contentRoot.localScale = Vector3.Lerp(startScale, endScale, scaleT);
                contentRoot.anchoredPosition = Vector3.Lerp(startOffset, endOffset, easeT);
            }

            // Geri zoom ilerledikçe monitör görüntüsü odanın görüntüsüne pürüzsüzce erir
            float fadeProgress = Mathf.SmoothStep(0f, 1f, t);
            SetAlpha(nightShiftDeskImage, fadeProgress);
            SetAlpha(dispatchScreenImage, 1f - (fadeProgress * 0.95f));

            yield return null;
        }

        // Zoom tamamlandı, gece vardiyası odasında sabitlen
        SetAlpha(dispatchScreenImage, 0f);
        SetAlpha(nightShiftDeskImage, 1f);
        if (contentRoot != null)
        {
            contentRoot.localScale = Vector3.one;
            contentRoot.anchoredPosition = originalRootPos;
        }

        // -------------------------------------------------------------
        // PERDE 3: Gece Vardiyasında Sessiz Bekleyiş ve IŞIKLARIN SÖNMESİ
        // -------------------------------------------------------------
        elapsed = 0f;
        while (elapsed < roomHoldDuration)
        {
            elapsed += Time.deltaTime;
            // Odanın derinliğini hissettiren çok hafif mikroskobik nefes alış hareketi
            float breathe = 1f + Mathf.Sin(elapsed * 1.5f) * 0.003f;
            if (contentRoot != null)
                contentRoot.localScale = Vector3.one * breathe;

            yield return null;
        }

        // ELEKTRİK ARIZASI VE IŞIK KESİNTİSİ (FLICKER & BLACKOUT)
        if (enableProceduralAudio)
            PlayElectricityFlickerSound();

        float flickerTimer = 0f;
        int flickerIndex = 0;
        bool lightsState = true;

        while (flickerTimer < blackoutFlickerDuration)
        {
            flickerTimer += Time.deltaTime;
            flickerIndex++;

            // Düzensiz florasan lamba patlama ritmi
            if (flickerIndex % 3 == 0)
            {
                lightsState = !lightsState;
                SetAlpha(nightShiftDeskImage, lightsState ? 1f : 0.15f);
                SetAlpha(lightsOffImage, lightsState ? 0f : 0.85f);

                // Elektrik cızırdamasında minik ekran sarsıntısı
                if (contentRoot != null)
                {
                    float shake = Random.Range(-4f, 4f) * (1f - (flickerTimer / blackoutFlickerDuration));
                    contentRoot.anchoredPosition = originalRootPos + new Vector3(shake, -shake * 0.5f, 0f);
                }
            }

            yield return null;
        }

        // ŞAT! Genel merkezde ışıklar tamamen kapandı!
        SetAlpha(nightShiftDeskImage, 0f);
        SetAlpha(lightsOffImage, 1f);
        if (contentRoot != null)
            contentRoot.anchoredPosition = originalRootPos;

        if (enableProceduralAudio)
            PlayBlackoutBoomSound();

        // -------------------------------------------------------------
        // PERDE 4: Karanlık Odada Bekleyiş ve EKRANIN YAVAŞÇA KARARMASI
        // -------------------------------------------------------------
        yield return new WaitForSeconds(darkHoldDuration);

        elapsed = 0f;
        while (elapsed < finalFadeDuration)
        {
            elapsed += Time.deltaTime;
            // Pürüzsüz ve ağır sinematik kararma
            float t = Mathf.Clamp01(elapsed / finalFadeDuration);
            float fadeT = Mathf.SmoothStep(0f, 1f, t);

            SetAlpha(blackoutOverlay, fadeT);

            // Ses de yavaşça karanlıkla birlikte söner
            if (audioSource != null)
                audioSource.volume = Mathf.Lerp(audioVolume, 0f, fadeT);

            yield return null;
        }

        // Ekran tamamen kapkaranlık ve öyle kalır
        SetAlpha(blackoutOverlay, 1f);
        if (audioSource != null)
            audioSource.Stop();

        isPlaying = false;
        onAnimationComplete?.Invoke();
    }

    private void SetupLetterbox()
    {
        if (topBar != null)
        {
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.anchoredPosition = Vector2.zero;
            topBar.sizeDelta = new Vector2(0f, letterboxHeight);
        }

        if (bottomBar != null)
        {
            bottomBar.anchorMin = new Vector2(0f, 0f);
            bottomBar.anchorMax = new Vector2(1f, 0f);
            bottomBar.pivot = new Vector2(0.5f, 0f);
            bottomBar.anchoredPosition = Vector2.zero;
            bottomBar.sizeDelta = new Vector2(0f, letterboxHeight);
        }
    }

    private static void SetAlpha(Image image, float alpha)
    {
        if (image == null) return;
        Color c = image.color;
        c.a = Mathf.Clamp01(alpha);
        image.color = c;
    }

    #region Procedural Audio Engine (Harici dosya gerektirmez)
    private void EnsureAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // 2D Stereo
    }

    private void PlayProceduralHum()
    {
        if (audioSource == null) return;
        int sampleRate = 44100;
        float duration = 12f;
        int totalSamples = (int)(sampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            // 55Hz elektrik trafo uğultusu + 110Hz harmonik + hafif pembe parazit
            float hum55 = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.35f;
            float hum110 = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.15f;
            float noise = (Random.value * 2f - 1f) * 0.04f;
            samples[i] = (hum55 + hum110 + noise);
        }

        AudioClip humClip = AudioClip.Create("TerminalHum", totalSamples, 1, sampleRate, false);
        humClip.SetData(samples, 0);

        audioSource.clip = humClip;
        audioSource.loop = true;
        audioSource.volume = audioVolume;
        audioSource.Play();
    }

    private void PlayElectricityFlickerSound()
    {
        int sampleRate = 44100;
        float duration = 1.1f;
        int totalSamples = (int)(sampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            float crackle = (Random.value > 0.88f) ? (Random.value * 2f - 1f) * 0.7f : 0f;
            float zap = Mathf.Sin(2f * Mathf.PI * (120f + Random.Range(-40f, 40f)) * t) * 0.4f;
            samples[i] = (crackle + zap) * (1f - (t / duration));
        }

        AudioClip zapClip = AudioClip.Create("PowerFault", totalSamples, 1, sampleRate, false);
        zapClip.SetData(samples, 0);
        audioSource.PlayOneShot(zapClip, audioVolume * 1.2f);
    }

    private void PlayBlackoutBoomSound()
    {
        int sampleRate = 44100;
        float duration = 2.5f;
        int totalSamples = (int)(sampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            // Düşük frekanslı sub-bass derin çarpma (blackout thump)
            float freq = Mathf.Lerp(90f, 28f, t / duration);
            float sub = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 2.2f) * 0.8f;
            samples[i] = sub;
        }

        AudioClip boomClip = AudioClip.Create("BlackoutBoom", totalSamples, 1, sampleRate, false);
        boomClip.SetData(samples, 0);
        audioSource.PlayOneShot(boomClip, audioVolume * 1.3f);
    }
    #endregion
}
