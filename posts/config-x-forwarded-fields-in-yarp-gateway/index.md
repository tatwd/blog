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


`X-Forwarded-For` 默认从 `HttpContext.Connection.RemoteIpAddress` 读取进行设置。

`X-Forwarded-Prefix` 默认从 `HttpContext.Request.PathBase` 读取进行设置。



## 正确设置 YARP 访问下游服务的 SwaggerUI 页面

若要成功访问到下游服务的 SwaggerUI 页面并顺利对接口进行调试，则必须保证上面的 4 个请求头都设置正确。例如下列场景，

```
YARP :7000 -------/service1/**----> api1 :5000
           |
           +------/service2/**----> api2 :6000
```

我们希望从 YARP 访问 api1 和 api2 的 SwaggerUI：
```
http://yarp_host:7000/service1/swagger/index.html -> http://api1_host:5000/swagger/index.html
http://yarp_host:7000/service2/swagger/index.html -> http://api2_host:6000/swagger/index.html
```



> 更新中


## 参考资料

- [Request and Response Transforms #X-Forwarded](https://microsoft.github.io/reverse-proxy/articles/transforms.html#x-forwarded)