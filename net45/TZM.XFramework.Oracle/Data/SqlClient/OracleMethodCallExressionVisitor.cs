
using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="OracleMethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    internal class OracleMethodCallExressionVisitor : MethodCallExpressionVisitor
    {
        private ISqlBuilder _builder = null;
        private IDbQueryProvider _provider = null;
        private ExpressionVisitorBase _visitor = null;
        private MemberVisitedMark _visitedMark = null;
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

        #region 构造函数

        /// <summary>
        /// 实例化 <see cref="OracleMethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="provider">查询语义提供者</param>
        /// <param name="visitor">表达式访问器</param>
        public OracleMethodCallExressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
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
        /// <param name="b">二元表达式节点</param>
        /// <returns></returns>
        protected override Expression VisitCoalesce(BinaryExpression b)
        {
            // 例： a.Name ?? "TAN" => ISNULL(a.Name,'TAN')
            Expression left = b.Left.NodeType == ExpressionType.Constant ? b.Right : b.Left;
            Expression right = b.Left.NodeType == ExpressionType.Constant ? b.Left : b.Right;

            _builder.Append("NVL(");
            _visitor.Visit(left);
            _builder.Append(",");
            _visitor.Visit(right);
            _builder.Append(')');

            return b;
        }

        /// <summary>
        /// 访问一元运算符
        /// </summary>
        /// <param name="node">一元运算符表达式</param>
        /// <returns></returns>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            // ORACLE的AVG函数，DataReader.GetValue()会抛异常
            // Number having precision p and scale s.The precision p can range from 1 to 38.The scale s can range from - 84 to 127.
            // Both precision and scale are in decimal digits. A NUMBER value requires from 1 to 22 bytes.
            // .net 的 decimal scale <= 28,will raise overflow error.
            // 79228162514264337593543950335
            if (node.NodeType == ExpressionType.Convert)
            {
                string name = "";
                if (node.Type == typeof(float)) name = "BINARY_FLOAT";
                else if (node.Type == typeof(double) || node.Type == typeof(decimal)) name = "BINARY_DOUBLE";
                if (!string.IsNullOrEmpty(name))
                {
                    _builder.Append("CAST(");
                    _visitor.Visit(node.Operand);
                    _builder.Append(" AS ");
                    _builder.Append(name);
                    _builder.Append(')');
                    return node;
                }
            }

            return base.VisitUnary(node);
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

            ColumnAttribute column = null;
            bool isUnicode = _provider.DbValue.IsUnicode(_visitedMark.Current, out column);
            bool isBytes = node.Type == typeof(byte[]);
            bool isDate = node.Type == typeof(DateTime) ||
                node.Type == typeof(DateTime?) ||
                node.Type == typeof(TimeSpan) ||
                node.Type == typeof(TimeSpan?) ||
                node.Type == typeof(DateTimeOffset) ||
                node.Type == typeof(DateTimeOffset?);

            if (!isBytes)
            {
                if (isUnicode)
                    _builder.Append("TO_NCHAR(");
                else
                    _builder.Append("TO_CHAR(");
            }

            // 其它类型转字符串
            if (isDate)
            {
                _visitor.Visit(node);

                string format = string.Empty;
                ColumnAttribute c = _provider.DbValue.GetColumnAttribute(_visitedMark.Current);
                if (c != null && DbTypeUtils.IsDate(c.DbType))
                    format = "yyyy-mm-dd";
                else if (c != null && (DbTypeUtils.IsDateTime(c.DbType) || DbTypeUtils.IsDateTime2(c.DbType)))
                    format = "yyyy-mm-dd hh24:mi:ss.ff";
                else if (c != null && DbTypeUtils.IsDateTimeOffset(c.DbType))
                    format = "yyyy-mm-dd hh24:mi:ss.ff tzh:tzm";

                // 没有显式指定数据类型，则根据表达式的类型来判断
                if (string.IsNullOrEmpty(format))
                {
                    if (node.Type == typeof(DateTime) || node.Type == typeof(DateTime?))
                        format = "yyyy-mm-dd hh24:mi:ss.ff";
                    else if (node.Type == typeof(DateTimeOffset) || node.Type == typeof(DateTimeOffset?))
                        format = "yyyy-mm-dd hh24:mi:ss.ff tzh:tzm";
                }

                if (!string.IsNullOrEmpty(format))
                {
                    _builder.Append(",'");
                    _builder.Append(format);
                    _builder.Append("'");
                }

            }
            else if (isBytes)
            {
                _builder.Append("RTRIM(DBMS_LOB.SUBSTR(");
                _visitor.Visit(node);
                _builder.Append(')');
            }
            else if (node.Type == typeof(Guid))
            {
                _builder.Append("REGEXP_REPLACE(REGEXP_REPLACE(");
                _visitor.Visit(node);
                _builder.Append(@",'(.{8})(.{4})(.{4})(.{4})(.{12})', '\1-\2-\3-\4-\5'),'(.{2})(.{2})(.{2})(.{2}).(.{2})(.{2}).(.{2})(.{2})(.{18})','\4\3\2\1-\6\5-\8\7\9')");
            }
            else
            {
                _visitor.Visit(node);
            }

            _builder.Append(')');
            return node;
        }

        /// <summary>
        /// 访问 string.Contains 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitStringContains(MethodCallExpression m)
        {
            _visitor.Visit(m.Object);
            if (this.NotMethods.Contains(m)) _builder.Append(" NOT");
            _builder.Append(" LIKE ");
            if (m.Arguments[0].CanEvaluate())
            {
                bool unicode = true;
                string value = this.GetSqlValue(m.Arguments[0].Evaluate(), ref unicode);

                if (_builder.Parameterized)
                {
                    _builder.Append("('%' || ");
                    _builder.Append(value);
                    _builder.Append(" || '%')");
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
            return m;
        }

        /// <summary>
        /// 访问 StartWidth 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitStartsWith(MethodCallExpression m)
        {
            _visitor.Visit(m.Object);
            if (this.NotMethods.Contains(m)) _builder.Append(" NOT");
            _builder.Append(" LIKE ");
            if (m.Arguments[0].CanEvaluate())
            {
                bool unicode = true;
                string value = this.GetSqlValue(m.Arguments[0].Evaluate(), ref unicode);

                if (_builder.Parameterized)
                {
                    _builder.Append("(");
                    _builder.Append(value);
                    _builder.Append(" || '%')");
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
            return m;
        }

        /// <summary>
        /// 访问 EndWidth 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitEndsWith(MethodCallExpression m)
        {
            _visitor.Visit(m.Object);
            if (this.NotMethods.Contains(m)) _builder.Append(" NOT");
            _builder.Append(" LIKE ");
            if (m.Arguments[0].CanEvaluate())
            {
                bool unicode = true;
                string value = this.GetSqlValue(m.Arguments[0].Evaluate(), ref unicode);

                if (_builder.Parameterized)
                {
                    _builder.Append("('%' || ");
                    _builder.Append(value);
                    _builder.Append(")");
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
            return m;
        }

        /// <summary>
        /// 访问 SubString 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitSubstring(MethodCallExpression m)
        {
            var expressions = new List<Expression>(m.Arguments);
            if (m.Object != null) expressions.Insert(0, m.Object);

            _builder.Append("SUBSTR(");
            _visitor.Visit(expressions[0]);
            _builder.Append(",");

            if (expressions[1].CanEvaluate())
            {
                var c = expressions[1].Evaluate();
                int index = Convert.ToInt32(c.Value);
                index += 1;
                _builder.Append(index, null);
                _builder.Append(",");
            }
            else
            {
                _visitor.Visit(expressions[1]);
                _builder.Append(" + 1,");
            }

            if (expressions.Count == 3)
            {
                // 带2个参数，Substring(n,n)
                if (expressions[2].CanEvaluate())
                    _builder.Append(expressions[2].Evaluate().Value, null);
                else
                    _visitor.Visit(expressions[2]);
            }
            else
            {
                // 带1个参数，Substring(n)
                _builder.Append("LENGTH(");
                _visitor.Visit(expressions[0]);
                _builder.Append(")");
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
                    this.VisitToStringImpl(expressions[i]);
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
            _builder.Append("NVL(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",'') = ''");
            return m;
        }

        /// <summary>
        /// 访问 Length 属性
        /// </summary>
        /// <param name="m">字段或者属性表达式</param>
        protected override Expression VisitLength(MemberExpression m)
        {
            _builder.Append("LENGTH(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
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
            _builder.Append(",");
            _visitor.Visit(m.Arguments[0]);

            if (m.Arguments.Count > 1 && m.Arguments[1].Type != typeof(StringComparison))
            {
                _builder.Append(",");
                if (m.Arguments[1].CanEvaluate())
                {
                    var c = m.Arguments[1].Evaluate();
                    int index = Convert.ToInt32(c.Value) + 1;
                    _builder.Append(index, null);
                }
                else
                {
                    _visitor.Visit(m.Arguments[1]);
                    _builder.Append(" + 1");
                }
            }

            _builder.Append(",1) - 1)");
            return m;
        }

        /// <summary>
        /// 访问 % 方法
        /// </summary>
        /// <param name="b">二元表达式</param>
        protected override Expression VisitModulo(BinaryExpression b)
        {
            Expression left = b.Left.CanEvaluate() ? b.Right : b.Left;
            Expression right = b.Left.CanEvaluate() ? b.Left : b.Right;

            _builder.Append("MOD(");
            _visitor.Visit(left);
            _builder.Append(",");
            _visitor.Visit(right);
            _builder.Append(')');
            return b;
        }

        /// <summary>
        /// 访问 Math.Ceiling 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitCeiling(MethodCallExpression m)
        {
            _builder.Append("CEIL(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Log 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitLog(MethodCallExpression m)
        {
            // LOG returns the logarithm,base n2,of n1.
            if (m.Arguments.Count == 1)
            {
                // 自然对数
                _builder.Append("LN(");
                _visitor.Visit(m.Arguments[0]);
                _builder.Append(')');
            }
            else
            {
                // 指定基数
                _builder.Append("LOG(");
                _visitor.Visit(m.Arguments[1]);
                _builder.Append(",");
                _visitor.Visit(m.Arguments[0]);
                _builder.Append(')');
            }
            return m;
        }

        /// <summary>
        /// 访问 Math.Log10 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitLog10(MethodCallExpression m)
        {
            _builder.Append("LOG(10,");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Atan2 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitAtan2(MethodCallExpression m)
        {
            _builder.Append("ATAN2(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");
            _visitor.Visit(m.Arguments[1]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Truncate 方法
        /// </summary>
        /// <param name="b">方法表达式</param>
        protected override Expression VisitTruncate(MethodCallExpression b)
        {
            _builder.Append("TRUNC(");
            _visitor.Visit(b.Arguments[0]);
            _builder.Append(",0)");
            return b;
        }

        /// <summary>
        /// 访问 DateTime.Now 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitNow(MemberExpression m)
        {
            _builder.Append("SYSTIMESTAMP");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Now 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitUtcNow(MemberExpression m)
        {
            _builder.Append("SYS_EXTRACT_UTC(SYSTIMESTAMP)");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Date 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitDate(MemberExpression m)
        {
            _builder.Append("TRUNC(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.DayOfWeek 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitDayOfWeek(MemberExpression m)
        {
            // select to_char(sysdate,'D') from dual;
            _builder.Append("(TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(",'D')) - 1)");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.DayOfYear 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitDayOfYear(MemberExpression m)
        {
            // https://www.techonthenet.com/oracle/functions/to_char.php
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(",'DDD'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.VisitYear 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitYear(MemberExpression m)
        {
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(",'YYYY'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Month 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitMonth(MemberExpression m)
        {
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(",'MM'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Day 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitDay(MemberExpression m)
        {
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(",'DD'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Hour 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitHour(MemberExpression m)
        {
            //  参考 EXTRACT：https://docs.oracle.com/en/database/oracle/oracle-database/12.2/sqlrf/EXTRACT-datetime.html#GUID-36E52BF8-945D-437D-9A3C-6860CABD210E

            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(",'HH24'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Minute 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitMinute(MemberExpression m)
        {
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(",'MI'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Second 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitSecond(MemberExpression m)
        {
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(",'SS'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Millisecond 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitMillisecond(MemberExpression m)
        {
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(",'FF3'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Ticks 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitTicks(MemberExpression m)
        {
            _builder.Append("((");
            // 年份
            _builder.Append("(TRUNC(");
            _visitor.Visit(m.Expression);
            _builder.Append(") - TO_DATE('1970-01-01','yyyy-mm-dd')) * 86400000 + ");
            // 时
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(",'hh24')) * 3600000 + ");
            // 分
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(",'mi')) * 60000 + ");
            // 秒
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(",'ss')) * 1000) * 10000 + ");
            // 微
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(",'ff7')) + 621355968000000000)");

            return m;
        }

        /// <summary>
        /// 访问 DateTime.TimeOfDay 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitTimeOfDay(MemberExpression m)
        {
            _builder.Append("TO_DSINTERVAL('0 '|| TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(",'HH24:MI:SS.FF7'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.DaysInMonth 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitDaysInMonth(MethodCallExpression m)
        {
            // SELECT EXTRACT(DAY FROM LAST_DAY(TO_DATE(TO_CHAR(2019) || '-' || TO_CHAR(10) || '-1','YYYY-MM-DD')))  FROM DUAL;
            _builder.Append("EXTRACT(DAY FROM LAST_DAY(TO_DATE(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(" || '-' || ");
            _visitor.Visit(m.Arguments[1]);
            _builder.Append(" || '-1','YYYY-MM-DD')))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.IsLeapYear 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitIsLeapYear(MethodCallExpression m)
        {
            _builder.Append("(MOD(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",4) = 0 AND MOD(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",100) <> 0 OR MOD(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",400) = 0)");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.VisitAddYears 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitAddYears(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + NUMTOYMINTERVAL(");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(",'YEAR'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddMonths 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitAddMonths(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + NUMTOYMINTERVAL(");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(",'MONTH'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddDays 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitAddDays(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + NUMTODSINTERVAL(");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(",'DAY'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddDays 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitAddHours(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + NUMTODSINTERVAL(");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(",'HOUR'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddMinutes 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitAddMinutes(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + NUMTODSINTERVAL(");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(",'MINUTE'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddSeconds 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitAddSeconds(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + NUMTODSINTERVAL(");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(",'SECOND'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddMilliseconds 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitAddMilliseconds(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + NUMTODSINTERVAL(");
            if (m.Arguments[0].CanEvaluate()) _builder.Append(Convert.ToDouble(m.Arguments[0].Evaluate().Value) / 1000.00, null);
            else
            {
                if (m.Arguments[0].NodeType != ExpressionType.MemberAccess) _builder.Append("(");
                _visitor.Visit(m.Arguments[0]);
                if (m.Arguments[0].NodeType != ExpressionType.MemberAccess) _builder.Append(")");
                _builder.Append(" / 1000.00");
            }
            _builder.Append(",'SECOND'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddTicks 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitAddTicks(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + NUMTODSINTERVAL(");
            if (m.Arguments[0].CanEvaluate()) _builder.Append(Convert.ToDecimal(m.Arguments[0].Evaluate().Value) / 10000000.00m, null);
            else
            {
                if (m.Arguments[0].NodeType != ExpressionType.MemberAccess) _builder.Append("(");
                _visitor.Visit(m.Arguments[0]);
                if (m.Arguments[0].NodeType != ExpressionType.MemberAccess) _builder.Append(")");
                _builder.Append(" / 10000000.00");
            }
            _builder.Append(",'SECOND'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Today 属性
        /// </summary>
        /// <param name="m">字段或属性表达式</param>
        protected override Expression VisitToday(MemberExpression m)
        {
            _builder.Append("TRUNC(SYSDATE)");
            return m;
        }

        /// <summary>
        /// 访问 new Guid 方法
        /// </summary>
        /// <param name="m">方法表达式</param>
        protected override Expression VisitNewGuid(MethodCallExpression m)
        {
            _builder.Append("SYS_GUID()");
            return m;
        }

        #endregion
    }
}
