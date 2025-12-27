using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// 特等奖获得界面 - 弹出动画、打字机效果、进度条
/// </summary>
public class JackpotWinPanel : MonoBehaviour
{
    private static JackpotWinPanel instance;
    public static JackpotWinPanel Instance
    {
        get
        {
            return instance;
        }
    }

    [Header("UI组件")]
    [SerializeField] private CanvasGroup canvasGroup;           // 用于整体淡入淡出
    [SerializeField] private RectTransform panelTransform;      // 面板Transform，用于缩放动画
    [SerializeField] private TextMeshProUGUI titleText;         // 标题文本（支持TMP）
    [SerializeField] private Text titleTextLegacy;              // 标题文本（支持Legacy UI Text）
    [SerializeField] private GameObject progressContainer;      // 进度条容器（可选，用于整体显示/隐藏）
    [SerializeField] private Slider progressSlider;             // 进度条
    [SerializeField] private TextMeshProUGUI sliderText;         // 进度条文本
    
    [Header("标题配置")]
    [SerializeField] private string titleContent = "恭喜获得特等奖！";  // 标题内容
    [SerializeField] private float typewriterSpeed = 0.1f;              // 打字机速度（每个字的间隔）
    
    [Header("面板动画配置")]
    [SerializeField] private float panelFadeInDuration = 0.3f;   // 面板淡入时长
    [SerializeField] private float panelScaleFrom = 0.5f;        // 面板初始缩放
    [SerializeField] private float panelScaleTo = 1f;            // 面板目标缩放
    [SerializeField] private float panelScaleDuration = 0.4f;    // 面板缩放动画时长
    
    [Header("进度条配置")]
    [SerializeField] private string progressText = "正在加载特等奖...";  // 进度条文本
    [SerializeField] private float progressDelay = 0.5f;         // 进度条开始前的延迟
    [SerializeField] private float progressDuration = 3f;        // 进度条填充时长
    [SerializeField] private AnimationCurve progressCurve = AnimationCurve.Linear(0, 0, 1, 1);  // 进度曲线
    
    [Header("视频配置")]
    [SerializeField] private VideoPlayer videoPlayer;            // 视频播放器
    [SerializeField] private RawImage videoDisplay;              // 视频显示的RawImage
    [SerializeField] private GameObject videoContainer;          // 视频容器（可选，用于整体显示/隐藏）
    [SerializeField] private List<VideoClip> videoClips = new List<VideoClip>();  // 要播放的视频列表（依次播放）
    [SerializeField] private bool loopLastVideo = false;         // 是否循环播放最后一个视频
    [SerializeField] private float videoFadeInDuration = 0.3f;   // 视频淡入时长
    [SerializeField] private float videoTransitionDelay = 0f;    // 视频之间的过渡延迟
    
    // 进度条完成回调
    public event Action OnProgressComplete;
    
    // 视频播放完成回调
    public event Action OnVideoComplete;
    
    // 是否正在显示
    private bool isShowing = false;
    private bool isVideoPlaying = false;
    private Coroutine currentAnimation;
    private RenderTexture videoRenderTexture;

    private void Awake()
    {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("JackpotWinPanel instance created");
        }

        // 初始隐藏
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        if (panelTransform != null)
        {
            panelTransform.localScale = Vector3.one * panelScaleFrom;
        }
        
        if (progressSlider != null)
        {
            progressSlider.value = 0f;
        }
        
        // 初始隐藏进度条
        SetProgressVisible(false);
        
        // 初始隐藏视频
        SetVideoVisible(false);
        InitializeVideoPlayer();
        
