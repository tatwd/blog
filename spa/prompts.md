---
title: 我的提示词
create_time: 2025-05-29
template: spa
---


## C# 枚举扩展方法生成

~~~txt
用户将提供一个 C# 枚举类型给你，需要你按以下规则生成一个枚举扩展类（不能使用反射）返回给用户：

```csharp
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
            // 枚举值过多时不能省略

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
            // 枚举值过多时不能省略
            // XML注释不影响输出

            <枚举类型名>.<枚举值1> => <枚举值1描述>, // [Description("枚举值1描述")]
            <枚举类型名>.<枚举值2> => <枚举值2描述>, // [DisplayName("枚举值2描述")]
            <枚举类型名>.<枚举值3> =>
                nameof(<枚举类型名>.<枚举值3>), // 无 Description 或者 DisplayName
            _ => value.ToString() // 未定义枚举值
        };
    }
}
```
~~~

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

~~~txt
用户将提供一个 C# 字典类型实例给你，需要你按以下规则生成一个对应 class 类返回给用户：

```csharp
public class <user_input_class_name>
{
    public <dict_value_type> <dict_key_name，驼峰转帕斯卡命名> { get; set; }
}
```
~~~

## 特殊类型 DataReader 转换

~~~txt
用户将提供一个参照以下规则的 C# 类定义：

```csharp
public class <user_input_class_name>
{
    public <val_type> <prop_name>
    {
        get { return this.TryGetValue<value_type>(<dict_key_name>, <default_val_if_not_exists>); }
        set { return this[<dict_key_name>] = value; } // 可能没有 set
    }
}
```

需要你将上述类型按以下规则进行转换，并将转换后结果输出给用户：

```csharp
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
~~~


## 批量数据转换成逗号分割字符串

```txt
用户将提供一组按特定字符分割的数据给你，格式参考以下：
<value_1><split_char><value_2>[...]

需要你将其按以下格式进行转换并返回给用户：
'<value_1>','<value_2>'[...]
```
