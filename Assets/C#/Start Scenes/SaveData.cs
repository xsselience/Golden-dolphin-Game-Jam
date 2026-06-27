using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveData
{ 
    // 玩家
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;
    public int playerHealth;
    public int hackCount;
    public int cyberPower;
    public bool cyberSystemEnabled;

    // Boss 状态
    public bool boss1Dead;
    public bool boss2Dead;

    // 传送门状态（简单记：已激活数量）
    public int portalsActivated;

    // 场景
    public string sceneName;

    // 存档时间
    public string saveTime;

    // 槽位空标记
    public bool isEmpty = true;
}
