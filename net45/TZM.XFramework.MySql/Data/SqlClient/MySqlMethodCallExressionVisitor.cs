
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="MySqlMethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    public class MySqlMethodCallExressionVisitor : MethodCallExressionVisitor
    {
        // https://dev.mysql.com/doc/refman/8.0/en/charset-national.html
        //SELECT N'some text';
        //SELECT n'some text';
        //SELECT _utf8'some text';

        private ITextBuilder _builder = null;
        private IDbQueryProvider _provider = null;
        private ExpressionVisitorBase _visitor = null;
        private MemberVisitedMark _visitedMark = null;

        #region 构造函数

        /// <summary>
        /// 实例化 <see cref="SqlMethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        public MySqlMethodCallExressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
            : base(provider, visitor)
        {
            _provider = provider;
            _visitor = visitor;
            _builder = visitor.SqlBuilder;
            _visitedMark = _visitor.VisitedMark;
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 访问表示 null 合并运算的节点 a ?? b
        /// </summary>
        public override Expression VisitCoalesce(BinaryExpression b)
        {
            // 例： a.Name ?? "TAN" => ISNULL(a.Name,'TAN')

            _builder.Append("IFNULL(");
            _visitor.Visit(b.Left is ConstantExpression ? b.Right : b.Left);
            _builder.Append(',');
            _visitor.Visit(b.Left is ConstantExpression ? b.Left : b.Right);
            _builder.Append(')');


            return b;
        }

        /// <summary>
        /// 访问 ToString 方法
        /// </summary>
        protected override Expression VisitToString(MethodCallExpression m)
        {
            _builder.Append("CAST(");
            _visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
            _builder.Append(" AS CHAR)");

            return m;
        }

        /// <summary>
        /// 访问 string.Contains 方法
        /// </summary>
        protected override Expression VisitStringContains(MethodCallExpression m)
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
                        _builder.Append("CONCAT('%',");
                        _builder.Append(value);
                        _builder.Append(",'%')");
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
                    _builder.Append("CONCAT('%',");
                    _visitor.Visit(m.Arguments[0]);
                    _builder.Append(",'%')");
                }
            }

            return m;
        }

        /// <summary>
        /// 访问 StartWidth 方法
        /// </summary>
        protected override Expression VisitStartsWith(MethodCallExpression m)
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
                        _builder.Append("CONCAT(");
                        _builder.Append(value);
                        _builder.Append(",'%')");
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
                    _builder.Append("CONCAT(");
                    _visitor.Visit(m.Arguments[0]);
                    _builder.Append(",'%')");
                }
            }

            return m;
        }

        /// <summary>
        /// 访问 EndWidth 方法
        /// </summary>
        protected override Expression VisitEndsWith(MethodCallExpression m)
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
                        _builder.Append("CONCAT(");
                        _builder.Append("'%',");
                        _builder.Append(value);
                        _builder.Append(')');
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
                    _builder.Append("CONCAT('%',");
                    _visitor.Visit(m.Arguments[0]);
                    _builder.Append(")");
                }
            }

            return m;
        }

        /// <summary>
        /// 访问 SubString 方法
        /// </summary>
        protected override Expression VisitSubstring(MethodCallExpression m)
        {
            if (m != null)
            {
                List<Expression> args = new List<Expression>(m.Arguments);
                if (m.Object != null) args.Insert(0, m.Object);

                _builder.Append("SUBSTRING(");
                _visitor.Visit(args[0]);
                _builder.Append(',');

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
                    _builder.Append("CHAR_LENGTH(");
                    _visitor.Visit(args[0]);
                    _builder.Append(')');
                }
                _builder.Append(')');
            }

            return m;
        }

        /// <summary>
        /// 访问 Length 属性
        /// </summary>
        protected override Expression VisitLength(MemberExpression m)
        {
            _builder.Append("CHAR_LENGTH(");
            _visitor.Visit(m.Expression);
            _builder.Append(')');

            return m;
        }

        /// <summary>
        /// 判断指定类型是否是unicode
        /// </summary>
        protected override bool IsUnicode(object dbType)
        {
            return DbTypeUtils.IsUnicode(dbType);
        }

        /// <summary>
        /// 访问 new Guid 方法
        /// </summary>
        protected override Expression VisitNewGuid(MethodCallExpression m)
        {
            _builder.Append("UUID()");
            return m;
        }

        #endregion
    }
}
