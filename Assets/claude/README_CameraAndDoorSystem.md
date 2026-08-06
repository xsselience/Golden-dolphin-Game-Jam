# 空洞骑士风格摄像机系统 —— 完整使用指南

## 设计原理

模仿 Hollow Knight 的房间摄像机系统：

```
┌──────────────────────┐     ┌──────────────────────┐
│  Zone_Lobby          │ door│  Zone_HiddenRoom      │
│  startsHidden=false  │ ──► │  startsHidden=true    │
│                      │     │  (黑色遮罩覆盖)       │
│  Polygon 边界 =      │     │  Polygon 边界 =       │
│  大厅区域            │     │  隐藏房间区域         │
└──────────────────────┘     └──────────────────────┘
         ↑                           ↑
    初始当前区域               HiddenWall 破坏后解锁
```

核心三点：
1. **每个房间 = 一个 CameraZone**（独立的 PolygonCollider2D 边界）
2. **进新房间 → 自动切换摄像机边界**
3. **隐藏区域初始被黑色遮罩覆盖 → 解锁后遮罩消失 → 区域可进入**

---

## 📁 文件

| 文件 | 作用 |
|---|---|
| `CameraZone.cs` | 单个房间的摄像机边界定义 |
| `CameraZoneManager.cs` | 管理器：区域切换 + 死区 + 遮罩 |
| `HiddenWall.cs` | 隐藏墙：被攻击后解锁对应 CameraZone |
| `OneWayDoor.cs` | 单侧门：攻击后打开（独立功能） |

---

## 搭建步骤

### 前置

- ✅ vcam 已存在
- ✅ vcam 上挂 CinemachineConfiner2D

---

### 第 1 步：创建 CameraZoneManager

1. Hierarchy → 创建空物体 → 命名 **`CameraZoneManager`**
2. 挂 `CameraZoneManager.cs`
3. 参数保持默认即可

---

### 第 2 步：为每个房间创建 CameraZone

**普通房间（初始可见）：**

1. 创建空物体 → 命名 `Zone_Lobby`
2. 挂 `CameraZone.cs`
3. 添加 `Polygon Collider 2D`
4. 点击 `Edit Collider` → 在 Scene 视图中拖动绿点，画出房间的摄像机边界
5. 设置：
   - `Zone Id` = `"Zone_Lobby"`
   - `Zone Display Name` = `"大厅"`
   - `Starts Hidden` = **不勾选**

**隐藏房间（初始被遮罩覆盖）：**

1. 创建空物体 → 命名 `Zone_HiddenRoom`
2. 挂 `CameraZone.cs`
3. 添加 PolygonCollider2D，画出隐藏区域的边界
4. 设置：
   - `Zone Id` = `"Zone_HiddenRoom"`
   - `Starts Hidden` = **✓ 勾选**
   - `Linked Hidden Wall Id` = `"HiddenWall_01"`

---

### 第 3 步：创建隐藏墙

1. 创建空物体 → 命名 `HiddenWall_01`
2. 挂 `Box Collider 2D` + `HiddenWall.cs`
3. 添加 `Sprite Renderer` → 放墙壁贴图
4. Layer 设为能被武器打到的层（如 `Enemy` 层）
5. 设置：
   - `Wall Id` = `"HiddenWall_01"`
   - `Linked Zone Id` = `"Zone_HiddenRoom"`

---

### 第 4 步：创建黑色遮罩预制体

在 Project 中：
1. 右键 → Create → 2D → Sprites → Square
2. 命名为 `DarkOverlay`，拖到 Prefabs 文件夹
3. Color 设为纯黑 (0,0,0,1)
4. 拖入 CameraZoneManager 的 `Dark Overlay Prefab` 字段

---

### ⚠ 清理旧系统

删掉之前创建的：
- `CameraConfiner` 物体
- `DynamicCameraBounds` 物体
- `Boundary_Main` 子物体

vcam 上的 `CinemachineConfiner2D` 保留。

---

## 参数速查

### CameraZoneManager

| 参数 | 说明 | 建议值 |
|---|---|---|
| Virtual Camera | Cinemachine vcam | 自动查找 |
| Dead Zone Width | X 轴死区 | 2 |
| Dead Zone Height | Y 轴死区 | 1.5 |
| Zone Transition Smooth Time | 切换平滑时间 | 0.4 |
| Dark Overlay Prefab | 黑色遮罩预制体 | 拖入 |

### CameraZone

| 参数 | 说明 |
|---|---|
| Zone Id | 唯一ID |
| Starts Hidden | 初始是否隐藏 |
| Linked Hidden Wall Id | 关联隐藏墙ID |
| Zone Type | Normal / Transition |

### HiddenWall

| 参数 | 说明 |
|---|---|
| Wall Id | 唯一ID |
| Linked Zone Id | 解锁的 CameraZone ID |
| Layer | 设为 Enemy（可被武器打到） |

---

## 完整数据流

```
玩家攻击 HiddenWall
    │
    ▼
HiddenWall.OnTriggerEnter2D → playerweapon 命中
    │
    ▼
StartCoroutine(DestroyWall)
    │
    ├─ 播放粒子/音效/渐隐
    ├─ CameraZoneManager.Instance.UnlockZone("Zone_HiddenRoom")
    │       │
    │       ├─ zone.isUnlocked = true
    │       ├─ 黑色遮罩渐隐消失
    │       └─ 玩家走入 Polygon → CheckZoneSwitch() →  检查到玩家点在 Polygon 内 → 切换到新区域
    │
    └─ Destroy(gameObject)
```

---

## 常见问题

| 问题 | 解决 |
|---|---|
| 进了新区域摄像机没切换 | ① CameraZone 的 Zone Type 设为 Normal（非 Transition） ② Zone 的 startsHidden=false 或已被 UnlockZone 解锁 |
| 黑色遮罩不消失 | ① HiddenWall 的 Linked Zone Id 和 CameraZone 的 Zone Id 是否一致 ② CameraZoneManager 的 Dark Overlay Prefab 是否赋值 |
| 武器打不到隐藏墙 | 隐藏墙的 Layer 必须是武器能命中的层 |
