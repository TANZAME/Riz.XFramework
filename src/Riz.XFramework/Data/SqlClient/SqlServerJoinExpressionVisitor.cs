
using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using Riz.XFramework.Data.SqlClient;

namespace Riz.XFramework.Data
{

    /// <summary>
    /// JOIN 表达式解析器，用于产生 WITH NOLOCK
    /// </summary>
    internal class SqlServerJoinExpressionVisitor : JoinExpressionVisitor
    {
        private bool _isNoLock = false;
        private string _withNoLock = string.Empty;
        private List<DbExpression> _joins = null;
        private AliasGenerator _aliasGenerator = null;

        /// <summary>
        /// 实例化 <see cref="SqlServerJoinExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="aliasGenerator">表别名解析器</param>
        /// <param name="joins">join 查询语义</param>
        internal SqlServerJoinExpressionVisitor(AliasGenerator aliasGenerator, List<DbExpression> joins)
            : base(aliasGenerator, joins)
        {
            _joins = joins;
            _aliasGenerator = aliasGenerator;           
        }

        /// <summary>
        /// 写入SQL片断
        /// </summary>
        public override void Write(ISqlBuilder builder)
        {
            this.Initialize(builder);
            foreach (DbExpression dbExpression in _joins)
            {
                builder.AppendNewLine();

                // [INNER/LEFT JOIN]
                if (dbExpression.DbExpressionType == DbExpressionType.GroupJoin || dbExpression.DbExpressionType == DbExpressionType.Join || dbExpression.DbExpressionType == DbExpressionType.GroupRightJoin)
                {
                    JoinType joinType = JoinType.InnerJoin;
                    if (dbExpression.DbExpressionType == DbExpressionType.GroupJoin) joinType = JoinType.LeftJoin;
                    else if (dbExpression.DbExpressionType == DbExpressionType.GroupRightJoin) joinType = JoinType.RightJoin;
                    this.AppendJoinType(builder, joinType);
                    this.AppendLfInJoin(builder, dbExpression, _aliasGenerator);
                }
                else if (dbExpression.DbExpressionType == DbExpressionType.SelectMany)
                {
                    this.AppendJoinType(builder, JoinType.CrossJoin);
                    this.AppendCrossJoin(builder, dbExpression, _aliasGenerator);
                }
            }
        }

        // 使用 SqlBuilder 初始化
        protected override void Initialize(ISqlBuilder builder)
        {
            base.Initialize(builder);

            var context = builder.TranslateContext.DbContext;
            _isNoLock = ((SqlServerDbContext)context).NoLock;
            _withNoLock = ((SqlServerDbQueryProvider)context.Provider).WidthNoLock;
        }

        // LEFT OR INNER JOIN
        private void AppendLfInJoin(ISqlBuilder builder, DbExpression dbExpression, AliasGenerator aliasGenerator)
        {
            bool withNoLock = false;
            builder.Append(' ');
            IDbQueryable dbQuery = (IDbQueryable)((dbExpression.Expressions[0] as ConstantExpression).Value);
            if (dbQuery.DbExpressions.Count == 1 && dbQuery.DbExpressions[0].DbExpressionType == DbExpressionType.GetTable)
            {
                Type type = dbExpression.Expressions[0].Type.GetGenericArguments()[0];
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);

                withNoLock = !typeRuntime.IsTemporary && _isNoLock && !string.IsNullOrEmpty(_withNoLock);
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
                ? aliasGenerator.GetTableAlias(dbExpression.Expressions[2])
                : aliasGenerator.GetTableAlias(right.Parameters[0]);
            builder.Append(' ');
            builder.Append(alias);
            builder.Append(' ');

            if (withNoLock)
            {
                builder.Append(_withNoLock);
                builder.Append(' ');
            }

            // ON a.Name = b.Name AND a.Id = b.Id
            builder.Append("ON ");

            if (left.Body.NodeType == ExpressionType.New)
            {

                var body1 = left.Body as NewExpression;
                var body2 = right.Body as NewExpression;

                for (int index = 0; index < body1.Arguments.Count; ++index)
                {
                    builder.AppendMember(aliasGenerator, body1.Arguments[index]);
                    builder.Append(" = ");
                    builder.AppendMember(aliasGenerator, body2.Arguments[index]);
                    if (index < body1.Arguments.Count - 1) builder.Append(" AND ");
                }
            }
            else if (left.Body.NodeType == ExpressionType.MemberInit)
            {
                var body1 = left.Body as MemberInitExpression;
                var body2 = right.Body as MemberInitExpression;

                for (int index = 0; index < body1.Bindings.Count; ++index)
                {
                    builder.AppendMember(aliasGenerator, (body1.Bindings[index] as MemberAssignment).Expression);
                    builder.Append(" = ");
                    builder.AppendMember(aliasGenerator, (body2.Bindings[index] as MemberAssignment).Expression);
                    if (index < body1.Bindings.Count - 1) builder.Append(" AND ");
                }
            }
            else
            {
                builder.AppendMember(aliasGenerator, left.Body.ReduceUnary());
                builder.Append(" = ");
                builder.AppendMember(aliasGenerator, right.Body.ReduceUnary());
            }
        }

        // Cross Join
        private void AppendCrossJoin(ISqlBuilder builder, DbExpression exp, AliasGenerator aliasGenerator)
        {
            var lambdaExp = exp.Expressions[1] as LambdaExpression;
            Type type = lambdaExp.Parameters[1].Type;
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
            bool withNoLock = !typeRuntime.IsTemporary && _isNoLock && !string.IsNullOrEmpty(_withNoLock);

            builder.Append(' ');
            builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);

            string alias = aliasGenerator.GetTableAlias(lambdaExp.Parameters[1]);
            builder.Append(' ');
            builder.Append(alias);
            builder.Append(' ');

            if (withNoLock)
            {
                builder.Append(_withNoLock);
                builder.Append(' ');
            }
        }
    }
}
