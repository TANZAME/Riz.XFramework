
using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{

    /// <summary>
    /// JOIN 表达式解析器，用于产生 WITH NOLOCK
    /// </summary>
    internal class SqlServerJoinExpressionVisitor : JoinExpressionVisitor
    {
        private List<DbExpression> _joins = null;
        private TableAlias _aliases = null;
        private SqlClient.SqlServerDbContext _context = null;
        private SqlClient.SqlServerDbQueryProvider _provider = null;

        /// <summary>
        /// 初始化 <see cref="SqlServerJoinExpressionVisitor"/> 类的新实例
        /// </summary>
        internal SqlServerJoinExpressionVisitor(SqlClient.SqlServerDbContext context, TableAlias aliases, List<DbExpression> joins)
            : base(context.Provider, aliases, joins)
        {
            _joins = joins;
            _aliases = aliases;
            _context = context;
            _provider = context.Provider as SqlClient.SqlServerDbQueryProvider;
        }

        /// <summary>
        /// 写入SQL片断
        /// </summary>
        public override void Write(ISqlBuilder builder)
        {
            base._builder = builder;
            if (base._methodVisitor == null) base._methodVisitor = _provider.CreateMethodVisitor(this);

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
                    this.AppendLfInJoin(builder, dbExpression, _aliases);
                }
                else if (dbExpression.DbExpressionType == DbExpressionType.SelectMany)
                {
                    this.AppendJoinType(builder, JoinType.CrossJoin);
                    this.AppendCrossJoin(builder, dbExpression, _aliases);
                }
            }
        }

        // LEFT OR INNER JOIN
        private void AppendLfInJoin(ISqlBuilder builder, DbExpression dbExpression, TableAlias aliases)
        {
            bool withNoLock = false;
            builder.Append(' ');
            IDbQueryable dbQuery = (IDbQueryable)((dbExpression.Expressions[0] as ConstantExpression).Value);
            if (dbQuery.DbExpressions.Count == 1 && dbQuery.DbExpressions[0].DbExpressionType == DbExpressionType.GetTable)
            {
                Type type = dbExpression.Expressions[0].Type.GetGenericArguments()[0];
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);

                withNoLock = !typeRuntime.IsTemporary && _context.NoLock && !string.IsNullOrEmpty(_provider.WidthNoLock);
            }
            else
            {
                // 嵌套
                var cmd = dbQuery.Resolve(builder.Indent + 1, false, builder.Token);
                builder.Append("(");
                builder.Append(cmd.CommandText);
                builder.AppendNewLine();
                builder.Append(')');
            }


            var left = dbExpression.Expressions[1] as LambdaExpression;
            var right = dbExpression.Expressions[2] as LambdaExpression;

            // t0(t1)
            string alias = !(left.Body.NodeType == ExpressionType.New || left.Body.NodeType == ExpressionType.MemberInit)
                ? aliases.GetTableAlias(dbExpression.Expressions[2])
                : aliases.GetTableAlias(right.Parameters[0]);
            builder.Append(' ');
            builder.Append(alias);
            builder.Append(' ');

            if (withNoLock)
            {
                builder.Append(_provider.WidthNoLock);
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
                    builder.AppendMember(aliases, body1.Arguments[index]);
                    builder.Append(" = ");
                    builder.AppendMember(aliases, body2.Arguments[index]);
                    if (index < body1.Arguments.Count - 1) builder.Append(" AND ");
                }
            }
            else if (left.Body.NodeType == ExpressionType.MemberInit)
            {
                var body1 = left.Body as MemberInitExpression;
                var body2 = right.Body as MemberInitExpression;

                for (int index = 0; index < body1.Bindings.Count; ++index)
                {
                    builder.AppendMember(aliases, (body1.Bindings[index] as MemberAssignment).Expression);
                    builder.Append(" = ");
                    builder.AppendMember(aliases, (body2.Bindings[index] as MemberAssignment).Expression);
                    if (index < body1.Bindings.Count - 1) builder.Append(" AND ");
                }
            }
            else
            {
                builder.AppendMember(aliases, left.Body.ReduceUnary());
                builder.Append(" = ");
                builder.AppendMember(aliases, right.Body.ReduceUnary());
            }
        }

        // Cross Join
        private void AppendCrossJoin(ISqlBuilder builder, DbExpression exp, TableAlias aliases)
        {
            var lambdaExp = exp.Expressions[1] as LambdaExpression;
            Type type = lambdaExp.Parameters[1].Type;
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
            bool withNoLock = !typeRuntime.IsTemporary && _context.NoLock && !string.IsNullOrEmpty(_provider.WidthNoLock);

            builder.Append(' ');
            builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);

            string alias = aliases.GetTableAlias(lambdaExp.Parameters[1]);
            builder.Append(' ');
            builder.Append(alias);
            builder.Append(' ');

            if (withNoLock)
            {
                builder.Append(_provider.WidthNoLock);
                builder.Append(' ');
            }
        }
    }
}
