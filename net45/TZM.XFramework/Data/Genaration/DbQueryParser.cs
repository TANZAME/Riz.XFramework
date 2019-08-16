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
            // 3、Uion 分页查询也使嵌套语义

            Type type = null;
            bool isDistinct = false;
            bool isAny = false;
            bool subQuery = false;
            int? skip = null;
            int? take = null;
            int? outerIndex = null;
            var where = new List<Expression>();     // WHERE
            var having = new List<Expression>();    // HAVING
            var join = new List<DbExpression>();    // JOIN
            var orderBy = new List<DbExpression>(); // ORDER BY
            var include = new List<DbExpression>(); // ORDER BY
            var union = new List<IDbQueryableInfo<TElement>>();

            Expression select = null;               // SELECT #
            DbExpression insert = null;             // INSERT #
            DbExpression update = null;             // UPDATE #
            DbExpression delete = null;             // DELETE #
            DbExpression groupBy = null;            // GROUP BY #
            DbExpression statis = null;             // SUM/MAX  #

            for (int index = startIndex; index < dbQuery.DbExpressions.Count; ++index)
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
                        if (curExpr.Expressions != null) where.Add(curExpr.Expressions[0]);
                        break;

                    case DbExpressionType.AsSubQuery:
                        subQuery = true;
                        break;

                    case DbExpressionType.Union:
                        var uQuery = (curExpr.Expressions[0] as ConstantExpression).Value as IDbQueryable<TElement>;
                        var u = DbQueryParser.Parse(uQuery);
                        union.Add(u);
                        continue;
                    case DbExpressionType.Include:
                        include.Add(curExpr);
                        continue;

                    case DbExpressionType.GroupBy:
                        groupBy = curExpr;
                        continue;

                    case DbExpressionType.GetTable:
                        type = (curExpr.Expressions[0] as ConstantExpression).Value as Type;
                        continue;

                    case DbExpressionType.Average:
                    case DbExpressionType.Min:
                    case DbExpressionType.Sum:
                    case DbExpressionType.Max:
                        statis = curExpr;
                        continue;

                    case DbExpressionType.Count:
                        statis = curExpr;
                        if (curExpr.Expressions != null) where.Add(curExpr.Expressions[0]);
                        continue;

                    case DbExpressionType.Distinct:
                        isDistinct = true;
                        continue;

                    case DbExpressionType.First:
                    case DbExpressionType.FirstOrDefault:
                        take = 1;
                        if (curExpr.Expressions != null) where.Add(curExpr.Expressions[0]);
                        continue;

                    case DbExpressionType.Join:
                    case DbExpressionType.GroupJoin:
                    case DbExpressionType.GroupRightJoin:
                        select = curExpr.Expressions[3];
                        join.Add(curExpr);
                        continue;

                    case DbExpressionType.OrderBy:
                    case DbExpressionType.OrderByDescending:
                        orderBy.Add(curExpr);
                        continue;
                    case DbExpressionType.Select:
                        select = curExpr.Expressions != null ? curExpr.Expressions[0] : null;
                        continue;

                    case DbExpressionType.SelectMany:
                        select = curExpr.Expressions[1];
                        if (CheckSelectMany(dbQuery.DbExpressions, curExpr, startIndex)) join.Add(curExpr);

                        continue;

                    case DbExpressionType.Single:
                    case DbExpressionType.SingleOrDefault:
                        take = 1;
                        if (curExpr.Expressions != null) where.Add(curExpr.Expressions[0]);
                        continue;

                    case DbExpressionType.Skip:
                        skip = (int)(curExpr.Expressions[0] as ConstantExpression).Value;
                        continue;

                    case DbExpressionType.Take:
                        take = (int)(curExpr.Expressions[0] as ConstantExpression).Value;
                        continue;

                    case DbExpressionType.ThenBy:
                    case DbExpressionType.ThenByDescending:
                        orderBy.Add(curExpr);
                        continue;

                    case DbExpressionType.Where:
                        var predicate = groupBy == null ? where : having;
                        if (curExpr.Expressions != null) predicate.Add(curExpr.Expressions[0]);
                        continue;

                    case DbExpressionType.Insert:
                        insert = curExpr;
                        continue;

                    case DbExpressionType.Update:
                        update = curExpr;
                        continue;

                    case DbExpressionType.Delete:
                        delete = curExpr;
                        continue;

                    default:
                        throw new NotSupportedException(string.Format("{0} is not support.", curExpr.DbExpressionType));
                }

                #endregion
            }

            // 没有解析到INSERT/DELETE/UPDATE/SELECT表达式，并且没有相关统计函数，则默认选择FromType的所有字段
            bool useFullFields = insert == null && delete == null && update == null && select == null && statis == null;
            if (useFullFields) select = Expression.Constant(type ?? typeof(TElement));

            var sQuery = new DbQueryableInfo_Select<TElement>();
            sQuery.FromType = type;
            sQuery.HaveDistinct = isDistinct;
            sQuery.HaveAny = isAny;
            sQuery.Join = join;
            sQuery.OrderBy = orderBy;
            sQuery.GroupBy = groupBy;
            sQuery.Statis = statis;
            sQuery.Union = union;
            sQuery.Include = include;
            sQuery.Skip = skip != null ? skip.Value : 0;
            sQuery.Take = take != null ? take.Value : 0;
            sQuery.Select = new DbExpression(DbExpressionType.Select, select);
            sQuery.Where = new DbExpression(DbExpressionType.Where, DbQueryParser.CombinePredicate(where));
            sQuery.Having = new DbExpression(DbExpressionType.None, DbQueryParser.CombinePredicate(having));
            sQuery.SourceQuery = dbQuery;

            #region 更新语义

            if (update != null)
            {
                var uQuery = new DbQueryableInfo_Update<TElement>();
                ConstantExpression constantExpression = update.Expressions != null ? update.Expressions[0] as ConstantExpression : null;
                if (constantExpression != null)
                    uQuery.Entity = constantExpression.Value;
                else
                    uQuery.Expression = update.Expressions[0];
                uQuery.SelectInfo = sQuery;
                uQuery.SourceQuery = dbQuery;
                return uQuery;
            }

            #endregion

            #region 删除语义

            else if (delete != null)
            {
                var dQuery = new DbQueryableInfo_Delete<TElement>();
                ConstantExpression constantExpression = delete.Expressions != null ? delete.Expressions[0] as ConstantExpression : null;
                if (constantExpression != null)
                    dQuery.Entity = constantExpression.Value;
                dQuery.SelectInfo = sQuery;
                dQuery.SourceQuery = dbQuery;
                return dQuery;
            }

            #endregion

            #region 插入语义

            else if (insert != null)
            {
                var nQuery = new DbQueryableInfo_Insert<TElement>();
                if (insert.Expressions != null)
                {
                    nQuery.Entity = (insert.Expressions[0] as ConstantExpression).Value;
                    if (insert.Expressions.Length > 1) nQuery.EntityColumns = (insert.Expressions[1] as ConstantExpression).Value as IList<Expression>;
                }
                nQuery.SelectInfo = sQuery;
                nQuery.Bulk = dbQuery.Bulk;
                nQuery.SourceQuery = dbQuery;
                dbQuery.DbQueryInfo = nQuery;
                return nQuery;
            }

            #endregion

            #region 选择语义

            else if (select != null)
            {
                // 如果有uion但是没分页，应去掉orderby子句
                if (sQuery.Union.Count > 0 && !(sQuery.Take > 0 || sQuery.Skip > 0)) sQuery.OrderBy = new List<DbExpression>();
                // 检查嵌套查询语义
                sQuery = DbQueryParser.TryBuildOutQuery(sQuery);
                sQuery.SourceQuery = dbQuery;
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
                        nQuery.SelectInfo.SubQueryInfo = sQuery;
                    else
                        nQuery.SelectInfo = sQuery;
                    nQuery.SourceQuery = dbQuery;
                    return nQuery;
                }
                else if (uQuery != null)
                {
                    if (uQuery.SelectInfo != null)
                        uQuery.SelectInfo.SubQueryInfo = sQuery;
                    else
                        uQuery.SelectInfo = sQuery;
                    uQuery.SourceQuery = dbQuery;
                    return uQuery;
                }
                else
                {
                    var rootQuery = outQuery;
                    while (rootQuery.SubQueryInfo != null) rootQuery = rootQuery.SubQueryInfo;
                    rootQuery.SubQueryInfo = sQuery;
                    outQuery.SourceQuery = dbQuery;
                    //var selectOutQuery = outQuery as DbQueryableInfo_Select<TElement>;
                    //if (selectOutQuery != null && selectOutQuery.Statis != null && selectOutQuery.OrderBy.Count > 0) selectOutQuery.OrderBy.Clear();

                    return outQuery;
                }
            }

            #endregion

            // 查询表达式
            return sQuery;
        }

        // 构造由一对多关系产生的嵌套查询
        static DbQueryableInfo_Select<TElement> TryBuildOutQuery<TElement>(DbQueryableInfo_Select<TElement> sQuery)
        {
            // @havePaging 是否有分页信息

            if (sQuery == null || sQuery.Select == null) return sQuery;

            Expression select = sQuery.Select.Expressions[0];
            List<DbExpression> include = sQuery.Include;
            Type type = sQuery.FromType;

            // 解析导航属性 如果有 一对多 的导航属性，那么查询的结果集的主记录将会有重复记录，这时就需要使用嵌套语义，先查主记录，再关联导航记录
            bool hasManyNavgation = false;
            Expression expression = select;
            var lambdaExpression = expression as LambdaExpression;
            if (lambdaExpression != null) expression = lambdaExpression.Body;
            var initExpression = expression as MemberInitExpression;
            var newExpression = expression as NewExpression;

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
                    sQuery.Select = new DbExpression(DbExpressionType.Select, lambdaExpression);
                }
                sQuery.ResultByManyNavigation = true;
                sQuery.Include = new List<DbExpression>();

                var outQuery = new DbQueryableInfo_Select<TElement>();
                outQuery.FromType = type;
                outQuery.SubQueryInfo = sQuery;
                outQuery.Join = new List<DbExpression>();
                outQuery.OrderBy = new List<DbExpression>();
                outQuery.Include = include;
                outQuery.HasManyNavigation = true;
                outQuery.Select = new DbExpression(DbExpressionType.Select, select);

                #region 排序

                if ((sQuery.Take > 0 || sQuery.Skip > 0) && sQuery.Statis == null && sQuery.OrderBy != null && sQuery.OrderBy.Count > 0)
                {
                    // 在有分页查询的前提下， order by 只保留主表的排序，从表的放在外层
                    List<DbExpression> innerOrderBy = null;
                    foreach (var dbExpression in sQuery.OrderBy)
                    {
                        hasManyNavgation = false;
                        Expression myExpression = (dbExpression.Expressions[0] as LambdaExpression).Body;
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
                                continue;
                            }
                        }

                        if (!hasManyNavgation)
                        {
                            if (innerOrderBy == null) innerOrderBy = new List<DbExpression>();
                            innerOrderBy.Add(dbExpression);
                        }
                    }

                    if (innerOrderBy != null && innerOrderBy.Count > 0)
                    {
                        outQuery.OrderBy = sQuery.OrderBy;
                        sQuery.OrderBy = innerOrderBy;
                    }
                }

                #endregion

                #region 分组

                if (sQuery.GroupBy != null)
                {
                    // 查看外层是否需要重新构造选择器。如果有分组并且有聚合函数，则需要重新构造选择器。否则外层解析不了聚合函数
                    // demo => line 640
                    bool newSelector = bindings.Any(x => ((MemberAssignment)x).Expression.NodeType == ExpressionType.Call) || newExpression.Arguments.Any(x => x.NodeType == ExpressionType.Call);
                    if (newSelector)
                    {
                        ParameterExpression newParameter = null;
                        List<DbExpression> dbExpressions = null;
                        if (outQuery.Include != null && outQuery.Include.Count > 0) dbExpressions = outQuery.Include;
                        else if (outQuery.OrderBy != null && outQuery.OrderBy.Count > 0) dbExpressions = outQuery.OrderBy;
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
                        outQuery.Select = new DbExpression(DbExpressionType.Select, lambdaExpression);
                    }
                }

                #endregion

                sQuery = outQuery;
            }

            #endregion

            #region 并集语义

            else if (sQuery.Union.Count > 0 && (sQuery.Take > 0 || sQuery.Skip > 0))
            {

                var outQuery = new DbQueryableInfo_Select<TElement>();
                outQuery.FromType = type;
                outQuery.Select = new DbExpression(DbExpressionType.Select, select);
                outQuery.SubQueryInfo = sQuery;
                outQuery.Skip = sQuery.Skip;
                outQuery.Take = sQuery.Take;
                outQuery.Join = new List<DbExpression>();
                outQuery.OrderBy = new List<DbExpression>();
                outQuery.OrderBy.AddRange(sQuery.OrderBy);

                sQuery.OrderBy = new List<DbExpression>();
                sQuery.Skip = 0;
                sQuery.Take = 0;

                sQuery = outQuery;
            }

            #endregion

            return sQuery;
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

        // 合并 'Where' 表达式谓词
        static Expression CombinePredicate(IList<Expression> predicates)
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
