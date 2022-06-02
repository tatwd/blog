---
title: 从 IL 指令再开始
create_time: 2021-03-18
tags:
  - dotnet
  - 技术笔记
---


CLR 实际处理的代码是 IL 代码 。以下是一个简单的 HelloWorld 程序：

```il
.assembly extern mscorlib{}
.assembly HelloWorld {}
.class HelloWorld extends [mscorlib]System.Object
{
    .method public static void Main()
    {
        .entrypoint
        .maxstack 1
        ldstr "Hello, world!"
        call void [mscorlib]System.Console::WriteLine(string)
        ret
    }
}
```

通过使用类似 `ilasm` 的工具可以将其编译成可执行的程序。

当然，我们并不直接使用 IL 进行程序编写，而是使用架构在这套标准之上的其他语言，由编译器来完成到 IL 的编译，再在运行时即时编译（JIT）成机器码。

IL 是基于指令的，它通过一系列的指令来完成上层语言需要做的操作。

## 内存分配

大多数引用类型变量使用 `newobj` 指令进行初始化。只有零基（zero-based）一维数组使用 `newarr` 指令。字符串是比较特殊的引用类型，它使用的指令是 `ldstr`。

值类型有时也使用 `newobj` 进行初始化，但大多数情况下使用的是 `initobj` 指令。后者不会调用构造函数（`ctor`）。值类型发生装箱（`box`）时也会发生托管堆空间的申请。

每一次对托管堆空间的申请，都会 check 空间大小，并可能触发 GC，甚至抛出异常。

那一个对象（Object）的内存结构到底是怎么样的呢？

简而言之，一个对象包含以下数据：

```
字段数据 + TypeHandler + SyncBlockIndex
```


## 装箱和拆箱

涉及指令：`box`、`unbox`、`unbox.any`。

`box` 会发送内存分配（可能发生 GC），属于昂贵的操作，应尽量避免。例如，对于下面发生的字符串插值在 .NET 6 以下版本会发生装箱。

```csharp
int foo = 101;
string str = $"hello {foo}";
```
对应的 IL 代码可能如下（.NET 6 以下）：

```il
IL_000d: ldstr        "hello {0}"
IL_0012: ldloc.1      // foo
IL_0013: box          [System.Runtime]System.Int32
IL_0018: call         string [System.Runtime]System.String::Format(string, object)
```

`unbox` 返回堆栈的是一个指向拆箱后值类型数据的地址。`unbox.any` 则返回的是实际的值类型数据。拆箱的过程会发生 null 检查和类型检查，并且只能对已装箱的数据作拆箱。


## 方法调用

涉及指令：`call`、`callvirt`。

`call` 执行静态调用，一般是发生在静态方法或非虚方法上。

`callvirt` 执行动态调用，根据引用变量指向的对象类型来调用方法，会发生递归调用，这个过程会发生引用检查，因此为了类型安全，一般引用类型中调用非虚方法也会使用到这个指令，否则下列代码会被正确执行（岂不荒谬！）。

```csharp
var foo = new Foo();
foo = null;
foo.Hello();
```

密封类型的引用调用虚方法时，采用 `call` 调用可以减少 `callvirt` 进行类型检查的时间，提高调用性能。

值类型调用虚方法时，因为值类型首先是密封的，其次 `call` 调用可以阻止值类型被执行装箱。

基类调用虚方法时，采用 `call` 可以避免 `callvirt` 递归调用本身引起的堆栈溢出。常见的覆写例如，实现 `System.Object` 的虚方法 `Equals()`、`ToString()` 时，就采用 `call` 调用方式。

```csharp
int num = 101;
num.ToString();
```

上面代码中，调用 `ToString()` 采用的就是 `call` 指令，因为 `System.Int32` 覆写了 `System.Object` 的 `ToString()` 方法。


总之，`call` 指令调用静态类型、声明类型的方法，而 `callvirt` 调用动态类型、实际类型的方法。


> 未完待续
