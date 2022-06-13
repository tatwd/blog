---
title: YARP 中配置 X-Forwarded-* 问题
tags:
  - dotnet
  - 技术笔记
create_time: 2022-06-06
---

YARP (以下使用 1.0 作为讨论对象) 支持对请求头中 X-Forwarded 下的 4 个字段进行设置：

```
X-Forwarded-For
X-Forwarded-Proto
X-Forwarded-Host
X-Forwarded-Prefix
```

## 默认取值

`X-Forwarded-For` 默认从 `HttpContext.Connection.RemoteIpAddress` 读取进行设置。

`X-Forwarded-Prefix` 默认从 `HttpContext.Request.PathBase` 读取进行设置。



## 正确访问下游服务的 SwaggerUI 页面

已如下事例来讨论：

```
YARP :7000 -------/service1/**----> api1 :5000
           |
           +------/service2/**----> api2 :6000

```

若要实现这个代理，需要添加下列路由匹配规则：

```json
"Match": {
  "Path": "/service1/{**remainder}",
}
```

同时，`Transforms` 要增加一条规则：
```json
{
  "PathRemovePrefix": "/service2"
},
```


若要成功访问到下游服务的 SwaggerUI 页面并顺利对接口进行调试，即我们希望从 YARP 实现如下转换：
```
http://yarp_host:7000/service1/swagger/index.html -> http://api1_host:5000/swagger/index.html
http://yarp_host:7000/service2/swagger/index.html -> http://api2_host:6000/swagger/index.html
```


以代理 api2 为例，`Transforms` 要新增如下规则：

```json
{
  "X-Forwarded": "Set",
  "Prefix": "Off",
  "Host": "Off",
  "Proto": "Off"
},
{
  "RequestHeader": "X-Forwarded-Prefix",
  "Set": "/service2"
}
```

此时，到达 api2 的请求头里这 4 个字段可能为：

```
x-forwarded-for: ::ffff:127.0.0.1
x-forwarded-host: yarp_host
x-forwarded-port: 80
x-forwarded-prefix: /service2
x-forwarded-proto: http
```

其中，对 `"Host": "Off"` 是因为如果有多层代理是，SwaggerUI 默认取最后一层代理地址，从而造成 servers 列表无法显示正确的地址。

完整配置查看[此处](https://github.com/tatwd/tryit/blob/master/yarp/MyProxy/appsettings.json)。

## 参考资料

- [Request and Response Transforms #X-Forwarded](https://microsoft.github.io/reverse-proxy/articles/transforms.html#x-forwarded)
