---
title: 我的提示词
create_time: 2025-05-29
template: spa
---


## C# 枚举扩展方法生成

```txt
用户将提供一个 C# 的枚举类型给你，需要按照以下规则生成一个枚举扩展类（不能使用反射），并最终以 C# 代码的形式返回给用户

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


## 字典转换到类

```txt
用户将提供一个 C# 的字典（Dictionary）类型实例给你，需要按照以下规则生成一个对应名称的 class 类，并最终以 C# 代码的形式返回给用户：

public class <user_input_class_name>
{
    public <dictonary_value_type> <dictionary_key_name，驼峰转帕斯卡命名> { get; set; }
}
```

## 特殊类型 DataReader 转换

```txt
用户将提供一个参照以下规则的 C# 类定义：

public class <user_input_class_name>
{
    public <val_type> <prop_name>
    {
        get { return this.TryGetValue<value_type>(<dict_key_name>, <default_val_if_not_exists>); }
        set { return this[<dict_key_name>] = value; } // 可能没有 set
    }
}

需要你将上述类型按以下规则进行转换，并将转换后结果输出给用户：

public class <user_input_class_name>Entity
{
    public <val_type> <prop_name> { get; <private 没有set时添加> set; }

    public void FillWithDataReader(IDataReader dataReader)
    {
        for (var i = 0; i < dataReader.FieldCount; i++)
        {
            var fieldName = dataReader.GetName(i);

            switch (fieldName)
            {
                case <dict_key_name>:
                    // DbConvert 的转换方法定义举例:
                    //      int DbConvert.ToInt32(object val, int defaultVal)
                    //      int? DbConvert.ToInt32(object val) // 无 defaultVal 时返回可为空类型
                    <prop_name> = DbConvert.To<val_type_FCL_type>(dataReader[i], <default_val_if_not_exists>);
                    break;
                default:
                    break;
            }
        }
    }
}
```
