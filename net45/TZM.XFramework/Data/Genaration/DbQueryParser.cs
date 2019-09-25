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
        internal static IDbQueryableInfo<TElement> Parse<TElement>(IDbQueryable<TElement> dbQuery)
        {
            return DbQueryParser.Parse(dbQuery, 0);
        }

        // 解析查询语义
        static IDbQueryableInfo<TElement> Parse<TElement>(IDbQueryable<TElement> dbQuery, int startIndex)
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
            var whereExpressions = new List<Expression>();    // WHERE
            var havingExpressions = new List<Expression>();   // HAVING
            var joins = new List<DbExpression>();             // JOIN
            var orderBys = new List<DbExpression>();          // ORDER BY
            var includes = new List<DbExpression>();          // ORDER BY
            var unions = new List<IDbQueryableInfo<TElement>>();

            Expression selectExpression = null;               // SELECT #
            DbExpression insertExpression = null;             // INSERT #
            DbExpression updateExpression = null;             // UPDATE #
            DbExpression deleteExpression = null;             // DELETE #
            DbExpression groupByExpression = null;            // GROUP BY #
            DbExpression statisExpression = null;             // SUM/MAX  #

            for (int index = startIndex; index < dbQuery.DbExpressions.Count; index++)
            {
                DbExpression curExpr = dbQuery.DbExpressions[index];

                // Take(n)
                if (take != null || (skip != null && curExpr.DbExpressionType != DbExpressionType.Take) || isDistinct || subQuery)
                {
                    outerIndex = index;
                    break;
                }

                #region 解析片断

                switch (curExpr.DbExpressionType)
                {
                    case DbExpressionType.None:
                    case DbExpressionType.All:
                        continue;

                    case DbExpressionType.Any:
                        isAny = true;
                        if (curExpr.Expressions != null) whereExpressions.Add(curExpr.Expressions[0]);
                        break;

                    case DbExpressionType.AsSubQuery:
                        subQuery = true;
                        continue;

                    case DbExpressionType.Union:
                        var uQuery = (curExpr.Expressions[0] as ConstantExpression).Value as IDbQueryable<TElement>;
                        var u = DbQueryParser.Parse(uQuery);
                        unions.Add(u);

                        // 如果下一个不是 union，就使用嵌套
                        if (index + 1 <= dbQuery.DbExpressions.Count - 1 && dbQuery.DbExpressions[index + 1].DbExpressionType != DbExpressionType.Union)
                            subQuery = true;
                        continue;

                    case DbExpressionType.Include:
                        includes.Add(curExpr);
                        continue;

                    case DbExpressionType.GroupBy:
                        groupByExpression = curExpr;
                        continue;

                    case DbExpressionType.GetTable:
                        type = (curExpr.Expressions[0] as ConstantExpression).Value as Type;
                        continue;

                    case DbExpressionType.Average:
                    case DbExpressionType.Min:
                    case DbExpressionType.Sum:
                    case DbExpressionType.Max:
                        statisExpression = curExpr;
                        continue;

                    case DbExpressionType.Count:
                        statisExpression = curExpr;
                        if (curExpr.Expressions != null) whereExpressions.Add(curExpr.Expressions[0]);
                        continue;

                    case DbExpressionType.Distinct:
                        isDistinct = true;
                        continue;

                    case DbExpressionType.First:
                    case DbExpressionType.FirstOrDefault:
                        take = 1;
                        if (curExpr.Expressions != null) whereExpressions.Add(curExpr.Expressions[0]);
                        continue;

                    case DbExpressionType.Join:
                    case DbExpressionType.GroupJoin:
                    case DbExpressionType.GroupRightJoin:
                        selectExpression = curExpr.Expressions[3];
                        joins.Add(curExpr);
                        continue;

                    case DbExpressionType.OrderBy:
                    case DbExpressionType.OrderByDescending:
                        orderBys.Add(curExpr);
                        continue;
                    case DbExpressionType.Select:
                        selectExpression = curExpr.Expressions != null ? curExpr.Expressions[0] : null;
                        continue;

                    case DbExpressionType.SelectMany:
                        selectExpression = curExpr.Expressions[1];
                        if (CheckSelectMany(dbQuery.DbExpressions, curExpr, startIndex)) joins.Add(curExpr);
                        continue;

                    case DbExpressionType.Single:
                    case DbExpressionType.SingleOrDefault:
                        take = 1;
                        if (curExpr.Expressions != null) whereExpressions.Add(curExpr.Expressions[0]);
                        continue;

                    case DbExpressionType.Skip:
                        skip = (int)(curExpr.Expressions[0] as ConstantExpression).Value;
                        continue;

                    case DbExpressionType.Take:
                        take = (int)(curExpr.Expressions[0] as ConstantExpression).Value;
                        continue;

                    case DbExpressionType.ThenBy:
                    case DbExpressionType.ThenByDescending:
                        orderBys.Add(curExpr);
                        continue;

                    case DbExpressionType.Where:
                        var predicate = groupByExpression == null ? whereExpressions : havingExpressions;
                        if (curExpr.Expressions != null) predicate.Add(curExpr.Expressions[0]);
                        continue;

                    case DbExpressionType.Insert:
                        insertExpression = curExpr;
                        continue;

                    case DbExpressionType.Update:
                        updateExpression = curExpr;
                        continue;

                    case DbExpressionType.Delete:
                        deleteExpression = curExpr;
                        continue;

                    default:
                        throw new NotSupportedException(string.Format("{0} is not support.", curExpr.DbExpressionType));
                }

                #endregion
            }

            // 没有解析到INSERT/DELETE/UPDATE/SELECT表达式，并且没有相关统计函数，则默认选择FromType的所有字段
            bool useFullFields = insertExpression == null && deleteExpression == null && updateExpression == null && selectExpression == null && statisExpression == null;
            if (useFullFields) selectExpression = Expression.Constant(type ?? typeof(TElement));

            var sQueryInfo = new DbQueryableInfo_Select<TElement>();
            sQueryInfo.FromType = type;
            sQueryInfo.HaveDistinct = isDistinct;
            sQueryInfo.HaveAny = isAny;
            sQueryInfo.Joins = joins;
            sQueryInfo.OrderBys = orderBys;
            sQueryInfo.GroupByExpression = groupByExpression;
            sQueryInfo.StatisExpression = statisExpression;
            sQueryInfo.Unions = unions;
            sQueryInfo.Includes = includes;
            sQueryInfo.Skip = skip != null ? skip.Value : 0;
            sQueryInfo.Take = take != null ? take.Value : 0;
            sQueryInfo.SelectExpression = new DbExpression(DbExpressionType.Select, selectExpression);
            sQueryInfo.WhereExpression = new DbExpression(DbExpressionType.Where, CombineWhere(whereExpressions));
            sQueryInfo.HavingExpression = new DbExpression(DbExpressionType.None, CombineWhere(havingExpressions));
            sQueryInfo.SourceQuery = dbQuery;

            #region 更新语义

            if (updateExpression != null)
            {
                var uQueryInfo = new DbQueryableInfo_Update<TElement>();
                var constantExpression = updateExpression.Expressions != null ? updateExpression.Expressions[0] as ConstantExpression : null;
                if (constantExpression != null)
                    uQueryInfo.Entity = constantExpression.Value;
                else
                    uQueryInfo.Expression = updateExpression.Expressions[0];
                uQueryInfo.SelectInfo = sQueryInfo;
                uQueryInfo.SourceQuery = dbQuery;
                return uQueryInfo;
            }

            #endregion

            #region 删除语义

            else if (deleteExpression != null)
            {
                var dQueryInfo = new DbQueryableInfo_Delete<TElement>();
                var constantExpression = deleteExpression.Expressions != null ? deleteExpression.Expressions[0] as ConstantExpression : null;
                if (constantExpression != null)
                    dQueryInfo.Entity = constantExpression.Value;
                dQueryInfo.SelectInfo = sQueryInfo;
                dQueryInfo.SourceQuery = dbQuery;
                return dQueryInfo;
            }

            #endregion

            #region 插入语义

            else if (insertExpression != null)
            {
                var nQueryInfo = new DbQueryableInfo_Insert<TElement>();
                if (insertExpression.Expressions != null)
                {
                    nQueryInfo.Entity = (insertExpression.Expressions[0] as ConstantExpression).Value;
                    if (insertExpression.Expressions.Length > 1) 
                        nQueryInfo.EntityColumns = (insertExpression.Expressions[1] as ConstantExpression).Value as IList<Expression>;
                }
                nQueryInfo.SelectInfo = sQueryInfo;
                nQueryInfo.Bulk = dbQuery.Bulk;
                nQueryInfo.SourceQuery = dbQuery;
                dbQuery.DbQueryInfo = nQueryInfo;
                return nQueryInfo;
            }

            #endregion

            #region 选择语义

            else if (selectExpression != null)
            {
                // 检查嵌套查询语义
                sQueryInfo = DbQueryParser.TryBuildOutQuery(sQueryInfo);
                // 查询来源
                sQueryInfo.SourceQuery = dbQuery;
            }

            #endregion

            #region 嵌套语义

            // 解析嵌套查询
            if (outerIndex != null)
            {
                var outQuery = DbQueryParser.Parse<TElement>(dbQuery, outerIndex.Value);
                var nQuery = outQuery as DbQueryableInfo_Insert<TElement>;
                var uQuery = outQuery as DbQueryableInfo_Update<TElement>;
                if (nQuery != null)
                {
                    if (nQuery.SelectInfo != null)
                        nQuery.SelectInfo.SubQueryInfo = sQueryInfo;
                    else
                        nQuery.SelectInfo = sQueryInfo;
                    nQuery.SourceQuery = dbQuery;
                    return nQuery;
                }
                else if (uQuery != null)
                {
                    if (uQuery.SelectInfo != null)
                        uQuery.SelectInfo.SubQueryInfo = sQueryInfo;
                    else
                        uQuery.SelectInfo = sQueryInfo;
                    uQuery.SourceQuery = dbQuery;
                    return uQuery;
                }
                else
                {
                    var rootQuery = outQuery;
                    while (rootQuery.SubQueryInfo != null) rootQuery = rootQuery.SubQueryInfo;
                    rootQuery.SubQueryInfo = sQueryInfo;
                    outQuery.SourceQuery = dbQuery;

                    // 如果外层是统计，内层没有分页，则不需要排序
                    rootQuery = outQuery;
                    while (rootQuery.SubQueryInfo != null)
                    {
                        var myOutQuery = rootQuery as IDbQueryableInfo_Select;
                        var mySubQuery = rootQuery.SubQueryInfo as IDbQueryableInfo_Select;
                        // 没有分页的嵌套统计，不需要排序
                        if (myOutQuery.StatisExpression != null && !(mySubQuery.Take > 0 || mySubQuery.Skip > 0) && mySubQuery.OrderBys.Count > 0)
                            mySubQuery.OrderBys = new List<DbExpression>(0);
                        // 继续下一轮迭代
                        rootQuery = rootQuery.SubQueryInfo;
                    }

                    return outQuery;
                }
            }

            #endregion

            // 查询表达式
            return sQueryInfo;
        }

        // 构造由一对多关系产生的嵌套查询
        static DbQueryableInfo_Select<TElement> TryBuildOutQuery<TElement>(DbQueryableInfo_Select<TElement> sQueryInfo)
        {
            // @havePaging 是否有分页信息

            if (sQueryInfo == null || sQueryInfo.SelectExpression == null) return sQueryInfo;

            Expression select = sQueryInfo.SelectExpression.Expressions[0];
            List<DbExpression> include = sQueryInfo.Includes;
            Type type = sQueryInfo.FromType;

            // 解析导航属性 如果有 1:n 的导航属性，那么查询的结果集的主记录将会有重复记录
            // 这时就需要使用嵌套语义，先查主记录，再关联导航记录
            Expression expression = select;
            var lambdaExpression = expression as LambdaExpression;
            if (lambdaExpression != null) expression = lambdaExpression.Body;
            var initExpression = expression as MemberInitExpression;
            var newExpression = expression as NewExpression;

            bool hasManyNavgation = CheckManyNavigation(include);
            if (!hasManyNavgation) hasManyNavgation = initExpression != null && CheckManyNavigation<TElement>(initExpression);

            #region 嵌套语义

            if (hasManyNavgation)
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
                    sQueryInfo.SelectExpression = new DbExpression(DbExpressionType.Select, lambdaExpression);
                }
                sQueryInfo.ResultByManyNavigation = true;
                sQueryInfo.Includes = new List<DbExpression>(0);

                var outQueryInfo = new DbQueryableInfo_Select<TElement>();
                outQueryInfo.FromType = type;
                outQueryInfo.SubQueryInfo = sQueryInfo;
                outQueryInfo.Joins = new List<DbExpression>(0);
                outQueryInfo.OrderBys = new List<DbExpression>(0);
                outQueryInfo.Includes = include;
                outQueryInfo.HaveManyNavigation = true;
                outQueryInfo.SelectExpression = new DbExpression(DbExpressionType.Select, select);

                #region 排序

                if (sQueryInfo.OrderBys.Count > 0)
                {
                    // 是否有分页
                    bool havePaging = (sQueryInfo.Take > 0 || sQueryInfo.Skip > 0);
                    if (!havePaging)
                    {
                        // 如果没有分页，则OrderBy需要放在外层
                        outQueryInfo.OrderBys = sQueryInfo.OrderBys;
                        sQueryInfo.OrderBys = new List<DbExpression>(0);
                    }
                    else
                    {
                        // 如果有分页，只有主表/用到的1:1从表放在内层，其它放在外层
                        List<DbExpression> innerOrderBy = null;
                        foreach (var dbExpression in sQueryInfo.OrderBys)
                        {
                            hasManyNavgation = CheckManyNavigation(dbExpression.Expressions[0] as LambdaExpression);
                            if (!hasManyNavgation)
                            {
                                if (innerOrderBy == null) innerOrderBy = new List<DbExpression>();
                                innerOrderBy.Add(dbExpression);
                            }
                        }

                        if (innerOrderBy != null && innerOrderBy.Count > 0)
                        {
                            outQueryInfo.OrderBys = sQueryInfo.OrderBys;
                            sQueryInfo.OrderBys = innerOrderBy;
                        }
                    }
                }

                #endregion

                #region 分组

                if (sQueryInfo.GroupByExpression != null)
                {
                    // 查看外层是否需要重新构造选择器。如果有分组并且有聚合函数，则需要重新构造选择器。否则外层解析不了聚合函数
                    // demo => line 640
                    bool newSelector = bindings.Any(x => ((MemberAssignment)x).Expression.NodeType == ExpressionType.Call) || newExpression.Arguments.Any(x => x.NodeType == ExpressionType.Call);
                    if (newSelector)
                    {
                        ParameterExpression newParameter = null;
                        List<DbExpression> dbExpressions = null;
                        if (outQueryInfo.Includes != null && outQueryInfo.Includes.Count > 0) dbExpressions = outQueryInfo.Includes;
                        else if (outQueryInfo.OrderBys != null && outQueryInfo.OrderBys.Count > 0) dbExpressions = outQueryInfo.OrderBys;
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
                        outQueryInfo.SelectExpression = new DbExpression(DbExpressionType.Select, lambdaExpression);
                    }
                }

                #endregion

                sQueryInfo = outQueryInfo;
            }

            #endregion

            return sQueryInfo;
        }

        // 判定 MemberInit 绑定是否声明了一对多关系的导航
        static bool CheckManyNavigation<T>(MemberInitExpression node)
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
                    bool hasManyNavgation = CheckManyNavigation<T>(initExpression);
                    if (hasManyNavgation) return true;
                }
            }

            return false;
        }

        // 判定 MemberInit 绑定是否声明了一对多关系的导航
        static bool CheckManyNavigation(LambdaExpression node)
        {
            bool hasManyNavgation = false;
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

                if (TypeUtils.IsCollectionType(myExpression.Type))
                {
                    hasManyNavgation = true;
                    break;
                }
            }

            return hasManyNavgation;
        }

        // 判定 MemberInit 绑定是否声明了一对多关系的导航
        static bool CheckManyNavigation(List<DbExpression> include)
        {
            bool hasManyNavgation = false;

            foreach (DbExpression dbExpression in include)
            {
                Expression myExpression = dbExpression.Expressions[0];
                if (myExpression.NodeType == ExpressionType.Lambda) myExpression = (myExpression as LambdaExpression).Body;
                else if (myExpression.NodeType == ExpressionType.Call) myExpression = (myExpression as MethodCallExpression).Object;

                // Include 如果包含List<>泛型导航，则可以判定整个查询包含一对多的导航
                if (TypeUtils.IsCollectionType(myExpression.Type))
                {
                    hasManyNavgation = true;
                    break;
                }
            }

            return hasManyNavgation;
        }

        // 合并 'Where' 表达式谓词
        static Expression CombineWhere(IList<Expression> predicates)
        {
            if (predicates.Count == 0) return null;

            Expression body = ((LambdaExpression)predicates[0].ReduceUnary()).Body;
            for (int i = 1; i < predicates.Count; i++)
            {
                Expression expression = predicates[i];
                if (expression != null) body = Expression.And(body, ((LambdaExpression)expression.ReduceUnary()).Body);
            }
            return body;

        }

        // 判断表达式是否是 CROSS JOIN
        static bool CheckSelectMany(IList<DbExpression> collection, DbExpression expr, int start = 0)
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
