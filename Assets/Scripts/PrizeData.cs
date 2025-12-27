using System;
using UnityEngine;

/// <summary>
/// 奖品数据
/// </summary>
[Serializable]
public class PrizeData
{
    [SerializeField] private string prizeName;        // 奖品名称
    [SerializeField] private Sprite prizeIcon;        // 奖品图标
    [SerializeField] private bool isJackpot;          // 是否是超级大奖
    [SerializeField] private bool isEmptyPrize;       // 是否是空奖（幸运奖）

    public string PrizeName => prizeName;
    public Sprite PrizeIcon => prizeIcon;
    public bool IsJackpot => isJackpot;
    public bool IsEmptyPrize => isEmptyPrize;

    public PrizeData(string name, Sprite icon, bool jackpot = false, bool empty = false)
    {
        prizeName = name;
        prizeIcon = icon;
        isJackpot = jackpot;
        isEmptyPrize = empty;
    }
}

