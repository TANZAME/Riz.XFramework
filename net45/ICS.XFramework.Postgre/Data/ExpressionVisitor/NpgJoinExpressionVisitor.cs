using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ICS.XFramework.Data
{
    /// <summary>
    /// DELETE / UPDATE 语句的 USING 表达式解析器
    /// </summary>
    public class NpgJoinExpressionVisitor : ExpressionVisitorBase
    {
        private List<DbExpression> _qJoin = null;
        private TableAliasCache _aliases = null;
        private NpgCommandType _operationType;
        private bool _appendedKeyword = false;
        private readonly string _keywordName = string.Empty;

        /// <summary>
        /// 初始化 <see cref="JoinExpressionVisitor"/> 类的新实例
        /// </summary>
        public NpgJoinExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, List<DbExpression> qJoin, NpgCommandType operationType)
            : base(provider, aliases, null, false)
        {
            _qJoin = qJoin;
            _aliases = aliases;
            _operationType = operationType;

            if (_operationType == NpgCommandType.DELETE) _keywordName = "USING";
            else if (_operationType == NpgCommandType.UPDATE) _keywordName = "FROM";
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        public void Write(NpgDeleteDbCommandDefinition cmd)
        {
            ISqlBuilder jf = cmd.JoinFragment;
            ISqlBuilder on = cmd.OnPhrase;
            ISqlBuilder wf = cmd.WhereFragment;

            if (_qJoin != null && _qJoin.Count > 0)
            {
                for (int i = 0; i < _qJoin.Count; i++)
                {
                    DbExpression qj = _qJoin[i];
                    if (qj.DbExpressionType == DbExpressionType.GroupJoin || qj.DbExpressionType == DbExpressionType.Join)
                        this.AppendLfInJoin(jf, wf, on, qj, _aliases);
                    else if (qj.DbExpressionType == DbExpressionType.SelectMany)
                        this.AppendCrossJoin(jf, qj, _aliases);
                }
            }
        }

        // LEFT OR INNER JOIN
        private void AppendLfInJoin(ISqlBuilder jf, ISqlBuilder wf, ISqlBuilder on, DbExpression exp, TableAliasCache aliases)
        {
            bool useExists = false;
            LambdaExpression left = exp.Expressions[1] as LambdaExpression;
            LambdaExpression right = exp.Expressions[2] as LambdaExpression;
            NewExpression body1 = left.Body as NewExpression;
            NewExpression body2 = right.Body as NewExpression;

            // t0(t1)
            string alias = body1 == null
                ? aliases.GetTableAlias(exp.Expressions[2])
                : aliases.GetTableAlias(right.Parameters[0]);//(body2.Arguments[0]);

            IDbQueryable sQuery = (IDbQueryable)((exp.Expressions[0] as ConstantExpression).Value);
            if (sQuery.DbExpressions.Count == 1 && sQuery.DbExpressions[0].DbExpressionType == DbExpressionType.GetTable)
            {
                if (!_appendedKeyword)
                {
                    jf.AppendNewLine();
                    jf.Append(_keywordName);
                    _appendedKeyword = true;
                }
                else
                {
                    jf.Append(',');
                    jf.AppendNewLine();
                    jf.Append("     ");
                }

                jf.Append(' ');
                Type type = exp.Expressions[0].Type.GetGenericArguments()[0];
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                jf.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);

                jf.Append(' ');
                jf.Append(alias);
                if (on.Length > 0) on.Append(" AND ");
            }
            else
            {
                useExists = true;
                if ((wf != null && wf.Length > 0) || (on != null && on.Length > 0))
                {
                    if (wf != null && wf.Length > 0) wf.Append("AND ");
                    wf.AppendNewLine();
                }

                wf.Append("EXISTS(");
                wf.Indent += 1;
                wf.AppendNewLine();
                wf.Append("SELECT 1 FROM(");
                var cmd = sQuery.Resolve(wf.Indent + 1, false, wf.Parameters);
                wf.Append(cmd.CommandText);
                wf.AppendNewLine();
                wf.Append(')');
                wf.Append(' ');
                wf.Append(alias);
                wf.Append(" WHERE ");
            }

            ISqlBuilder tbuilder = useExists ? wf : on;
            if (body1 == null)
            {
                tbuilder.AppendMember(aliases, left.Body.ReduceUnary());
                tbuilder.Append(" = ");
                tbuilder.AppendMember(aliases, right.Body.ReduceUnary());
            }
            else
            {
                for (int index = 0; index < body1.Arguments.Count; ++index)
                {
                    tbuilder.AppendMember(aliases, body1.Arguments[index]);
                    tbuilder.Append(" = ");
                    tbuilder.AppendMember(aliases, body2.Arguments[index]);
                    if (index < body1.Arguments.Count - 1) tbuilder.Append(" AND ");
                }
            }

            if (useExists)
            {
                wf.Indent -= 1;
                wf.AppendNewLine();
                wf.Append(')');
            }
        }

        // Cross Join
        private void AppendCrossJoin(ISqlBuilder jf, DbExpression exp, TableAliasCache aliases)
        {
            if (!_appendedKeyword)
            {
                jf.AppendNewLine();
                jf.Append(_keywordName);
                _appendedKeyword = true;
            }
            else
            {
                jf.Append(',');
                jf.AppendNewLine();
                jf.Append("     ");
            }

            LambdaExpression lambdaExp = exp.Expressions[1] as LambdaExpression;
            Type type = lambdaExp.Parameters[1].Type;
            var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
            jf.Append(' ');
            jf.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);

            string alias = aliases.GetTableAlias(lambdaExp.Parameters[1]);
            jf.Append(' ');
            jf.Append(alias);
        }
    }
}
