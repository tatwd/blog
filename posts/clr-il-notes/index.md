---
title: 从 IL 指令再开始
create_time: 2021-03-18
tags:
  - dotnet
  - 技术笔记
---


CLR 实际运行的代码是 IL 代码 。以下是一个简单的 HelloWorld 程序：

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

每一次对托管堆空间的申请，都会 check 空间大小，并可能触发 GC，甚至抛出。

那一个对象（Object）的内存结构到底是怎么样的呢？

简而言之，一个对象包含以下数据：

```
字段数据 + TypeHandler + SyncBlockIndex
```


## 装箱和拆箱

指令：`box`、`unbox`、`unbox.any`。

`unbox` 返回堆栈的是一个指向拆箱后值类型数据的地址。`unbox.any` 则返回的是实际的值类型数据。拆箱的过程会发生 null 检查和类型检查，并且只能对已装箱的数据作拆箱。

<!-- ## 方法调度 -->


> 未完待续
