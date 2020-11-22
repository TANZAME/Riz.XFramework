
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
        private ISqlBuilder _builder = null;
        private AliasGenerator _aliasGenerator = null;

        /// <summary>
        /// 初始化 <see cref="SqlServerJoinExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="aliasGenerator">表别名解析器</param>
        /// <param name="builder">SQL 语句生成器</param>
        public SqlServerJoinExpressionVisitor(AliasGenerator aliasGenerator, ISqlBuilder builder)
            : base(aliasGenerator, builder)
        {
            _builder = builder;
            _aliasGenerator = aliasGenerator;

            var context = builder.TranslateContext.DbContext;
            _isNoLock = ((SqlServerDbContext)context).NoLock;
            _withNoLock = ((SqlServerDbQueryProvider)context.Provider).WidthNoLock;
        }

        // LEFT OR INNER JOIN
        protected override void AppendLfInJoin( DbExpression dbExpression)
        {
            bool withNoLock = false;
            _builder.Append(' ');
            IDbQueryable dbQuery = (IDbQueryable)((dbExpression.Expressions[0] as ConstantExpression).Value);
            if (dbQuery.DbExpressions.Count == 1 && dbQuery.DbExpressions[0].DbExpressionType == DbExpressionType.GetTable)
            {
                Type type = dbExpression.Expressions[0].Type.GetGenericArguments()[0];
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                _builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);

                withNoLock = !typeRuntime.IsTemporary && _isNoLock && !string.IsNullOrEmpty(_withNoLock);
            }
            else
            {
                // 嵌套
                var cmd = dbQuery.Translate(_builder.Indent + 1, false, _builder.TranslateContext);
                _builder.Append("(");
                _builder.Append(cmd.CommandText);
                _builder.AppendNewLine();
                _builder.Append(')');
            }


            var left = dbExpression.Expressions[1] as LambdaExpression;
            var right = dbExpression.Expressions[2] as LambdaExpression;

            // t0(t1)
            string alias = !(left.Body.NodeType == ExpressionType.New || left.Body.NodeType == ExpressionType.MemberInit)
                ? _aliasGenerator.GetTableAlias(dbExpression.Expressions[2])
                : _aliasGenerator.GetTableAlias(right.Parameters[0]);
            _builder.Append(' ');
            _builder.Append(alias);
            _builder.Append(' ');

            if (withNoLock)
            {
                _builder.Append(_withNoLock);
                _builder.Append(' ');
            }

            // ON a.Name = b.Name AND a.Id = b.Id
            _builder.Append("ON ");

            if (left.Body.NodeType == ExpressionType.New)
            {

                var body1 = left.Body as NewExpression;
                var body2 = right.Body as NewExpression;

                for (int index = 0; index < body1.Arguments.Count; ++index)
                {
                    _builder.AppendMember(_aliasGenerator, body1.Arguments[index]);
                    _builder.Append(" = ");
                    _builder.AppendMember(_aliasGenerator, body2.Arguments[index]);
                    if (index < body1.Arguments.Count - 1) _builder.Append(" AND ");
                }
            }
            else if (left.Body.NodeType == ExpressionType.MemberInit)
            {
                var body1 = left.Body as MemberInitExpression;
                var body2 = right.Body as MemberInitExpression;

                for (int index = 0; index < body1.Bindings.Count; ++index)
                {
                    _builder.AppendMember(_aliasGenerator, (body1.Bindings[index] as MemberAssignment).Expression);
                    _builder.Append(" = ");
                    _builder.AppendMember(_aliasGenerator, (body2.Bindings[index] as MemberAssignment).Expression);
                    if (index < body1.Bindings.Count - 1) _builder.Append(" AND ");
                }
            }
            else
            {
                _builder.AppendMember(_aliasGenerator, left.Body.ReduceUnary());
                _builder.Append(" = ");
                _builder.AppendMember(_aliasGenerator, right.Body.ReduceUnary());
            }
        }

        // Cross Join
        protected override void AppendCrossJoin(DbExpression dbExpression)
        {
            var lambdaExp = dbExpression.Expressions[1] as LambdaExpression;
            Type type = lambdaExp.Parameters[1].Type;
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
            bool withNoLock = !typeRuntime.IsTemporary && _isNoLock && !string.IsNullOrEmpty(_withNoLock);

            _builder.Append(' ');
            _builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);

            string alias = _aliasGenerator.GetTableAlias(lambdaExp.Parameters[1]);
            _builder.Append(' ');
            _builder.Append(alias);
            _builder.Append(' ');

            if (withNoLock)
            {
                _builder.Append(_withNoLock);
                _builder.Append(' ');
            }
        }
    }
}
