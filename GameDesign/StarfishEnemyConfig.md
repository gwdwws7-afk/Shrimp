# 海星人敌人 (Starfish Man) 生成配置

## 🎯 AI生成提示词

### 模型生成 (Meshy AI)
**提示词**：
```
Starfish humanoid warrior, deep sea mutant creature, five arms with sharp claws, 
organic armor made of coral and shells, bioluminescent spots, dark blue and purple color scheme,
game character design, 3D render, PBR materials, front and side view, 8K details
```

**参数**：
- 多边形数: 6000-8000
- 格式: FBX
- 包含: 漫反射贴图、法线贴图

### 贴图生成

#### Albedo (基础颜色)
**提示词**：
```
Starfish creature skin texture, deep sea blue purple gradient, 
rough wet surface, small bioluminescent spots, organic patterns,
2K resolution, seamless, PBR texture
```

#### Normal (法线)
**提示词**：
```
Starfish skin normal map, rough bumpy surface, organic texture,
2K resolution, seamless
```

#### Metallic (金属度)
**提示词**：
```
Starfish armor metallic map, shell and coral pieces, 
variable roughness, 2K resolution
```

---

## 📦 文件命名规范

生成后文件应该命名为：
```
ENM_Starfish_01.fbx           # 模型
ENM_Starfish_01_Albedo.png    # 漫反射
ENM_Starfish_01_Normal.png    # 法线
ENM_Starfish_01_Metallic.png  # 金属度
ENM_Starfish_01_Roughness.png # 粗糙度
```

---

## 🚀 快速生成脚本

使用 Meshy AI 批量生成：
1. 打开 https://www.meshy.ai/
2. 选择 "Text to 3D"
3. 粘贴上面的提示词
4. 等待生成完成
5. 下载 FBX + 贴图

---

## 📋 Unity导入检查清单

导入到 `Assets/Models/Enemies/Starfish/` 后检查：

- [ ] FBX 模型导入设置正确
- [ ] 贴图分辨率是 2K (2048x2048)
- [ ] 贴图格式设置为 Texture (不是 Sprite)
- [ ] 材质球使用 Standard Shader
- [ ] 所有贴图槽位已绑定
- [ ] 模型缩放正确 (1 unit = 1 meter)
- [ ] 碰撞体已添加
- [ ] 动画已绑定

---

## 🎨 材质参数参考

```yaml
Shader: Standard

Albedo (RGB):
  - Color: #2A2A4A (深蓝紫)
  - Map: ENM_Starfish_01_Albedo.png

Metallic (R):
  - Value: 0.3
  - Map: ENM_Starfish_01_Metallic.png
  
Smoothness (A):
  - Value: 0.4
  - Map: ENM_Starfish_01_Roughness.png (invert)

Normal Map:
  - Map: ENM_Starfish_01_Normal.png
  - Scale: 1.0

Emission (Optional):
  - Color: #00FFFF (青色发光点)
  - Map: ENM_Starfish_01_Emissive.png
```

---

## ⚔️ 敌人属性设计

**海星人 (Starfish Man)**
```yaml
Type: Grunt (杂兵)
HP: 40 (比标准50低，因为海星人较脆弱)
Damage: 8
Speed: 3 (较慢)
Attack Range: 1.5
Special: 
  - 再生能力: 每秒回血1点
  - 死亡分裂: 死亡时分裂成2个小海星
```

---

## 🎬 动画需求

需要的动画状态：
1. **Idle** - 待机，触手摆动
2. **Walk** - 缓慢移动
3. **Attack** - 用触手刺击
4. **Hit** - 受击
5. **Death** - 死亡，身体瓦解

---

## 🔧 下一步操作

1. 使用 Meshy AI 生成模型和贴图
2. 导入到 Unity `Assets/Models/Enemies/Starfish/`
3. 运行我提供的配置脚本
4. 创建 Prefab
5. 测试!

需要我帮你写自动配置脚本吗？