        ClearTitle();
    }
    
    /// <summary>
    /// 初始化视频播放器
    /// </summary>
    private void InitializeVideoPlayer()
    {
        if (videoPlayer == null) return;
        
        // 立即停止可能正在播放的视频
        videoPlayer.Stop();
        
        // 设置第一个视频源（如果有）
        if (videoClips != null && videoClips.Count > 0 && videoClips[0] != null)
        {
            videoPlayer.clip = videoClips[0];
        }
        
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false; // 初始不循环，由播放逻辑控制
        
        // 创建RenderTexture用于显示视频（使用第一个视频的尺寸）
        if (videoDisplay != null && videoClips != null && videoClips.Count > 0 && videoClips[0] != null)
        {
            videoRenderTexture = new RenderTexture((int)videoClips[0].width, (int)videoClips[0].height, 0);
            videoPlayer.targetTexture = videoRenderTexture;
            videoDisplay.texture = videoRenderTexture;
        }
        
        // 注册视频播放完成事件
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    /// <summary>
    /// 显示特等奖界面
    /// </summary>
    public void Show()
    {
        Show(titleContent, null);
    }

    /// <summary>
    /// 显示特等奖界面（带回调）
    /// </summary>
    public void Show(Action onComplete)
    {
        Show(titleContent, onComplete);
    }

    /// <summary>
    /// 显示特等奖界面（自定义标题和回调）
    /// </summary>
    public void Show(string title, Action onComplete)
    {
        if (isShowing) return;
        
        gameObject.SetActive(true);
        
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        currentAnimation = StartCoroutine(ShowAnimation(title, onComplete));
    }

    /// <summary>
    /// 隐藏界面
    /// </summary>
    public void Hide()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        StartCoroutine(HideAnimation());
    }

    /// <summary>
    /// 显示动画协程
    /// </summary>
    private IEnumerator ShowAnimation(string title, Action onComplete)
    {
        isShowing = true;
        
        // 重置状态
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        if (panelTransform != null)
        {
            panelTransform.localScale = Vector3.one * panelScaleFrom;
        }
        
        if (progressSlider != null)
        {
            progressSlider.value = 0f;
        }
        
        // 隐藏进度条，等打字机效果完成后再显示
        SetProgressVisible(false);
        
        ClearTitle();
        
        // 阶段1：面板淡入 + 缩放动画
        float elapsed = 0f;
        float maxDuration = Mathf.Max(panelFadeInDuration, panelScaleDuration);
        
        while (elapsed < maxDuration)
        {
            float fadeProgress = Mathf.Clamp01(elapsed / panelFadeInDuration);
            float scaleProgress = Mathf.Clamp01(elapsed / panelScaleDuration);
            float easedScaleProgress = EaseOutBack(scaleProgress);
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = fadeProgress;
            }
            
            if (panelTransform != null)
            {
                float scale = Mathf.Lerp(panelScaleFrom, panelScaleTo, easedScaleProgress);
                panelTransform.localScale = Vector3.one * scale;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 确保最终状态
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (panelTransform != null) panelTransform.localScale = Vector3.one * panelScaleTo;
        
        // 阶段2：打字机效果
        yield return StartCoroutine(TypewriterAnimation(title, (str)=> SetTitleText(str)));
        
        // 阶段3：等待后显示进度条并开始动画
        yield return new WaitForSeconds(progressDelay);
        
        // 显示进度条
        SetProgressVisible(true);
        
        // 阶段4：进度条动画
        yield return StartCoroutine(ProgressBarAnimation());
        
        // 进度条完成回调
        OnProgressComplete?.Invoke();
        
        // 阶段5：播放视频（支持多个视频依次播放）
        if (videoPlayer != null && videoClips != null && videoClips.Count > 0)
        {
            yield return StartCoroutine(PlayAllVideosAnimation());
        }
        
        // 全部完成回调
        onComplete?.Invoke();
        
        isShowing = false;
    }

    /// <summary>
    /// 打字机动画
    /// </summary>
    private IEnumerator TypewriterAnimation(string text, Action<string> onUpdate)
    {
        if (string.IsNullOrEmpty(text)) yield break;
        
        for (int i = 0; i <= text.Length; i++)
        {
            string displayText = text.Substring(0, i);

            onUpdate?.Invoke(displayText);
            if (i < text.Length)
            {
                yield return new WaitForSeconds(typewriterSpeed);
            }
        }
    }
    

    /// <summary>
    /// 进度条动画
    /// </summary>
    private IEnumerator ProgressBarAnimation()
    {
        if (progressSlider == null) yield break;
        
        float elapsed = 0f;

        // 打字机效果 显示进度条文本
        if (sliderText != null) {
            StartCoroutine(TypewriterAnimation(progressText, (str)=> sliderText.text = str));
        }
        
        while (elapsed < progressDuration)
        {
            float progress = elapsed / progressDuration;
            float curvedProgress = progressCurve.Evaluate(progress);
            
            progressSlider.value = curvedProgress;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        progressSlider.value = 1f;
    }

    /// <summary>
    /// 隐藏动画
    /// </summary>
    private IEnumerator HideAnimation()
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        
        while (elapsed < panelFadeInDuration)
        {
            float progress = elapsed / panelFadeInDuration;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        isShowing = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 设置标题文本
    /// </summary>
    private void SetTitleText(string text)
    {
        if (titleText != null)
        {
            titleText.text = text;
        }
        
        if (titleTextLegacy != null)
        {
            titleTextLegacy.text = text;
        }
    }

    /// <summary>
    /// 清空标题
    /// </summary>
    private void ClearTitle()
    {
        SetTitleText("");
    }

    /// <summary>
    /// 设置进度条显示/隐藏
    /// </summary>
    private void SetProgressVisible(bool visible)
    {
        // 如果有进度条容器，优先使用容器控制
        if (progressContainer != null)
        {
            progressContainer.SetActive(visible);
        }
        else
        {
            // 否则分别控制进度条和文本
            if (progressSlider != null)
            {
                progressSlider.gameObject.SetActive(visible);
            }
            
            if (sliderText != null)
            {
                sliderText.gameObject.SetActive(visible);
            }
        }
    }

    /// <summary>
    /// 设置视频显示/隐藏
    /// </summary>
    private void SetVideoVisible(bool visible)
    {
        if (videoContainer != null)
        {
            videoContainer.SetActive(visible);
        }
        else if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 播放所有视频（依次播放）
    /// </summary>
    private IEnumerator PlayAllVideosAnimation()
    {
        if (videoPlayer == null || videoClips == null || videoClips.Count == 0) yield break;
        
        isVideoPlaying = true;
        
        // 隐藏进度条，显示视频
        SetProgressVisible(false);
        SetVideoVisible(true);
        
        // 设置视频显示初始透明度为0（只在第一个视频时淡入）
        if (videoDisplay != null)
        {
            videoDisplay.color = new Color(1f, 1f, 1f, 0f);
        }
        
        for (int i = 0; i < videoClips.Count; i++)
        {
            VideoClip clip = videoClips[i];
            if (clip == null) continue;
            
            bool isFirstVideo = (i == 0);
            bool isLastVideo = (i == videoClips.Count - 1);
            
            // 播放单个视频
            yield return StartCoroutine(PlaySingleVideoAnimation(clip, isFirstVideo, isLastVideo && loopLastVideo));
            
            // 如果是最后一个视频且循环播放，不继续
            if (isLastVideo && loopLastVideo) yield break;
            
            // 视频之间的过渡延迟
            if (!isLastVideo && videoTransitionDelay > 0)
            {
                yield return new WaitForSeconds(videoTransitionDelay);
            }
        }
        
        isVideoPlaying = false;
        OnVideoComplete?.Invoke();
    }
    
    /// <summary>
    /// 播放单个视频动画
    /// </summary>
    /// <param name="clip">要播放的视频</param>
    /// <param name="fadeIn">是否淡入</param>
    /// <param name="loop">是否循环</param>
    private IEnumerator PlaySingleVideoAnimation(VideoClip clip, bool fadeIn, bool loop)
    {
        if (videoPlayer == null || clip == null) yield break;
        
        // 设置视频源
        videoPlayer.clip = clip;
        videoPlayer.isLooping = loop;
        
        // 更新RenderTexture尺寸（如果视频尺寸不同）
        if (videoRenderTexture == null || 
            videoRenderTexture.width != (int)clip.width || 
            videoRenderTexture.height != (int)clip.height)
        {
            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
            }
            videoRenderTexture = new RenderTexture((int)clip.width, (int)clip.height, 0);
            videoPlayer.targetTexture = videoRenderTexture;
            if (videoDisplay != null)
            {
                videoDisplay.texture = videoRenderTexture;
            }
        }
        
        // 准备视频
        videoPlayer.Prepare();
        
        // 等待视频准备完成
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }
        
        // 开始播放
        videoPlayer.Play();
        
        // 视频淡入效果（只在第一个视频时）
        if (fadeIn && videoDisplay != null && videoFadeInDuration > 0)
        {
            float elapsed = 0f;
            while (elapsed < videoFadeInDuration)
            {
                float alpha = elapsed / videoFadeInDuration;
                videoDisplay.color = new Color(1f, 1f, 1f, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
            videoDisplay.color = Color.white;
        }
        
        // 如果不循环，等待视频播放完成
        if (!loop)
        {
            while (videoPlayer.isPlaying)
            {
                yield return null;
            }
        }
    }
    
    /// <summary>
    /// 视频播放完成回调
    /// </summary>
    private void OnVideoFinished(VideoPlayer vp)
    {
        isVideoPlaying = false;
        OnVideoComplete?.Invoke();
    }
    
    /// <summary>
    /// 手动播放视频（播放所有视频列表）
    /// </summary>
    public void PlayVideo()
    {
        if (videoPlayer != null && !isVideoPlaying && videoClips != null && videoClips.Count > 0)
        {
            StartCoroutine(PlayAllVideosAnimation());
        }
    }
    
    /// <summary>
    /// 停止视频播放
    /// </summary>
    public void StopVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            isVideoPlaying = false;
        }
    }
    
    /// <summary>
    /// 暂停视频播放
    /// </summary>
    public void PauseVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
    }
    
    /// <summary>
    /// 恢复视频播放
    /// </summary>
    public void ResumeVideo()
    {
        if (videoPlayer != null && !videoPlayer.isPlaying && isVideoPlaying)
        {
            videoPlayer.Play();
        }
    }
    
    /// <summary>
    /// 设置单个视频源（会清除之前的视频列表）
    /// </summary>
    public void SetVideoClip(VideoClip clip)
    {
        videoClips.Clear();
        if (clip != null)
        {
            videoClips.Add(clip);
        }
        
        if (videoPlayer != null && clip != null)
        {
            videoPlayer.clip = clip;
            
            // 重新创建RenderTexture
            if (videoDisplay != null)
            {
                if (videoRenderTexture != null)
                {
                    videoRenderTexture.Release();
                }
                videoRenderTexture = new RenderTexture((int)clip.width, (int)clip.height, 0);
                videoPlayer.targetTexture = videoRenderTexture;
                videoDisplay.texture = videoRenderTexture;
            }
        }
    }
    
    /// <summary>
    /// 设置多个视频源（依次播放）
    /// </summary>
    public void SetVideoClips(List<VideoClip> clips)
    {
        videoClips.Clear();
        if (clips != null)
        {
            videoClips.AddRange(clips);
        }
        
        // 设置第一个视频
        if (videoPlayer != null && videoClips.Count > 0 && videoClips[0] != null)
        {
            videoPlayer.clip = videoClips[0];
            
            if (videoDisplay != null)
            {
                if (videoRenderTexture != null)
                {
                    videoRenderTexture.Release();
                }
                videoRenderTexture = new RenderTexture((int)videoClips[0].width, (int)videoClips[0].height, 0);
                videoPlayer.targetTexture = videoRenderTexture;
                videoDisplay.texture = videoRenderTexture;
            }
        }
    }
    
    /// <summary>
    /// 添加视频到列表末尾
    /// </summary>
    public void AddVideoClip(VideoClip clip)
    {
        if (clip != null)
        {
            videoClips.Add(clip);
        }
    }
    
    /// <summary>
    /// 清空视频列表
    /// </summary>
    public void ClearVideoClips()
    {
        videoClips.Clear();
    }

    /// <summary>
    /// 缓动函数 - OutBack效果
    /// </summary>
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    /// <summary>
    /// 设置进度条完成回调
    /// </summary>
    public void SetOnCompleteCallback(Action callback)
    {
        OnProgressComplete = callback;
    }

    /// <summary>
    /// 获取当前是否正在显示
    /// </summary>
    public bool IsShowing => isShowing;
    
    /// <summary>
    /// 获取视频是否正在播放
    /// </summary>
    public bool IsVideoPlaying => isVideoPlaying;
    
    /// <summary>
    /// 设置视频播放完成回调
    /// </summary>
    public void SetOnVideoCompleteCallback(Action callback)
    {
        OnVideoComplete = callback;
    }
    
    private void OnDestroy()
    {
        // 清理RenderTexture
        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            Destroy(videoRenderTexture);
        }
        
        // 取消视频事件订阅
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}

