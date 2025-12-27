using System;
using UnityEngine;

/// <summary>
/// 洞洞乐抽奖项目 - 挂载在每个可点击的Sprite上
/// 需要在GameObject上添加Collider2D组件（如BoxCollider2D或CircleCollider2D）
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LotteryItem : MonoBehaviour
{
    [Header("基础设置")]
    [SerializeField] private int itemIndex;           // 当前item的索引
    [SerializeField] private string prizeName;        // 奖品名称
    [SerializeField] private SpriteRenderer coverIconRender;
    [SerializeField] private SpriteRenderer rewardIconRender;
    
    [Header("状态")]
    [SerializeField] private bool isClicked = false;  // 是否已被点击
    
    // 当前分配的奖品
    private PrizeData currentPrize;
    // 原始位置
    private Vector3 originalPosition;
    
    // 点击事件，供外部订阅
    public event Action<LotteryItem> OnItemClicked;
    
    public int ItemIndex => itemIndex;
    public string PrizeName => currentPrize != null ? currentPrize.PrizeName : prizeName;
    public bool IsClicked => isClicked;
    public PrizeData CurrentPrize => currentPrize;

    private void Start()
    {
        // 初始化：隐藏奖励图标
        if (rewardIconRender != null)
        {
            SetSpriteAlpha(rewardIconRender, 0f);
        }
    }

    /// <summary>
    /// 鼠标点击检测 - 需要Collider2D组件
    /// </summary>
    private void OnMouseDown()
    {
        if (isClicked)
        {
            return;
        }

        HandleClick();
    }

    /// <summary>
    /// 处理点击逻辑
    /// </summary>
    private void HandleClick()
    {
        isClicked = true;
        
        // 先触发点击事件，让Controller处理奖品交换逻辑
        OnItemClicked?.Invoke(this);
        
        // 播放点击动画序列：先缩放，再渐隐
        StartCoroutine(PlayClickSequence());
    }

    /// <summary>
    /// 播放点击动画序列
    /// </summary>
    private System.Collections.IEnumerator PlayClickSequence()
    {
        // 先播放缩放动画
        yield return StartCoroutine(PlayScaleAnimation());
        
        // 然后再播放揭开动画效果
        yield return StartCoroutine(RevealAnimationCoroutine());

        // 如果是大奖 则播放大奖动画
        if (currentPrize != null && currentPrize.IsJackpot) {
            JackpotWinPanel.Instance.Show();
        }
    }

    /// <summary>
    /// 播放点击缩放动画
    /// </summary>
    private System.Collections.IEnumerator PlayScaleAnimation()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 0.85f;  // 缩小到85%
        float duration = 0.1f;
        float elapsed = 0f;
        
        // 缩小
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetScale;
        
        // 放大回原来
        elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originalScale;
    }

    /// <summary>
    /// 播放揭开动画效果 - 封面淡出，奖励淡入
    /// </summary>
    private void PlayRevealAnimation()
    {
        StartCoroutine(RevealAnimationCoroutine());
    }

    private System.Collections.IEnumerator RevealAnimationCoroutine()
    {
        float duration = 0.5f;  // 动画持续时间
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            
            // 封面背景和图标淡出 (1 -> 0)
            float coverAlpha = 1f - progress;
            if (coverIconRender != null)
            {
                SetSpriteAlpha(coverIconRender, coverAlpha);
            }
            
            // 奖励图标淡入 (0 -> 1)
            float rewardAlpha = progress;
            if (rewardIconRender != null)
            {
                SetSpriteAlpha(rewardIconRender, rewardAlpha);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 确保最终状态正确
        if (coverIconRender != null)
        {
            SetSpriteAlpha(coverIconRender, 0f);
        }
        if (rewardIconRender != null)
        {
            SetSpriteAlpha(rewardIconRender, 1f);
        }
    }

    /// <summary>
    /// 设置SpriteRenderer的透明度
    /// </summary>
    private void SetSpriteAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null) return;
        
        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }

    /// <summary>
    /// 初始化Item
    /// </summary>
    public void Initialize(int index, string prize)
    {
        itemIndex = index;
        prizeName = prize;
        isClicked = false;
        
        // 重置透明度状态
        ResetAlphaState();
    }

    /// <summary>
    /// 设置封面图标
    /// </summary>
    public void SetCoverIcon(Sprite sprite)
    {
        if (coverIconRender != null && sprite != null)
        {
            coverIconRender.sprite = sprite;
        }
    }

    /// <summary>
    /// 设置奖品
    /// </summary>
    public void SetPrize(PrizeData prize)
    {
        currentPrize = prize;
        
        if (prize != null)
        {
            prizeName = prize.PrizeName;
            
            // 设置奖励图标（即使为null也要更新，防止显示旧图标）
            if (rewardIconRender != null)
            {
                rewardIconRender.sprite = prize.PrizeIcon;
            }
        }
    }

    /// <summary>
    /// 设置item索引
    /// </summary>
    public void SetItemIndex(int index)
    {
        itemIndex = index;
    }

    /// <summary>
    /// 设置原始位置
    /// </summary>
    public void SetOriginalPosition() {
        originalPosition = transform.position;
    }

    /// <summary>
    /// 获取原始位置
    /// </summary>
    public Vector3 GetOriginalPosition()
    {
        return originalPosition;
    }

    /// <summary>
    /// 重置Item状态
    /// </summary>
    public void ResetItem()
    {
        isClicked = false;
        ResetAlphaState();
    }

    /// <summary>
    /// 重置透明度状态 - 封面显示，奖励隐藏
    /// </summary>
    private void ResetAlphaState()
    {
        if (coverIconRender != null)
        {
            SetSpriteAlpha(coverIconRender, 1f);
        }
        if (rewardIconRender != null)
        {
            SetSpriteAlpha(rewardIconRender, 0f);
        }
    }
}
