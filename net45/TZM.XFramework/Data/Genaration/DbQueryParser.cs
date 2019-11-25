using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 查询语义转换
    /// </summary>
    internal class DbQueryParser
    {
        /// <summary>
        /// 解析查询语义
        /// </summary>
        internal static IDbQueryableInfo<TElement> Parse<TElement>(IDbQueryable<TElement> source)
        {
            return DbQueryParser.Parse(source, 0);
        }

        // 解析查询语义
        static IDbQueryableInfo<TElement> Parse<TElement>(IDbQueryable<TElement> source, int startIndex)
        {
            // 目的：将query 转换成增/删/改/查
            // 1、from a in context.GetTable<T>() select a 此时query里面可能没有SELECT 表达式
            // 2、Take 视为一个查询的结束位，如有更多查询，应使用嵌套查询
            // 3、uion 分页查询也使嵌套语义
            // 4、uion 后面跟着 WHERE,GROUP BY,SELECT,JOIN语句时需要使用嵌套查询

            Type type = null;
            bool isDistinct = false;
            bool isAny = false;
            bool subQuery = false;
            int? skip = null;
            int? take = null;
            int? outerIndex = null;
            var conditions = new List<Expression>();            // WHERE
            var havings = new List<Expression>();               // HAVING
            var joins = new List<DbExpression>();               // JOIN
            var orderBys = new List<DbExpression>();            // ORDER BY
            var includes = new List<DbExpression>();            // ORDER BY
            var unions = new List<IDbQueryableInfo<TElement>>();

            Expression select = null;                           // SELECT #
            DbExpression insert = null;                         // INSERT #
            DbExpression update = null;                         // UPDATE #
            DbExpression delete = null;                         // DELETE #
            DbExpression group = null;                          // GROUP BY #
            DbExpression aggregate = null;                      // SUM&MAX  #

            for (int index = startIndex; index < source.DbExpressions.Count; index++)
            {
                DbExpression item = source.DbExpressions[index];

                // Take(n)
                if (take != null || (skip != null && item.DbExpressionType != DbExpressionType.Take) || isDistinct || subQuery)
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
                        if (item.Expressions != null) conditions.Add(item.Expressions[0]);
                        break;

                    case DbExpressionType.AsSubQuery:
                        subQuery = true;
                        continue;

                    case DbExpressionType.Union:
                        var uQuery = (item.Expressions[0] as ConstantExpression).Value as IDbQueryable<TElement>;
                        var u = DbQueryParser.Parse(uQuery);
                        unions.Add(u);

                        // 如果下一个不是 union，就使用嵌套
                        if (index + 1 <= source.DbExpressions.Count - 1 && source.DbExpressions[index + 1].DbExpressionType != DbExpressionType.Union)
                            subQuery = true;
                        continue;

                    case DbExpressionType.Include:
                        includes.Add(item);
                        continue;

                    case DbExpressionType.GroupBy:
                        group = item;
                        continue;

                    case DbExpressionType.GetTable:
                        type = (item.Expressions[0] as ConstantExpression).Value as Type;
                        continue;

                    case DbExpressionType.Average:
                    case DbExpressionType.Min:
                    case DbExpressionType.Sum:
                    case DbExpressionType.Max:
                        aggregate = item;
                        continue;

                    case DbExpressionType.Count:
                        aggregate = item;
                        if (item.Expressions != null) conditions.Add(item.Expressions[0]);
                        continue;

                    case DbExpressionType.Distinct:
                        isDistinct = true;
                        continue;

                    case DbExpressionType.First:
                    case DbExpressionType.FirstOrDefault:
                        take = 1;
                        if (item.Expressions != null) conditions.Add(item.Expressions[0]);
                        continue;

                    case DbExpressionType.Join:
                    case DbExpressionType.GroupJoin:
                    case DbExpressionType.GroupRightJoin:
                        select = item.Expressions[3];
                        joins.Add(item);
                        continue;

                    case DbExpressionType.OrderBy:
                    case DbExpressionType.OrderByDescending:
                        orderBys.Add(item);
                        continue;
                    case DbExpressionType.Select:
                        select = item.Expressions != null ? item.Expressions[0] : null;
                        continue;

                    case DbExpressionType.SelectMany:
                        select = item.Expressions[1];
                        if (IsSelectMany(source.DbExpressions, item, startIndex)) joins.Add(item);
                        continue;

                    case DbExpressionType.Single:
                    case DbExpressionType.SingleOrDefault:
                        take = 1;
                        if (item.Expressions != null) conditions.Add(item.Expressions[0]);
                        continue;

                    case DbExpressionType.Skip:
                        skip = (int)(item.Expressions[0] as ConstantExpression).Value;
                        continue;

                    case DbExpressionType.Take:
                        take = (int)(item.Expressions[0] as ConstantExpression).Value;
                        continue;

                    case DbExpressionType.ThenBy:
                    case DbExpressionType.ThenByDescending:
                        orderBys.Add(item);
                        continue;

                    case DbExpressionType.Where:
                        var predicate = group == null ? conditions : havings;
                        if (item.Expressions != null) predicate.Add(item.Expressions[0]);
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
            bool pickAllColumns = insert == null && delete == null && update == null && select == null && aggregate == null;
            if (pickAllColumns) select = Expression.Constant(type ?? typeof(TElement));

            var result_Query = new DbQueryableInfo_Select<TElement>();
            result_Query.FromEntityType = type;
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
            result_Query.Select = new DbExpression(DbExpressionType.Select, select);
            result_Query.Condtion = new DbExpression(DbExpressionType.Where, CombineCondition(conditions));
            result_Query.Having = new DbExpression(DbExpressionType.None, CombineCondition(havings));
            result_Query.SourceQuery = source;

            #region 更新语义

            if (update != null)
            {
                var result_Update = new DbQueryableInfo_Update<TElement>();
                var constantExpression = update.Expressions != null ? update.Expressions[0] as ConstantExpression : null;
                if (constantExpression != null)
                    result_Update.Entity = constantExpression.Value;
                else
                    result_Update.Expression = update.Expressions[0];
                result_Update.SelectInfo = result_Query;
                result_Update.SourceQuery = source;
                return result_Update;
            }

            #endregion

            #region 删除语义

            else if (delete != null)
            {
                var result_Delete = new DbQueryableInfo_Delete<TElement>();
                var constantExpression = delete.Expressions != null ? delete.Expressions[0] as ConstantExpression : null;
                if (constantExpression != null)
                    result_Delete.Entity = constantExpression.Value;
                result_Delete.SelectInfo = result_Query;
                result_Delete.SourceQuery = source;
                return result_Delete;
            }

            #endregion

            #region 插入语义

            else if (insert != null)
            {
                var result_Insert = new DbQueryableInfo_Insert<TElement>();
                if (insert.Expressions != null)
                {
                    result_Insert.Entity = (insert.Expressions[0] as ConstantExpression).Value;
                    if (insert.Expressions.Length > 1) 
                        result_Insert.EntityColumns = (insert.Expressions[1] as ConstantExpression).Value as IList<Expression>;
                }
                result_Insert.SelectInfo = result_Query;
                result_Insert.Bulk = source.Bulk;
                result_Insert.SourceQuery = source;
                source.DbQueryInfo = result_Insert;
                return result_Insert;
            }

            #endregion

            #region 选择语义

            else if (select != null)
            {
                // 检查嵌套查询语义
                result_Query = DbQueryParser.ParseOutQuery(result_Query);
                // 查询来源
                result_Query.SourceQuery = source;
            }

            #endregion

            #region 嵌套语义

            // 解析嵌套查询
            if (outerIndex != null)
            {
                var outQuery = DbQueryParser.Parse<TElement>(source, outerIndex.Value);
                var nQuery = outQuery as DbQueryableInfo_Insert<TElement>;
                var uQuery = outQuery as DbQueryableInfo_Update<TElement>;
                if (nQuery != null)
                {
                    if (nQuery.SelectInfo != null)
                        nQuery.SelectInfo.SubQueryInfo = result_Query;
                    else
                        nQuery.SelectInfo = result_Query;
                    nQuery.SourceQuery = source;
                    return nQuery;
                }
                else if (uQuery != null)
                {
                    if (uQuery.SelectInfo != null)
                        uQuery.SelectInfo.SubQueryInfo = result_Query;
                    else
                        uQuery.SelectInfo = result_Query;
                    uQuery.SourceQuery = source;
                    return uQuery;
                }
                else
                {
                    var rootQuery = outQuery;
                    while (rootQuery.SubQueryInfo != null) rootQuery = rootQuery.SubQueryInfo;
                    rootQuery.SubQueryInfo = result_Query;
                    outQuery.SourceQuery = source;

                    // 如果外层是统计，内层没有分页，则不需要排序
                    rootQuery = outQuery;
                    while (rootQuery.SubQueryInfo != null)
                    {
                        var myOutQuery = rootQuery as IDbQueryableInfo_Select;
                        var mySubQuery = rootQuery.SubQueryInfo as IDbQueryableInfo_Select;
                        // 没有分页的嵌套统计，不需要排序
                        if (myOutQuery.Aggregate != null && !(mySubQuery.Take > 0 || mySubQuery.Skip > 0) && mySubQuery.OrderBys.Count > 0)
                            mySubQuery.OrderBys = new List<DbExpression>(0);
                        // 继续下一轮迭代
                        rootQuery = rootQuery.SubQueryInfo;
                    }

                    return outQuery;
                }
            }

            #endregion

            // 查询表达式
            return result_Query;
        }

        // 构造由一对多关系产生的嵌套查询
        static DbQueryableInfo_Select<TElement> ParseOutQuery<TElement>(DbQueryableInfo_Select<TElement> dbQuery)
        {
            // @havePaging 是否有分页信息

            if (dbQuery == null || dbQuery.Select == null) return dbQuery;

            Expression select = dbQuery.Select.Expressions[0];
            List<DbExpression> includes = dbQuery.Includes;
            Type type = dbQuery.FromEntityType;

            // 解析导航属性 如果有 1:n 的导航属性，那么查询的结果集的主记录将会有重复记录
            // 这时就需要使用嵌套语义，先查主记录，再关联导航记录
            Expression myExpression = select;
            var lambdaExpression = myExpression as LambdaExpression;
            if (lambdaExpression != null) myExpression = lambdaExpression.Body;
            var initExpression = myExpression as MemberInitExpression;
            var newExpression = myExpression as NewExpression;

            bool hasMany = DbQueryParser.IsHasMany(includes);
            if (!hasMany) hasMany = initExpression != null && IsHasMany<TElement>(initExpression);

            #region 嵌套语义

            if (hasMany)
            {

                newExpression = initExpression != null ? initExpression.NewExpression : newExpression;
                List<MemberBinding> bindings = new List<MemberBinding>();
                if (initExpression != null)
                    bindings = initExpression.Bindings.ToList(x => x, x => TypeUtils.IsPrimitiveType((x.Member as System.Reflection.PropertyInfo).PropertyType));

                if (newExpression != null || bindings.Count() > 0)
                {
                    // 简化内层选择器，只选择最小字段，不选择导航字段，导航字段在外层加进去
                    initExpression = Expression.MemberInit(newExpression, bindings);
                    lambdaExpression = Expression.Lambda(initExpression, lambdaExpression.Parameters);
                    dbQuery.Select = new DbExpression(DbExpressionType.Select, lambdaExpression);
                }
                dbQuery.SubQueryOfMany = true;
                dbQuery.Includes = new List<DbExpression>(0);

                var result_Query = new DbQueryableInfo_Select<TElement>();
                result_Query.FromEntityType = type;
                result_Query.SubQueryInfo = dbQuery;
                result_Query.Joins = new List<DbExpression>(0);
                result_Query.OrderBys = new List<DbExpression>(0);
                result_Query.Includes = includes;
                result_Query.HasMany = true;
                result_Query.Select = new DbExpression(DbExpressionType.Select, select);

                #region 排序

                if (dbQuery.OrderBys.Count > 0)
                {
                    // 是否有分页
                    bool havePaging = (dbQuery.Take > 0 || dbQuery.Skip > 0);
                    if (!havePaging)
                    {
                        // 如果没有分页，则OrderBy需要放在外层
                        result_Query.OrderBys = dbQuery.OrderBys;
                        dbQuery.OrderBys = new List<DbExpression>(0);
                    }
                    else
                    {
                        // 如果有分页，只有主表/用到的1:1从表放在内层，其它放在外层
                        List<DbExpression> innerOrderBy = null;
                        foreach (var dbExpression in dbQuery.OrderBys)
                        {
                            hasMany = IsHasMany(dbExpression.Expressions[0] as LambdaExpression);
                            if (!hasMany)
                            {
                                if (innerOrderBy == null) innerOrderBy = new List<DbExpression>();
                                innerOrderBy.Add(dbExpression);
                            }
                        }

                        if (innerOrderBy != null && innerOrderBy.Count > 0)
                        {
                            result_Query.OrderBys = dbQuery.OrderBys;
                            dbQuery.OrderBys = innerOrderBy;
                        }
                    }
                }

                #endregion

                #region 分组

                if (dbQuery.GroupBy != null)
                {
                    // 查看外层是否需要重新构造选择器。如果有分组并且有聚合函数，则需要重新构造选择器。否则外层解析不了聚合函数
                    // demo => line 1280
                    bool newSelector = bindings.Any(x => ((MemberAssignment)x).Expression.NodeType == ExpressionType.Call) || newExpression.Arguments.Any(x => x.NodeType == ExpressionType.Call);
                    if (newSelector)
                    {
                        ParameterExpression newParameter = null;
                        List<DbExpression> dbExpressions = null;
                        if (result_Query.Includes != null && result_Query.Includes.Count > 0) dbExpressions = result_Query.Includes;
                        else if (result_Query.OrderBys != null && result_Query.OrderBys.Count > 0) dbExpressions = result_Query.OrderBys;
                        if (dbExpressions != null && dbExpressions.Count > 0) newParameter = (dbExpressions[0].Expressions[0] as LambdaExpression).Parameters[0];

                        // 1对多导航嵌套查询外层的的第一个表别名固定t0，参数名可随意
                        var parameterExpression = newParameter != null ? newParameter : Expression.Parameter(newExpression.Type, "__g");
                        bindings = bindings.ToList(x => (MemberBinding)Expression.Bind(x.Member, Expression.MakeMemberAccess(parameterExpression, x.Member)));
                        List<Expression> arguments = null;
                        if (newExpression.Members != null)
                        {
                            arguments = new List<Expression>(newExpression.Arguments.Count);
                            for (int i = 0; i < newExpression.Arguments.Count; i++)
                            {
                                var member = newExpression.Members[i];
                                var arg = Expression.MakeMemberAccess(parameterExpression, member);
                                arguments.Add(arg);
                            }
                        }

                        newExpression = Expression.New(newExpression.Constructor, arguments, newExpression.Members);
                        initExpression = Expression.MemberInit(newExpression, bindings);
                        lambdaExpression = Expression.Lambda(initExpression, parameterExpression);
                        result_Query.Select = new DbExpression(DbExpressionType.Select, lambdaExpression);
                    }
                }

                #endregion

                dbQuery = result_Query;
            }

            #endregion

            return dbQuery;
        }

        // 判定 MemberInit 绑定是否声明了一对多关系的导航
        static bool IsHasMany<T>(MemberInitExpression node)
        {
            for (int i = 0; i < node.Bindings.Count; i++)
            {
                // primitive 类型
                Type type = (node.Bindings[i].Member as System.Reflection.PropertyInfo).PropertyType;
                if (TypeUtils.IsPrimitiveType(type)) continue;

                // complex 类型
                if (TypeUtils.IsCollectionType(type)) return true;

                MemberAssignment memberAssignment = node.Bindings[i] as MemberAssignment;
                if (memberAssignment != null && memberAssignment.Expression.NodeType == ExpressionType.MemberInit)
                {
                    MemberInitExpression initExpression = memberAssignment.Expression as MemberInitExpression;
                    bool hasManyNavgation = IsHasMany<T>(initExpression);
                    if (hasManyNavgation) return true;
                }
            }

            return false;
        }

        // 判定 MemberInit 绑定是否声明了一对多关系的导航
        static bool IsHasMany(LambdaExpression node)
        {
            bool hasMany = false;
            Expression myExpression = node.Body;
            while (myExpression.Acceptable())
            {
                if (myExpression.NodeType == ExpressionType.MemberAccess) myExpression = (myExpression as MemberExpression).Expression;
                else if (myExpression.NodeType == ExpressionType.Call)
                {
                    var methodExpression = myExpression as MethodCallExpression;
                    bool isGetItem = methodExpression.IsGetListItem();
                    if (isGetItem) myExpression = methodExpression.Object;
                }

                // 如果包含List<>泛型导航，则可以判定整个查询包含一对多的导航
                if (TypeUtils.IsCollectionType(myExpression.Type))
                {
                    hasMany = true;
                    break;
                }
            }

            return hasMany;
        }

        // 判定 MemberInit 绑定是否声明了一对多关系的导航
        static bool IsHasMany(List<DbExpression> include)
        {
            bool hasMany = false;
            foreach (DbExpression dbExpression in include)
            {
                Expression myExpression = dbExpression.Expressions[0];
                if (myExpression.NodeType == ExpressionType.Lambda) myExpression = (myExpression as LambdaExpression).Body;
                else if (myExpression.NodeType == ExpressionType.Call) myExpression = (myExpression as MethodCallExpression).Object;

                // Include 如果包含List<>泛型导航，则可以判定整个查询包含一对多的导航
                if (TypeUtils.IsCollectionType(myExpression.Type))
                {
                    hasMany = true;
                    break;
                }
            }

            return hasMany;
        }

        // 合并 'Where' 表达式谓词
        static Expression CombineCondition(IList<Expression> predicates)
        {
            if (predicates.Count == 0) return null;

            Expression body = ((LambdaExpression)predicates[0].ReduceUnary()).Body;
            for (int i = 1; i < predicates.Count; i++)
            {
                Expression expression = predicates[i];
                if (expression != null) body = Expression.And(body, ((LambdaExpression)expression.ReduceUnary()).Body);
            }

            LambdaExpression lambda = Expression.Lambda(body, ((LambdaExpression)predicates[0]).Parameters);
            return lambda;

        }

        // 判断表达式是否是 CROSS JOIN
        static bool IsSelectMany(IList<DbExpression> collection, DbExpression expr, int start = 0)
        {
            Expression node = expr.Expressions[0];
            if (node.NodeType == ExpressionType.Lambda)
            {
                var lambda = node as LambdaExpression;
                node = lambda.Body;
            }
            if (node.NodeType == ExpressionType.Call)
            {
                // 如果是 DefaultIfEmpty，则不是 CROSS JOIN
                var call = node as MethodCallExpression;
                if (call.Method.Name == "DefaultIfEmpty")
                {
                    // 右关联
                    if (call.Arguments != null && call.Arguments.Count == 2 && (bool)(((ConstantExpression)call.Arguments[1]).Value))
                    {
                        string name = ((MemberExpression)call.Arguments[0]).Member.Name;
                        for (int i = start; i < collection.Count; i++)
                        {
                            var curExpr = collection[i];
                            if (curExpr.DbExpressionType == DbExpressionType.GroupJoin)
                            {
                                LambdaExpression lambda = curExpr.Expressions[3] as LambdaExpression;
                                NewExpression new0 = lambda.Body as NewExpression;
                                string pName = (new0.Arguments[1] as ParameterExpression).Name;
                                if (name == pName)
                                {
                                    curExpr.DbExpressionType = DbExpressionType.GroupRightJoin;
                                    break;
                                }
                            }

                        }
                    }

                    return false;
                }
            }

            // 根据系统生成的变量名判断 
            return !expr.Expressions[0].IsAnonymous();
        }
    }
}
