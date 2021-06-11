using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 支持将查询表达式转换为查询表达式树(CQT)的类。
    /// </summary>
    internal sealed class DbQueryableParser
    {
        /// <summary>
        /// 解析查询语义，将其转换成命令表达式
        /// </summary>
        internal static IDbQueryTree Parse<TElement>(IDbQueryable<TElement> source) => DbQueryableParser.Parse(source, typeof(TElement), 0);

        // 解析查询语义
        private static IDbQueryTree Parse(IDbQueryable source, Type elmentType, int startIndex)
        {
            // 目的：将query 转换成增/删/改/查
            // 1、from a in context.GetTable<T>() select a 此时query里面可能没有SELECT 表达式
            // 2、Take 视为一个查询的结束位，如有更多查询，应使用嵌套查询
            // 3、uion 分页查询也使嵌套语义
            // 4、uion 后面跟着 WHERE,GROUP BY,SELECT,JOIN语句时需要使用嵌套查询

            Type fromType = null;
            DbRawSql rawSql = null;
            bool isDistinct = false;
            bool isAny = false;
            bool isSubquery = false;
            int? skip = null;
            int? take = null;
            int? outerIndex = null;
            List<DbExpression> wheres = null;       // WHERE
            List<DbExpression> havings = null;      // HAVING
            List<DbExpression> joins = null;        // JOIN
            List<DbExpression> orderBys = null;     // ORDER BY
            List<DbExpression> includes = null;     // ORDER BY
            List<DbQuerySelectTree> unions = null;  // UNION ALL

            Expression pickExpression = null;
            DbExpression insert = null;             // INSERT #
            DbExpression update = null;             // UPDATE #
            DbExpression delete = null;             // DELETE #
            DbExpression group = null;              // GROUP BY #
            DbExpression aggregate = null;          // SUM&MAX  #

            for (int index = startIndex; index < source.DbExpressions.Count; index++)
            {
                DbExpression item = source.DbExpressions[index];

                // Take(n)
                if (take != null || (skip != null && item.DbExpressionType != DbExpressionType.Take) || isDistinct || isSubquery)
                {
                    outerIndex = index;
                    break;
                }

                #region 解析片断

                switch (item.DbExpressionType)
                {
                    case DbExpressionType.None:
                    case DbExpressionType.All:
                        continue;

                    case DbExpressionType.Any:
                        isAny = true;
                        if (item.Expressions != null)
                        {
                            if (wheres == null) wheres = new List<DbExpression>();
                            wheres.Add(item);
                        }
                        break;

                    case DbExpressionType.AsSubquery:
                        isSubquery = true;
                        continue;

                    case DbExpressionType.Union:
                        var constExpression = item.Expressions[0] as ConstantExpression;
                        var uQuery = constExpression.Value as IDbQueryable;
                        var u = DbQueryableParser.Parse(uQuery, constExpression.Type.GetGenericArguments()[0], 0);
                        if (unions == null) unions = new List<DbQuerySelectTree>();
                        unions.Add((DbQuerySelectTree)u);

                        // 如果下一个不是 union，就使用嵌套
                        if (index + 1 <= source.DbExpressions.Count - 1 && source.DbExpressions[index + 1].DbExpressionType != DbExpressionType.Union)
                            isSubquery = true;
                        continue;

                    case DbExpressionType.Include:
                        if (includes == null) includes = new List<DbExpression>();
                        includes.Add(item);
                        continue;

                    case DbExpressionType.GroupBy:
                        group = item;
                        continue;

                    case DbExpressionType.GetTable:
                        fromType = (item.Expressions[0] as ConstantExpression).Value as Type;

                        if (fromType == null)
                        {
                            string text = (item.Expressions[0] as ConstantExpression).Value as string;
                            if (text != null)
                                rawSql = new DbRawSql(null, text, (object[])(item.Expressions[1] as ConstantExpression).Value);
                        }

                        continue;

                    case DbExpressionType.Average:
                    case DbExpressionType.Min:
                    case DbExpressionType.Sum:
                    case DbExpressionType.Max:
                        aggregate = item;
                        continue;

                    case DbExpressionType.Count:
                        aggregate = item;
                        if (item.Expressions != null)
                        {
                            if (wheres == null) wheres = new List<DbExpression>();
                            wheres.Add(item);
                        }
                        continue;

                    case DbExpressionType.Distinct:
                        isDistinct = true;
                        continue;

                    case DbExpressionType.First:
                    case DbExpressionType.FirstOrDefault:
                        take = 1;
                        if (item.Expressions != null)
                        {
                            if (wheres == null) wheres = new List<DbExpression>();
                            wheres.Add(item);
                        }
                        continue;

                    case DbExpressionType.Join:
                    case DbExpressionType.LeftOuterJoin:
                    case DbExpressionType.RightOuterJoin:
                        pickExpression = item.Expressions[3];

                        var j = item;

                        // GetTable 的参数
                        var inner = (j.Expressions[0] as ConstantExpression).Value as IDbQueryable;
                        if (inner.DbExpressions.Count == 1 && inner.DbExpressions[0].DbExpressionType == DbExpressionType.GetTable)
                        {
                            // 区别 GetTable 有三个重载，这里处理指定外键路径的重载
                            if (inner.DbExpressions[0].Expressions.Length == 2 && (inner.DbExpressions[0].Expressions[0] as ConstantExpression).Value is Type)
                            {
                                var expressions = new Expression[item.Expressions.Length + 1];
                                Array.Copy(item.Expressions, expressions, item.Expressions.Length);
                                expressions[expressions.Length - 1] = inner.DbExpressions[0].Expressions[1];
                                j = new DbExpression(item.DbExpressionType, expressions);
                            }
                        }

                        if (joins == null)
                            joins = new List<DbExpression>();
                        joins.Add(j);

                        continue;

                    case DbExpressionType.OrderBy:
                    case DbExpressionType.OrderByDescending:
                        if (orderBys == null) orderBys = new List<DbExpression>();
                        orderBys.Add(item);
                        continue;
                    case DbExpressionType.Select:
                        pickExpression = item.Expressions != null ? item.Expressions[0] : null;
                        continue;

                    case DbExpressionType.SelectMany:
                        pickExpression = item.Expressions[1];
                        if (IsCrossJoinExression(source.DbExpressions, item, startIndex))
                        {
                            if (joins == null)
                                joins = new List<DbExpression>();
                            joins.Add(item);
                        }
                        continue;

                    case DbExpressionType.Single:
                    case DbExpressionType.SingleOrDefault:
                        take = 1;
                        if (item.Expressions != null)
                        {
                            if (wheres == null) wheres = new List<DbExpression>();
                            wheres.Add(item);
                        }
                        continue;

                    case DbExpressionType.Skip:
                        skip = (int)(item.Expressions[0] as ConstantExpression).Value;
                        continue;

                    case DbExpressionType.Take:
                        take = (int)(item.Expressions[0] as ConstantExpression).Value;
                        continue;

                    case DbExpressionType.ThenBy:
                    case DbExpressionType.ThenByDescending:
                        if (orderBys == null) orderBys = new List<DbExpression>();
                        orderBys.Add(item);
                        continue;

                    case DbExpressionType.Where:
                        if (item.Expressions != null)
                        {
                            if (group == null)
                            {
                                if (wheres == null) wheres = new List<DbExpression>();
                                wheres.Add(item);
                            }
                            else
                            {
                                if (havings == null) havings = new List<DbExpression>();
                                havings.Add(item);
                            }
                        }
                        continue;

                    case DbExpressionType.Insert:
                        insert = item;
                        continue;

                    case DbExpressionType.Update:
                        update = item;
                        continue;

                    case DbExpressionType.Delete:
                        delete = item;
                        continue;

                    default:
                        throw new NotSupportedException(string.Format("{0} is not support.", item.DbExpressionType));
                }

                #endregion
            }

            // 没有解析到INSERT/DELETE/UPDATE/SELECT表达式，并且没有相关聚合函数，则默认选择 FromEntityType 的所有字段
            if (insert == null && delete == null && update == null && pickExpression == null && aggregate == null)
                pickExpression = Expression.Constant(fromType ?? elmentType);
            DbExpression select = new DbExpression(DbExpressionType.Select, pickExpression);
            if (fromType == null)
                fromType = elmentType;

            var result_Query = new DbQuerySelectTree();
            result_Query.From = fromType;
            result_Query.FromSql = rawSql;
            result_Query.HasDistinct = isDistinct;
            result_Query.HasAny = isAny;
            result_Query.Joins = joins;
            result_Query.OrderBys = orderBys;
            result_Query.GroupBy = group;
            result_Query.Aggregate = aggregate;
            result_Query.Unions = unions;
            result_Query.Includes = includes;
            result_Query.Skip = skip != null ? skip.Value : 0;
            result_Query.Take = take != null ? take.Value : 0;
            result_Query.Select = select;
            result_Query.Wheres = wheres;
            result_Query.Havings = havings;

            #region 更新语义

            if (update != null)
            {
                var result_Update = new DbQueryUpdateTree();
                var constantExpression = update.Expressions != null ? update.Expressions[0] as ConstantExpression : null;
                if (constantExpression != null)
                    result_Update.Entity = constantExpression.Value;
                else
                    result_Update.Expression = update.Expressions[0];
                result_Update.Select = result_Query;
                return result_Update;
            }

            #endregion

            #region 删除语义

            else if (delete != null)
            {
                var result_Delete = new DbQueryDeleteTree();
                var constantExpression = delete.Expressions != null ? delete.Expressions[0] as ConstantExpression : null;
                if (constantExpression != null)
                    result_Delete.Entity = constantExpression.Value;
                result_Delete.Select = result_Query;
                return result_Delete;
            }

            #endregion

            #region 插入语义

            else if (insert != null)
            {
                var result_Insert = new DbQueryInsertTree();
                if (insert.Expressions != null)
                {
                    result_Insert.Entity = (insert.Expressions[0] as ConstantExpression).Value;
                    if (insert.Expressions.Length > 1)
                        result_Insert.EntityColumns = (insert.Expressions[1] as ConstantExpression).Value as IList<Expression>;
                }
                result_Insert.Select = result_Query;
                result_Insert.Bulk = ((DbQueryable)source).Bulk;
                return result_Insert;
            }

            #endregion

            #region 选择语义

            else if (pickExpression != null)
            {
                // 检查嵌套查询语义
                result_Query = DbQueryableParser.TryParseOutQuery(result_Query);
            }

            #endregion

            #region 嵌套语义

            // 解析嵌套查询
            if (outerIndex != null)
            {
                // todo => elementType ???
                var outQueryTree = DbQueryableParser.Parse(source, elmentType, outerIndex.Value);
                var result_Insert = outQueryTree as DbQueryInsertTree;
                var result_Update = outQueryTree as DbQueryUpdateTree;
                var result_Delete = outQueryTree as DbQueryDeleteTree;
                if (result_Insert != null)
                {
                    if (result_Insert.Select != null)
                        result_Insert.Select.Subquery = result_Query;
                    else
                        result_Insert.Select = result_Query;
                    return result_Insert;
                }
                else if (result_Update != null)
                {
                    if (result_Update.Select != null)
                        result_Update.Select.Subquery = result_Query;
                    else
                        result_Update.Select = result_Query;
                    return result_Update;
                }
                else if (result_Delete != null)
                {
                    if (result_Delete.Select != null)
                        result_Delete.Select.Subquery = result_Query;
                    else
                        result_Delete.Select = result_Query;
                    return result_Delete;
                }
                else
                {
                    // 指定子查询
                    var iterator = (DbQuerySelectTree)outQueryTree;
                    while (iterator.Subquery != null) iterator = iterator.Subquery;
                    iterator.Subquery = result_Query;

                    //// ?? AsSubquery(a=>a)，那么它所有的外层字段都要基于里层的字段
                    //if (isSubquery && startIndex == 0)
                    //    LimitSelector((DbQuerySelectTree)outQueryTree);

                    // 如果外层是统计，内层没有分页，则不需要排序
                    iterator = (DbQuerySelectTree)outQueryTree;
                    while (iterator.Subquery != null)
                    {
                        // 没有分页的嵌套统计，不需要排序
                        if (iterator.Aggregate != null && !(iterator.Subquery.Take > 0 || iterator.Subquery.Skip > 0) && iterator.Subquery.OrderBys != null && iterator.Subquery.OrderBys.Count > 0)
                            iterator.Subquery.OrderBys = new List<DbExpression>(0);

                        // 继续下一轮迭代
                        iterator = iterator.Subquery;
                    }

                    return outQueryTree;
                }
            }

            #endregion

            // 查询表达式
            return result_Query;
        }

        // 构造由一对多关系产生的嵌套查询
        private static DbQuerySelectTree TryParseOutQuery(DbQuerySelectTree tree)
        {
            if (tree == null || tree.Select == null) return tree;

            Expression select = tree.Select.Expressions[0];
            List<DbExpression> includes = tree.Includes;
            Type fromType = tree.From;

            // 解析导航属性 如果有 1:n 的导航属性，那么查询的结果集的主记录将会有重复记录
            // 这时就需要使用嵌套语义，先查主记录，再关联导航记录
            Expression expression = select;
            var lambdaExpression = expression as LambdaExpression;
            if (lambdaExpression != null) expression = lambdaExpression.Body;
            var initExpression = expression as MemberInitExpression;
            var newExpression = expression as NewExpression;

            Navigation navigation = DbQueryableParser.CheckSelectedNavigation(includes);
            if (navigation != Navigation.All && initExpression != null)
            {
                Navigation n = CheckSelectedNavigation(initExpression);
                if (navigation == Navigation.None)
                    navigation = n;
                else
                    navigation = navigation | n;
            }

            #region 嵌套语义

            if ((navigation & Navigation.Many) == Navigation.Many || ((navigation & Navigation.One) == Navigation.One && tree.GroupBy != null))
            {
                newExpression = initExpression != null ? initExpression.NewExpression : newExpression;
                List<MemberBinding> simplfyBindings = new List<MemberBinding>();
                if (initExpression != null)
                {
                    simplfyBindings = initExpression.Bindings.ToList(a =>
                    {
                        var property = a.Member as System.Reflection.PropertyInfo;
                        if (property != null)
                            return TypeUtils.IsPrimitiveType(property.PropertyType);

                        var field = a.Member as System.Reflection.FieldInfo;
                        if (field != null)
                            return TypeUtils.IsPrimitiveType(field.FieldType);

                        return false;
                    });
                }

                if (newExpression != null || simplfyBindings.Count() > 0)
                {
                    // 简化内层选择器，只选择最小字段，不选择导航字段，导航字段在外层加进去
                    initExpression = Expression.MemberInit(newExpression, simplfyBindings);
                    lambdaExpression = Expression.Lambda(initExpression, lambdaExpression.Parameters);
                    tree.Select = new DbExpression(DbExpressionType.Select, lambdaExpression);
                }
                tree.Includes = new List<DbExpression>(0);

                var result_Query = new DbQuerySelectTree();
                result_Query.From = fromType;
                result_Query.Subquery = tree;
                result_Query.Joins = null;
                result_Query.OrderBys = null;
                result_Query.Includes = includes;
                result_Query.SelectHasMany = true;
                result_Query.Select = new DbExpression(DbExpressionType.Select, select);

                #region 排序

                if (tree.OrderBys != null && tree.OrderBys.Count > 0)
                {
                    // 是否有分页
                    bool havePaging = (tree.Take > 0 || tree.Skip > 0);
                    if (!havePaging)
                    {
                        // 如果没有分页，则OrderBy需要放在外层
                        result_Query.OrderBys = tree.OrderBys;
                        tree.OrderBys = new List<DbExpression>(0);
                    }
                    else
                    {
                        // 如果有分页，只有主表/用到的1:1从表放在内层，其它放在外层
                        List<DbExpression> innerOrderBys = null;
                        foreach (var dbExpression in tree.OrderBys)
                        {
                            bool isOrderByMany = IsOrderByManyMember(dbExpression.Expressions[0] as LambdaExpression);
                            if (!isOrderByMany)
                            {
                                if (innerOrderBys == null)
                                    innerOrderBys = new List<DbExpression>();
                                innerOrderBys.Add(dbExpression);
                            }
                        }

                        if (innerOrderBys != null && innerOrderBys.Count > 0)
                        {
                            result_Query.OrderBys = tree.OrderBys;
                            tree.OrderBys = innerOrderBys;
                        }
                    }
                }

                #endregion

                #region 分组

                if (tree.GroupBy != null)
                {
                    // 查看外层是否需要重新构造选择器。如果有分组并且有聚合函数，则需要重新构造选择器。否则外层解析不了聚合函数
                    // demo => line 1280
                    bool newSelector =
                        simplfyBindings.Any(x => ((MemberAssignment)x).Expression.NodeType == ExpressionType.Call &&
                            ExpressionExtensions.Aggregates.Any(a => a.Key.ToString() == (((MemberAssignment)x).Expression as MethodCallExpression).Method.Name)) ||
                        newExpression.Arguments.Any(x => x.NodeType == ExpressionType.Call &&
                            ExpressionExtensions.Aggregates.Any(a => a.Key.ToString() == (x as MethodCallExpression).Method.Name));
                    if (newSelector)
                    {
                        ParameterExpression newParameter = null;
                        List<DbExpression> dbExpressions = null;
                        if (result_Query.Includes != null && result_Query.Includes.Count > 0)
                            dbExpressions = result_Query.Includes;
                        else if (result_Query.OrderBys != null && result_Query.OrderBys.Count > 0)
                            dbExpressions = result_Query.OrderBys;
                        if (dbExpressions != null && dbExpressions.Count > 0)
                            newParameter = (dbExpressions[0].Expressions[0] as LambdaExpression).Parameters[0];

                        // 1对多导航嵌套查询外层的的第一个表别名固定t0，参数名可随意
                        var paramExpression = newParameter != null ? newParameter : Expression.Parameter(newExpression.Type, "__g");
                        simplfyBindings = simplfyBindings.ToList(x => (MemberBinding)Expression.Bind(x.Member, Expression.MakeMemberAccess(paramExpression, x.Member)));
                        List<Expression> arguments = null;
                        if (newExpression.Members != null)
                        {
                            arguments = new List<Expression>(newExpression.Arguments.Count);
                            for (int i = 0; i < newExpression.Arguments.Count; i++)
                            {
                                var member = newExpression.Members[i];
                                var arg = Expression.MakeMemberAccess(paramExpression, member);
                                arguments.Add(arg);
                            }
                        }

                        newExpression = Expression.New(newExpression.Constructor, arguments, newExpression.Members);
                        initExpression = Expression.MemberInit(newExpression, simplfyBindings);
                        lambdaExpression = Expression.Lambda(initExpression, paramExpression);
                        result_Query.Select = new DbExpression(DbExpressionType.Select, lambdaExpression);
                    }
                }

                #endregion

                tree = result_Query;
            }

            #endregion

            return tree;
        }

        // 判定当前 select 语义的导航属性
        private static Navigation CheckSelectedNavigation(MemberInitExpression node)
        {
            Navigation result = Navigation.None;

            for (int i = 0; i < node.Bindings.Count; i++)
            {
                // primitive 类型
                Type type = (node.Bindings[i].Member as System.Reflection.PropertyInfo).PropertyType;
                if (TypeUtils.IsPrimitiveType(type)) continue;
                // list 类型
                if (TypeUtils.IsCollectionType(type))
                {
                    if (result == Navigation.None)
                        result = Navigation.Many;
                    else
                        result = result | Navigation.Many;

                    if (result == Navigation.All)
                        break;
                    else
                        continue;
                }

                // new Model
                MemberAssignment memberAssignment = node.Bindings[i] as MemberAssignment;
                if (memberAssignment != null && memberAssignment.Expression.NodeType == ExpressionType.MemberInit)
                {
                    MemberInitExpression initExpression = memberAssignment.Expression as MemberInitExpression;
                    Navigation n = CheckSelectedNavigation(initExpression);
                    if (result == Navigation.None)
                        result = n;
                    else
                        result = result | n;

                    if (result == Navigation.All)
                        break;
                    else
                        continue;
                }

                // a.Model
                if (memberAssignment != null && memberAssignment.Expression.NodeType == ExpressionType.MemberAccess)
                {
                    if (result == Navigation.None)
                        result = Navigation.One;
                    else
                        result = result | Navigation.One;

                    if (result == Navigation.All)
                        break;
                    else
                        continue;
                }
            }

            return result;
        }

        // 判定当前 select 语义的导航属性
        private static Navigation CheckSelectedNavigation(List<DbExpression> includes)
        {
            if (includes == null)
                return Navigation.None;

            Navigation result = Navigation.None;
            foreach (DbExpression dbExpression in includes)
            {
                Expression expression = dbExpression.Expressions[0];
                if (expression.NodeType == ExpressionType.Lambda) expression = (expression as LambdaExpression).Body;
                else if (expression.NodeType == ExpressionType.Call) expression = (expression as MethodCallExpression).Object;

                Navigation n = TypeUtils.IsCollectionType(expression.Type)
                    ? Navigation.Many
                    : Navigation.One;

                if (result == Navigation.None)
                    result = n;
                else
                    result = result | n;

                if (result == Navigation.All)
                    break;
            }

            return result;
        }

        // 判定 OrderBy 语义里是否声明了一对多关系的导航
        private static bool IsOrderByManyMember(LambdaExpression orderByNode)
        {
            bool result = false;
            Expression expression = orderByNode.Body;
            while (expression.IsChildNode())
            {
                // => a.Accounts[0].Member
                if (expression.NodeType == ExpressionType.MemberAccess) expression = (expression as MemberExpression).Expression;
                else if (expression.NodeType == ExpressionType.Call)
                {
                    var methodExpression = expression as MethodCallExpression;
                    bool isIndex = methodExpression.IsCollectionIndex();
                    if (isIndex) expression = methodExpression.Object;
                }

                // 如果包含List<>泛型导航，则可以判定整个查询包含一对多的导航
                if (TypeUtils.IsCollectionType(expression.Type))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        // 判断表达式是否是 CROSS JOIN
        private static bool IsCrossJoinExression(IList<DbExpression> collection, DbExpression dbExpression, int start = 0)
        {
            Expression node = dbExpression.Expressions[0];
            if (node.NodeType == ExpressionType.Lambda)
            {
                var lambda = node as LambdaExpression;
                node = lambda.Body;
            }
            if (node.NodeType == ExpressionType.Call)
            {
                // 如果是 DefaultIfEmpty，则不是 CROSS JOIN
                var methodCall = node as MethodCallExpression;
                if (methodCall.Method.Name == "DefaultIfEmpty")
                {
                    // 右关联
                    if (methodCall.Arguments != null && methodCall.Arguments.Count == 2 && (bool)(((ConstantExpression)methodCall.Arguments[1]).Value))
                    {
                        string name = ((MemberExpression)methodCall.Arguments[0]).Member.Name;
                        for (int i = start; i < collection.Count; i++)
                        {
                            var item = collection[i];
                            if (item.DbExpressionType == DbExpressionType.LeftOuterJoin)
                            {
                                LambdaExpression lambda = item.Expressions[3] as LambdaExpression;
                                NewExpression newExpression = lambda.Body as NewExpression;
                                string pName = (newExpression.Arguments[1] as ParameterExpression).Name;
                                if (name == pName)
                                {
                                    item.DbExpressionType = DbExpressionType.RightOuterJoin;
                                    break;
                                }
                            }

                        }
                    }

                    return false;
                }
            }

            // 根据系统生成的变量名判断 
            return !dbExpression.Expressions[0].IsAnonymous();
        }

        // 导航属性
        [Flags]
        enum Navigation
        {
            /// <summary>
            /// 无
            /// </summary>
            None = 0,

            /// <summary>
            /// 1:1导航
            /// </summary>
            One = 1,

            /// <summary>
            /// 1：n导航
            /// </summary>
            Many = 2,

            /// <summary>
            /// 1:1导航 以及 1：n导航
            /// </summary>
            All = One | Many
        }
    }
}
