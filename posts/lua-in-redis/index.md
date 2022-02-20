---
title: Lua 脚本在 Redis 中的应用
tags:
  - redis
  - 技术笔记
create_time: 2021-03-04 23:55:17
---

Redis 从 2.6 开始便支持执行 Lua 5.1 脚本，使用的命令是 [`EVAL`](https://redis.io/commands/eval)。

在 Lua 脚本中，可以使用全局数组变量 `KEYS` 访问键，`ARGV` 访问参数（这两个变量都是**从数组下标 1 开始**进行访问）。另外，如果需要执行 Redis 的指令，可以通过函数 `redis.call()` 和 `redis.pcall()` 做到。

<!--前者在执行发生错误时会抛给执行者，而后者会捕获错误。-->

EVAL 执行过程是一个**事务操作**，并且是 Redis 中一种更快速和高效的（simpler and faster）事务执行方式，它的效果相当于 `MULTI` + `EXEC`。这一特性的存在，让我们可以在一次指令执行中（一次通信）包含一系列可程序化的操作。在一定程度上，可以简化客户端代码及可能存在的并发问题。

例如，下面脚本实现的功能是对一个 key 进行计数并在首次缓存时设置过期时间，执行返回的结果是当前计数值：

```lua
local key = KEYS[1]
local seconds = tonumber(ARGV[1])

local count = redis.call('incr', key)

if count == 1 then
  redis.call('expire', key, seconds)
end

return count
```

从这个例子可以看出，Lua 脚本在此时扮演的角色实际上等同于一个“函数”，对这段脚本的执行就相当于在执行一个“函数”，所不同于一般函数的是 Redis 保证其执行具有了原子性。

但需要注意的是，尽量不要在脚本中执行大量耗时的操作（如 `keys *`），否则可能会对其他 client 造成一定影响。

那么，Redis 是如何执行 EVAL 的呢？

## EVAL 执行过程

下面是 Redis 2.6 源码中 `redisServer` 结构体关于 LUA 脚本执行相关的属性：

```c reids/2.6/src/redis.h
struct redisServer {
  //...
  lua_State *lua; /* The Lua interpreter. We use just one for all clients */
  redisClient *lua_client;   /* The "fake client" to query Redis from Lua */
  redisClient *lua_caller;   /* The client running EVAL right now, or NULL */
  dict *lua_scripts;         /* A dictionary of SHA1 -> Lua scripts */
  long long lua_time_limit;  /* Script timeout in seconds */
  long long lua_time_start;  /* Start time of script */
  int lua_write_dirty;  /* True if a write command was called during the
                           execution of the current script. */
  int lua_random_dirty; /* True if a random command was called during the
                           execution of the current script. */
  int lua_timedout;     /* True if we reached the time limit for script
                           execution. */
  int lua_kill;         /* Kill the script if true. */
  //...
};
```

一个 EVAL 指令（[`evalGenericCommand()`](https://github.com/redis/redis/blob/51943a78b0d2e6c7c4d0647ba1a60c33d3a1a4d9/src/scripting.c#L787)）的大致主要过程如下：

1. 将客户端脚本生成对应的 Lua 函数
2. 将客户端的脚本放入 `lua_scripts` 字典
3. 执行生成的 Lua 函数

如果脚本中执行了 `redis.call` 和 `redis.pcall`，则会启动一个 Redis “伪客户端”（fake client，即 `lua_client`）用于在 Lua 环境中执行 Redis 指令。

字典 `lua_scripts` 主要是用来缓存已执行过的客户端输入的 Lua 脚本，下次执行时直接从中取出进行执行。它的 key 是计算脚本等得到的 SHA1 值。此外，它还用于 Redis 的主从复制时关于 EVALSHA 传递失败时转换成 EVAL 的问题。

## 参考资料

1. [Redis Lua scripting](https://redis.io/commands/eval)
2. [Redis transactions](https://redis.io/topics/transactions)
3. [Lua 5.1 Reference Manual](http://www.lua.org/manual/5.1/)
4. [Redis 2.6 Source Code](https://github.com/redis/redis/tree/2.6)
