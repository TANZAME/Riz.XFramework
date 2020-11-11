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
        private List<DbExpression> _joins = null;
        private AliasGenerator _aliasGenerator = null;
        private bool usedKeyword = false;
        private readonly string _keywordName = string.Empty;
        private DbExpressionType _dbExpressionType = DbExpressionType.None;
        private string _pad = "";

        /// <summary>
        /// 初始化 <see cref="NpgJoinExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="aliasGenerator">表别名解析器</param>
        /// <param name="joins">JOIN 子句</param>
        /// <param name="dbExpressionType">表达式类型</param>
        public NpgJoinExpressionVisitor(AliasGenerator aliasGenerator, List<DbExpression> joins, DbExpressionType dbExpressionType)
            : base(aliasGenerator, joins)
        {
            _joins = joins;
            _aliasGenerator = aliasGenerator;
            _dbExpressionType = dbExpressionType;

            if (_dbExpressionType == DbExpressionType.Delete) _keywordName = "USING ";
            else if (_dbExpressionType == DbExpressionType.Update) _keywordName = "FROM ";
            _pad = "".PadLeft(_keywordName.Length, ' ');
        }

        /// <summary>
        /// 写入SQL片断
        /// </summary>
        /// <param name="cmd">SQL 命令</param>
        public void Write(NpgDbSelectCommand cmd)
        {
            ISqlBuilder jf = cmd.JoinFragment;
            ISqlBuilder on = cmd.OnPhrase;

            this.Initialize(on);
            if (_joins != null && _joins.Count > 0)
            {
                for (int i = 0; i < _joins.Count; i++)
                {
                    DbExpression qj = _joins[i];
                    if (qj.DbExpressionType == DbExpressionType.GroupJoin || qj.DbExpressionType == DbExpressionType.Join || qj.DbExpressionType == DbExpressionType.GroupRightJoin)
                        this.AppendLfInJoin(jf, on, qj, _aliasGenerator);
                    else if (qj.DbExpressionType == DbExpressionType.SelectMany)
                        this.AppendCrossJoin(jf, qj, _aliasGenerator);
                }
            }
        }

        // LEFT OR INNER JOIN
        private void AppendLfInJoin(ISqlBuilder jf, ISqlBuilder on, DbExpression dbExpression, AliasGenerator aliasGenerator)
        {
            IDbQueryable sQuery = (IDbQueryable)((dbExpression.Expressions[0] as ConstantExpression).Value);
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

            if (sQuery.DbExpressions.Count == 1 && sQuery.DbExpressions[0].DbExpressionType == DbExpressionType.GetTable)
            {
                Type type = dbExpression.Expressions[0].Type.GetGenericArguments()[0];
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                jf.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
            }
            else
            {
                // 嵌套
                var cmd = sQuery.Translate(jf.Indent + _dbExpressionType == DbExpressionType.Delete ? 2 : 1, false, jf.TranslateContext);
                jf.Append("( ");
                jf.Append(_dbExpressionType == DbExpressionType.Delete ? cmd.CommandText.TrimStart() : cmd.CommandText);
                jf.Append(')');
            }

            LambdaExpression left = dbExpression.Expressions[1] as LambdaExpression;
            LambdaExpression right = dbExpression.Expressions[2] as LambdaExpression;

            // t0(t1)
            string alias = !(left.Body.NodeType == ExpressionType.New || left.Body.NodeType == ExpressionType.MemberInit)
                ? aliasGenerator.GetTableAlias(dbExpression.Expressions[2])
                : aliasGenerator.GetTableAlias(right.Parameters[0]);
            jf.Append(' ');
            jf.Append(alias);

            if (on.Length > 0) on.Append(" AND ");

            if (left.Body.NodeType == ExpressionType.New)
            {

                NewExpression body1 = left.Body as NewExpression;
                NewExpression body2 = right.Body as NewExpression;

                for (int index = 0; index < body1.Arguments.Count; ++index)
                {
                    on.AppendMember(aliasGenerator, body1.Arguments[index]);
                    on.Append(" = ");
                    on.AppendMember(aliasGenerator, body2.Arguments[index]);
                    if (index < body1.Arguments.Count - 1) on.Append(" AND ");
                }
            }
            else if (left.Body.NodeType == ExpressionType.MemberInit)
            {
                MemberInitExpression body1 = left.Body as MemberInitExpression;
                MemberInitExpression body2 = right.Body as MemberInitExpression;

                for (int index = 0; index < body1.Bindings.Count; ++index)
                {
                    on.AppendMember(aliasGenerator, (body1.Bindings[index] as MemberAssignment).Expression);
                    on.Append(" = ");
                    on.AppendMember(aliasGenerator, (body2.Bindings[index] as MemberAssignment).Expression);
                    if (index < body1.Bindings.Count - 1) on.Append(" AND ");
                }
            }
            else
            {
                on.AppendMember(aliasGenerator, left.Body.ReduceUnary());
                on.Append(" = ");
                on.AppendMember(aliasGenerator, right.Body.ReduceUnary());
            }
        }

        // Cross Join
        private void AppendCrossJoin(ISqlBuilder jf, DbExpression dbExpression, AliasGenerator aliasGenerator)
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

            string alias = aliasGenerator.GetTableAlias(lambda.Parameters[1]);
            jf.Append(' ');
            jf.Append(alias);
        }
    }
}
