
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ICS.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="NpgMethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    public class NpgMethodCallExressionVisitor : MethodCallExressionVisitor
    {
        private ExpressionVisitorBase _visitor = null;
        private IDbQueryProvider _provider = null;
        private ISqlBuilder _builder = null;
        private VisitedMemberList _visitedMember = null;

        #region 构造函数

        /// <summary>
        /// 实例化 <see cref="SqlMethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        public NpgMethodCallExressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
            : base(provider, visitor)
        {
            _provider = provider;
            _visitor = visitor;
            _builder = visitor.SqlBuilder;
            _visitedMember = _visitor.VisitedMember;
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 访问表示 null 合并运算的节点 a ?? b
        /// </summary>
        public override Expression VisitCoalesce(BinaryExpression b)
        {
            // 例： a.Name ?? "TAN" => ISNULL(a.Name,'TAN')

            _builder.Append("COALESCE(");
            _visitor.Visit(b.Left is ConstantExpression ? b.Right : b.Left);
            _builder.Append(",");
            _visitor.Visit(b.Left is ConstantExpression ? b.Left : b.Right);
            _builder.Append(")");


            return b;
        }

        protected override Expression VisitSTRContains(MethodCallExpression m)
        {
            if (m != null)
            {
                _visitor.Visit(m.Object);
                _builder.Append(" LIKE ");
                if (m.Arguments[0].CanEvaluate())
                {
                    bool unicode = true;
                    string value = this.GetSqlValue(m.Arguments[0].Evaluate(), ref unicode);

                    if (_builder.Parameterized)
                    {
                        _builder.Append("'%' || ");
                        _builder.Append(value);
                        _builder.Append(" || '%'");
                    }
                    else
                    {
                        if (unicode) _builder.Append('N');
                        _builder.Append("'%");
                        _builder.Append(value);
                        _builder.Append("%'");
                    }
                }
                else
                {
                    _builder.Append("('%' || ");
                    _visitor.Visit(m.Arguments[0]);
                    _builder.Append(" || '%')");
                }
            }

            return m;
        }

        protected override Expression VisitSTRStartsWith(MethodCallExpression m)
        {
            if (m != null)
            {
                _visitor.Visit(m.Object);
                _builder.Append(" LIKE ");
                if (m.Arguments[0].CanEvaluate())
                {
                    bool unicode = true;
                    string value = this.GetSqlValue(m.Arguments[0].Evaluate(), ref unicode);

                    if (_builder.Parameterized)
                    {
                        _builder.Append(value);
                        _builder.Append(" || '%'");
                    }
                    else
                    {
                        if (unicode) _builder.Append('N');
                        _builder.Append("'");
                        _builder.Append(value);
                        _builder.Append("%'");
                    }
                }
                else
                {
                    _builder.Append("(");
                    _visitor.Visit(m.Arguments[0]);
                    _builder.Append(" || '%')");
                }
            }

            return m;
        }

        protected override Expression VisitSTREndsWith(MethodCallExpression m)
        {
            if (m != null)
            {
                _visitor.Visit(m.Object);
                _builder.Append(" LIKE ");
                if (m.Arguments[0].CanEvaluate())
                {
                    bool unicode = true;
                    string value = this.GetSqlValue(m.Arguments[0].Evaluate(), ref unicode);

                    if (_builder.Parameterized)
                    {
                        _builder.Append("'%' || ");
                        _builder.Append(value);
                    }
                    else
                    {
                        if (unicode) _builder.Append('N');
                        _builder.Append("'%");
                        _builder.Append(value);
                        _builder.Append("'");
                    }
                }
                else
                {
                    _builder.Append("('%' || ");
                    _visitor.Visit(m.Arguments[0]);
                    _builder.Append(")");
                }
            }

            return m;
        }

        protected override Expression VisitSTRToString(MethodCallExpression m)
        {
            _builder.Append("CAST(");
            _visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
            _builder.Append(" AS VARCHAR)");

            return m;
        }

        protected override Expression VisitSTRSubstring(MethodCallExpression m)
        {
            if (m != null)
            {
                List<Expression> args = new List<Expression>(m.Arguments);
                if (m.Object != null) args.Insert(0, m.Object);

                _builder.Append("SUBSTRING(");
                _visitor.Visit(args[0]);
                _builder.Append(",");

                if (args[1].CanEvaluate())
                {
                    ConstantExpression c = args[1].Evaluate();
                    int index = Convert.ToInt32(c.Value);
                    index += 1;
                    string value = _builder.GetSqlValue(index);
                    _builder.Append(value);
                    _builder.Append(',');
                }
                else
                {
                    _visitor.Visit(args[1]);
                    _builder.Append(" + 1,");
                }

                if (args.Count == 3) _visitor.Visit(args[2]);
                else
                {
                    _builder.Append("LENGTH(");
                    _visitor.Visit(args[0]);
                    _builder.Append(')');
                }
                _builder.Append(')');
            }

            return m;
        }

        protected override Expression VisitSTRLength(MemberExpression m)
        {
            _builder.Append("LENGTH(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");

            return m;
        }

        protected override Expression VisitSQLNewGuid(MethodCallExpression m)
        {
            Guid guid = System.Guid.NewGuid();
            _builder.Append(guid, null);
            return m;
        }

        protected override bool IsUnicode(object dbType)
        {
            return NpgDbTypeInfo.IsUnicode2(dbType);
        }

        #endregion
    }
}
