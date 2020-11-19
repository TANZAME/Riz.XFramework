using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 支持将查询表达式转换为查询表达式树(CQT)的类。
    /// </summary>
    internal class DbQueryableParser
    {
        /// <summary>
        /// 解析查询语义，将其转换成命令表达式
        /// </summary>
        internal static DbQueryTree Parse<TElement>(IDbQueryable<TElement> source)
        {
            return DbQueryableParser.Parse(source, typeof(TElement), 0);
        }

        // 解析查询语义
        private static DbQueryTree Parse(IDbQueryable source, Type elmentType, int startIndex)
        {
            // 目的：将query 转换成增/删/改/查
            // 1、from a in context.GetTable<T>() select a 此时query里面可能没有SELECT 表达式
            // 2、Take 视为一个查询的结束位，如有更多查询，应使用嵌套查询
            // 3、uion 分页查询也使嵌套语义
            // 4、uion 后面跟着 WHERE,GROUP BY,SELECT,JOIN语句时需要使用嵌套查询

            Type fromType = null;
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
            var unions = new List<DbQuerySelectTree>();         // UNION ALL

            Expression select = null;                           // SELECT #
            DbExpression insert = null;                         // INSERT #
            DbExpression update = null;                         // UPDATE #
            DbExpression delete = null;                         // DELETE #
            DbExpression group = null;                          // GROUP BY #
            DbExpression aggregate = null;                      // SUM&MAX  #

            //var parameters = new List<ParameterExpression>();
            for (int index = startIndex; index < source.DbExpressions.Count; index++)
            {
                DbExpression item = source.DbExpressions[index];
                //if (item.Expressions != null) item.Expressions.ForEach(expr =>
                //{
                //    if (expr != null && expr.NodeType == ExpressionType.Parameter) parameters.Add(((ParameterExpression)expr));
                //    else if (expr != null && expr.NodeType == ExpressionType.Lambda) parameters.AddRange(((LambdaExpression)expr).Parameters);
                //});

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

                    case DbExpressionType.AsSubquery:
                        subQuery = true;
                        continue;

                    case DbExpressionType.Union:
                        var constExpression = item.Expressions[0] as ConstantExpression;
                        var uQuery = constExpression.Value as IDbQueryable;
                        var u = DbQueryableParser.Parse(uQuery, constExpression.Type.GetGenericArguments()[0], 0);
                        unions.Add((DbQuerySelectTree)u);

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
                        fromType = (item.Expressions[0] as ConstantExpression).Value as Type;
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

                        var j = item;

                        // GetTable 的参数
                        var inner = (j.Expressions[0] as ConstantExpression).Value as IDbQueryable;
                        if (inner.DbExpressions.Count == 1 &&
                            inner.DbExpressions[0].DbExpressionType == DbExpressionType.GetTable &&
                            inner.DbExpressions[0].Expressions.Length == 2)
                        {
                            var expressions = new Expression[item.Expressions.Length + 1];
                            Array.Copy(item.Expressions, expressions, item.Expressions.Length);
                            expressions[expressions.Length - 1] = inner.DbExpressions[0].Expressions[1];
                            j = new DbExpression(item.DbExpressionType, expressions);
                        }
                        joins.Add(j);


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
            bool nonePick = insert == null && delete == null && update == null && select == null && aggregate == null;
            if (nonePick) select = Expression.Constant(fromType ?? elmentType);

            var result_Query = new DbQuerySelectTree();
            result_Query.FromType = fromType;
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
            result_Query.Where = new DbExpression(DbExpressionType.Where, CombineCondition(conditions));
            result_Query.Having = new DbExpression(DbExpressionType.Having, CombineCondition(havings));

            #region 更新语义

            if (update != null)
            {
                var result_Update = new DbQueryUpdateTree();
                var constantExpression = update.Expressions != null ? update.Expressions[0] as ConstantExpression : null;
                if (constantExpression != null)
                    result_Update.Entity = constantExpression.Value;
                else
                    result_Update.Expression = update.Expressions[0];
                result_Update.SelectTree = result_Query;
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
                result_Delete.SelectTree = result_Query;
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
                result_Insert.SelectTree = result_Query;
                result_Insert.Bulk = source.Bulk;
                return result_Insert;
            }

            #endregion

            #region 选择语义

            else if (select != null)
            {
                // 检查嵌套查询语义
                result_Query = DbQueryableParser.ParseOutQuery(result_Query);
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
                    if (result_Insert.SelectTree != null)
                        result_Insert.SelectTree.Subquery = result_Query;
                    else
                        result_Insert.SelectTree = result_Query;
                    return result_Insert;
                }
                else if (result_Update != null)
                {
                    if (result_Update.SelectTree != null)
                        result_Update.SelectTree.Subquery = result_Query;
                    else
                        result_Update.SelectTree = result_Query;
                    return result_Update;
                }
                else if (result_Delete != null)
                {
                    if (result_Delete.SelectTree != null)
                        result_Delete.SelectTree.Subquery = result_Query;
                    else
                        result_Delete.SelectTree = result_Query;
                    return result_Delete;
                }
                else
                {
                    // ?? what
                    var iterator = (DbQuerySelectTree)outQueryTree;
                    while (iterator.Subquery != null) iterator = iterator.Subquery;
                    iterator.Subquery = result_Query;

                    // 如果外层是统计，内层没有分页，则不需要排序
                    iterator = (DbQuerySelectTree)outQueryTree;
                    while (iterator.Subquery != null)
                    {
                        var myOutQuery = iterator as DbQuerySelectTree;
                        var mySubquery = iterator.Subquery as DbQuerySelectTree;
                        // 没有分页的嵌套统计，不需要排序
                        if (myOutQuery.Aggregate != null && !(mySubquery.Take > 0 || mySubquery.Skip > 0) && mySubquery.OrderBys.Count > 0)
                            mySubquery.OrderBys = new List<DbExpression>(0);
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
        private static DbQuerySelectTree ParseOutQuery(DbQuerySelectTree tree)
        {
            // @havePaging 是否有分页信息

            if (tree == null || tree.Select == null) return tree;

            Expression select = tree.Select.Expressions[0];
            List<DbExpression> includes = tree.Includes;
            Type fromType = tree.FromType;

            // 解析导航属性 如果有 1:n 的导航属性，那么查询的结果集的主记录将会有重复记录
            // 这时就需要使用嵌套语义，先查主记录，再关联导航记录
            Expression expression = select;
            var lambdaExpression = expression as LambdaExpression;
            if (lambdaExpression != null) expression = lambdaExpression.Body;
            var initExpression = expression as MemberInitExpression;
            var newExpression = expression as NewExpression;

            bool hasMany = DbQueryableParser.HasMany(includes);
            if (!hasMany) hasMany = initExpression != null && HasMany(initExpression);

            #region 嵌套语义

            if (hasMany)
            {

                newExpression = initExpression != null ? initExpression.NewExpression : newExpression;
                List<MemberBinding> bindings = new List<MemberBinding>();
                if (initExpression != null)
                    bindings = initExpression.Bindings.ToList(a => TypeUtils.IsPrimitiveType((a.Member as System.Reflection.PropertyInfo).PropertyType));

                if (newExpression != null || bindings.Count() > 0)
                {
                    // 简化内层选择器，只选择最小字段，不选择导航字段，导航字段在外层加进去
                    initExpression = Expression.MemberInit(newExpression, bindings);
                    lambdaExpression = Expression.Lambda(initExpression, lambdaExpression.Parameters);
                    tree.Select = new DbExpression(DbExpressionType.Select, lambdaExpression);
                }
                tree.IsParsedByMany = true;
                tree.Includes = new List<DbExpression>(0);

                var result_Query = new DbQuerySelectTree();
                result_Query.FromType = fromType;
                result_Query.Subquery = tree;
                result_Query.Joins = new List<DbExpression>(0);
                result_Query.OrderBys = new List<DbExpression>(0);
                result_Query.Includes = includes;
                result_Query.HasMany = true;
                result_Query.Select = new DbExpression(DbExpressionType.Select, select);

                #region 排序

                if (tree.OrderBys.Count > 0)
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
                        List<DbExpression> innerOrderBy = null;
                        foreach (var dbExpression in tree.OrderBys)
                        {
                            hasMany = HasMany(dbExpression.Expressions[0] as LambdaExpression);
                            if (!hasMany)
                            {
                                if (innerOrderBy == null) innerOrderBy = new List<DbExpression>();
                                innerOrderBy.Add(dbExpression);
                            }
                        }

                        if (innerOrderBy != null && innerOrderBy.Count > 0)
                        {
                            result_Query.OrderBys = tree.OrderBys;
                            tree.OrderBys = innerOrderBy;
                        }
                    }
                }

                #endregion

                #region 分组

                if (tree.GroupBy != null)
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

                tree = result_Query;
            }

            #endregion

            return tree;
        }

        // 判定 MemberInit 绑定是否声明了一对多关系的导航
        private static bool HasMany(MemberInitExpression node)
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
                    bool hasManyNavgation = HasMany(initExpression);
                    if (hasManyNavgation) return true;
                }
            }

            return false;
        }

        // 判定 MemberInit 绑定是否声明了一对多关系的导航
        private static bool HasMany(LambdaExpression node)
        {
            bool result = false;
            Expression expression = node.Body;
            while (expression.Visitable())
            {
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

        // 判定 MemberInit 绑定是否声明了一对多关系的导航
        private static bool HasMany(List<DbExpression> includes)
        {
            bool result = false;
            foreach (DbExpression dbExpression in includes)
            {
                Expression expression = dbExpression.Expressions[0];
                if (expression.NodeType == ExpressionType.Lambda) expression = (expression as LambdaExpression).Body;
                else if (expression.NodeType == ExpressionType.Call) expression = (expression as MethodCallExpression).Object;

                // Include 如果包含List<>泛型导航，则可以判定整个查询包含一对多的导航
                if (TypeUtils.IsCollectionType(expression.Type))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        // 合并 'Where' 表达式谓词
        private static Expression CombineCondition(IList<Expression> predicates)
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
        private static bool IsSelectMany(IList<DbExpression> collection, DbExpression dbExpression, int start = 0)
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
                            if (item.DbExpressionType == DbExpressionType.GroupJoin)
                            {
                                LambdaExpression lambda = item.Expressions[3] as LambdaExpression;
                                NewExpression newExpression = lambda.Body as NewExpression;
                                string pName = (newExpression.Arguments[1] as ParameterExpression).Name;
                                if (name == pName)
                                {
                                    item.DbExpressionType = DbExpressionType.GroupRightJoin;
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
    }
}
