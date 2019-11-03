
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="NpgMethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    public class NpgMethodCallExressionVisitor : MethodCallExpressionVisitor
    {
        private ISqlBuilder _builder = null;
        private IDbQueryProvider _provider = null;
        private ExpressionVisitorBase _visitor = null;
        private MemberVisitedMark _visitedMark = null;

        #region 构造函数

        /// <summary>
        /// 实例化 <see cref="NpgMethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        public NpgMethodCallExressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
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
        protected override Expression VisitCoalesce(BinaryExpression b)
        {
            // 例： a.Name ?? "TAN" => ISNULL(a.Name,'TAN')

            _builder.Append("COALESCE(");
            _visitor.Visit(b.Left is ConstantExpression ? b.Right : b.Left);
            _builder.Append(", ");
            _visitor.Visit(b.Left is ConstantExpression ? b.Left : b.Right);
            _builder.Append(")");


            return b;
        }

        /// <summary>
        /// 访问 ToString 方法
        /// </summary>
        protected override Expression VisitToString(MethodCallExpression m)
        {
            // => a.ID.ToString()
            Expression node = null;
            if (m.Object != null) node = m.Object;
            else if (m.Arguments != null && m.Arguments.Count > 0) node = m.Arguments[0];
            // 字符串不进行转换
            if (node == null || node.Type == typeof(string)) return _visitor.Visit(node);

            if (node != null && node.Type == typeof(DateTime))
            {
                _builder.Append("TO_CHAR(");
                _visitor.Visit(node);
                _builder.Append(", 'yyyy-mm-dd hh24:mi:ss.us')");
            }
            else
            {
                _builder.Append("CAST(");
                _visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
                _builder.Append(" AS VARCHAR)");
            }

            return m;
        }

        /// <summary>
        /// 访问 Contains 方法
        /// </summary>
        protected override Expression VisitStringContains(MethodCallExpression m)
        {
            _visitor.Visit(m.Object);
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
        protected override Expression VisitStartsWith(MethodCallExpression m)
        {
            _visitor.Visit(m.Object);
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
            }

            return m;
        }

        /// <summary>
        /// 访问 TrimStart 方法
        /// </summary>
        protected override Expression VisitSubstring(MethodCallExpression m)
        {
            var expressions = new List<Expression>(m.Arguments);
            if (m.Object != null) expressions.Insert(0, m.Object);

            _builder.Append("SUBSTRING(");
            _visitor.Visit(expressions[0]);
            _builder.Append(", ");

            if (expressions[1].CanEvaluate())
            {
                var c = expressions[1].Evaluate();
                int index = Convert.ToInt32(c.Value);
                index += 1;
                _builder.Append(index, null);
                _builder.Append(", ");
            }
            else
            {
                _visitor.Visit(expressions[1]);
                _builder.Append(" + 1, ");
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
        protected override Expression VisitConcat(MethodCallExpression m)
        {
            if (m.Arguments.Count == 1) _visitor.Visit(m.Arguments[0]);
            else
            {
                _builder.Append("(");
                for (int i = 0; i < m.Arguments.Count; i++)
                {
                    _visitor.Visit(m.Arguments[i]);
                    if (i < m.Arguments.Count - 1) _builder.Append(" || ");
                }
                _builder.Append(")");
            }
            return m;
        }

        /// <summary>
        /// 访问 TrimEnd 方法
        /// </summary>
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
        protected override Expression VisitIndexOf(MethodCallExpression m)
        {
            _builder.Append("(STRPOS(");
            _visitor.Visit(m.Object);
            _builder.Append(',');
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(") - 1)");
            return m;
        }

        /// <summary>
        /// 访问 Math.Truncate 方法
        /// </summary>
        protected override Expression VisitTruncate(MethodCallExpression b)
        {
            _builder.Append("TRUNC(");
            _visitor.Visit(b.Arguments[0]);
            _builder.Append(", 0)");
            return b;
        }

        /// <summary>
        /// 访问 Math.Log 方法
        /// </summary>
        protected override Expression VisitLog(MethodCallExpression m)
        {
            //log(b numeric, x numeric)   logarithm to base b
            if (m.Arguments.Count == 1)
            {
                _builder.Append("LN(");
                _visitor.Visit(m.Arguments[0]);
            }
            else
            {
                // 指定基数
                _builder.Append("LOG(");
                _visitor.Visit(m.Arguments[1]);
                _builder.Append(", ");
                _visitor.Visit(m.Arguments[0]);
            }

            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Log 方法
        /// </summary>
        protected override Expression VisitLog10(MethodCallExpression m)
        {
            //log(b numeric, x numeric)   logarithm to base b
            _builder.Append("LOG(10, ");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Atan2 方法
        /// </summary>
        protected override Expression VisitAtan2(MethodCallExpression b)
        {
            _builder.Append("ATAN2(");
            _visitor.Visit(b.Arguments[0]);
            _builder.Append(", ");
            _visitor.Visit(b.Arguments[1]);
            _builder.Append(')');
            return b;
        }

        /// <summary>
        /// 访问 DateTime.Now 属性
        /// </summary>
        protected override Expression VisitNow(MemberExpression m)
        {
            // LOCALTIMESTAMP deliver values without time zone.
            _builder.Append("LOCALTIMESTAMP");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Today 属性
        /// </summary>
        protected override Expression VisitToday(MemberExpression m)
        {
            _builder.Append("DATE_TRUNC('DAY', LOCALTIMESTAMP)");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Now 属性
        /// </summary>
        protected override Expression VisitUtcNow(MemberExpression m)
        {
            _builder.Append("(CURRENT_TIMESTAMP AT TIME ZONE 'UTC')");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Date 属性
        /// </summary>
        protected override Expression VisitDate(MemberExpression m)
        {
            _builder.Append("DATE_TRUNC('DAY', ");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Day 属性
        /// </summary>
        protected override Expression VisitDay(MemberExpression m)
        {
            _builder.Append("DATE_PART('DAY', ");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.DayOfWeek 属性
        /// </summary>
        protected override Expression VisitDayOfWeek(MemberExpression m)
        {
            _builder.Append("(DATE_PART('DOW', ");
            _visitor.Visit(m.Expression);
            _builder.Append(") + 1)");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.DayOfYear 属性
        /// </summary>
        protected override Expression VisitDayOfYear(MemberExpression m)
        {
            _builder.Append("DATE_PART('DOY', ");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Hour 属性
        /// </summary>
        protected override Expression VisitHour(MemberExpression m)
        {
            _builder.Append("DATE_PART('HOUR', ");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Millisecond 属性
        /// </summary>
        protected override Expression VisitMillisecond(MemberExpression m)
        {
            // EXTRACT(MILLISECONDS FROM A::TIMESTAMP)-EXTRACT(SECOND FROM A::TIMESTAMP)*1000
            _builder.Append("(DATE_PART('MILLISECONDS', ");
            _visitor.Visit(m.Expression);
            _builder.Append(") - DATE_PART('SECOND', ");
            _visitor.Visit(m.Expression);
            _builder.Append(") * 1000)");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Minute 属性
        /// </summary>
        protected override Expression VisitMinute(MemberExpression m)
        {
            _builder.Append("DATE_PART('MINUTE', ");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Month 属性
        /// </summary>
        protected override Expression VisitMonth(MemberExpression m)
        {
            _builder.Append("DATE_PART('MONTH', ");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Second 属性
        /// </summary>
        protected override Expression VisitSecond(MemberExpression m)
        {
            _builder.Append("DATE_PART('SECOND', ");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Ticks 属性
        /// </summary>
        protected override Expression VisitTicks(MemberExpression m)
        {
            _builder.Append("(");
            // 年份
            _builder.Append("(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(", 'yyyy-mm-dd')::DATE - '1970-01-01'::DATE + 693595) * CAST(24 AS NUMERIC) * 3600 * 1000 + ");
            // 时
            _builder.Append("DATE_PART('HOUR', ");
            _visitor.Visit(m.Expression);
            _builder.Append(") * 3600000 + ");
            // 分
            _builder.Append("DATE_PART('MINUTE', ");
            _visitor.Visit(m.Expression);
            _builder.Append(") * 60000 + ");
            // 毫秒
            _builder.Append("DATE_PART('MILLISECOND', ");
            _visitor.Visit(m.Expression);
            _builder.Append(")) * 10000 ");

            return m;
        }

        /// <summary>
        /// 访问 DateTime.TimeOfDay 属性
        /// </summary>
        protected override Expression VisitTimeOfDay(MemberExpression m)
        {
            _visitor.Visit(m.Expression);
            _builder.Append("::TIMESTAMP");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.TimeOfDay 属性
        /// </summary>
        protected override Expression VisitYear(MemberExpression m)
        {
            _builder.Append("DATE_PART('YEAR', ");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.DaysInMonth 方法
        /// </summary>
        protected override Expression VisitDaysInMonth(MethodCallExpression m)
        {
            //  DATE_PART('days', DATE_TRUNC('month', '2019-01-01'::DATE) + '1 MONTH'::INTERVAL  - '1 DAY'::INTERVAL)
            _builder.Append("DATE_PART('DAYS', DATE_TRUNC('MONTH',(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(" || '-' || ");
            _visitor.Visit(m.Arguments[1]);
            _builder.Append(" || '-1')::DATE) + '1 MONTH'::INTERVAL  - '1 DAY'::INTERVAL)");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddTicks 方法
        /// </summary>
        protected override Expression VisitAddYears(MethodCallExpression m)
        {
            return this.VisitAddDateTime(m, "YEAR");
        }

        /// <summary>
        /// 访问 DateTime.AddMonths 方法
        /// </summary>
        protected override Expression VisitAddMonths(MethodCallExpression m)
        {
            return this.VisitAddDateTime(m, "MONTH");
        }

        /// <summary>
        /// 访问 DateTime.AddDays 方法
        /// </summary>
        protected override Expression VisitAddDays(MethodCallExpression m)
        {
            return this.VisitAddDateTime(m, "DAY");
        }

        /// <summary>
        /// 访问 DateTime.AddDays 方法
        /// </summary>
        protected override Expression VisitAddHours(MethodCallExpression m)
        {
            return this.VisitAddDateTime(m, "HOUR");
        }

        /// <summary>
        /// 访问 DateTime.AddMinutes 方法
        /// </summary>
        protected override Expression VisitAddMinutes(MethodCallExpression m)
        {
            return this.VisitAddDateTime(m, "MINUTE");
        }

        /// <summary>
        /// 访问 DateTime.AddSeconds 方法
        /// </summary>
        protected override Expression VisitAddSeconds(MethodCallExpression m)
        {
            return this.VisitAddDateTime(m, "SECOND");
        }

        /// <summary>
        /// 访问 DateTime.AddMilliseconds 方法
        /// </summary>
        protected override Expression VisitAddMilliseconds(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + ");
            if (m.Arguments[0].CanEvaluate())
            {
                double obj = Convert.ToDouble(m.Arguments[0].Evaluate().Value) / 1000;
                if (_builder.Parameterized)
                {
                    _builder.Append("(");
                    _builder.Append(obj, null);
                    _builder.Append(" || ' SECOND')");
                }
                else
                {
                    _builder.Append("'");
                    _builder.Append(obj, null);
                    _builder.Append(" SECOND'");
                }
            }
            else
            {
                _builder.Append("(");
                _visitor.Visit(m.Arguments[0]);
                _builder.Append(" / 1000 || ' SECOND')");

            }
            _builder.Append("::INTERVAL)");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddTicks 方法
        /// </summary>
        protected override Expression VisitAddTicks(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + ");
            if (m.Arguments[0].CanEvaluate())
            {
                double obj = Convert.ToDouble(m.Arguments[0].Evaluate().Value) / 10;
                if (_builder.Parameterized)
                {
                    _builder.Append("(");
                    _builder.Append(obj, null);
                    _builder.Append(" || ' MICROSECOND')");
                }
                else
                {
                    _builder.Append("'");
                    _builder.Append(obj, null);
                    _builder.Append(" MICROSECOND'");
                }
            }
            else
            {
                _builder.Append("(");
                _visitor.Visit(m.Arguments[0]);
                _builder.Append(" / 10 || ' MICROSECOND')");

            }
            _builder.Append("::INTERVAL)");
            return m;
        }

        /// <summary>
        /// 访问 new Guid 方法
        /// </summary>
        protected override Expression VisitNewGuid(MethodCallExpression m)
        {
            System.Guid guid = System.Guid.NewGuid();
            _builder.Append(guid, null);
            return m;
        }

        // 访问时间相加
        Expression VisitAddDateTime(MethodCallExpression m, string interval)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + ");
            if (m.Arguments[0].CanEvaluate())
            {
                object obj = m.Arguments[0].Evaluate().Value;
                if (_builder.Parameterized)
                {
                    _builder.Append("(");
                    _builder.Append(obj, null);
                    _builder.Append(" || ' ");
                    _builder.Append(interval);
                    _builder.Append("')");
                }
                else
                {
                    _builder.Append("'");
                    _builder.Append(obj, null);
                    _builder.Append(" ");
                    _builder.Append(interval);
                    _builder.Append("'");
                }
            }
            else
            {
                _builder.Append("(");
                _visitor.Visit(m.Arguments[0]);
                _builder.Append(" || ' ");
                _builder.Append(interval);
                _builder.Append("')");

            }
            _builder.Append("::INTERVAL)");
            return m;
        }

        #endregion
    }
}
