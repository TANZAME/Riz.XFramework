using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// Postgre 删除/更新中的关联语法
    /// </summary>
    public class NpgJoinExpressionVisitor : JoinExpressionVisitor
    {
        private List<DbExpression> _qJoin = null;
        private TableAliasCache _aliases = null;
        private IDbQueryProvider _provider = null;
        private bool usedKeyword = false;
        private readonly string _keywordName = string.Empty;
        private DbExpressionType _dbExpressionType = DbExpressionType.None;
        private string _pad = "";

        /// <summary>
        /// 初始化 <see cref="NpgJoinExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="provider">查询语义提供者</param>
        /// <param name="aliases">表别名集合</param>
        /// <param name="joins">JOIN 子句</param>
        /// <param name="dbExpressionType">表达式类型</param>
        public NpgJoinExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, List<DbExpression> joins, DbExpressionType dbExpressionType)
            : base(provider, aliases, joins)
        {
            _qJoin = joins;
            _aliases = aliases;
            _provider = provider;
            _dbExpressionType = dbExpressionType;

            if (_dbExpressionType == DbExpressionType.Delete) _keywordName = "USING ";
            else if (_dbExpressionType == DbExpressionType.Update) _keywordName = "FROM ";
            _pad = "".PadLeft(_keywordName.Length, ' ');
        }

        /// <summary>
        /// 写入SQL片断
        /// </summary>
        /// <param name="builder">SQL 语句生成器</param>
        public void Write(NpgMapperCommand cmd)
        {
            ISqlBuilder jf = cmd.JoinFragment;
            ISqlBuilder on = cmd.OnPhrase;

            base._builder = on;
            if (base._methodVisitor == null) base._methodVisitor = _provider.CreateMethodVisitor(this);

            if (_qJoin != null && _qJoin.Count > 0)
            {
                for (int i = 0; i < _qJoin.Count; i++)
                {
                    DbExpression qj = _qJoin[i];
                    if (qj.DbExpressionType == DbExpressionType.GroupJoin || qj.DbExpressionType == DbExpressionType.Join || qj.DbExpressionType == DbExpressionType.GroupRightJoin)
                        this.AppendLfInJoin(jf, on, qj, _aliases);
                    else if (qj.DbExpressionType == DbExpressionType.SelectMany)
                        this.AppendCrossJoin(jf, qj, _aliases);
                }
            }
        }

        // LEFT OR INNER JOIN
        private void AppendLfInJoin(ISqlBuilder jf, ISqlBuilder on, DbExpression dbExpression, TableAliasCache aliases)
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
                var cmd = sQuery.Resolve(jf.Indent + _dbExpressionType == DbExpressionType.Delete ? 2 : 1, false, jf.Token);
                jf.Append("( ");
                jf.Append(_dbExpressionType == DbExpressionType.Delete ? cmd.CommandText.TrimStart() : cmd.CommandText);
                jf.Append(')');
            }

            LambdaExpression left = dbExpression.Expressions[1] as LambdaExpression;
            LambdaExpression right = dbExpression.Expressions[2] as LambdaExpression;

            // t0(t1)
            string alias = !(left.Body.NodeType == ExpressionType.New || left.Body.NodeType == ExpressionType.MemberInit)
                ? aliases.GetTableAlias(dbExpression.Expressions[2])
                : aliases.GetTableAlias(right.Parameters[0]);
            jf.Append(' ');
            jf.Append(alias);

            if (on.Length > 0) on.Append(" AND ");

            if (left.Body.NodeType == ExpressionType.New)
            {

                NewExpression body1 = left.Body as NewExpression;
                NewExpression body2 = right.Body as NewExpression;

                for (int index = 0; index < body1.Arguments.Count; ++index)
                {
                    on.AppendMember(aliases, body1.Arguments[index]);
                    on.Append(" = ");
                    on.AppendMember(aliases, body2.Arguments[index]);
                    if (index < body1.Arguments.Count - 1) on.Append(" AND ");
                }
            }
            else if (left.Body.NodeType == ExpressionType.MemberInit)
            {
                MemberInitExpression body1 = left.Body as MemberInitExpression;
                MemberInitExpression body2 = right.Body as MemberInitExpression;

                for (int index = 0; index < body1.Bindings.Count; ++index)
                {
                    on.AppendMember(aliases, (body1.Bindings[index] as MemberAssignment).Expression);
                    on.Append(" = ");
                    on.AppendMember(aliases, (body2.Bindings[index] as MemberAssignment).Expression);
                    if (index < body1.Bindings.Count - 1) on.Append(" AND ");
                }
            }
            else
            {
                on.AppendMember(aliases, left.Body.ReduceUnary());
                on.Append(" = ");
                on.AppendMember(aliases, right.Body.ReduceUnary());
            }
        }

        // Cross Join
        private void AppendCrossJoin(ISqlBuilder jf, DbExpression dbExpression, TableAliasCache aliases)
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

            string alias = aliases.GetTableAlias(lambda.Parameters[1]);
            jf.Append(' ');
            jf.Append(alias);
        }
    }
}
