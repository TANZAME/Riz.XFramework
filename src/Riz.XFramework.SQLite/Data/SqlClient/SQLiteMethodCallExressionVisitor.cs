
using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Riz.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="SQLiteMethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    class SQLiteMethodCallExressionVisitor : MethodCallExpressionVisitor
    {
        private ISqlBuilder _builder = null;
        private SQLParser _funcletizer = null;
        private LinqExpressionVisitor _visitor = null;
        private MemberVisitedStack _visitedMark = null;
        private static TypeRuntimeInfo _typeRuntime = null;

        /// <summary>
        /// 运行时类成员
        /// </summary>
        protected override TypeRuntimeInfo TypeRuntime
        {
            get
            {
                if (_typeRuntime == null)
                    _typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(this.GetType(), true);
                return _typeRuntime;
            }
        }

        /// <summary>
        /// 实例化 <see cref="SQLiteMethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        public SQLiteMethodCallExressionVisitor(LinqExpressionVisitor visitor)
            : base(visitor)
        {
            _visitor = visitor;
            _builder = visitor.SqlBuilder;
            _visitedMark = _visitor.VisitedStack;
            _funcletizer = _builder.TranslateContext.DbContext.Provider.Funcletizer;
        }

        /// <summary>
        /// 访问表示 null 合并运算的节点 a ?? b
        /// </summary>
        /// <param name="b">二元表达式节点</param>
        protected override Expression VisitCoalesce(BinaryExpression b)
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
        /// <param name="node">即将访问的表达式</param>
        protected override Expression VisitToStringImpl(Expression node)
        {
            // => a.ID.ToString()
            // 字符串不进行转换
            if (node == null || node.Type == typeof(string)) return _visitor.Visit(node);

            _builder.Append("CAST(");
            _visitor.Visit(node);
            _builder.Append(" AS TEXT)");

            //// 其它类型转字符串
            //bool isDate =
            //    node.Type == typeof(TimeSpan) ||
            //    node.Type == typeof(TimeSpan?) ||
            //    node.Type == typeof(DateTime) ||
            //    node.Type == typeof(DateTime?) ||
            //    node.Type == typeof(DateTimeOffset) ||
            //    node.Type == typeof(DateTimeOffset?);
            //if (isDate)
            //{
            //    _builder.Append("TO_CHAR(");
            //    _visitor.Visit(node);

            //    string format = string.Empty;
            //    ColumnAttribute c = _provider.DbValue.GetColumnAttribute(_visitedMark.Current);
            //    if (c != null && DbTypeUtils.IsTime(c.DbType))
            //        format = "hh24:mi:ss.us";
            //    else if (c != null && DbTypeUtils.IsDate(c.DbType))
            //        format = "yyyy-mm-dd";
            //    else if (c != null && (DbTypeUtils.IsDateTime(c.DbType) || DbTypeUtils.IsDateTime2(c.DbType)))
            //        format = "yyyy-mm-dd hh24:mi:ss.us";
            //    else if (c != null && DbTypeUtils.IsDateTimeOffset(c.DbType))
            //        format = "yyyy-mm-dd hh24:mi:ss.us TZH:TZM";

            //    // 没有显式指定数据类型，则根据表达式的类型来判断
            //    if (string.IsNullOrEmpty(format))
            //    {
            //        if (node.Type == typeof(TimeSpan) || node.Type == typeof(TimeSpan?))
            //            format = "hh24:mi:ss.us";
            //        else if (node.Type == typeof(DateTime) || node.Type == typeof(DateTime?))
            //            format = "yyyy-mm-dd hh24:mi:ss.us";
            //        else if (node.Type == typeof(DateTimeOffset) || node.Type == typeof(DateTimeOffset?))
            //            format = "yyyy-mm-dd hh24:mi:ss.us TZH:TZM";
            //    }

            //    if (!string.IsNullOrEmpty(format))
            //    {
            //        _builder.Append(",'");
            //        _builder.Append(format);
            //        _builder.Append("'");
            //    }
            //    _builder.Append(')');
            //}
            //else if (node.Type == typeof(byte[]))
            //{
            //    _builder.Append("ENCODE(");
            //    _visitor.Visit(node);
            //    _builder.Append(",'hex')");
            //}
            //else
            //{
            //    _builder.Append("CAST(");
            //    _visitor.Visit(node);
            //    _builder.Append(" AS TEXT)");
            //}

            return node;
        }

        /// <summary>
        /// 访问 Contains 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitStringContains(MethodCallExpression m)
        {
            _visitor.Visit(m.Object);
            if (this.NotOperands != null && this.NotOperands.Contains(m)) _builder.Append(" NOT");
            _builder.Append(" LIKE ");
            if (m.Arguments[0].CanEvaluate())
            {
                ColumnAttribute column = null;
                bool isUnicode = DbTypeUtils.IsUnicode(_visitedMark.Current, out column);
                string value = _funcletizer.GetSqlValue(m.Arguments[0].Evaluate().Value, _builder.TranslateContext, column);
                if (!_builder.Parameterized && value != null) value = value.TrimStart('N').Trim('\'');

                if (_builder.Parameterized)
                {
                    _builder.Append("'%' || ");
                    _builder.Append(value);
                    _builder.Append(" || '%'");
                }
                else
                {
                    if (isUnicode) _builder.Append('N');
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
            return m;
        }

        /// <summary>
        /// 访问 StartWidth 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitStartsWith(MethodCallExpression m)
        {
            _visitor.Visit(m.Object);
            if (this.NotOperands != null && this.NotOperands.Contains(m)) _builder.Append(" NOT");
            _builder.Append(" LIKE ");
            if (m.Arguments[0].CanEvaluate())
            {
                ColumnAttribute column = null;
                bool isUnicode = DbTypeUtils.IsUnicode(_visitedMark.Current, out column);
                string value = _funcletizer.GetSqlValue(m.Arguments[0].Evaluate().Value, _builder.TranslateContext, column);
                if (!_builder.Parameterized && value != null) value = value.TrimStart('N').Trim('\'');

                if (_builder.Parameterized)
                {
                    _builder.Append(value);
                    _builder.Append(" || '%'");
                }
                else
                {
                    if (isUnicode) _builder.Append('N');
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
            return m;
        }

        /// <summary>
        /// 访问 EndWidth 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitEndsWith(MethodCallExpression m)
        {
            _visitor.Visit(m.Object);
            if (this.NotOperands != null && this.NotOperands.Contains(m)) _builder.Append(" NOT");
            _builder.Append(" LIKE ");
            if (m.Arguments[0].CanEvaluate())
            {
                ColumnAttribute column = null;
                bool isUnicode = DbTypeUtils.IsUnicode(_visitedMark.Current, out column);
                string value = _funcletizer.GetSqlValue(m.Arguments[0].Evaluate().Value, _builder.TranslateContext, column);
                if (!_builder.Parameterized && value != null) value = value.TrimStart('N').Trim('\'');

                if (_builder.Parameterized)
                {
                    _builder.Append("'%' || ");
                    _builder.Append(value);
                }
                else
                {
                    if (isUnicode) _builder.Append('N');
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
            return m;
        }

        /// <summary>
        /// 访问 TrimStart 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitSubstring(MethodCallExpression m)
        {
            List<Expression> args = new List<Expression>(m.Arguments);
            if (m.Object != null) args.Insert(0, m.Object);

            _builder.Append("SUBSTR(");
            _visitor.Visit(args[0]);
            _builder.Append(",");

            if (args[1].CanEvaluate())
            {
                ConstantExpression c = args[1].Evaluate();
                int index = Convert.ToInt32(c.Value);
                index += 1;
                string value = _funcletizer.GetSqlValue(index, _builder.TranslateContext);
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
            return m;
        }

        /// <summary>
        /// 访问 Concat 方法
        /// </summary>
        /// <param name="b">二元表达式</param>
        protected override Expression VisitConcat(BinaryExpression b)
        {
            _builder.Append("(");
            _visitor.Visit(b.Left);
            _builder.Append(" || ");
            _visitor.Visit(b.Right);
            _builder.Append(")");
            return b;
        }

        /// <summary>
        /// 访问 Concat 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitConcat(MethodCallExpression m)
        {
            IList<Expression> expressions = null;
            if (m.Arguments.Count > 1) expressions = m.Arguments;
            else if (m.Arguments.Count == 1 && m.Arguments[0].NodeType == ExpressionType.NewArrayInit)
            {
                expressions = (m.Arguments[0] as NewArrayExpression).Expressions;
            }

            if (expressions == null) _visitor.Visit(m.Arguments[0]);
            else
            {
                _builder.Append("(");
                for (int i = 0; i < expressions.Count; i++)
                {
                    _visitor.VisitWithoutRemark(_ => this.VisitToStringImpl(expressions[i]));
                    if (i < expressions.Count - 1) _builder.Append(" || ");
                }
                _builder.Append(")");
            }
            return m;
        }

        /// <summary>
        /// 访问 IsNullOrEmpty 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitIsNullOrEmpty(MethodCallExpression m)
        {
            _builder.Append("IFNULL(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",'') = ''");
            return m;
        }

        /// <summary>
        /// 访问 IndexOf 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitIndexOf(MethodCallExpression m)
        {
            _builder.Append("(INSTR(");
            _visitor.Visit(m.Object);
            _builder.Append(',');
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(") - 1)");
            return m;
        }

        /// <summary>
        /// 访问 TrimEnd 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitLength(MemberExpression m)
        {
            _builder.Append("LENGTH(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");

            return m;
        }

        /// <summary>
        /// 访问 Math.Truncate 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitTruncate(MethodCallExpression m)
        {
            if (m != null)
            {
                _builder.Append("TRUNC(");
                _visitor.Visit(m.Arguments[0]);
                _builder.Append(",0)");
            }

            return m;
        }

        /// <summary>
        /// 访问 DateTime.Now 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitNow(MemberExpression m)
        {
            _builder.Append("STRFTIME('%Y-%m-%d %H:%M:%f',CURRENT_TIMESTAMP,'LOCALTIME')");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Today 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitToday(MemberExpression m)
        {
            _builder.Append("DATE('NOW','LOCALTIME')");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Now 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitUtcNow(MemberExpression m)
        {
            _builder.Append("STRFTIME('%Y-%m-%d %H:%M:%f',CURRENT_TIMESTAMP)");
            return m;
        }

        /// <summary>
        /// 访问 new Guid 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitNewGuid(MethodCallExpression m)
        {
            System.Guid guid = System.Guid.NewGuid();
            _builder.Append(guid, null);
            return m;
        }

        /// <summary>
        /// 访问 IDbQueryable.Contains 方法
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitQueryableContains(MethodCallExpression m)
        {
            ITranslateContext context = _builder.TranslateContext;
            IDbQueryable subquery = m.Arguments[0].Evaluate().Value as IDbQueryable;
            subquery.Parameterized = _builder.Parameterized;

            var clone = context != null ? context.Clone("s") : null;
            bool isDelete = context != null && ((SQLiteTranslateContext)context).IsDelete;
            var cmd = subquery.Translate(_builder.Indent + 1, false, clone) as DbSelectCommand;

            if (this.NotOperands != null && this.NotOperands.Contains(m)) _builder.Append("NOT ");
            _builder.Append("EXISTS(");
            _builder.Append(cmd.CommandText);

            if (((DbSelectCommand)cmd).WhereFragment.Length > 0)
                _builder.Append(" AND ");
            else
                _builder.Append("WHERE ");
            _builder.Append(cmd.PickColumnText);
            _builder.Append(" = ");

            // exists 不能用别名
            if (isDelete)
            {
                var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(((MemberExpression)m.Arguments[1]).Expression.Type);
                _builder.AppendMember(typeRuntime.TableName);
                _builder.Append('.');
            }

            _visitor.Visit(m.Arguments[1]);
            _builder.Append(")");

            return m;
        }
    }
}
