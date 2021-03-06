﻿
using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using Riz.XFramework.Data.SqlClient;
using System.Linq;
using System.Data;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// JOIN 表达式解析器，用于产生 WITH NOLOCK
    /// </summary>
    internal class SqlServerJoinExpressionVisitor : JoinExpressionVisitor
    {
        private bool _isNoLock = false;
        private string _withNoLock = string.Empty;
        private AliasGenerator _ag = null;
        private ISqlBuilder _builder = null;

        /// <summary>
        /// 初始化 <see cref="SqlServerJoinExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="ag">表别名解析器</param>
        /// <param name="builder">SQL 语句生成器</param>
        public SqlServerJoinExpressionVisitor(AliasGenerator ag, ISqlBuilder builder)
            : base(ag, builder)
        {
            _ag = ag;
            _builder = builder;

            var context = builder.TranslateContext.DbContext;
            _isNoLock = ((SqlServerDbContext)context).NoLock;
            _withNoLock = ((SqlServerDbQueryProvider)context.Provider).WidthNoLock;
        }

        // LEFT OR INNER JOIN
        protected override void AppendLfInJoin(DbExpression dbExpression)
        {
            bool withNoLock = false;
            _builder.Append(' ');
            DbQueryable dbQueryable = (DbQueryable)((dbExpression.Expressions[0] as ConstantExpression).Value);
            dbQueryable.Parameterized = _builder.Parameterized;

            if (dbQueryable.DbExpressions.Count == 1 && dbQueryable.DbExpressions[0].DbExpressionType == DbExpressionType.GetTable)
            {
                // 区别 GetTable 有三个重载
                var expressions = dbQueryable.DbExpressions[0].Expressions;
                if (expressions.Length == 2 && ((expressions[0] as ConstantExpression).Value as string) != null)
                {
                    string text = (expressions[0] as ConstantExpression).Value as string;
                    object[] @params = (expressions[1] as ConstantExpression).Value as object[];

                    var provider = ((DbQueryProvider)_builder.Provider);
                    var context = _builder.TranslateContext;
                    // 解析参数
                    object[] args = null;
                    if (@params != null)
                        args = @params.Select(x => provider.Constor.GetSqlValue(x, context)).ToArray();
                    string sql = text;
                    if (args != null && args.Length > 0)
                        sql = string.Format(sql, args);

                    var cmd = new DbRawCommand(sql, context.Parameters, CommandType.Text);
                    _builder.Append("(");
                    _builder.Append(cmd.CommandText);
                    _builder.AppendNewLine();
                    _builder.Append(')');
                }
                else
                {
                    Type type = dbExpression.Expressions[0].Type.GetGenericArguments()[0];
                    var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                    _builder.AppendTable(typeRuntime.TableSchema, typeRuntime.TableName, typeRuntime.IsTemporary);

                    withNoLock = !typeRuntime.IsTemporary && _isNoLock && !string.IsNullOrEmpty(_withNoLock);
                }
            }
            else
            {
                // 嵌套
                var cmd = dbQueryable.Translate(_builder.Indent + 1, false, _builder.TranslateContext);
                _builder.Append("(");
                _builder.Append(cmd.CommandText);
                _builder.AppendNewLine();
                _builder.Append(')');
            }


            var left = dbExpression.Expressions[1] as LambdaExpression;
            var right = dbExpression.Expressions[2] as LambdaExpression;

            // t0(t1)
            string alias = !(left.Body.NodeType == ExpressionType.New || left.Body.NodeType == ExpressionType.MemberInit)
                ? _ag.GetTableAlias(dbExpression.Expressions[2])
                : _ag.GetTableAlias(right.Parameters[0]);
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
                    base.Visit(body1.Arguments[index]);
                    _builder.Append(" = ");
                    base.Visit(body2.Arguments[index]);
                    if (index < body1.Arguments.Count - 1) _builder.Append(" AND ");
                }
            }
            else if (left.Body.NodeType == ExpressionType.MemberInit)
            {
                var body1 = left.Body as MemberInitExpression;
                var body2 = right.Body as MemberInitExpression;

                for (int index = 0; index < body1.Bindings.Count; ++index)
                {
                    base.Visit((body1.Bindings[index] as MemberAssignment).Expression);
                    _builder.Append(" = ");
                    base.Visit((body2.Bindings[index] as MemberAssignment).Expression);
                    if (index < body1.Bindings.Count - 1) _builder.Append(" AND ");
                }
            }
            else
            {
                base.Visit(left.Body.ReduceUnary());
                _builder.Append(" = ");
                base.Visit(right.Body.ReduceUnary());
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
            _builder.AppendTable(typeRuntime.TableSchema, typeRuntime.TableName, typeRuntime.IsTemporary);

            string alias = _ag.GetTableAlias(lambdaExp.Parameters[1]);
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
