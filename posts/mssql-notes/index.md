---
title: 数据库拾遗之 MSSQL 篇
create_time: 2021-09-25 00:37:44
tags:
  - mssql
  - sql
  - 技术笔记
---


以下使用 SQL Server 2017 作为实验环境。

## SELECT 执行顺序

一般情况的执行顺序：

```
 1. FROM
 2. ON
 3. JOIN
 4. WHERE
 5. GROUP BY
 6. WITH CUBE 或 WITH ROLLUP
 7. HAVING
 8. SELECT
 9. DISTINCT
10. ORDER BY
11. TOP
```

参考：[SELECT 语句的逻辑处理顺序](https://docs.microsoft.com/zh-cn/sql/t-sql/queries/select-transact-sql?view=sql-server-2017#logical-processing-order-of-the-select-statement)


## 合计行生成

例如，我们需要统计出每个国家/地区的总销售额，然后给出了所有国家/地区的总和。


利用 [`UNION ALL`](https://docs.microsoft.com/zh-cn/sql/t-sql/language-elements/set-operators-union-transact-sql?view=sql-server-2017) 语句：

```sql
SELECT Country, SUM(Sales) AS TotalSales
FROM Sales
UNION ALL
SELECT '合计', SUM(Sales) AS TotalSales
FROM Sales
```

利用 [`GROUP BY ROLLUP`](https://docs.microsoft.com/zh-cn/sql/t-sql/queries/select-group-by-transact-sql?view=sql-server-2017#group-by-rollup) 语句：

```sql
SELECT Country, SUM(Sales) AS TotalSales
FROM Sales
GROUP BY ROLLUP ( Country );
```

注意：当多列分组时，此种分组将生成：每个分组列小计+总计。

利用 [`GROUPING SETS`](https://docs.microsoft.com/zh-cn/sql/t-sql/queries/select-group-by-transact-sql?view=sql-server-2017#group-by-grouping-sets--) 语句：

```sql
SELECT Country, SUM(Sales) AS TotalSales
FROM Sales
GROUP BY GROUPING SETS ( Country, () );
```

以上 3 种写法适用于 SQL Server 所有支持的版本。

## 分页查询实现

使用 [`ROW_NUMBER()`](https://docs.microsoft.com/zh-cn/sql/t-sql/functions/row-number-transact-sql?view=sql-server-2017) 函数生成行号在筛选指定范围的数据行：

```sql
WITH OrderedOrders AS
(
    SELECT SalesOrderID, OrderDate,
    ROW_NUMBER() OVER (ORDER BY OrderDate) AS RowNumber
    FROM Sales.SalesOrderHeader
)
SELECT SalesOrderID, OrderDate, RowNumber
FROM OrderedOrders
WHERE RowNumber BETWEEN 50 AND 60;
-- 将返回行 50 到 60（含）
```

使用 [`ORDER BY OFFSET FETCH`](https://docs.microsoft.com/zh-cn/sql/t-sql/queries/select-order-by-clause-transact-sql?view=sql-server-2017) 子句：

```sql
SELECT DepartmentID, Name, GroupName
FROM HumanResources.Department
ORDER BY DepartmentID
    OFFSET 0 ROWS
    FETCH NEXT 10 ROWS ONLY;
-- 将返回行 1 到 10（含）
```

推荐第二种写法，因为第一种中的利用的函数 `ROW_NUMBER()` 具有不确定性，可能无法充分利用到查询优化器。

## 函数的确定性与不确定性

函数的确定性是指对于特定的输入能始终得到相同结果（具有幂等性）。例如：

- 除 [FORMAT](https://docs.microsoft.com/zh-cn/sql/t-sql/functions/format-transact-sql?view=sql-server-2017) 外，所有字符串内置函数都是确定性的。具体函数列表参看[字符串函数](https://docs.microsoft.com/zh-cn/sql/t-sql/functions/string-functions-transact-sql?view=sql-server-2017)。
- 除非用 OVER 和 ORDER BY 子句指定聚合函数，否则所有聚合函数都具有确定性。具体函数列表参看[聚合函数](https://docs.microsoft.com/zh-cn/sql/t-sql/functions/aggregate-functions-transact-sql?view=sql-server-2017)。
- CAST 函数除非与 datetime、 smalldatetime 或 sql_variant 一起使用，否则其他时候都是确定性的。
- CONVERT 函数若要为确定样式，则样式参数必须是常量，此外，除了样式 20 和 21，小于或等于 100 的样式都具有不确定性。 大于 100 的样式具有确定性，但样式 106、107、109 和 113 除外。

以上只列出了常见的几种函数，具体请参看：[确定性函数和不确定性函数](https://docs.microsoft.com/zh-cn/sql/relational-databases/user-defined-functions/deterministic-and-nondeterministic-functions?view=sql-server-2017)

## 常量折叠和表达式计算

### 可折叠表达式

基于常量的表达式是可以折叠的（Constant Folding）。如：

```sql
WHERE TotalDue > 117.00 + 1000.00;
```

将被查询优化器优化成：

```sql
WHERE TotalDue > 1117.00;
```

**被 SQL Server 认为可折叠的内置函数包括 CAST 和 CONVERT**。 通常，如果内部函数只与输入有关而与其他上下文信息（例如 SET 选项、语言设置、数据库选项和加密密钥）无关，则该内部函数是可折叠的。**不确定性函数是不可折叠的。 确定性内置函数是可折叠的**，但也有例外情况。

### 不可折叠表达式
所有其他表达式类型都是不可折叠的。 特别是下列类型的表达式是不可折叠的：

- 非常量表达式，例如，结果取决于列值的表达式。
- 结果取决于局部变量或参数的表达式，例如 @x。
- 不确定性函数。
- 用户定义 Transact-SQL 函数<sup>1</sup>。
- 结果取决于语言设置的表达式。
- 结果取决于 SET 选项的表达式。
- 结果取决于服务器配置选项的表达式。

<sup>1</sup> 在 SQL Server 2012 (11.x) 之前，确定性标量值 CLR 用户定义函数和 CLR 用户定义类型的方法不可折叠。

### 表达式计算

不可折叠的表达式有时也能被优化，当表达式的参数在编译时是已知的，那么将会被优化器中的结果集大小（基数）估计器来计算，它能在一定程度上估计结果集的大小，有助于其选择较好的查询计划。

示例：[编译时表达式计算示例](https://docs.microsoft.com/zh-cn/sql/relational-databases/query-processing-architecture-guide?view=sql-server-2017#examples-of-compile-time-expression-evaluation)

参考：[优化 SELECT 语句](https://docs.microsoft.com/zh-cn/sql/relational-databases/query-processing-architecture-guide?view=sql-server-2017#optimizing-select-statements)

## 工作表

如果 ORDER BY 子句引用了不为任何索引涵盖的列，则关系引擎可能需要生成一个工作表以按所请求的顺序对结果集进行排序。

工作表在 tempdb 中生成，并在不再需要时自动删除。


## 索引

在 SQL Server 中，索引是按 [B 树](https://en.wikipedia.org/wiki/B-tree)结构组织的。索引 B 树中的每一页称为一个索引节点。 B 树的顶端节点称为根节点。 索引中的底层节点称为叶节点。 根节点与叶节点之间的任何索引级别统称为中间级。

### 聚集索引

每个表只能有一个聚集索引，因为数据行本身只能按一个顺序存储。

**在聚集索引中，叶节点包含基础表的数据页。**根节点和中间级节点包含存有索引行的索引页。 每个索引行包含一个键值和一个指针，该指针指向 B 树上的某一中间级页或叶级索引中的某个数据行。 每级索引中的页均被链接在双向链接列表中。

下图显式了聚集索引单个分区中的结构。

![图 1](./clustered_index.gif)

数据链内的页和行将**按聚集索引键值进行排序**（实际上可以看作是一种物理排序控制）。利用这一特性，我们可以对查询进行优化，例如：

- 对聚集索引列使用范围查询时，在找到包含第一个值的行后，便可以确保包含后续索引值的行物理相邻。
- 在 ORDER BY 或 GROUP BY 子句中指定的列如果是聚集索引，可以使数据库引擎不必对数据进行排序，因为这些行已经排序。这会有助于提升查询性能。

### 非聚集索引

非聚集索引包含索引键值和指向表数据存储位置的行定位器<sup>2</sup>。表或索引视图可以有多个非聚集索引。

<sup>2</sup> 行定位器有时是指向行的指针（表没有建立聚集索引，即表是堆），有时是行的聚集索引键。

查询优化器在搜索数据值时，先搜索非聚集索引以找到数据值在表中的位置，然后直接从该位置检索数据。

非聚集索引与聚集索引具有相同的 B 树结构，它们之间的显著差别在于以下两点：

- 基础表的数据行不按非聚集键的顺序排序和存储。
- 非聚集索引的叶级别是由索引页而不是由数据页组成。

下图说明了单个分区中的非聚集索引结构。

![图 2](./nonclustered_index.gif)

考虑对具有以下属性的查询使用非聚集索引：

- 使用 JOIN 或 GROUP BY 子句。
- 不返回大型结果集的查询。
- 包含经常包含在查询的搜索条件（例如返回完全匹配的 WHERE 子句）中的列。


创建非聚集索引时，可以考虑使用[包含列](https://docs.microsoft.com/zh-cn/sql/relational-databases/sql-server-index-design-guide?view=sql-server-2017#Included_Columns)来覆盖查询<sup>3</sup>。当然，如果表有聚集索引，则该聚集索引中定义的列将自动追加到表上每个非聚集索引的末端，这样也可以生成覆盖查询。

<sup>3</sup> 当索引包含查询引用的所有列时，它通常称为“覆盖查询”。

例如，假设要设计覆盖下列查询的索引。

```sql
SELECT AddressLine1, AddressLine2, City, StateProvinceID, PostalCode
FROM Person.Address
WHERE PostalCode BETWEEN N'98000' and N'99999';
```

实际参与查询的列是 `PostalCode`，如果只对这个字段添加索引，则 SELECT 的其它列将会到聚集索引上去那数据，我们如果要避免这次 IO，可以考虑将 `PostalCode` 定义为键列并包含作为非键列的所有其他列。

```sql
CREATE INDEX IX_Address_PostalCode
ON Person.Address (PostalCode)
INCLUDE (AddressLine1, AddressLine2, City, StateProvinceID);
```

添加过多的索引列（键列或非键列）会对性能产生下列影响：

- 一页上能容纳的索引行将更少。 这样会使 I/O 增加并降低缓存效率。
- 需要更多的磁盘空间来存储索引。
- 索引维护可能会增加对基础表或索引视图执行修改、插入、更新或删除操作所需的时间。

创建索引时应该确定修改数据时在查询性能上的提升是否超过了对性能的影响，以及是否需要额外的磁盘空间要求，不能一味的为了查询快而建立索引。


> 未完待续
