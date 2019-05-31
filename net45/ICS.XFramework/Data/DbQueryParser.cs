using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ICS.XFramework.Data
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
            bool isSubQuery = false;
            int? skip = null;
            int? take = null;
            int? outerIndex = null;
            List<Expression> where = new List<Expression>();                  // WHERE
            List<Expression> having = new List<Expression>();                 // HAVING
            List<DbExpression> join = new List<DbExpression>();               // JOIN
            List<DbExpression> orderBy = new List<DbExpression>();            // ORDER BY
            List<DbExpression> include = new List<DbExpression>();            // ORDER BY
            List<IDbQueryableInfo<TElement>> union = new List<IDbQueryableInfo<TElement>>();

            Expression select = null;       // SELECT #
            DbExpression insert = null;     // INSERT #
            DbExpression update = null;     // UPDATE #
            DbExpression delete = null;     // DELETE #
            DbExpression groupBy = null;    // GROUP BY #
            DbExpression statis = null;     // SUM/MAX  #

            for (int index = startIndex; index < dbQuery.DbExpressions.Count; ++index)
            {
                DbExpression curExp = dbQuery.DbExpressions[index];

                // Take(n)
                if (take != null || (skip != null && curExp.DbExpressionType != DbExpressionType.Take) || isDistinct || isSubQuery)
                {
                    outerIndex = index;
                    break;
                }

                #region 分析语义

                switch (curExp.DbExpressionType)
                {
                    case DbExpressionType.None:
                    case DbExpressionType.All:
                        continue;

                    case DbExpressionType.Any:
                        isAny = true;
                        if (curExp.Expressions != null) where.Add(curExp.Expressions[0]);
                        break;

                    case DbExpressionType.AsSubQuery:
                        isSubQuery = true;
                        //if (curExp.Expressions != null) where.Add(curExp.Expressions[0]);
                        break;

                    case DbExpressionType.Union:
                        var uQuery = (curExp.Expressions[0] as ConstantExpression).Value as IDbQueryable<TElement>;
                        var u = DbQueryParser.Parse(uQuery);
                        union.Add(u);
                        continue;
                    case DbExpressionType.Include:
                        include.Add(curExp);
                        continue;

                    case DbExpressionType.GroupBy:
                        groupBy = curExp;
                        continue;

                    case DbExpressionType.GetTable:
                        type = (curExp.Expressions[0] as ConstantExpression).Value as Type;
                        continue;

                    case DbExpressionType.Average:
                    case DbExpressionType.Min:
                    case DbExpressionType.Sum:
                    case DbExpressionType.Max:
                        statis = curExp;
                        continue;

                    case DbExpressionType.Count:
                        statis = curExp;
                        if (curExp.Expressions != null) where.Add(curExp.Expressions[0]);
                        continue;

                    case DbExpressionType.Distinct:
                        isDistinct = true;
                        continue;

                    case DbExpressionType.First:
                    case DbExpressionType.FirstOrDefault:
                        take = 1;
                        if (curExp.Expressions != null) where.Add(curExp.Expressions[0]);
                        continue;

                    case DbExpressionType.Join:
                    case DbExpressionType.GroupJoin:
                    case DbExpressionType.GroupRightJoin:
                        select = curExp.Expressions[3];
                        join.Add(curExp);
                        continue;

                    case DbExpressionType.OrderBy:
                    case DbExpressionType.OrderByDescending:
                        orderBy.Add(curExp);
                        continue;
                    case DbExpressionType.Select:
                        select = curExp.Expressions != null ? curExp.Expressions[0] : null;
                        continue;

                    case DbExpressionType.SelectMany:
                        select = curExp.Expressions[1];
                        if (CheckSelectMany(dbQuery.DbExpressions, curExp, startIndex)) join.Add(curExp);

                        continue;

                    case DbExpressionType.Single:
                    case DbExpressionType.SingleOrDefault:
                        take = 1;
                        if (curExp.Expressions != null) where.Add(curExp.Expressions[0]);
                        continue;

                    case DbExpressionType.Skip:
                        skip = (int)(curExp.Expressions[0] as ConstantExpression).Value;
                        continue;

                    case DbExpressionType.Take:
                        take = (int)(curExp.Expressions[0] as ConstantExpression).Value;
                        continue;

                    case DbExpressionType.ThenBy:
                    case DbExpressionType.ThenByDescending:
                        orderBy.Add(curExp);
                        continue;

                    case DbExpressionType.Where:
                        var predicate = groupBy == null ? where : having;
                        if (curExp.Expressions != null) predicate.Add(curExp.Expressions[0]);
                        continue;

                    case DbExpressionType.Insert:
                        insert = curExp;
                        continue;

                    case DbExpressionType.Update:
                        update = curExp;
                        continue;

                    case DbExpressionType.Delete:
                        delete = curExp;
                        continue;

                    default:
                        throw new NotSupportedException(string.Format("{0} is not support.", curExp.DbExpressionType));
                }

                #endregion
            }

            // 没有解析到INSERT/DELETE/UPDATE/SELECT表达式，并且没有相关统计函数，则默认选择FromType的所有字段
            bool useFullColumns = insert == null && delete == null && update == null && select == null && statis == null;
            if (useFullColumns) select = Expression.Constant(type ?? typeof(TElement));

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
            sQuery.Where = new DbExpression(DbExpressionType.Where, DbQueryParser.CombineWhere(where));
            sQuery.Having = new DbExpression(DbExpressionType.None, DbQueryParser.CombineWhere(having));
            sQuery.SourceQuery = dbQuery;

            // 更新
            if (update != null)
            {
                var uQuery = new DbQueryableInfo_Update<TElement>();
                ConstantExpression expression2 = update.Expressions != null ? update.Expressions[0] as ConstantExpression : null;
                if (expression2 != null)
                    uQuery.Entity = expression2.Value;
                else
                    uQuery.Expression = update.Expressions[0];
                uQuery.SelectInfo = sQuery;
                uQuery.SourceQuery = dbQuery;
                return uQuery;
            }

            // 删除
            if (delete != null)
            {
                var dQuery = new DbQueryableInfo_Delete<TElement>();
                ConstantExpression expression2 = delete.Expressions != null ? delete.Expressions[0] as ConstantExpression : null;
                if (expression2 != null)
                    dQuery.Entity = expression2.Value;
                dQuery.SelectInfo = sQuery;
                dQuery.SourceQuery = dbQuery;
                return dQuery;
            }

            // 新增
            if (insert != null)
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

            // 如果有一对多的导航关系，则产生嵌套语义的查询
            if (select != null)
            {
                // 如果有uion但是没分页，应去掉orderby子句
                if (sQuery.Union.Count > 0 && !(sQuery.Take > 0 || sQuery.Skip > 0)) sQuery.OrderBy = new List<DbExpression>();
                // 检查嵌套查询语义
                sQuery = DbQueryParser.TryBuilOuter(sQuery);
                sQuery.SourceQuery = dbQuery;
            }

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
                    outQuery.SubQueryInfo = sQuery;
                    outQuery.SourceQuery = dbQuery;
                    return outQuery;
                }
            }

            // 查询表达式
            return sQuery;
        }

        // 构造由一对多关系产生的嵌套查询
        static DbQueryableInfo_Select<TElement> TryBuilOuter<TElement>(DbQueryableInfo_Select<TElement> sQuery)
        {
            if (sQuery == null || sQuery.Select == null) return sQuery;

            Expression select = sQuery.Select.Expressions[0];
            List<DbExpression> include = sQuery.Include;
            Type type = sQuery.FromType;

            // 解析导航属性 如果有 一对多 的导航属性，那么查询的结果集的主记录将会有重复记录，这时就需要使用嵌套语义，先查主记录，再关联导航记录
            bool checkListNavgation = false;
            Expression expression = select;
            LambdaExpression lambdaExpression = expression as LambdaExpression;
            if (lambdaExpression != null) expression = lambdaExpression.Body;
            MemberInitExpression initExpression = expression as MemberInitExpression;
            NewExpression newExpression = expression as NewExpression;

            foreach (DbExpression d in include)
            {
                Expression exp = d.Expressions[0];
                if (exp.NodeType == ExpressionType.Lambda) exp = (exp as LambdaExpression).Body;
                else if (exp.NodeType == ExpressionType.Call) exp = (exp as MethodCallExpression).Object;

                // Include 如果包含List<>泛型导航，则可以判定整个查询包含一对多的导航
                //if (exp.Type.IsGenericType && exp.Type.GetGenericTypeDefinition() == typeof(List<>)) checkListNavgation = true;
                if (TypeUtils.IsCollectionType(exp.Type)) checkListNavgation = true;
                if (checkListNavgation) break;
            }
            if (!checkListNavgation) checkListNavgation = initExpression != null && CheckListNavigation<TElement>(initExpression);

            if (checkListNavgation)
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
                sQuery.GenByListNavigation = true;
                sQuery.Include = new List<DbExpression>();

                var outQuery = new DbQueryableInfo_Select<TElement>();
                outQuery.FromType = type;
                outQuery.SubQueryInfo = sQuery;
                outQuery.Join = new List<DbExpression>();
                outQuery.OrderBy = new List<DbExpression>();
                outQuery.Include = include;
                outQuery.HaveListNavigation = true;
                outQuery.Select = new DbExpression(DbExpressionType.Select, select);
                if (sQuery.GroupBy != null)
                {
                    // 查看外层是否需要重新构造选择器。如果有分组并且有聚合函数，则需要重新构造选择器。否则外层解析不了聚合函数
                    // demo=> line 640
                    bool newSelector = bindings.Any(x => ((MemberAssignment)x).Expression.NodeType == ExpressionType.Call) ||
                         newExpression.Arguments.Any(x => x.NodeType == ExpressionType.Call);
                    if (newSelector)
                    {
                        // 1对多导航嵌套查询外层的的第一个表别名固定t0，参数名可随意
                        ParameterExpression p = Expression.Parameter(newExpression.Type, "__g");
                        bindings = bindings.ToList(x => (MemberBinding)Expression.Bind(x.Member, Expression.MakeMemberAccess(p, x.Member)));
                        List<Expression> arguments = null;
                        if (newExpression.Members != null)
                        {
                            arguments = new List<Expression>(newExpression.Arguments.Count);
                            for (int i = 0; i < newExpression.Arguments.Count; i++)
                            {
                                var member = newExpression.Members[i];
                                var arg = Expression.MakeMemberAccess(p, member);
                                arguments.Add(arg);
                            }
                        }

                        newExpression = Expression.New(newExpression.Constructor, arguments, newExpression.Members);
                        initExpression = Expression.MemberInit(newExpression, bindings);
                        lambdaExpression = Expression.Lambda(initExpression, lambdaExpression.Parameters);
                        outQuery.Select = new DbExpression(DbExpressionType.Select, lambdaExpression);
                    }
                }

                sQuery = outQuery;
            }
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

            return sQuery;
        }

        // 判定 MemberInit 绑定是否声明了一对多关系的导航
        static bool CheckListNavigation<T>(MemberInitExpression node)
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
                    bool checkListNavgation = CheckListNavigation<T>(initExpression);
                    if (checkListNavgation) return true;
                }
            }

            return false;
        }

        // 合并 'Where' 表达式语义
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
                LambdaExpression lambda = node as LambdaExpression;
                node = lambda.Body;
            }
            if (node.NodeType == ExpressionType.Call)
            {
                // 如果是 DefaultIfEmpty，则不是 CROSS JOIN
                MethodCallExpression call = node as MethodCallExpression;
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
