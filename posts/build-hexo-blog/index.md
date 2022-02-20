---
title: Hexo 博客搭建简易指南
create_time: 2017-06-14 19:18:31
tags:
  - 技术笔记
---
本文是我建立这个博客的一些笔记。目的有三：一防忘记，二供参考，三作开篇。

![bulid-hexo-blog](./build-hexo-blog.jpg)

## 准备工作

首先，先安装建立 Hexo 博客站点的依赖环境，即安装 Node.js、Git 以及在 GitHub 上建立站点仓库。

<!-- more -->

### 安装Node.js环境

参考文章：[W3Cschool 上 Node.js 教程之 Node.js 安装配置](http://www.w3cschool.cn/nodejs/nodejs-install-setup.html)。

> 后面要使用其中的包管理工具`npm`进行 Hexo 安装。

### 安装Git工具包

参考文章：[W3Cschool 上 Git 教程之 Git 安装配置](http://www.w3cschool.cn/git/git-install-setup.html)。

### GitHub 上建立站点仓库

1、如果没有 GitHub 账户，先用邮箱在 [Github](https://github.com) 免费注册一个账户；
2、创建一个仓库（repository），将其命名：`username.github.io`（username：账户名），比如：tatwd.github.io。

![new-site-repository](./new-site-repository.png)

> 后面的站点就是部署在这个仓库（即`站点仓库`）上，以后访问博客只需在 URL 上输入`https://username.github.io`即可。

## 本地建站

准备工作完成后，便可以开始建立站点了。在 windows 环境下，启动命令行 cmd（Linux 环境启动终端），任意选择一个文件夹用来建立站点，然后输入命令：

### 安装 Hexo

``` bash
npm install hexo-cli -g
```
### 初始化 Hexo 站点

``` bash
hexo init [site-name]
```

> site-name： 站点文件夹名，可有可无。若加，则会在当前文件夹下新建一个以 site-name 命名的文件夹（即`站点文件夹`）。比如：输入`hexo init blog`后，当前文件夹会有一个叫`blog`文件夹。

### 安装依赖包

``` bash
cd [site-name]   #进入站点文件夹，如果在上步新建了站点文件夹
npm install
```

### 本地启动站点

``` bash
hexo generate  #可简写：hexo g，此命令会生成一个 public 文件夹
hexo server    #可简写：hexo s
```

至此，本地站点建立完毕。在浏览器的 URL 中输入`http://localhost:4000`即可查看站点。

> 按`Ctrl+C`键关闭，再次启动要先输入`hexo clean`清理缓存（即 public 文件夹)，之后如上。

## 更换主题

### 下载主题

法一：在 Hexo 官方网站上[下载](https://hexo.io/themes/)，然后解压到`站点文件夹`下的`themes`文件夹下，注意命名。

法二：使用`git clone [主题的 GitHub 仓库]`命令，将其克隆到`themes`文件夹下即可，注意命名。注意，此时整个项目将包含两个 Git 仓库，需要将主题仓库改成子仓库 `submodule` 或删除主题仓库的 `.git` 文件夹。

### 使用主题

修改站点文件夹下的`_config.yml`（以下称之为`主站配置文件`）：把`theme: landscape`改为`theme: 主题文件夹名`。

> 注意在`:`之后有一个空格。

### 主题设置

不同主题参见该主题的使用文档即可。

## 网上部署

在 GitHub 上部署站点：

### 修改主站配置文件

修改处前：

``` yml
# Deployment
# Docs: https://hexo.io/docs/deployment.html
deploy:
  type:
```

修改处后：

``` yml
# Deployment
# Docs: https://hexo.io/docs/deployment.html
deploy:
  type: git
  repo: git@github.com:tatwd/username.github.io.git
  branch: master
```

> 注意在`:`之后有一个空格，`username`改为你的用户名，以下同。

### 开始部署

如果在本地已启动了站点，则先关闭，然后输入命令：

``` bash
npm install hexo-deployer-git  #很重要
hexo clean
hexo g       #hexo generate的简写
hexo deploy  #可简写：hexo d
```

至此，网上部署完毕，在浏览器的 URL 中输入`https://username.github.io`验证是否成功。

> 后两条命令可合写为： `hexo g -d`

## 简单管理

### 平常写作

进入站点文件夹，输入命令：

``` bash
hexo new [layout] <title>
```

写完（Markdown 语言写作，参考文章：[Markdown 语法说明(简体中文版)](http://www.appinn.com/markdown/)）之后保存，然后输入命令：

``` bash
hexo clean
hexo g -d
```

此时，所写文章便被部署到 GitHub 上去了。

> layout 的值：page\post\draft，默认为 post，详见 [Hexo 文档之写作](https://hexo.io/zh-cn/docs/writing.html)。

### 管理站点文件

如果本地站点文件丢失了或换了电脑怎么办？为解决这个问题，我们利用了 GitHub 的多分支来管理站点文件：

1、用`master`分支来管理发布的文件，即`public`文件夹下的文件；
2、用`hexo`分支来管理主站点文件，即除`public`下和`.gitignore`忽视的其他文件；
3、将`hexo`设为默认分支。

为此，我们要：

#### 建立远程仓库

先将远程仓库关联到本地。进入站点文件夹，输入命令：

``` bash
git init
git remote add origin git@github.com:username/username.github.io.git
git pull
```

> 参考文章：[W3Cschool 上 Git 教程之 Git 远程仓库](http://www.w3cschool.cn/git/git-remote-repo.html)。

#### 创建 hexo 分支

``` bash
git checkout -b hexo  #创建并切换到 hexo 分支
```

#### 将 hexo 设为默认分支

在 GitHub 上的站点仓库上，点击`Settings`=>`Branches`，将 Default branch 切换成 hexo，然后点击`Update`即可。

![set-default-branch](./set-default-branch.png)

#### 将主站点文件 push 到 hexo 分支

切换到 hexo 分支（使用`git branch`命令查看当前所在分支）下，输入命令：

``` bash
git add .
git commit -m "提交记录"
git push -u origin hexo  #初次push要加-u，此后可省
```

> 输命令之前，查看`站点文件夹`下的`.gitignore`文件，是否忽略 public 文件夹，若无，则添加`public/`。另外，此方法在`下载主题`使用`法二`时似乎有bug。

### 本地站点恢复

1、使用`git clone`命令克隆`站点仓库`（默认分支为hexo）：

``` bash
git clone git@github.com:username/username.github.io.git
```

2、在本地新拷贝的`username.github.io`文件夹下依次执行命令：

``` bash
npm install hexo-cli
npm install
```
> 注意，此时不需要执行`hexo init`这条命令。

到此，便完成了对站点的一些简单管理。

### 关于域名配置的问题

在 GitHub 上，站点仓库是支持域名配置的。具体见参考链接，这里需要注意的是，之前看到有人采用的是在域名提供商的后台通过添加 A 记录的方式进行配置的，这在 GitHub 上会被提示警告，建议改成通过添加 CNAME 记录进行配置。

> 参考：[配置 GitHub Pages 站点的自定义域](https://docs.github.com/cn/free-pro-team@latest/github/working-with-github-pages/configuring-a-custom-domain-for-your-github-pages-site)
