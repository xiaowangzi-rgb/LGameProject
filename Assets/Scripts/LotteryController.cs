using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 洞洞乐抽奖控制器 - 管理所有抽奖Item
/// </summary>
public class LotteryController : MonoBehaviour
{
    [Header("抽奖配置")]
    [SerializeField] private List<LotteryItem> lotteryItems = new List<LotteryItem>();
    [SerializeField] private int maxClickCount = 1;  // 最大可点击次数
    
    [Header("封面图标图集")]
    [SerializeField] private List<Sprite> coverIconSprites = new List<Sprite>();  // 封面图标Sprite集合
    
    [Header("奖池配置")]
    [SerializeField] private List<PrizeData> prizePool = new List<PrizeData>();   // 奖池（数量应小于item数量）
    [SerializeField] private Sprite emptyPrizeIcon;   // 空奖/幸运奖的图标
    [SerializeField] private string emptyPrizeName = "幸运奖";  // 空奖名称
    
    [Header("洗牌动画配置")]
    [SerializeField] private float shuffleScatterRadius = 3f;     // 打散半径
    [SerializeField] private float shuffleScatterDuration = 0.3f; // 打散动画时长
    [SerializeField] private float shuffleWaitDuration = 0.5f;    // 打散后等待时长
    [SerializeField] private float shuffleReturnDuration = 0.5f;  // 回到原位动画时长
    [SerializeField] private float shuffleItemDelay = 0.05f;      // 每个item之间的延迟
    [SerializeField] private int shuffleTimes = 5;                // 洗牌次数
    
    [Header("事件")]
    public UnityEvent<LotteryItem> onItemClicked;     // Item被点击时触发
    public UnityEvent<PrizeData> onPrizeWon;          // 抽中奖品时触发
    public UnityEvent onJackpotWon;                   // 抽中大奖时触发
    public UnityEvent onLotteryComplete;              // 抽奖完成时触发
    public UnityEvent onShuffleComplete;              // 洗牌完成时触发
    
    private int currentClickCount = 0;
    private bool isShuffling = false;                 // 是否正在洗牌
    private bool isLotteryActive = true;
    private bool isJackpotTriggered = false;          // 是否触发必中大奖
    
    // 已分配的奖品映射（itemIndex -> PrizeData）
    private Dictionary<int, PrizeData> assignedPrizes = new Dictionary<int, PrizeData>();
    // 大奖所在的item索引（触发大奖后使用）
    private int jackpotItemIndex = -1;

