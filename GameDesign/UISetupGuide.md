# UI配置指南

## 快速配置UI

### 方法一：使用编辑器工具（推荐）

1. 打开Unity编辑器
2. 点击菜单：`Tools > UI Setup > Configure UI`
3. 选择需要创建的UI组件（默认全部选中）
4. 点击"一键配置UI"按钮

### 方法二：运行时自动配置

1. 创建一个空物体，命名为"UISetup"
2. 添加 `UIAutoSetup` 脚本
3. 运行游戏时会自动配置所有UI

## UI组件说明

### 1. HPBar (血条)
- 位置：左上角
- 功能：显示玩家生命值
- 特性：平滑动画、颜色变化（绿→黄→红）

### 2. StaminaBar (耐力条)
- 位置：血条下方
- 功能：显示耐力值
- 特性：力竭时闪烁警告

### 3. ComboCounter (连击计数器)
- 位置：右上角
- 功能：显示当前连击数
- 特性：DOTween动画、等级颜色变化

### 4. SkillBar (技能栏)
- 位置：底部中央
- 功能：6个技能槽 (Q/W/E/R/T/F)
- 特性：冷却显示、按键提示

### 5. DamageText (伤害数字)
- 位置：动态生成
- 功能：显示伤害数值
- 需要：创建预制体后配置到UIManager

## 手动绑定检查清单

运行游戏后，检查以下绑定是否正确：

- [ ] UIManager.hpBar 已赋值
- [ ] UIManager.staminaBar 已赋值
- [ ] UIManager.comboCounter 已赋值
- [ ] UIManager.skillBar 已赋值
- [ ] HPBar.hpSlider 已赋值
- [ ] HPBar.fillImage 已赋值
- [ ] StaminaBar.staminaSlider 已赋值
- [ ] ComboCounter.comboText 已赋值
- [ ] ComboCounter.canvasGroup 已赋值

## 故障排除

### UI不显示
1. 检查Canvas的Render Mode是否为Screen Space - Overlay
2. 检查CanvasScaler设置是否正确
3. 检查UI元素是否在Canvas下

### UI不更新
1. 检查UIManager是否正确引用
2. 检查PlayerHealth/StaminaSystem是否有事件触发
3. 查看Console是否有错误日志

### 事件未触发
1. 确认GameEvents脚本已正确配置
2. 检查Player脚本是否正确调用事件

## 测试步骤

1. 配置完成后运行游戏
2. 检查左上角是否有绿色血条显示 "100/100"
3. 检查血条下方是否有黄色耐力条
4. 尝试攻击敌人，检查右上角是否显示连击数
5. 检查底部是否有6个技能槽位

## 注意事项

- UI配置工具会自动创建所有必要的GameObject
- 所有UI元素会在场景保存时保留
- 建议在配置完成后保存场景（Ctrl+S）
- 使用版本控制时，记得提交.meta文件

## 扩展UI

如需添加更多UI元素：

1. 在UIManager中添加新UI组件的引用
2. 创建对应的UI脚本（继承MonoBehaviour）
3. 在UISetupEditor中添加创建逻辑
4. 运行配置工具或手动配置
