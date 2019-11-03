
using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="MySqlMethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    public class MySqlMethodCallExressionVisitor : MethodCallExpressionVisitor
    {
        // https://dev.mysql.com/doc/refman/8.0/en/charset-national.html
        //SELECT N'some text';
        //SELECT n'some text';
        //SELECT _utf8'some text';

        private ISqlBuilder _builder = null;
        private IDbQueryProvider _provider = null;
        private ExpressionVisitorBase _visitor = null;
        private MemberVisitedMark _visitedMark = null;

        #region 构造函数

        /// <summary>
        /// 实例化 <see cref="SqlServerMethodCallExressionVisitor"/> 类的新实例
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
        protected override Expression VisitCoalesce(BinaryExpression b)
        {
            // 例： a.Name ?? "TAN" => ISNULL(a.Name,'TAN')

            _builder.Append("IFNULL(");
            _visitor.Visit(b.Left is ConstantExpression ? b.Right : b.Left);
            _builder.Append(", ");
            _visitor.Visit(b.Left is ConstantExpression ? b.Left : b.Right);
            _builder.Append(')');
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
                _builder.Append("DATE_FORMAT(");
                _visitor.Visit(node);
                _builder.Append(", '%Y-%m-%d %H:%i:%s.%f')");
            }
            else
            {
                _builder.Append("CAST(");
                _visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
                _builder.Append(" AS CHAR)");
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

            return m;
        }

        /// <summary>
        /// 访问 SubString 方法
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
                _builder.Append("CHAR_LENGTH(");
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
            _builder.Append("CONCAT(");
            _visitor.Visit(b.Left);
            _builder.Append(", ");
            _visitor.Visit(b.Right);
            _builder.Append(')');
            return b;
        }

        /// <summary>
        /// 访问 Concat 方法
        /// </summary>
        protected override Expression VisitConcat(MethodCallExpression m)
        {
            if (m != null && m.Arguments != null && m.Arguments.Count > 0)
            {
                if (m.Arguments.Count == 1) _visitor.Visit(m.Arguments[0]);
                else
                {
                    _builder.Append("CONCAT(");
                    for (int i = 0; i < m.Arguments.Count; i++)
                    {
                        _visitor.Visit(m.Arguments[i]);
                        if (i < m.Arguments.Count - 1) _builder.Append(", ");
                    }
                    _builder.Append(')');
                }
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
        /// 访问 IndexOf 方法
        /// </summary>
        protected override Expression VisitIndexOf(MethodCallExpression m)
        {
            _builder.Append("(LOCATE(");
            _visitor.Visit(m.Object);
            _builder.Append(',');
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

            _builder.Append(") - 1)");
            return m;
        }

        /// <summary>
        /// 访问 Math.Truncate 方法
        /// </summary>
        protected override Expression VisitTruncate(MethodCallExpression m)
        {
            _builder.Append("TRUNCATE(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",0)");
            return m;
        }

        /// <summary>
        /// 访问 Math.Log 方法
        /// </summary>
        protected override Expression VisitLog(MethodCallExpression m)
        {
            // LOG(B,X) 
            _builder.Append("LOG(");
            if (m.Arguments.Count == 1) _visitor.Visit(m.Arguments[0]);
            else
            {
                // 指定基数
                _visitor.Visit(m.Arguments[1]);
                _builder.Append(", ");
                _visitor.Visit(m.Arguments[0]);
            }
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
            _builder.Append(',');
            _visitor.Visit(b.Arguments[1]);
            _builder.Append(')');
            return b;
        }

        /// <summary>
        /// 访问 DateTime.Now 属性
        /// </summary>
        protected override Expression VisitNow(MemberExpression m)
        {
            _builder.Append("NOW()");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Now 属性
        /// </summary>
        protected override Expression VisitUtcNow(MemberExpression m)
        {
            _builder.Append("UTC_TIMESTAMP()");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Date 属性
        /// </summary>
        protected override Expression VisitDate(MemberExpression m)
        {
            _builder.Append("DATE(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Day 属性
        /// </summary>
        protected override Expression VisitDay(MemberExpression m)
        {
            _builder.Append("DAYOFMONTH(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.DayOfWeek 属性
        /// </summary>
        protected override Expression VisitDayOfWeek(MemberExpression m)
        {
            _builder.Append("DAYOFWEEK(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.DayOfYear 属性
        /// </summary>
        protected override Expression VisitDayOfYear(MemberExpression m)
        {
            _builder.Append("DAYOFYEAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Hour 属性
        /// </summary>
        protected override Expression VisitHour(MemberExpression m)
        {
            _builder.Append("HOUR(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Millisecond 属性
        /// </summary>
        protected override Expression VisitMillisecond(MemberExpression m)
        {
            _builder.Append("FLOOR(MICROSECOND(");
            _visitor.Visit(m.Expression);
            _builder.Append(") / 1000)");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Minute 属性
        /// </summary>
        protected override Expression VisitMinute(MemberExpression m)
        {
            _builder.Append("MINUTE(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Month 属性
        /// </summary>
        protected override Expression VisitMonth(MemberExpression m)
        {
            _builder.Append("MONTH(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Second 属性
        /// </summary>
        protected override Expression VisitSecond(MemberExpression m)
        {
            _builder.Append("SECOND(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Ticks 属性
        /// </summary>
        protected override Expression VisitTicks(MemberExpression m)
        {
            _builder.Append("(TIMESTAMPDIFF(MICROSECOND, '0001-1-1', ");
            _visitor.Visit(m.Expression);
            _builder.Append(") * 10)");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.TimeOfDay 属性
        /// </summary>
        protected override Expression VisitTimeOfDay(MemberExpression m)
        {
            _builder.Append("TIME(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.TimeOfDay 属性
        /// </summary>
        protected override Expression VisitYear(MemberExpression m)
        {
            _builder.Append("YEAR(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");            
            return m;
        }

        /// <summary>
        /// 访问 DateTime.DaysInMonth 方法
        /// </summary>
        protected override Expression VisitDaysInMonth(MethodCallExpression m)
        {
            _builder.Append("DAYOFMONTH(LAST_DAY(CONCAT(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(", '-', ");
            _visitor.Visit(m.Arguments[1]);
            _builder.Append(", '-1')))");
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
            _builder.Append("DATE_ADD(");
            _visitor.Visit(m.Object);
            _builder.Append(", INTERVAL ");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(" DAY)");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddDays 方法
        /// </summary>
        protected override Expression VisitAddHours(MethodCallExpression m)
        {
            _builder.Append("DATE_ADD(");
            _visitor.Visit(m.Object);
            _builder.Append(", INTERVAL ");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(" HOUR)");
            return m;

        }

        /// <summary>
        /// 访问 DateTime.AddMilliseconds 方法
        /// </summary>
        protected override Expression VisitAddMilliseconds(MethodCallExpression m)
        {
            _builder.Append("DATE_ADD(");
            _visitor.Visit(m.Object);
            _builder.Append(", INTERVAL ");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(" * 1000 MICROSECOND)");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddMinutes 方法
        /// </summary>
        protected override Expression VisitAddMinutes(MethodCallExpression m)
        {
            _builder.Append("DATE_ADD(");
            _visitor.Visit(m.Object);
            _builder.Append(", INTERVAL ");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(" MINUTE)");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddMonths 方法
        /// </summary>
        protected override Expression VisitAddMonths(MethodCallExpression m)
        {
            _builder.Append("DATE_ADD(");
            _visitor.Visit(m.Object);
            _builder.Append(", INTERVAL ");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(" MONTH)");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddSeconds 方法
        /// </summary>
        protected override Expression VisitAddSeconds(MethodCallExpression m)
        {
            _builder.Append("DATE_ADD(");
            _visitor.Visit(m.Object);
            _builder.Append(", INTERVAL ");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(" SECOND)");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddTicks 方法
        /// </summary>
        protected override Expression VisitAddTicks(MethodCallExpression m)
        {
            _builder.Append("DATE_ADD(");
            _visitor.Visit(m.Object);
            _builder.Append(", INTERVAL ");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(Convert.ToDouble(m.Arguments[0].Evaluate().Value) / 10, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(" / 10 MICROSECOND)");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddTicks 方法
        /// </summary>
        protected override Expression VisitAddYears(MethodCallExpression m)
        {
            _builder.Append("DATE_ADD(");
            _visitor.Visit(m.Object);
            _builder.Append(", INTERVAL ");
            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value, null);
            else
                _visitor.Visit(m.Arguments[0]);
            _builder.Append(" YEAR)");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Today 属性
        /// </summary>
        protected override Expression VisitToday(MemberExpression m)
        {
            _builder.Append("CURDATE()");            
            return m;
        }

        /// <summary>
        /// 访问 new Guid 方法
        /// </summary>
        protected override Expression VisitNewGuid(MethodCallExpression m)
        {
            _builder.Append("UUID()");
            return m;
        }

        /// <summary>
        /// 访问 IDbQueryable.Contains 方法
        /// </summary>
        protected override Expression VisitQueryableContains(MethodCallExpression m)
        {
            IDbQueryable query = m.Arguments[0].Evaluate().Value as IDbQueryable;
            ResolveToken token = _builder.Token;
            bool isDelete = token != null && token.Extendsions != null && token.Extendsions.ContainsKey("MySqlDelete");

            var cmd = query.Resolve(_builder.Indent + 1, false, token != null ? new ResolveToken
            {
                Parameters = token.Parameters,
                TableAliasName = "s",
                IsDebug = token.IsDebug
            } : null);
            Column column = ((MappingCommand)cmd).PickColumns.First();
            _builder.Append("EXISTS(");

            //if (isDelete)
            //{
            //    _builder.Append("SELECT 1 FROM(");
            //    _builder.Append(cmd.CommandText);
            //    _builder.Append(") ");
            //    _builder.Append(column.TableAlias);
            //    _builder.Append(" WHERE ");

            //    _builder.AppendMember(column.TableAlias, column.Name);
            //    _builder.Append(" = ");
            //    _visitor.Visit(m.Arguments[1]);
            //    _builder.Append(")");
            //}
            //else
            //{
            //    _builder.Append(cmd.CommandText);
            //    if (((MappingCommand)cmd).WhereFragment.Length > 0)
            //        _builder.Append(" AND ");
            //    else
            //        _builder.Append("WHERE ");

            //    _builder.AppendMember(column.TableAlias, column.Name);
            //    _builder.Append(" = ");
            //    _visitor.Visit(m.Arguments[1]);
            //    _builder.Append(")");
            //}

            return m;
        }

        #endregion
    }
}