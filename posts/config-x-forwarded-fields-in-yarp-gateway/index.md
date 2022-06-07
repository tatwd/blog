---
title: YARP 网关配置 X-Forwarded-* 问题
tags:
  - dotnet
  - 技术笔记
create_time: 2021-03-08
draft: true
---

YARP 支持对请求头中 X-Forwarded 下的 4 个字段进行设置：

```
X-Forwarded-For
X-Forwarded-Proto
X-Forwarded-Host
X-Forwarded-Prefix
```

> 更新中


## 参考资料

- [Request and Response Transforms #X-Forwarded](https://microsoft.github.io/reverse-proxy/articles/transforms.html#x-forwarded)