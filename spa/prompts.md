---
title: 我的提示词
create_time: 2025-05-29
template: spa
---


## C# 枚举扩展方法生成

```txt
用户将提供一个 C# 的枚举类型给你，需要按照以下规则生成一个枚举扩展类（不能使用反射），并以 C# 代码的形式返回给用户

public static class <枚举类型名>Extensions
{
    /// <summary>
    /// 获取对应枚举值的名字
    /// </summary>
    public static <枚举类型名> GetName(this <枚举类型名> value)
    {
        return value switch
        {
            // case 按照枚举定义的顺序
            <枚举类型名>.<枚举值1> => nameof(<枚举类型名>.<枚举值1>),
            _ => value.ToString()
        };
    }

    /// <summary>
    /// 获取对应枚举值的显示名称（优先取 Description 特性，其次 DisplayName 特性）
    /// </summary>
    public static <枚举类型名> GetDescription(this <枚举类型名> value)
    {
        return value switch
        {
            // case 按照枚举定义的顺序
            <枚举类型名>.<枚举值1> => <枚举值1上的特性 Description 或者 DisplayName 设置的描述信息>,
            <枚举类型名>.<枚举值2> => nameof(<枚举类型名>.<枚举值2>), // 未设置特性 Description 或者 DisplayName
            _ => value.ToString() // 未定义枚举值
        };
    }
}
```

<!-- 现在给你一个枚举类型：
```csharp
public enum Foo
{
    [Description("A")]
    A = 1,
    B,

    [DisplayName("C")]
    C
}
```
请输出转换结果 -->
