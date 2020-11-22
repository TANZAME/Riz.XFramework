using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 关联表达式解析器
    /// </summary>
    internal class JoinExpressionVisitor : DbExpressionVisitor
    {
        private ISqlBuilder _builder = null;
        private AliasGenerator _aliasGenerator = null;

        /// <summary>
        /// 初始化 <see cref="JoinExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="aliasGenerator">表别名解析器</param>
        /// <param name="builder">SQL 语句生成器</param>
        public JoinExpressionVisitor(AliasGenerator aliasGenerator, ISqlBuilder builder)
            : base(aliasGenerator, builder)
        {
            _builder = builder;
            _aliasGenerator = aliasGenerator;
        }

        /// <summary>
        /// 访问表达式节点
        /// </summary>
        /// <param name="joins">关联表达式</param>
        public override Expression Visit(List<DbExpression> joins)
        {
            for (int index = 0; index < (joins != null ? joins.Count : -1); index++)
            {
                DbExpression d = joins[index];
                _builder.AppendNewLine();

                if (d.DbExpressionType == DbExpressionType.GroupJoin || d.DbExpressionType == DbExpressionType.Join ||
                    d.DbExpressionType == DbExpressionType.GroupRightJoin)
                {
                    // INNER/LEFT JOIN
                    JoinType joinType = JoinType.InnerJoin;
                    if (d.DbExpressionType == DbExpressionType.GroupJoin) joinType = JoinType.LeftJoin;
                    else if (d.DbExpressionType == DbExpressionType.GroupRightJoin) joinType = JoinType.RightJoin;
                    this.AppendJoinType(_builder, joinType);
                    this.AppendLfInJoin(d);
                }
                else if (d.DbExpressionType == DbExpressionType.SelectMany)
                {
                    // CROSS JOIN
                    this.AppendJoinType(_builder, JoinType.CrossJoin);
                    this.AppendCrossJoin(d);
                }
            }

            return null;
        }

        /// <summary>
        /// 解析左关联
        /// </summary>
        protected virtual void AppendLfInJoin(DbExpression dbExpression)
        {
            _builder.Append(' ');
            IDbQueryable dbQuery = (IDbQueryable)((dbExpression.Expressions[0] as ConstantExpression).Value);
            if (dbQuery.DbExpressions.Count == 1 && dbQuery.DbExpressions[0].DbExpressionType == DbExpressionType.GetTable)
            {
                Type type = dbExpression.Expressions[0].Type.GetGenericArguments()[0];
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                _builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
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

        /// <summary>
        /// 解析全关联
        /// </summary>
        protected virtual void AppendCrossJoin(DbExpression dbExpression)
        {
            var lambdaExp = dbExpression.Expressions[1] as LambdaExpression;
            Type type = lambdaExp.Parameters[1].Type;
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
            _builder.Append(' ');
            _builder.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);

            string alias = _aliasGenerator.GetTableAlias(lambdaExp.Parameters[1]);
            _builder.Append(' ');
            _builder.Append(alias);
            _builder.Append(' ');
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
