using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 关联表达式解析器
    /// </summary>
    public class JoinExpressionVisitor : LinqExpressionVisitor
    {
        private List<DbExpression> _joins = null;
        private TableAliasResolver _aliasResolver = null;

        /// <summary>
        /// 初始化 <see cref="JoinExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="aliasResolver">表别名解析器</param>
        /// <param name="joins">JOIN 子句</param>
        public JoinExpressionVisitor(TableAliasResolver aliasResolver, List<DbExpression> joins)
            : base(aliasResolver, null)
        {
            _joins = joins;
            _aliasResolver = aliasResolver;
        }

        /// <summary>
        /// 写入SQL片断
        /// </summary>
        /// <param name="builder">SQL 语句生成器</param>
        public override void Write(ISqlBuilder builder)
        {
            this.Initialize(builder);
            foreach (DbExpression expression in _joins)
            {
                builder.AppendNewLine();

                // [INNER/LEFT JOIN]
                if (expression.DbExpressionType == DbExpressionType.GroupJoin || expression.DbExpressionType == DbExpressionType.Join || 
                    expression.DbExpressionType == DbExpressionType.GroupRightJoin)
                {
                    JoinType joinType = JoinType.InnerJoin;
                    if (expression.DbExpressionType == DbExpressionType.GroupJoin) joinType = JoinType.LeftJoin;
                    else if (expression.DbExpressionType == DbExpressionType.GroupRightJoin) joinType = JoinType.RightJoin;
                    this.AppendJoinType(builder, joinType);
                    this.AppendLfInJoin(builder, expression, _aliasResolver);
                }
                else if (expression.DbExpressionType == DbExpressionType.SelectMany)
                {
                    this.AppendJoinType(builder, JoinType.CrossJoin);
                    this.AppendCrossJoin(builder, expression, _aliasResolver);
                }
            }
        }

        // LEFT OR INNER JOIN
        private void AppendLfInJoin(ISqlBuilder builder, DbExpression dbExpression, TableAliasResolver aliasResolver)
        {
            builder.Append(' ');
            IDbQueryable dbQuery = (IDbQueryable)((dbExpression.Expressions[0] as ConstantExpression).Value);
            if (dbQuery.DbExpressions.Count == 1 && dbQuery.DbExpressions[0].DbExpressionType == DbExpressionType.GetTable)
            {
                Type type = dbExpression.Expressions[0].Type.GetGenericArguments()[0];
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
            }
            else
            {
                // 嵌套
                var cmd = dbQuery.Translate(builder.Indent + 1, false, builder.TranslateContext);
                builder.Append("(");
                builder.Append(cmd.CommandText);
                builder.AppendNewLine();
                builder.Append(')');
            }


            var left = dbExpression.Expressions[1] as LambdaExpression;
            var right = dbExpression.Expressions[2] as LambdaExpression;

            // t0(t1)
            string alias = !(left.Body.NodeType == ExpressionType.New || left.Body.NodeType == ExpressionType.MemberInit)
                ? aliasResolver.GetTableAlias(dbExpression.Expressions[2])
                : aliasResolver.GetTableAlias(right.Parameters[0]);
            builder.Append(' ');
            builder.Append(alias);
            builder.Append(' ');

            // ON a.Name = b.Name AND a.Id = b.Id
            builder.Append("ON ");

            if (left.Body.NodeType == ExpressionType.New)
            {

                var body1 = left.Body as NewExpression;
                var body2 = right.Body as NewExpression;

                for (int index = 0; index < body1.Arguments.Count; ++index)
                {
                    builder.AppendMember(aliasResolver, body1.Arguments[index]);
                    builder.Append(" = ");
                    builder.AppendMember(aliasResolver, body2.Arguments[index]);
                    if (index < body1.Arguments.Count - 1) builder.Append(" AND ");
                }
            }
            else if (left.Body.NodeType == ExpressionType.MemberInit)
            {
                var body1 = left.Body as MemberInitExpression;
                var body2 = right.Body as MemberInitExpression;

                for (int index = 0; index < body1.Bindings.Count; ++index)
                {
                    builder.AppendMember(aliasResolver, (body1.Bindings[index] as MemberAssignment).Expression);
                    builder.Append(" = ");
                    builder.AppendMember(aliasResolver, (body2.Bindings[index] as MemberAssignment).Expression);
                    if (index < body1.Bindings.Count - 1) builder.Append(" AND ");
                }
            }
            else
            {
                builder.AppendMember(aliasResolver, left.Body.ReduceUnary());
                builder.Append(" = ");
                builder.AppendMember(aliasResolver, right.Body.ReduceUnary());
            }
        }

        // Cross Join
        private void AppendCrossJoin(ISqlBuilder builder, DbExpression exp, TableAliasResolver aliasResolver)
        {
            var lambdaExp = exp.Expressions[1] as LambdaExpression;
            Type type = lambdaExp.Parameters[1].Type;
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
            builder.Append(' ');
            builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);

            string alias = aliasResolver.GetTableAlias(lambdaExp.Parameters[1]);
            builder.Append(' ');
            builder.Append(alias);
            builder.Append(' ');
        }

        /// <summary>
        /// 写入关联类型
        /// </summary>
        /// <param name="builder">SQL 语句生成器</param>
        /// <param name="joinType">关联类型</param>
        protected void AppendJoinType(ISqlBuilder builder, JoinType joinType)
        {
            switch (joinType)
            {
                case JoinType.InnerJoin:
                    builder.Append("INNER JOIN");
                    break;
                case JoinType.LeftJoin:
                    builder.Append("LEFT JOIN");
                    break;
                case JoinType.RightJoin:
                    builder.Append("RIGHT JOIN");
                    break;
                case JoinType.CrossJoin:
                    builder.Append("CROSS JOIN");
                    break;
            }
        }

        /// <summary>
        /// 关联类型
        /// </summary>
        protected enum JoinType
        {
            /// <summary>
            /// 内关联
            /// </summary>
            InnerJoin,

            /// <summary>
            /// 左关联
            /// </summary>
            LeftJoin,

            /// <summary>
            /// 右关联
            /// </summary>
            RightJoin,

            /// <summary>
            /// 全关联
            /// </summary>
            CrossJoin,
        }
    }
}
