---
title: Tips & Snippets
create_time: 2022-03-22
---

记录我的个人 code 片段

## 如何看指定程序的标准输出
> created 2023/5/24

```bash
strace -ewrite -s 300 -f -p $PID 2>&1
```


## PowerShell
> created 2023/4/5

统计文件 lines
```ps1
cat MyLogger.log | Measure-Object -Line
```

## windows 下 curl 跳过证书吊销检查
> 2022-03-20

以处理代理 https 抓包时，curl 出现的错误 `curl: (35) schannel: next InitializeSecurityContext failed: Unknown error (0x80092012) - 吊销功无法检查证书是否吊销。`

```ps1
curl --proxy 127.0.0.1:8080 --ssl-no-revoke https://httpbin.org/get
```
或者使用 `--insecure`

## Git 实用技巧
> 2022-03-17

回退上一次本地提交：
```bash
git reset HARD~
```



## 使用 InstallUtil 安装和卸载 window service
> 2022-07-19

安装：
```ps1
cd /d %~dp0
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe 程序.exe
pause
```

卸载：
```ps1
cd /d %~dp0
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /uninstall 程序.exe
pause
```
需要以管理员身份运行才行。

## MSSQL 查看表字段信息
> 2022-07-15

```sql
select a.name '字段',
b.name '字段类型',
a.max_length '最大长度(字节)',
case a.is_nullable when 0 then '否' else '是' end '是否为空',
case a.is_identity when 0 then '否' else '是' end '是否为主键',
c.value '说明'
from sys.columns a
inner join sys.types b on a.system_type_id=b.system_type_id
left join sys.extended_properties c on c.major_id=a.object_id and c.minor_id=a.column_id
where a.object_id=object_id('[database_name].[sechema_name].[table_name]')
and b.name<>'sysname'
order by a.column_id
```

## Escaped JavaScript string in Razor
> 2022-07-05

```razor
const str = '@HttpUtility.JavaScriptStringEncode(Model.StrVal)'
```

