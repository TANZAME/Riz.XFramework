
using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="OracleMethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    public class OracleMethodCallExressionVisitor : MethodCallExressionVisitor
    {
        private ISqlBuilder _builder = null;
        private IDbQueryProvider _provider = null;
        private ExpressionVisitorBase _visitor = null;
        private MemberVisitedMark _visitedMark = null;

        #region 构造函数

        /// <summary>
        /// 实例化 <see cref="OracleMethodCallExressionVisitor"/> 类的新实例
        /// </summary>
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
        protected override Expression VisitCoalesce(BinaryExpression b)
        {
            // 例： a.Name ?? "TAN" => ISNULL(a.Name,'TAN')
            Expression left = b.Left.NodeType == ExpressionType.Constant ? b.Right : b.Left;
            Expression right = b.Left.NodeType == ExpressionType.Constant ? b.Left : b.Right;

            _builder.Append("NVL(");
            _visitor.Visit(left);
            _builder.Append(", ");
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
            // .net 的 decimal scale <= 28, will raise overflow error.
            // 79228162514264337593543950335
            if (node.NodeType == ExpressionType.Convert)
            {
                string name = "";
                if (node.Type == typeof(float)) name = "BINARY_FLOAT";
                else if (node.Type == typeof(double)|| node.Type == typeof(decimal)) name = "BINARY_DOUBLE";
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
                _builder.Append("DATE_FORMAT(");
                _visitor.Visit(node);
                _builder.Append(", '%Y-%M-%D %H:%I:%S.%F')");
            }
            else
            {
                _builder.Append("TO_CHAR(");
                _visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
                _builder.Append(")");
            }

            return m;
        }

        /// <summary>
        /// 访问 string.Contains 方法
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
            return m;
        }

        /// <summary>
        /// 访问 SubString 方法
        /// </summary>
        protected override Expression VisitSubstring(MethodCallExpression m)
        {
            var expressions = new List<Expression>(m.Arguments);
            if (m.Object != null) expressions.Insert(0, m.Object);

            _builder.Append("SUBSTR(");
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
        /// 访问 Length 属性
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
            _builder.Append("(INSTR(");
            _visitor.Visit(m.Object);
            _builder.Append(", ");
            _visitor.Visit(m.Arguments[0]);

            if (m.Arguments.Count > 1 && m.Arguments[1].Type != typeof(StringComparison))
            {
                _builder.Append(", ");
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

            _builder.Append(", 1) - 1)");
            return m;
        }

        /// <summary>
        /// 访问 % 方法
        /// </summary>
        protected override Expression VisitModulo(BinaryExpression b)
        {
            _builder.Append("MOD(");
            _visitor.Visit(b.Left);
            _builder.Append(", ");
            _visitor.Visit(b.Right);
            _builder.Append(')');
            return b;
        }

        /// <summary>
        /// 访问 Math.Ceiling 方法
        /// </summary>
        protected override Expression VisitCeiling(MethodCallExpression b)
        {
            _builder.Append("CEIL(");
            _visitor.Visit(b.Arguments[0]);
            _builder.Append(')');
            return b;
        }

        /// <summary>
        /// 访问 Math.Log 方法
        /// </summary>
        protected override Expression VisitLog(MethodCallExpression b)
        {
            _builder.Append("LOG(");
            _builder.Append(Math.E, null);
            _builder.Append(", ");
            _visitor.Visit(b.Arguments[0]);
            _builder.Append(')');
            return b;
        }

        /// <summary>
        /// 访问 Math.Log10 方法
        /// </summary>
        protected override Expression VisitLog10(MethodCallExpression b)
        {
            _builder.Append("LOG(10, ");
            _visitor.Visit(b.Arguments[0]);
            _builder.Append(')');
            return b;
        }

        /// <summary>
        /// 访问 Math.Atan2 方法
        /// </summary>
        protected override Expression VisitAtan2(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("ATAN2(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(", ");
                _visitor.Visit(b.Arguments[1]);
                _builder.Append(')');
            }
            
            return b;
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
        /// 访问 DateTime.Now 属性
        /// </summary>
        protected override Expression VisitNow(MemberExpression m)
        {
            _builder.Append("SYSDATE");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Now 属性
        /// </summary>
        protected override Expression VisitUtcNow(MemberExpression m)
        {
            _builder.Append("SYS_EXTRACT_UTC(SYSTIMESTAMP)");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Date 属性
        /// </summary>
        protected override Expression VisitDate(MemberExpression m)
        {
            _builder.Append("TRUNC(SYSDATE)");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Day 属性
        /// </summary>
        protected override Expression VisitDay(MemberExpression m)
        {
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(", 'DD'))");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.DayOfWeek 属性
        /// </summary>
        protected override Expression VisitDayOfWeek(MemberExpression m)
        {
            // select to_char(sysdate,'D') from dual;
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(", 'D'))");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.DayOfYear 属性
        /// </summary>
        protected override Expression VisitDayOfYear(MemberExpression m)
        {
            // https://www.techonthenet.com/oracle/functions/to_char.php
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(", 'DDD'))");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Hour 属性
        /// </summary>
        protected override Expression VisitHour(MemberExpression m)
        {
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(", 'HH24'))");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Millisecond 属性
        /// </summary>
        protected override Expression VisitMillisecond(MemberExpression m)
        {
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(", 'FF3'))");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Minute 属性
        /// </summary>
        protected override Expression VisitMinute(MemberExpression m)
        {
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(",'MI'))");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Month 属性
        /// </summary>
        protected override Expression VisitMonth(MemberExpression m)
        {
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(", 'MM'))");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Second 属性
        /// </summary>
        protected override Expression VisitSecond(MemberExpression m)
        {
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(",'SS'))");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Ticks 属性
        /// </summary>
        protected override Expression VisitTicks(MemberExpression m)
        {

            //SELECT TO_CHAR(
            //       (
            //      (TO_DATE('2019-01-01', 'YYYY-MM-DD') -TO_DATE('1900-01-01', 'YYYY-MM-DD') + 693595) *24 * 3600 * 1000 +
            //      EXTRACT(hour from TO_TIMESTAMP('2019-01-01 23:59:59.123456', 'yyyy-mm-dd hh24:mi:ss.ff')) * 3600000 +
            //      EXTRACT(minute from TO_TIMESTAMP('2019-01-01 23:59:59.123456', 'yyyy-mm-dd hh24:mi:ss.ff')) * 60000 +
            //      EXTRACT(second from TO_TIMESTAMP('2019-01-01 23:59:59.123456', 'yyyy-mm-dd hh24:mi:ss.ff')) * 1000) * 10000
            //)
            //FROM dual

            _builder.Append("((");
            // 年份
            _builder.Append("(TRUNC(");
            _visitor.Visit(m.Expression);
            _builder.Append(") - TO_DATE('1900-01-01', 'YYYY-MM-DD') + 693595) * 24 * 3600 * 1000 + ");
            // 时
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(", 'HH24')) * 3600000 + ");
            // 分
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(", 'MM')) * 60000 + ");
            // 秒
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(", 'SS')) * 1000 + ");
            // ms
            _builder.Append("TO_NUMBER(TO_CHAR(CAST(");
            _visitor.Visit(m.Expression);
            _builder.Append(" AS TIMESTAMP), 'FF7'))) * 10000) ");
            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.TimeOfDay 属性
        /// </summary>
        protected override Expression VisitTimeOfDay(MemberExpression m)
        {
            _builder.Append("NUMTODSINTERVAL(");
            _visitor.Visit(m.Expression);
            _builder.Append(" - TRUNC(");
            _visitor.Visit(m.Expression);
            _builder.Append("), 'SECOND')");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.TimeOfDay 属性
        /// </summary>
        protected override Expression VisitYear(MemberExpression m)
        {
            _builder.Append("TO_NUMBER(TO_CHAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(", 'YYYY'))");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.DaysInMonth 方法
        /// </summary>
        protected override Expression VisitDaysInMonth(MethodCallExpression m)
        {
            // SELECT EXTRACT(DAY FROM LAST_DAY(TO_DATE(TO_CHAR(2019) || '-' || TO_CHAR(10) || '-1','YYYY-MM-DD')))  FROM DUAL;
            _builder.Append("EXTRACT(DAY FROM LAST_DAY(TO_DATE(TO_CHAR(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(") || '-' || TO_CHAR(");
            _visitor.Visit(m.Arguments[1]);
            _builder.Append(") || '-1','YYYY-MM-DD')))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.IsLeapYear 方法
        /// </summary>
        protected override Expression VisitIsLeapYear(MethodCallExpression m)
        {
            _builder.Append("(MOD(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(", 4) = 0 AND MOD(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(", 100) <> 0 OR MOD(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(", 400) = 0)");
            return m;
        }

        ///// <summary>
        ///// 访问 DateTime.Add 方法
        ///// </summary>
        //protected override Expression VisitAdd(MethodCallExpression b)
        //{
        //    if (b != null)
        //    {
        //        if (!b.Arguments[0].CanEvaluate()) throw new NotSupportedException("DateTime.Add reqiure a local variable as parameter.");

        //        var c = b.Arguments[0].Evaluate();
        //        _builder.Append("DATEADD(MILLISECOND, ");
        //        _builder.Append(_provider.DbValue.GetSqlValue(((TimeSpan)c.Value).TotalMilliseconds, _builder.Token));
        //        _builder.Append(", ");
        //        _visitor.Visit(b.Object);
        //        _builder.Append(')');
        //    }

        //    
        //    return b;
        //}

        ///// <summary>
        ///// 访问 DateTime.Add 方法
        ///// </summary>
        //protected override Expression VisitSubtract(MethodCallExpression b)
        //{
        //    if (b != null)
        //    {
        //        _builder.Append("DATEADD(MILLISECOND, DATEDIFF(MILLISECOND, ");
        //        _visitor.Visit(b.Arguments[0]);
        //        _builder.Append(", ");
        //        _visitor.Visit(b.Object);
        //        _builder.Append("),'1970-01-01')");
        //    }

        //    
        //    return b;
        //}

        /// <summary>
        /// 访问 DateTime.AddDays 方法
        /// </summary>
        protected override Expression VisitAddDays(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + NUMTODSINTERVAL(");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(", 'DAY'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddDays 方法
        /// </summary>
        protected override Expression VisitAddHours(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + NUMTODSINTERVAL(");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(", 'HOUR'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddMilliseconds 方法
        /// </summary>
        protected override Expression VisitAddMilliseconds(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + NUMTODSINTERVAL(");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(" / 1000, 'SECOND'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddMinutes 方法
        /// </summary>
        protected override Expression VisitAddMinutes(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + NUMTODSINTERVAL(");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(", 'MINUTE'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddMonths 方法
        /// </summary>
        protected override Expression VisitAddMonths(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + NUMTOYMINTERVAL(");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(", 'MONTH'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddSeconds 方法
        /// </summary>
        protected override Expression VisitAddSeconds(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + NUMTODSINTERVAL(");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(", 'SECOND'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddTicks 方法
        /// </summary>
        protected override Expression VisitAddTicks(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + NUMTODSINTERVAL(");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(" / 10000000, 'SECOND'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddTicks 方法
        /// </summary>
        protected override Expression VisitAddYears(MethodCallExpression m)
        {
            _builder.Append('(');
            _visitor.Visit(m.Object);
            _builder.Append(" + NUMTOYMINTERVAL(");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(", 'YEAR'))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Today 属性
        /// </summary>
        protected override Expression VisitToday(MemberExpression m)
        {
            _builder.Append("TRUNC(SYSDATE)");            
            return m;
        }

        /// <summary>
        /// 访问 new Guid 方法
        /// </summary>
        protected override Expression VisitNewGuid(MethodCallExpression m)
        {
            _builder.Append("SYS_GUID()");
            return m;
        }

        #endregion
    }
}
