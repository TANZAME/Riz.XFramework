using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// DELETE 或者 UPDATE 语法中的 JOIN 解析成 EXISTS
    /// </summary>
    public class OracleExistsExpressionVisitor : ExpressionVisitorBase
    {
        private IDbQueryProvider _provider = null;
        private List<DbExpression> _qJoin = null;
        private TableAliasCache _aliases = null;
        private DbExpression _where = null;

        /// <summary>
        /// 初始化 <see cref="OracleExistsExpressionVisitor"/> 类的新实例
        /// </summary>
        public OracleExistsExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, List<DbExpression> qJoin, DbExpression qWhere)
            : base(provider, aliases, null, false)
        {
            _provider = provider;
            _qJoin = qJoin;
            _aliases = aliases;
            _where = qWhere;
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        public void Write(OracleSelectInfoCommand cmd)
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
                useExists = true;
                if ((wf != null && wf.Length > 0) || (on != null && on.Length > 0))
                {
                    wf.AppendNewLine();
                    if (wf != null && wf.Length > 0) wf.Append("AND ");
                }

                wf.Append("EXISTS(");
                wf.Indent += 1;
                wf.AppendNewLine();
                wf.Append("SELECT 1 FROM ");

                Type type = exp.Expressions[0].Type.GetGenericArguments()[0];
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                wf.AppendMember(typeRuntime.TableName, !typeRuntime.IsTemporary);
                wf.Append(' ');
                wf.Append(alias);

                wf.AppendNewLine();
                wf.Append("WHERE ");
            }
            else
            {
                useExists = true;
                if ((wf != null && wf.Length > 0) || (on != null && on.Length > 0))
                {
                    wf.AppendNewLine();
                    if (wf != null && wf.Length > 0) wf.Append("AND ");
                }

                wf.Append("EXISTS(");
                wf.Indent += 1;
                wf.AppendNewLine();
                wf.Append("SELECT 1 FROM(");
                var cmd2 = sQuery.Resolve(wf.Indent + 1, false, wf.Parameter);
                wf.Append(cmd2.CommandText);
                wf.AppendNewLine();
                wf.Append(')');
                wf.Append(' ');
                wf.Append(alias);
                wf.Append(" WHERE ");
            }

            ISqlBuilder builder = useExists ? wf : on;
            if (body1 == null)
            {
                builder.AppendMember(aliases, left.Body.ReduceUnary());
                builder.Append(" = ");
                builder.AppendMember(aliases, right.Body.ReduceUnary());
            }
            else
            {
                for (int index = 0; index < body1.Arguments.Count; ++index)
                {
                    builder.AppendMember(aliases, body1.Arguments[index]);
                    builder.Append(" = ");
                    builder.AppendMember(aliases, body2.Arguments[index]);
                    if (index < body1.Arguments.Count - 1) builder.Append(" AND ");
                }
            }

            //if (_where != null && _where.Expressions != null) this.AppendWhere(builder, right);

            if (useExists)
            {
                wf.Indent -= 1;
                wf.AppendNewLine();
                wf.Append(')');
            }
        }

        private void AppendWhere(ISqlBuilder builder, LambdaExpression lambda)
        {
            ParameterExpression parameter = lambda.Parameters[0];
            Expression expression = _where.Expressions[0];
            List<Expression> predicates = new List<Expression>();
            List<SegExpression> others = new List<SegExpression>();

            while (expression != null && (expression is BinaryExpression))
            {
                bool ignore = false;
                BinaryExpression source = expression as BinaryExpression;

                // 找出对应的表别名
                BinaryExpression rightBinary = source.Right as BinaryExpression;
                BinaryExpression binary = rightBinary != null ? rightBinary : source;

                Expression right = binary.Left.CanEvaluate() ? binary.Right : binary.Left;
                while (right != null && right.Acceptable())
                {
                    MemberExpression m = right as MemberExpression;
                    if (m != null) right = m.Expression;
                    else right = null;
                }
                if (right != null && right.NodeType == ExpressionType.MemberAccess)
                {
                    MemberExpression m = right as MemberExpression;
                    if (m.Member.Name == parameter.Name)
                    {
                        predicates.Add(binary);
                        ignore = true;
                    }
                }

                expression = source.Left;
                others.Add(new SegExpression
                {
                    Ignore = ignore,
                    NodeType = source.NodeType,
                    Expression = binary
                });
            }

            if (predicates.Count > 0)
            {
                Expression body = predicates[0];
                for (int i = 1; i < predicates.Count; i++)
                {
                    if (predicates[i] != null) body = Expression.And(body, predicates[i]);
                }

                OracleWhereExpressionVisitor visitor = new OracleWhereExpressionVisitor(_provider, _aliases, new DbExpression(DbExpressionType.Where, body));
                visitor.Write(builder);
            }

            //if (others.Any(x => x.Ignore))
            //{
            //    Expression body = null;
            //    for(int )
            //}
        }

        // Cross Join
        private void AppendCrossJoin(ISqlBuilder jf, DbExpression exp, TableAliasCache aliases)
        {
            throw new NotSupportedException("Oracle not support.");
        }

        class SegExpression
        {
            public bool Ignore { get; set; }

            public ExpressionType NodeType { get; set; }

            public Expression Expression { get; set; }
        }
    }
}