参考：[Javascript, Razor and Escape characters. Like apostrophe](https://stackoverflow.com/questions/5225796/javascript-razor-and-escape-characters-like-apostrophe)


## SQL 中按指定分割符连接字符串表达式
> 2022-06-13

MySql 5.6+ 可以使用 [`GROUP_CONCAT`](https://dev.mysql.com/doc/refman/5.6/en/aggregate-functions.html#function_group-concat)

Sql Server 2017+ 可以使用 [`STRING_AGG`](https://docs.microsoft.com/en-us/sql/t-sql/functions/string-agg-transact-sql)，低版本可以类似如下实现：

```sql
select a.custid,
  stuff( (select ',' + b.product
    from customer_product b
    where b.custid=a.custid
    for xml path('')), 1, 1, NULL ) as products
from customer_product a
group by custid
```


## 关于季度的日期操作
> 2022-03-22

获取对应日期季度编号：
```csharp
private static int GetQuarterNum(DateTime date)
{
    var (d, r) =  Math.DivRem(date.Month, 3);
    return r == 0 ? r : d + 1;
}
```

获取对应日期之后的下一季度的开始日期：

```csharp
private static DateTime NextQuarterStartDate(DateTime date)
{
    var q =  GetQuarterNum(date);
    var r = (q * 3 + 1) % 12;
    var nextQuarterStartMonth = r == 0 ? 1 : r;
    var nextYear = nextQuarterStartMonth < date.Month ? date.Year + 1 : date.Year;
    return new DateTime(nextYear, nextQuarterStartMonth, 1);
}
```


## Set Folder Link in `csproj`
> 2022-03-21

Make `myfolder` link to `..\otherfolder`:

```xml
<ItemGroup>
  <None Include="..\otherfolder\**\*">
    <Link>myfolder\%(RecursiveDir)/%(FileName)%(Extension)</Link>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```


## Load assembly from file and read config file ok
> 2021-12-23

```ps1
$dir =  "dll文件所在目录"
$dll = "$dir\dll文件名称.dll"
$config = "$dir\dll文件名称.config"

#[AppDomain]::CurrentDomain.SetData("APPBASE", $dir)
[AppDomain]::CurrentDomain.SetData("APP_CONFIG_FILE", $config)

#处理 ConfigurationManager 读不到配置的问题
[Configuration.ConfigurationManager].GetField("s_initState", "NonPublic, Static").SetValue($null, 0)
[Configuration.ConfigurationManager].GetField("s_configSystem", "NonPublic, Static").SetValue($null, $null)
([Configuration.ConfigurationManager].Assembly.GetTypes() | where {$_.FullName -eq "System.Configuration.ClientConfigPaths"})[0].GetField("s_current", "NonPublic, Static").SetValue($null, $null)

#加载对应程序集
Import-Module $dll

#回滚对应升级
#MemberCode logID
$foo = "foo value"
$ret=[名称空间]::静态方法名($foo)
echo $ret
```

## QueryDB by PowerShell

```ps1
#配置信息
#$Database   = 'tablename'
#$Server     = '"localhost,1433"'
#$UserName   = 'username'
#$Password   = 'test@1234'

#创建连接对象
$SqlConn = New-Object System.Data.SqlClient.SqlConnection

#使用账号连接MSSQL
$SqlConn.ConnectionString = "Data Source=$Server;Initial Catalog=$Database;user id=$UserName;pwd=$Password;Connect Timeout=500;" #ApplicationIntent=ReadOnly;

#或者以 windows 认证连接 MSSQL
#$SqlConn.ConnectionString = "Data Source=$Server;Initial Catalog=$Database;Integrated Security=SSPI;"

#打开数据库连接
$SqlConn.open()
#$SqlCmd = New-Object System.Data.SqlClient.SqlCommand
$SqlCmd = $SqlConn.CreateCommand()
$SqlCmd.commandtext = "
  select @@version
"

$SqlAdapter = New-Object System.Data.SqlClient.SqlDataAdapter
$SqlAdapter.SelectCommand = $SqlCmd
$set = New-Object data.dataset
$SqlAdapter.Fill($set)

$dateStr= get-date -Format "yyyyMMdd"
$set.Tables[0] |
#Format-Table -Auto
Export-Csv "out-$dateStr-v1.csv" -Encoding UTF8
#关闭数据库连接
$SqlConn.close()
```

## WCF Client by PowerShell

```ps1
#http binding 地址
$uri = "http://localhost:2074/service"
$srv = New-WebServiceProxy -Uri $uri -UseDefaultCredential

try {
  #调用对应函数
  $ret = $srv.方法名(参数列表)
  echo $ret
} finally {
  $srv.Abort()
}
```

## AspNetCore 快速配置 Serilog

```shell
dotnet add package Serilog.AspNetCore
```

Startup.cs

```csharp
public static IHostBuilder CreateHostBuilder(string[] args) =>
  Host.CreateDefaultBuilder(args)
    .UseSerilog((hostBuilderContext, loggerConfiguration) => loggerConfiguration
      .ReadFrom.Configuration(hostBuilderContext.Configuration))
    .ConfigureWebHostDefaults(webBuilder =>
    {
      webBuilder.UseStartup<Startup>();
    });
```

appsettings.json
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    },
    "Enrich": [
      "FromLogContext"
    ],
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {}
      },
      {
        "Name": "File",
        "Args": {
          "path": "d:/logs/app/log.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```



## 解决 ASP.NET Core 使用 Windows Service 托管时 ContentRoot 问题

```cs
/// <summary>
/// 使用应用程序文件所在目录作为ContentRoot
/// </summary>
/// <param name="hostBuilder"></param>
/// <returns></returns>
public static IHostBuilder UseBinaryPathContentRoot(this IHostBuilder hostBuilder)
{
    var contentRoot = Path.GetDirectoryName(Environment.GetCommandLineArgs().First());
    if (contentRoot != null)
    {
        Environment.CurrentDirectory = contentRoot;
        hostBuilder.UseContentRoot(contentRoot);
    }
    return hostBuilder;
}
```

## 自定义 `JsonTextWriter`

```cs
class MyJsonTextWriter : JsonTextWriter
{
    public MyJsonTextWriter(TextWriter textWriter) : base(textWriter)
    {
    }

    public override void WriteNull()
    {
        WriteValue(string.Empty); // null => ""
    }
}
```
使用：
```cs
/*
class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int? Age { get; set; }
    public Person Father { get; set; }
    public Person[] Friends { get; set; }
}
*/
var obj = new Person
{
    Id = 1,
    Father = new Person(),
    Friends = Array.Empty<Person>()
};
// var serializer = new JsonSerializer();
var serializer = JsonSerializer.CreateDefault();
var sw = new StringWriter(new StringBuilder(256));
using (var jtw = new MyJsonTextWriter(sw))
{
    serializer.Serialize(jtw, obj);
}
Console.WriteLine(sw.ToString());
```


## 批量下载 by PowerShell

```ps1
$outDir = "output"

if (!(Test-Path -LiteralPath $outDir)) {
    md $outDir | out-null
}

Import-Csv -Path "data.csv" -UseCulture -Encoding GB2312 |
ForEach-Object -Parallel  {
    $localOutDir = "$using:outDir"
    $dir = "$localOutDir\最好唯一"

    $imgName = [system.IO.Path]::GetFileNameWithoutExtension($链接)
    $imgExt = [system.IO.path]::GetExtension($链接)
    $newFile = "$dir\${imgName}${imgExt}"

    if (!(Test-Path -LiteralPath $newFile)) {
        echo "下载文件 $newFile"

        iwr $链接 -OutFile $newFile

    }


} -ThrottleLimit 20
```

## 服务器性能调优

**Window Server**

```
TcpTimedWaitDelay => 30
```
查看值
```ps1
Get-ItemPropertyValue -Path 'HKLM:HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters' -Name TcpTimedWaitDelay
```

[TCP/IP and NBT configuration parameters for Windows XP](https://docs.microsoft.com/en-us/troubleshoot/windows-client/networking/tcpip-and-nbt-configuration-parameters)

**Linux**

在 linux 服务器上请通过变更 `/etc/sysctl.conf` 文件去修改该缺省值（秒）：
```
net.ipv4.tcp_fin_timeout = 30
```

## Redis

1. 删除指定模式匹配的 keys

```shell
redis-cli -h 127.0.0.1 -p 6379 --scan --pattern "foo_*" | xargs -L 2000 redis-cli -h 127.0.0.1 -p 6379 del
```




## MongoDB

1.删字段
```js
db.collections.update({},{$unset:{func_node_settings:1}},false,true)
```

2.添加字段
```js
// 3.6+ 数组更新操作新增 $[] 和 $[<idenitifier>]
db.collections.update({},{$set:{"node_settings.$[].is_func":false}},{multi:1})
```

3.添加用户
```js
use products
db.createUser(
   {
     user: "accountUser",
     pwd: passwordPrompt(),  // Or  "<cleartext password>"
     roles: [ "readWrite", "dbAdmin" ]
   }
)
```

4.use admin create

```js
use admin
db.createUser(
  {
    user: "myUserAdmin",
    pwd: passwordPrompt(), // or cleartext password
    roles: [ { role: "userAdminAnyDatabase", db: "admin" }, "readWriteAnyDatabase" ]
  }
)
```
> [Enable Access Control](https://docs.mongodb.com/manual/tutorial/enable-authentication/#user-administrator)


## RabbitMQ

```shell
rabbitmq-plugins enable rabbitmq_management
```
visit it on `http://localhost:15672/` default username and password is `guest`

> [Management Plugin](https://www.rabbitmq.com/management.html)


## MySQL

初次启动
```cmd
bin\mysqld --initialize
```
修改密码
```shell
ALTER USER 'root'@'localhost' IDENTIFIED BY 'MyNewPass';
```

服务安装（CMD）
```cmd
cd /d %~dp0
bin\mysqld --install MySQL --defaults-file=D:\devtools\mysql-5.7.31-winx64\my.cnf
```


## ASP.NET Core 模块的托管模式

2.1 只支持进程外

> [ASP.NET Core 2.1 模块](https://docs.microsoft.com/zh-cn/aspnet/core/host-and-deploy/aspnet-core-module?view=aspnetcore-2.1)

3.1 支持进程外和进程内（从3.0开始引入）

默认使用进程内

> [ASP.NET Core 3.1 模块](https://docs.microsoft.com/zh-cn/aspnet/core/host-and-deploy/aspnet-core-module?view=aspnetcore-3.1)



## IIS 应用程序池设置

闲置超时(分钟)：20
超时会Session会过期

> [ASP.NET Core IIS 托管空闲超时解决方案](https://docs.microsoft.com/zh-cn/aspnet/core/host-and-deploy/iis/?view=aspnetcore-3.1#idle-timeout)


## Nginx 配置

```shell
./configure --prefix=$(pwd)/build \
  --with-pcre=/usr/lib \
  --with-zlib=/usr/lib \
  --with-openssl=/usr/lib \
  --with-http_ssl_module \
  --with-stream \ #tcp/udp代理支持
  --with-http_stub_status_module
```
> [Building nginx from Sources](https://nginx.org/en/docs/configure.html) or [Building nginx on the Win32 platform with Visual C](http://nginx.org/en/docs/howto_build_on_win32.html)
> [Module ngx_stream_core_module](http://nginx.org/en/docs/stream/ngx_stream_core_module.html)


## WinDbg的配置

调试前需对 Symbol File Path (以下简称 SFP) 进行设置。例如：

```txt
c:\mysymbols;srv*c:\cachesymbols*https://msdl.microsoft.com/download/symbols
```

或者

```txt
c:\mysymbols;cache*c:\cachesymbols;srv*https://msdl.microsoft.com/download/symbols
```

> [Windows 调试器的符号路径](https://docs.microsoft.com/zh-cn/windows-hardware/drivers/debugger/symbol-path)

然后，在 windbg 中执行以下命令加载符号（注意保持网络通畅）
```shell
.reload [-f]
```

当进行调试时，最好拿到程序的 `*.pdb` 文件并放到某个文件夹（如：`c:\mysymobls`），要加入到 SFP。这样调试时，可以拿到源码的相关信息，有助于快速定位到问题所在。

针对 .NET 程序，最好配合 `SOS.dll` 工具进行
```shell
.loadby sos clr #建议先加载symbols再执行这条命令
```
Or
```shell
.load <full path to sos.dll>
```
`SOS.dll` 可以在类似于 `C:\Windows\Microsoft.NET\Framework64\v4.0.30319` 的，目录下找到。

> [SOS.dll（SOS 调试扩展）](https://docs.microsoft.com/zh-cn/dotnet/framework/tools/sos-dll-sos-debugging-extension)
> [SOS debugging extension for Windows](https://github.com/dotnet/diagnostics/blob/master/documentation/sos-debugging-extension-windows.md)
