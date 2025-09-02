# FFramework.Kit PoolKit 对象池模块文档

---

## 一、简介

PoolKit 是 FFramework 的对象池工具，专为高效管理和复用游戏对象设计，适用于特效、子弹、UI 等场景，显著减少实例化和销毁带来的性能损耗。

---

## 二、核心类说明

- **ObjectPoolKit**：对象池管理器，负责对象的获取、回收和池的维护。
- **PoolRoot**：对象池根节点，所有池对象的父级，便于统一管理和场景清理。

---

## 三、快速上手

### 1. 获取对象

```csharp
GameObject obj = ObjectPoolKit.Spawn("PrefabName");
```

### 2. 回收对象

```csharp
ObjectPoolKit.Recycle(obj);
```

### 3. 预加载对象池

```csharp
ObjectPoolKit.Preload("PrefabName", count: 10);
```

### 4. 清理对象池

```csharp
ObjectPoolKit.ClearAll();
```

---

## 四、常用 API

- `Spawn(string prefabName)`：从池中获取对象，若池为空则自动实例化。
- `Recycle(GameObject obj)`：将对象回收到池中，自动隐藏并重置状态。
- `Preload(string prefabName, int count)`：预加载指定数量的对象到池中。
- `Clear(string prefabName)`：清理指定类型的池对象。
- `ClearAll()`：清理所有对象池。

---

## 五、使用建议

1. 预加载常用对象，提升运行时性能。
2. 所有临时对象建议通过 `Recycle` 回收，而不是直接 `Destroy`。
3. 所有池对象建议挂在 `PoolRoot` 下，便于场景清理和调试。
4. 场景切换时调用 `ClearAll()`，避免残留对象影响新场景。

---

## 六、示例代码

```csharp
// 预加载
ObjectPoolKit.Preload("ExplosionEffect", 5);

// 使用对象
var effect = ObjectPoolKit.Spawn("ExplosionEffect");
effect.transform.position = hitPoint;

// 回收对象
ObjectPoolKit.Recycle(effect);

// 场景切换时清理
ObjectPoolKit.ClearAll();
```

---

## 七、常见问题

- 对象未回收：确保所有临时对象生命周期结束后调用 `Recycle`。
- 池对象丢失：场景切换后建议重新预加载，或检查 `PoolRoot` 是否被销毁。
- 性能问题：合理设置预加载数量，避免运行时频繁实例化。

---

如需扩展高级功能或自定义池逻辑，请参考源码注释或联系维护者。
