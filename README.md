# RhythmBase.Addition

Added some tools into [RhythmBase](https://www.nuget.org/packages/RhythmBase/).

Sponsored by RhythmBase Project. Visit [RhythmBase](https://github.com/RDCN-Community-Developers/RhythmToolkit) for more info.

**[Core Library →](https://github.com/RDCN-Community-Developers/RhythmToolkit)**  

## Features

### Tools

- **AssetManager**: A tool to manage assets in RhythmBase.  
  Since RhythmBase doesn't give a way and no plan to manage assets, AssetManager is created to fill this gap.
- **WaveDataUtils**: Get wave data from audio files.  
  Usually used to make audio visualization.  
- **SpriteSheetCanvas**: A sprite builder that can build a sprite easily.  
  You can use it to put lots of images in different size into one sprite sheet.  
  No need to focus on the position of each image.  
- **ParticleGenerator**: A tool to generate and manage particles.  
  You can use it to create particle effects easily.
- **DecorationPool**: A tool to manage reusable decorations.  
  Usually used to manage decorations that are frequently shown and hiden, such as bullets, enemies, etc.  
- **RDExpressionBuilder**: An expression builder system.  
  With the help of IDE's intelligent tips, building complex expressions becomes easier and more intuitive, and it comes with simple automatic simplification.

## TODO

- **OneshotHelper**: A tool to manage oneshot patterns.  
- **PerspectiveBuilder**: A tool to build decorations with perspective easily.  
- **CharacterFontManager**: A tool to manage character font sprites.  
- **Some other useful tools**.

## Related Projects

- [RhythmBase](https://github.com/RDCN-Community-Developers/RhythmToolkit) - Core library for Rhythm Doctor level manipulation

---

# RhythmBase.Addition（中文）

为 [RhythmBase](https://www.nuget.org/packages/RhythmBase/) 增加了一些工具。

## 功能

### 工具

- **AssetManager**：RhythmBase 的资源管理工具。  
  由于 RhythmBase 没有资源管理方案和计划，AssetManager 用于填补这一空白。
- **WaveDataUtils**：从音频文件获取波形相关数据。  
  通常用于音频可视化和 BPM 测量。  
- **SpriteSheetCanvas**：精灵图构建工具。  
  可以轻松将不同尺寸的图片合并到一个精灵图中，无需关心每张图片的位置。
- **ParticleGenerator**：粒子生成与管理工具。  
  可用于快速创建粒子特效。
- **DecorationPool**：可复用装饰管理工具。  
  通常用于频繁显示/隐藏的装饰管理，如子弹、敌人等。
- **Perspective.Builder(Part1)**：轻松构建带有透视效果的装饰。  
  基于房间拉抻的平行透视效果实现。  
- **TextFontManager**：字符字体装饰管理工具。  
  用于创建并管理自定义字体装饰，结合 **DecorationPool** 实现自定义动画效果。  
- **RDExpressionBuilder**: 表达式构建系统。  
  借助 IDE 的智能提示功能，构建复杂的表达式变得更加简单和直观，并且自带简单的自动化简。

## TODO

- **Perspective.Builder(Part2,3)**：基于房间视角的平行透视和点透视效果实现。  
- **OneshotHelper**：单发拍的模式管理工具。  
- **其他实用工具**。

## 相关项目

- [RhythmBase](https://github.com/RDCN-Community-Developers/RhythmToolkit) - Rhythm Doctor 关卡操作核心库。

<p align="center">Made with ❤️ for the Rhythm Doctor community</p>