    private void Start()
    {
        InitializeLotteryItems();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TriggerJackpot();
        }
    }

    public void OnClickShuffle() {
        StartCoroutine(PlayShuffleAnimation());
    }

    /// <summary>
    /// 初始化所有抽奖Item，订阅点击事件
    /// </summary>
    private void InitializeLotteryItems()
    {
        // 清空并重新查找场景中所有的LotteryItem（不限于子物体）
        lotteryItems.Clear();
        LotteryItem[] allItems = FindObjectsOfType<LotteryItem>();
        lotteryItems.AddRange(allItems);
        
        // 按名称排序，确保顺序一致
        lotteryItems.Sort((a, b) => a.gameObject.name.CompareTo(b.gameObject.name));

        // 设置每个Item的索引（非常重要！）
        for (int i = 0; i < lotteryItems.Count; i++)
        {
            if (lotteryItems[i] != null)
            {
                lotteryItems[i].SetItemIndex(i);
                lotteryItems[i].SetOriginalPosition();
            }
        }

        // 随机分配封面图标
        AssignRandomCoverIcons();
        
        // 随机分配奖品到各个Item
        AssignRandomPrizes();

        // 订阅每个Item的点击事件
        foreach (var item in lotteryItems)
        {
            if (item != null)
            {
                item.OnItemClicked += HandleItemClicked;
            }
        }
    }

    /// <summary>
    /// 播放洗牌动画
    /// </summary>
    private IEnumerator PlayShuffleAnimation()
    {
        if (lotteryItems.Count == 0) yield break;
        
        isShuffling = true;
                    // 计算中心点
            Vector3 center = Vector3.zero;
            foreach (var item in lotteryItems)
            {
                if (item != null)
                {
                    center += item.transform.position;
                }
            }
            center /= lotteryItems.Count;

        
        // 生成随机打散位置
        List<Vector3> scatterPositions = new List<Vector3>();

        for (int j = 0; j < shuffleTimes; j++)
        {
            scatterPositions.Clear();

            foreach (var item in lotteryItems)
            {
                if (item != null)
                {
                    // 随机角度和距离
                    float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    float distance = Random.Range(shuffleScatterRadius * 0.5f, shuffleScatterRadius);
                    Vector3 scatterPos = center + new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0);
                    scatterPositions.Add(scatterPos);
                }
            }

            // 阶段1：打散 - 所有item移动到随机位置
            float elapsed = 0f;
            List<Vector3> originalPositions = new List<Vector3>();
            foreach (var item in lotteryItems)
            {
                originalPositions.Add(item != null ? item.transform.position : Vector3.zero);
            }

            while (elapsed < shuffleScatterDuration)
            {
                float progress = elapsed / shuffleScatterDuration;

                for (int i = 0; i < lotteryItems.Count; i++)
                {
                    if (lotteryItems[i] != null)
                    {
                        lotteryItems[i].transform.position = Vector3.Lerp(originalPositions[i], scatterPositions[i], progress);
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 确保到达目标位置
            for (int i = 0; i < lotteryItems.Count; i++)
            {
                if (lotteryItems[i] != null)
                {
                    lotteryItems[i].transform.position = scatterPositions[i];
                }
            }
        }


        // 阶段2：等待一下
        yield return new WaitForSeconds(shuffleWaitDuration);
        
        // 阶段3：回到原位 - 依次移动回原来的位置
        for (int i = 0; i < lotteryItems.Count; i++)
        {
            if (lotteryItems[i] != null)
            {
                StartCoroutine(MoveItemToOriginalPosition(lotteryItems[i], scatterPositions[i]));
                yield return new WaitForSeconds(shuffleItemDelay);
            }
        }
        
        // 等待最后一个动画完成
        yield return new WaitForSeconds(shuffleReturnDuration);
        
        isShuffling = false;
        onShuffleComplete?.Invoke();
    }

    /// <summary>
    /// 将单个item移动回原始位置
    /// </summary>
    private IEnumerator MoveItemToOriginalPosition(LotteryItem item, Vector3 fromPosition)
    {
        if (item == null) yield break;
        
        Vector3 toPosition = item.GetOriginalPosition();
        float elapsed = 0f;
        
        while (elapsed < shuffleReturnDuration)
        {
            float progress = elapsed / shuffleReturnDuration;
            float easedProgress = EaseOutBack(progress);
            
            item.transform.position = Vector3.Lerp(fromPosition, toPosition, easedProgress);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        item.transform.position = toPosition;
    }

    /// <summary>
    /// 缓动函数 - OutBack效果（有弹性）
    /// </summary>
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    /// <summary>
    /// 随机分配奖品到各个Item
    /// </summary>
    private void AssignRandomPrizes()
    {
        assignedPrizes.Clear();
        jackpotItemIndex = -1;
        
        if (lotteryItems.Count == 0) return;

        // 创建可用奖品列表（不包含大奖，因为大奖需要特殊处理）
        List<PrizeData> normalPrizes = new List<PrizeData>();
        PrizeData jackpotPrize = null;
        
        foreach (var prize in prizePool)
        {
            if (prize.IsJackpot)
            {
                jackpotPrize = prize;
            }
            else
            {
                normalPrizes.Add(prize);
            }
        }

        // 创建所有item索引的列表
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < lotteryItems.Count; i++)
        {
            availableIndices.Add(i);
        }
        
        // 打乱索引顺序
        ShuffleList(availableIndices);

        // 如果有大奖，先随机分配大奖到一个位置（但正常情况下抽不到）
        if (jackpotPrize != null && availableIndices.Count > 0)
        {
            jackpotItemIndex = availableIndices[0];
            assignedPrizes[jackpotItemIndex] = jackpotPrize;
            availableIndices.RemoveAt(0);
            
            // 设置大奖Item的奖品图标
            if (lotteryItems[jackpotItemIndex] != null)
            {
                lotteryItems[jackpotItemIndex].SetPrize(jackpotPrize);
            }
        }

        // 打乱普通奖品顺序
        ShuffleList(normalPrizes);

        // 分配普通奖品
        int prizeIndex = 0;
        for (int i = 0; i < availableIndices.Count; i++)
        {
            int itemIdx = availableIndices[i];
            PrizeData prize;
            
            if (prizeIndex < normalPrizes.Count)
            {
                // 分配普通奖品
                prize = normalPrizes[prizeIndex];
                prizeIndex++;
            }
            else
            {
                // 超出奖品数量，分配空奖
                prize = new PrizeData(emptyPrizeName, emptyPrizeIcon, false, true);
            }
            
            assignedPrizes[itemIdx] = prize;
            
            if (lotteryItems[itemIdx] != null)
            {
                lotteryItems[itemIdx].SetPrize(prize);
            }
        }
    }

    /// <summary>
    /// 打乱列表顺序（Fisher-Yates洗牌算法）
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    /// <summary>
    /// 随机分配封面图标给每个Item（不重复）
    /// </summary>
    private void AssignRandomCoverIcons()
    {
        if (coverIconSprites == null || coverIconSprites.Count == 0)
        {
            return;
        }

        // 创建一个可用Sprite的副本列表，用于随机不重复选择
        List<Sprite> availableSprites = new List<Sprite>(coverIconSprites);
        
        // 打乱顺序（Fisher-Yates洗牌算法）
        for (int i = availableSprites.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            // 交换
            Sprite temp = availableSprites[i];
            availableSprites[i] = availableSprites[randomIndex];
            availableSprites[randomIndex] = temp;
        }

        // 分配给每个Item
        for (int i = 0; i < lotteryItems.Count; i++)
        {
            if (lotteryItems[i] != null && i < availableSprites.Count)
            {
                lotteryItems[i].SetCoverIcon(availableSprites[i]);
            }
        }
    }

    /// <summary>
    /// 处理Item点击事件
    /// </summary>
    private void HandleItemClicked(LotteryItem clickedItem)
    {
        // 记录本次点击是否是触发大奖后的点击
        bool wasJackpotTriggeredThisClick = isJackpotTriggered;

        // 【重要】无论抽奖是否结束，都要处理大奖保护逻辑
        // 检查是否触发了必中大奖
        if (isJackpotTriggered)
        {
            // 触发了大奖，需要交换奖品到点击位置
            SwapPrizeToJackpot(clickedItem.ItemIndex);
            // 使用后重置触发状态
            isJackpotTriggered = false;
        }
        else
        {
            // 没有触发大奖时，如果点到了大奖位置，把大奖换走（确保抽不到大奖）
            if (clickedItem.ItemIndex == jackpotItemIndex)
            {
                SwapJackpotAway(clickedItem.ItemIndex);
            }
        }

        // 如果抽奖已结束，不增加计数，直接返回
        if (!isLotteryActive)
        {
            return;
        }

        currentClickCount++;
        
        // 获取该Item的奖品
        PrizeData prize = GetPrizeForItem(clickedItem.ItemIndex);
        
        // 最终检查：如果没有触发大奖但抽到了大奖，强制替换为空奖
        if (prize != null && prize.IsJackpot && !wasJackpotTriggeredThisClick)
        {
            prize = new PrizeData(emptyPrizeName, emptyPrizeIcon, false, true);
            assignedPrizes[clickedItem.ItemIndex] = prize;
            clickedItem.SetPrize(prize);
        }
        
        // 触发点击事件
        onItemClicked?.Invoke(clickedItem);
        
        // 触发奖品事件
        if (prize != null)
        {
            onPrizeWon?.Invoke(prize);
            
            if (prize.IsJackpot)
            {
                onJackpotWon?.Invoke();
            }
        }
        
        // 检查是否达到最大点击次数
        if (currentClickCount >= maxClickCount)
        {
            CompleteLottery();
        }
    }

    /// <summary>
    /// 将点击位置的奖品与大奖位置交换
    /// </summary>
    private void SwapPrizeToJackpot(int clickedIndex)
    {
        if (jackpotItemIndex < 0)
        {
            return;
        }
        
        if (!assignedPrizes.ContainsKey(jackpotItemIndex))
        {
            return;
        }

        // 如果点击的就是大奖位置，不需要交换
        if (clickedIndex == jackpotItemIndex)
        {
            return;
        }

        // 获取当前位置的奖品和大奖
        PrizeData clickedPrize = assignedPrizes.ContainsKey(clickedIndex) ? assignedPrizes[clickedIndex] : null;
        PrizeData jackpotPrize = assignedPrizes[jackpotItemIndex];

        // 交换奖品数据
        assignedPrizes[clickedIndex] = jackpotPrize;
        if (clickedPrize != null)
        {
            assignedPrizes[jackpotItemIndex] = clickedPrize;
        }

        // 更新点击位置的Item显示为大奖
        if (lotteryItems[clickedIndex] != null)
        {
            lotteryItems[clickedIndex].SetPrize(jackpotPrize);
        }
        
        // 更新原大奖位置的Item显示为普通奖品
        if (lotteryItems[jackpotItemIndex] != null && clickedPrize != null)
        {
            lotteryItems[jackpotItemIndex].SetPrize(clickedPrize);
        }

        // 更新大奖位置记录
        jackpotItemIndex = clickedIndex;
    }

    /// <summary>
    /// 把大奖从点击位置换走（确保未触发时抽不到大奖）
    /// </summary>
    private void SwapJackpotAway(int clickedIndex)
    {
        // 找一个未被点击的位置来放大奖
        int newJackpotIndex = -1;
        for (int i = 0; i < lotteryItems.Count; i++)
        {
            if (i != clickedIndex && lotteryItems[i] != null && !lotteryItems[i].IsClicked)
            {
                newJackpotIndex = i;
                break;
            }
        }

        if (newJackpotIndex < 0)
        {
            // 没有其他未点击的位置了，给用户一个空奖
            PrizeData emptyPrize = new PrizeData(emptyPrizeName, emptyPrizeIcon, false, true);
            assignedPrizes[clickedIndex] = emptyPrize;
            lotteryItems[clickedIndex].SetPrize(emptyPrize);
            jackpotItemIndex = -1;  // 大奖没地方放了
            return;
        }

        // 获取新位置的奖品和大奖
        PrizeData newPositionPrize = assignedPrizes.ContainsKey(newJackpotIndex) ? assignedPrizes[newJackpotIndex] : null;
        PrizeData jackpotPrize = assignedPrizes[clickedIndex];

        // 交换：把大奖放到新位置，把新位置的奖品放到点击位置
        assignedPrizes[newJackpotIndex] = jackpotPrize;
        if (newPositionPrize != null)
        {
            assignedPrizes[clickedIndex] = newPositionPrize;
            lotteryItems[clickedIndex].SetPrize(newPositionPrize);
        }
        else
        {
            // 新位置没有奖品，给点击位置一个空奖
            PrizeData emptyPrize = new PrizeData(emptyPrizeName, emptyPrizeIcon, false, true);
            assignedPrizes[clickedIndex] = emptyPrize;
            lotteryItems[clickedIndex].SetPrize(emptyPrize);
        }

        // 更新新位置的Item显示
        if (lotteryItems[newJackpotIndex] != null)
        {
            lotteryItems[newJackpotIndex].SetPrize(jackpotPrize);
        }

        // 更新大奖位置
        jackpotItemIndex = newJackpotIndex;
    }

    /// <summary>
    /// 获取指定Item的奖品
    /// </summary>
    private PrizeData GetPrizeForItem(int itemIndex)
    {
        if (assignedPrizes.ContainsKey(itemIndex))
        {
            return assignedPrizes[itemIndex];
        }
        return null;
    }

    /// <summary>
    /// 触发必中大奖 - 调用此方法后，下一次点击必定抽中大奖
    /// </summary>
    public void TriggerJackpot()
    {
        if (jackpotItemIndex < 0)
        {
            return;
        }
        
        // 检查大奖是否已经被抽走了
        if (!assignedPrizes.ContainsKey(jackpotItemIndex) || !assignedPrizes[jackpotItemIndex].IsJackpot)
        {
            return;
        }
        
        // 如果抽奖已结束，重新激活（但不重置格子状态）
        if (!isLotteryActive)
        {
            isLotteryActive = true;
        }
        
        isJackpotTriggered = true;
    }

    /// <summary>
    /// 取消必中大奖触发
    /// </summary>
    public void CancelJackpotTrigger()
    {
        isJackpotTriggered = false;
    }

    /// <summary>
    /// 检查是否已触发必中大奖
    /// </summary>
    public bool IsJackpotTriggered()
    {
        return isJackpotTriggered;
    }

    /// <summary>
    /// 完成抽奖
    /// </summary>
    private void CompleteLottery()
    {
        isLotteryActive = false;
        onLotteryComplete?.Invoke();
    }

    /// <summary>
    /// 重置抽奖
    /// </summary>
    public void ResetLottery()
    {
        currentClickCount = 0;
        isLotteryActive = true;
        isJackpotTriggered = false;
        
        foreach (var item in lotteryItems)
        {
            if (item != null)
            {
                item.ResetItem();
            }
        }
        
        // 重新随机分配封面图标
        AssignRandomCoverIcons();
        
        // 重新随机分配奖品
        AssignRandomPrizes();
    }

    /// <summary>
    /// 设置最大点击次数
    /// </summary>
    public void SetMaxClickCount(int count)
    {
        maxClickCount = Mathf.Max(1, count);
    }

    /// <summary>
    /// 获取当前点击次数
    /// </summary>
    public int GetCurrentClickCount()
    {
        return currentClickCount;
    }

    /// <summary>
    /// 获取剩余点击次数
    /// </summary>
    public int GetRemainingClickCount()
    {
        return Mathf.Max(0, maxClickCount - currentClickCount);
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        foreach (var item in lotteryItems)
        {
            if (item != null)
            {
                item.OnItemClicked -= HandleItemClicked;
            }
        }
    }
}
