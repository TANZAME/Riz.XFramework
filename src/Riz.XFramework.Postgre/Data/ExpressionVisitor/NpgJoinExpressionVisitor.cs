using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// Postgre 删除/更新中的关联语法
    /// </summary>
    internal class NpgJoinExpressionVisitor : JoinExpressionVisitor
    {
        private string _pad = "";
        private bool usedKeyword = false;
        private readonly string _keywordName = string.Empty;

        private AliasGenerator _ag = null;
        private NpgDbSelectCommand _cmd = null;
        private DbExpressionType _dbExpressionType = DbExpressionType.None;

        /// <summary>
        /// 初始化 <see cref="NpgJoinExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="ag">表别名解析器</param>
        /// <param name="builder">SQL 语句生成器</param>
        /// <param name="dbExpressionType">表达式类型</param>
        /// <param name="cmd">SQL 命令</param>
        public NpgJoinExpressionVisitor(AliasGenerator ag, ISqlBuilder builder, DbExpressionType dbExpressionType, NpgDbSelectCommand cmd)
            : base(ag, builder)
        {
            _ag = ag;
            _dbExpressionType = dbExpressionType;
            _cmd = cmd;

            if (_dbExpressionType == DbExpressionType.Delete) _keywordName = "USING ";
            else if (_dbExpressionType == DbExpressionType.Update) _keywordName = "FROM ";
            _pad = "".PadLeft(_keywordName.Length, ' ');
        }

        /// <summary>
        /// 访问表达式节点
        /// </summary>
        /// <param name="joins">关联表达式</param>
        public override Expression Visit(List<DbExpression> joins)
        {
            ISqlBuilder jf = _cmd.JoinFragment;
            ISqlBuilder on = _cmd.OnPhrase;

            for (int index = 0; index < (joins != null ? joins.Count : -1); index++)
            {
                DbExpression d = joins[index];
                if (d.DbExpressionType == DbExpressionType.GroupJoin || d.DbExpressionType == DbExpressionType.Join || d.DbExpressionType == DbExpressionType.GroupRightJoin)
                    this.AppendLfInJoin(jf, on, d);
                else if (d.DbExpressionType == DbExpressionType.SelectMany)
                    this.AppendCrossJoin(jf, d);
            }

            return null;
        }

        // LEFT OR INNER JOIN
        private void AppendLfInJoin(ISqlBuilder jf, ISqlBuilder on, DbExpression dbExpression)
        {
            DbQueryable dbQuery = (DbQueryable)((dbExpression.Expressions[0] as ConstantExpression).Value);
            if (!usedKeyword)
            {
                jf.AppendNewLine();
                jf.Append(_keywordName);
                usedKeyword = true;
            }
            else
            {
                jf.Append(',');
                jf.AppendNewLine();
                jf.Append(_pad);
            }

            if (dbQuery.DbExpressions.Count == 1 && dbQuery.DbExpressions[0].DbExpressionType == DbExpressionType.GetTable)
            {
                Type type = dbExpression.Expressions[0].Type.GetGenericArguments()[0];
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                jf.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
            }
            else
            {
                // 嵌套
                var cmd = dbQuery.Translate(jf.Indent + _dbExpressionType == DbExpressionType.Delete ? 2 : 1, false, jf.TranslateContext);
                jf.Append("( ");
                jf.Append(_dbExpressionType == DbExpressionType.Delete ? cmd.CommandText.TrimStart() : cmd.CommandText);
                jf.Append(')');
            }

            LambdaExpression left = dbExpression.Expressions[1] as LambdaExpression;
            LambdaExpression right = dbExpression.Expressions[2] as LambdaExpression;

            // t0(t1)
            string alias = !(left.Body.NodeType == ExpressionType.New || left.Body.NodeType == ExpressionType.MemberInit)
                ? _ag.GetTableAlias(dbExpression.Expressions[2])
                : _ag.GetTableAlias(right.Parameters[0]);
            jf.Append(' ');
            jf.Append(alias);

            if (on.Length > 0) on.Append(" AND ");

            if (left.Body.NodeType == ExpressionType.New)
            {

                NewExpression body1 = left.Body as NewExpression;
                NewExpression body2 = right.Body as NewExpression;

                for (int index = 0; index < body1.Arguments.Count; ++index)
                {
                    on.AppendMember(_ag, body1.Arguments[index]);
                    on.Append(" = ");
                    on.AppendMember(_ag, body2.Arguments[index]);
                    if (index < body1.Arguments.Count - 1) on.Append(" AND ");
                }
            }
            else if (left.Body.NodeType == ExpressionType.MemberInit)
            {
                MemberInitExpression body1 = left.Body as MemberInitExpression;
                MemberInitExpression body2 = right.Body as MemberInitExpression;

                for (int index = 0; index < body1.Bindings.Count; ++index)
                {
                    on.AppendMember(_ag, (body1.Bindings[index] as MemberAssignment).Expression);
                    on.Append(" = ");
                    on.AppendMember(_ag, (body2.Bindings[index] as MemberAssignment).Expression);
                    if (index < body1.Bindings.Count - 1) on.Append(" AND ");
                }
            }
            else
            {
                on.AppendMember(_ag, left.Body.ReduceUnary());
                on.Append(" = ");
                on.AppendMember(_ag, right.Body.ReduceUnary());
            }
        }

        // Cross Join
        private void AppendCrossJoin(ISqlBuilder jf, DbExpression dbExpression)
        {
            if (!usedKeyword)
            {
                jf.AppendNewLine();
                jf.Append(_keywordName);
                usedKeyword = true;
            }
            else
            {
                jf.Append(',');
                jf.AppendNewLine();
                jf.Append(_pad);
            }

            LambdaExpression lambda = dbExpression.Expressions[1] as LambdaExpression;
            Type type = lambda.Parameters[1].Type;
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
            jf.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);

            string alias = _ag.GetTableAlias(lambda.Parameters[1]);
            jf.Append(' ');
            jf.Append(alias);
        }
    }
}
