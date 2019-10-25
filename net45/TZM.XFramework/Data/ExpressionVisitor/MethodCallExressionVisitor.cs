
using System;
using System.Linq;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// <see cref="MethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    public abstract class MethodCallExressionVisitor : IMethodCallExressionVisitor
    {
        private ISqlBuilder _builder = null;
        private IDbQueryProvider _provider = null;
        private ExpressionVisitorBase _visitor = null;
        private MemberVisitedMark _visitedMark = null;
        private static TypeRuntimeInfo _typeRuntime = null;

        #region 构造函数

        /// <summary>
        /// 实例化 <see cref="MethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        public MethodCallExressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
        {
            _provider = provider;
            _visitor = visitor;
            _builder = visitor.SqlBuilder;
            _visitedMark = _visitor.VisitedMark;
        }

        #endregion

        #region 接口方法

        /// <summary>
        /// 访问表示 null 判断运算的节点 a.Name == null
        /// </summary>
        public virtual Expression VisitEqualNull(BinaryExpression b)
        {
            // a.Name == null => a.Name Is Null
            Expression left = b.Left.NodeType == ExpressionType.Constant ? b.Right : b.Left;
            Expression right = b.Left.NodeType == ExpressionType.Constant ? b.Left : b.Right;
            string oper = b.NodeType == ExpressionType.Equal ? " IS " : " IS NOT ";

            _visitor.Visit(left);
            _builder.Append(oper);
            _visitor.Visit(right);

            return b;
        }

        /// <summary>
        /// 访问表示 null 合并运算的节点 a ?? b
        /// </summary>
        /// <param name="b">二元表达式节点</param>
        /// <returns></returns>
        public virtual Expression VisitCoalesce(BinaryExpression b)
        {
            // 例： a.Name ?? "TAN" => ISNULL(a.Name,'TAN')
            Expression left = b.Left.NodeType == ExpressionType.Constant ? b.Right : b.Left;
            Expression right = b.Left.NodeType == ExpressionType.Constant ? b.Left : b.Right;

            _builder.Append("ISNULL(");
            _visitor.Visit(left);
            _builder.Append(',');
            _visitor.Visit(right);
            _builder.Append(')');


            return b;
        }

        /// <summary>
        /// 访问表示方法调用的节点
        /// </summary>
        /// <param name="node">方法调用节点</param>
        /// <returns></returns>
        public virtual Expression VisitMethodCall(MethodCallExpression node)
        {
            if (_typeRuntime == null)
                _typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(this.GetType(), true);
            MemberInvokerBase invoker = null;
            if (node.Method.Name == "Concat") invoker = _typeRuntime.GetMethod("Visit" + node.Method.Name, new[] { typeof(MethodCallExpression) });
            else invoker = _typeRuntime.GetInvoker("Visit" + node.Method.Name);

            if (invoker == null) throw new XFrameworkException("{0}.{1} is not supported.", node.Method.DeclaringType, node.Method.Name);
            else
            {
                object exp = invoker.Invoke(this, new object[] { node });
                return exp as Expression;
            }
        }

        /// <summary>
        /// 访问表示方法调用的节点
        /// </summary>
        /// <param name="node">方法调用节点</param>
        /// <returns></returns>
        public virtual Expression VisitMethodCall(BinaryExpression node)
        {
            if (_typeRuntime == null)
                _typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(this.GetType(), true);
            string methodName = string.Empty;
            if (node.NodeType == ExpressionType.Modulo) methodName = "Modulo";
            else methodName = node.Method.Name;

            MemberInvokerBase invoker = _typeRuntime.GetInvoker("Visit" + methodName);
            if (invoker == null) throw new XFrameworkException("{0}.{1} is not supported.", node.Method.DeclaringType, node.Method.Name);
            else
            {
                object exp = invoker.Invoke(this, new object[] { node });
                return exp as Expression;
            }
        }

        /// <summary>
        /// 访问表示字段或者属性的属性的节点 a.Name.Length
        /// </summary>
        public virtual Expression VisitMemberMember(MemberExpression node)
        {
            if (_typeRuntime == null) _typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(this.GetType(), true);
            MemberInvokerBase invoker = _typeRuntime.GetInvoker("Visit" + node.Member.Name);
            if (invoker == null) throw new XFrameworkException("{0}.{1} is not supported.", node.Member.DeclaringType, node.Member.Name);
            else
            {
                object exp = invoker.Invoke(this, new object[] { node });
                return exp as Expression;
            }
        }

        /// <summary>
        /// 访问一元运算符
        /// </summary>
        /// <param name="node">一元运算符表达式</param>
        /// <returns></returns>
        public virtual Expression VisitUnary(UnaryExpression node)
        {
            //if (node.NodeType == ExpressionType.Convert && node.Type != node.Operand.Type && node.Operand.Type != typeof(char))
            //{

            //}
            _visitor.Visit(node.Operand);
            return node;
        }

        #endregion

        #region 解析方法

        /// <summary>
        /// 访问 ToString 方法
        /// </summary>
        protected virtual Expression VisitToString(MethodCallExpression m)
        {
            // => a.ID.ToString()
            Expression node = null;
            if (m.Object != null) node = m.Object;
            else if (m.Arguments != null && m.Arguments.Count > 0) node = m.Arguments[0];
            // 字符串不进行转换
            if (node == null || node.Type == typeof(string)) return _visitor.Visit(node);

            string name = "NVARCHAR";
            var member = _visitedMark.Current;
            var column = _provider.DbValue.GetColumnAttribute(member != null ? member.Member : null, member != null ? member.Expression.Type : null);
            bool isUnicode = _provider.DbValue.IsUnicode(column != null ? column.DbType : null);
            name = isUnicode ? "NVARCHAR" : "VARCHAR";

            if (node != null && node.Type == typeof(DateTime))
            {
                _builder.Append("CONVERT(");
                _builder.Append(name);
                _builder.Append(", ");
                _visitor.Visit(node);
                _builder.Append(", 121)");
            }
            else
            {
                if (column != null && column.Size > 0) name = string.Format("{0}({1})", name, column.Size);
                else if (column != null && column.Size == -1) name = string.Format("{0}(max)", name);
                else name = string.Format("{0}(max)", name);

                _builder.Append("CAST(");
                _visitor.Visit(node);
                _builder.Append(" AS ");
                _builder.Append(name);
                _builder.Append(')');
            }

            return m;
        }

        /// <summary>
        /// 访问 Contains 方法
        /// </summary>
        protected Expression VisitContains(MethodCallExpression node)
        {
            Type type = node.Method.ReflectedType != null ? node.Method.ReflectedType : node.Method.DeclaringType;
            if (type == typeof(string)) return this.VisitStringContains(node);
            else if (type == typeof(DbQueryableExtensions) || type == typeof(IDbQueryable)) return this.VisitQueryableContains(node);
            else if (type == typeof(Enumerable) || typeof(IEnumerable).IsAssignableFrom(type)) return this.VisitEnumerableContains(node);
            else throw new XFrameworkException("{0}.{1} is not supported.", node.Method.DeclaringType, node.Method.Name);
        }

        /// <summary>
        /// 访问 StartWidth 方法
        /// </summary>
        protected virtual Expression VisitStartsWith(MethodCallExpression m)
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
                        _builder.Append("(");
                        _builder.Append(value);
                        _builder.Append(" + '%')");
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
                    _builder.Append(" + '%')");
                }
            }

            return m;
        }

        /// <summary>
        /// 访问 EndWidth 方法
        /// </summary>
        protected virtual Expression VisitEndsWith(MethodCallExpression m)
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
                        _builder.Append("('%' + ");
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
                    _builder.Append("('%' + ");
                    _visitor.Visit(m.Arguments[0]);
                    _builder.Append(')');
                }
            }

            return m;
        }

        /// <summary>
        /// 访问 TrimStart 方法
        /// </summary>
        protected virtual Expression VisitTrimStart(MethodCallExpression m)
        {
            if (m != null)
            {
                _builder.Append("LTRIM(");
                _visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
                _builder.Append(")");
            }

            return m;
        }

        /// <summary>
        /// 访问 TrimEnd 方法
        /// </summary>
        protected virtual Expression VisitTrimEnd(MethodCallExpression m)
        {
            if (m != null)
            {
                _builder.Append("RTRIM(");
                _visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
                _builder.Append(")");
            }

            return m;
        }

        /// <summary>
        /// 访问 Trim 方法
        /// </summary>
        protected virtual Expression VisitTrim(MethodCallExpression m)
        {
            if (m != null)
            {
                _builder.Append("RTRIM(LTRIM(");
                _visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
                _builder.Append("))");
            }

            return m;
        }

        /// <summary>
        /// 访问 SubString 方法
        /// </summary>
        protected virtual Expression VisitSubstring(MethodCallExpression m)
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
                    string value = _provider.DbValue.GetSqlValue(index, _builder.Token, System.Data.DbType.Int32);
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
                    _builder.Append("LEN(");
                    _visitor.Visit(args[0]);
                    _builder.Append(")");
                }
                _builder.Append(")");
            }

            return m;
        }

        /// <summary>
        /// 访问 Concat 方法
        /// </summary>
        protected virtual Expression VisitConcat(BinaryExpression b)
        {
            if (b != null)
            {
                _builder.Append("(");
                _visitor.Visit(b.Left);
                _builder.Append(" + ");
                _visitor.Visit(b.Right);
                _builder.Append(")");
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Concat 方法
        /// </summary>
        protected virtual Expression VisitConcat(MethodCallExpression m)
        {
            if (m != null && m.Arguments != null)
            {
                if (m.Arguments.Count == 1) _visitor.Visit(m.Arguments[0]);
                else
                {
                    _builder.Append("(");
                    for (int i = 0; i < m.Arguments.Count; i++)
                    {
                        _visitor.Visit(m.Arguments[i]);
                        if (i < m.Arguments.Count - 1) _builder.Append(" + ");
                    }
                    _builder.Append(")");
                }
            }

            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 IsNullOrEmpty 方法
        /// </summary>
        protected virtual Expression VisitIsNullOrEmpty(MethodCallExpression m)
        {
            if (m != null)
            {
                _builder.Append('(');
                _visitor.Visit(m.Arguments[0]);
                _builder.Append(" IS NULL OR ");
                _visitor.Visit(m.Arguments[0]);
                _builder.Append(" = ");

                bool isUnicode = _provider.DbValue.IsUnicode(_visitedMark.Current);
                if (isUnicode) _builder.Append('N');

                _builder.Append("'')");
            }

            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 Length 属性
        /// </summary>
        protected virtual Expression VisitLength(MemberExpression m)
        {
            _builder.Append("LEN(");
            _visitor.Visit(m.Expression);
            _builder.Append(")");

            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 ToUpper 方法
        /// </summary>
        protected virtual Expression VisitToUpper(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("UPPER(");
                _visitor.Visit(b.Object);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 ToLower 方法
        /// </summary>
        protected virtual Expression VisitToLower(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("LOWER(");
                _visitor.Visit(b.Object);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Replace 方法
        /// </summary>
        protected virtual Expression VisitReplace(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("REPLACE(");
                _visitor.Visit(b.Object);
                _builder.Append(',');
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(',');
                _visitor.Visit(b.Arguments[1]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 PadLeft 方法
        /// </summary>
        protected virtual Expression VisitPadLeft(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("LPAD(");
                _visitor.Visit(b.Object);
                _builder.Append(',');
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(',');

                if (b.Arguments.Count == 1)
                    _builder.Append("' '");
                else
                    _visitor.Visit(b.Arguments[1]);

                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 PadRight 方法
        /// </summary>
        protected virtual Expression VisitPadRight(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("RPAD(");
                _visitor.Visit(b.Object);
                _builder.Append(',');
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(',');

                if (b.Arguments.Count == 1)
                    _builder.Append("' '");
                else
                    _visitor.Visit(b.Arguments[1]);

                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 IndexOf 方法
        /// </summary>
        protected virtual Expression VisitIndexOf(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("(CHARINDEX(");
                _visitor.Visit(b.Object);
                _builder.Append(',');
                _visitor.Visit(b.Arguments[0]);
                if (b.Arguments.Count > 1)
                {
                    _builder.Append(',');
                    if (b.Arguments[1].CanEvaluate())
                    {
                        var c = b.Arguments[1].Evaluate();
                        int index = Convert.ToInt32(c.Value) + 1;
                        _builder.Append(_provider.DbValue.GetSqlValue(index, _builder.Token));
                    }
                    else
                    {
                        _visitor.Visit(b.Arguments[1]);
                        _builder.Append(" + 1");
                    }
                }
                _builder.Append(") - 1)");
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 % 方法
        /// </summary>
        protected virtual Expression VisitModulo(BinaryExpression b)
        {
            if (b != null)
            {
                _visitor.Visit(b.Left);
                _builder.Append(" % ");
                _visitor.Visit(b.Right);
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Abs 方法
        /// </summary>
        protected virtual Expression VisitAbs(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("ABS(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Acos 方法
        /// </summary>
        protected virtual Expression VisitAcos(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("ACOS(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Asin 方法
        /// </summary>
        protected virtual Expression VisitAsin(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("ASIN(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Atan 方法
        /// </summary>
        protected virtual Expression VisitAtan(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("ATAN(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Atan2 方法
        /// </summary>
        protected virtual Expression VisitAtan2(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("ATN2(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(',');
                _visitor.Visit(b.Arguments[1]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Ceiling 方法
        /// </summary>
        protected virtual Expression VisitCeiling(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("CEILING(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Cos 方法
        /// </summary>
        protected virtual Expression VisitCos(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("COS(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Exp 方法
        /// </summary>
        protected virtual Expression VisitExp(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("EXP(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Floor 方法
        /// </summary>
        protected virtual Expression VisitFloor(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("FLOOR(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Log 方法
        /// </summary>
        protected virtual Expression VisitLog(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("LOG(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Log10 方法
        /// </summary>
        protected virtual Expression VisitLog10(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("LOG10(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Pow 方法
        /// </summary>
        protected virtual Expression VisitPow(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("POWER(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(',');
                _visitor.Visit(b.Arguments[1]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Round 方法
        /// </summary>
        protected virtual Expression VisitRound(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("ROUND(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(',');
                _visitor.Visit(b.Arguments[1]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Sign 方法
        /// </summary>
        protected virtual Expression VisitSign(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("SIGN(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Sin 方法
        /// </summary>
        protected virtual Expression VisitSin(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("Sin(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Sqrt 方法
        /// </summary>
        protected virtual Expression VisitSqrt(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("SQRT(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Tan 方法
        /// </summary>
        protected virtual Expression VisitTan(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("TAN(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 Math.Truncate 方法
        /// </summary>
        protected virtual Expression VisitTruncate(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("FLOOR(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 DateTime.Now 属性
        /// </summary>
        protected virtual Expression VisitNow(MemberExpression m)
        {
            _builder.Append("GETDATE()");
            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Now 属性
        /// </summary>
        protected virtual Expression VisitUtcNow(MemberExpression m)
        {
            _builder.Append("GETUTCDATE()");
            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Now 属性
        /// </summary>
        protected virtual Expression VisitToday(MemberExpression m)
        {
            _builder.Append("CONVERT(DATE,CONVERT(CHAR(10),GETDATE(),120))");
            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Date 属性
        /// </summary>
        protected virtual Expression VisitDate(MemberExpression m)
        {
            _builder.Append("CONVERT(CHAR(10), ");
            _visitor.Visit(m.Expression);
            _builder.Append(", 120)");
            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Day 属性
        /// </summary>
        protected virtual Expression VisitDay(MemberExpression m)
        {
            _builder.Append("DATEPART(DAY,");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 DateTime.DayOfWeek 属性
        /// </summary>
        protected virtual Expression VisitDayOfWeek(MemberExpression m)
        {
            _builder.Append("DATEPART(WEEKDAY,");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 DateTime.DayOfYear 属性
        /// </summary>
        protected virtual Expression VisitDayOfYear(MemberExpression m)
        {
            _builder.Append("DATEPART(DAYOFYEAR,");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Hour 属性
        /// </summary>
        protected virtual Expression VisitHour(MemberExpression m)
        {
            _builder.Append("DATEPART(HOUR,");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Millisecond 属性
        /// </summary>
        protected virtual Expression VisitMillisecond(MemberExpression m)
        {
            _builder.Append("DATEPART(MILLISECOND,");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Minute 属性
        /// </summary>
        protected virtual Expression VisitMinute(MemberExpression m)
        {
            _builder.Append("DATEPART(MINUTE,");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Month 属性
        /// </summary>
        protected virtual Expression VisitMonth(MemberExpression m)
        {
            _builder.Append("DATEPART(MONTH,");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Second 属性
        /// </summary>
        protected virtual Expression VisitSecond(MemberExpression m)
        {
            _builder.Append("DATEPART(SECOND,");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Ticks 属性
        /// </summary>
        protected virtual Expression VisitTicks(MemberExpression m)
        {
            // tick = microsecond * 10 1microsecond = 1000nanosecond
            _builder.Append("(DATEDIFF_BIG (NANOSECOND, '1970-1-1', ");
            _visitor.Visit(m.Expression);
            _builder.Append(") / 100 + 621355968000000000)");
            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 DateTime.TimeOfDay 属性
        /// </summary>
        protected virtual Expression VisitTimeOfDay(MemberExpression m)
        {
            _builder.Append("CONVERT(TIME,CONVERT(VARCHAR, ");
            _visitor.Visit(m.Expression);
            _builder.Append(", 14))");
            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 DateTime.TimeOfDay 属性
        /// </summary>
        protected virtual Expression VisitYear(MemberExpression m)
        {
            _builder.Append("DATEPART(YEAR,");
            _visitor.Visit(m.Expression);
            _builder.Append(")");
            _visitedMark.Clear();
            return m;
        }

        /// <summary>
        /// 访问 DateTime.DaysInMonth 方法
        /// </summary>
        protected virtual Expression VisitDaysInMonth(MethodCallExpression b)
        {
            if (b != null)
            {
                // 下个月一号减去一天就是上个月最后一天
                // DATEPART(DAY, DATEADD(DAY, -1, DATEADD(MONTH, 1, CAST(2019 AS char(4)) + '-' + CAST(10 AS char(2)) + '-1')))
                _builder.Append("DATEPART(DAY, DATEADD(DAY, -1, DATEADD(MONTH, 1, CAST(");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(" AS CHAR(4)) + '-' + CAST(");
                _visitor.Visit(b.Arguments[1]);
                _builder.Append(" AS char(2)) + '-1')))");
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 DateTime.IsLeapYear 方法
        /// </summary>
        protected virtual Expression VisitIsLeapYear(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append('(');
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(" % 4 = 0 AND ");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(" % 100 <> 0 OR ");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(" % 400 = 0)");
            }

            _visitedMark.Clear();
            return b;
        }

        ///// <summary>
        ///// 访问 DateTime.Add 方法
        ///// </summary>
        //protected virtual Expression VisitAdd(MethodCallExpression b)
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

        //    _visitedMark.Clear();
        //    return b;
        //}

        ///// <summary>
        ///// 访问 DateTime.Add 方法
        ///// </summary>
        //protected virtual Expression VisitSubtract(MethodCallExpression b)
        //{
        //    if (b != null)
        //    {
        //        _builder.Append("DATEADD(MILLISECOND, DATEDIFF(MILLISECOND, ");
        //        _visitor.Visit(b.Arguments[0]);
        //        _builder.Append(", ");
        //        _visitor.Visit(b.Object);
        //        _builder.Append("),'1970-01-01')");
        //    }

        //    _visitedMark.Clear();
        //    return b;
        //}

        /// <summary>
        /// 访问 DateTime.AddDays 方法
        /// </summary>
        protected virtual Expression VisitAddDays(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("DATEADD(DAY, ");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(", ");
                _visitor.Visit(b.Object);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 DateTime.AddDays 方法
        /// </summary>
        protected virtual Expression VisitAddHours(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("DATEADD(HOUR, ");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(", ");
                _visitor.Visit(b.Object);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 DateTime.AddMilliseconds 方法
        /// </summary>
        protected virtual Expression VisitAddMilliseconds(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("DATEADD(MILLISECOND, ");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(", ");
                _visitor.Visit(b.Object);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 DateTime.AddMinutes 方法
        /// </summary>
        protected virtual Expression VisitAddMinutes(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("DATEADD(MINUTE, ");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(", ");
                _visitor.Visit(b.Object);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 DateTime.AddMonths 方法
        /// </summary>
        protected virtual Expression VisitAddMonths(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("DATEADD(MONTH, ");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(", ");
                _visitor.Visit(b.Object);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 DateTime.AddSeconds 方法
        /// </summary>
        protected virtual Expression VisitAddSeconds(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("DATEADD(SECOND, ");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(", ");
                _visitor.Visit(b.Object);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 DateTime.AddTicks 方法
        /// </summary>
        protected virtual Expression VisitAddTicks(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("DATEADD(MILLISECOND, ");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(" / 10000, ");
                _visitor.Visit(b.Object);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 访问 DateTime.AddYears 方法
        /// </summary>
        protected virtual Expression VisitAddYears(MethodCallExpression b)
        {
            if (b != null)
            {
                _builder.Append("DATEADD(YEAR, ");
                _visitor.Visit(b.Arguments[0]);
                _builder.Append(", ");
                _visitor.Visit(b.Object);
                _builder.Append(')');
            }

            _visitedMark.Clear();
            return b;
        }

        /// <summary>
        /// 生成SQL片断
        /// </summary>
        protected string GetSqlValue(ConstantExpression c, ref bool unicode)
        {
            var member = _visitedMark.Current;
            var column = _provider.DbValue.GetColumnAttribute(member != null ? member.Member : null, member != null ? member.Expression.Type : null);
            unicode = _provider.DbValue.IsUnicode(column != null ? column.DbType : null);

            if (_builder.Parameterized)
            {
                unicode = false;
                string value = _provider.DbValue.GetSqlValue(c.Value, _builder.Token, member != null ? member.Member : null, member != null ? member.Expression.Type : null);
                return value;
            }
            else
            {
                string value = c.Value != null ? c.Value.ToString() : string.Empty;
                value = _provider.DbValue.EscapeQuote(value, false, true, false);
                unicode = _provider.DbValue.IsUnicode(column != null ? column.DbType : null);

                return value;
            }
        }

        /// <summary>
        /// 访问 RowNumber 方法
        /// </summary>
        protected virtual Expression VisitRowNumber(MethodCallExpression m)
        {
            if (m == null) return m;

            _builder.Append("ROW_NUMBER() Over(Order By ");
            _visitor.Visit(m.Arguments[0]);
            if (m.Arguments.Count > 1)
            {
                var c = (ConstantExpression)m.Arguments[1];
                if (!((bool)c.Value)) _builder.Append(" DESC");
            }

            _builder.Append(')');

            return m;
        }

        /// <summary>
        /// 访问 RowNumber 方法
        /// </summary>
        protected virtual Expression VisitPartitionRowNumber(MethodCallExpression m)
        {
            if (m == null) return m;

            _builder.Append("ROW_NUMBER() Over(");

            // PARTITION BY
            _builder.Append("PARTITION BY ");
            _visitor.Visit(m.Arguments[0]);

            // ORDER BY
            _builder.Append(" ORDER BY ");
            _visitor.Visit(m.Arguments[1]);
            if (m.Arguments.Count > 2)
            {
                var c = (ConstantExpression)m.Arguments[2];
                if (!((bool)c.Value)) _builder.Append(" DESC");
            }

            _builder.Append(')');

            return m;
        }

        /// <summary>
        /// 访问 new Guid 方法
        /// </summary>
        protected virtual Expression VisitNewGuid(MethodCallExpression m)
        {
            _builder.Append("NEWID()");
            return m;
        }

        /// <summary>
        /// 访问 string.Contains 方法
        /// </summary>
        protected virtual Expression VisitStringContains(MethodCallExpression m)
        {
            // https://www.cnblogs.com/yangmingyu/p/6928209.html
            // 对于其他的特殊字符：'^'， '-'， ']' 因为它们本身在包含在 '[]' 中使用，所以需要用另外的方式来转义，于是就引入了 like 中的 escape 子句，另外值得注意的是：escape 可以转义所有的特殊字符。
            // EF 的 Like 不用参数化...

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
                        _builder.Append("(");
                        _builder.Append("'%' + ");
                        _builder.Append(value);
                        _builder.Append(" + '%'");
                        _builder.Append(")");
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
                    _builder.Append("('%' + ");
                    _visitor.Visit(m.Arguments[0]);
                    _builder.Append(" + '%')");
                }
            }

            return m;
        }

        /// <summary>
        /// 访问 IEnumerable.Contains 方法
        /// </summary>
        protected virtual Expression VisitEnumerableContains(MethodCallExpression m)
        {
            if (m == null) return m;

            _visitedMark.ClearImmediately = false;
            _visitor.Visit(m.Arguments[m.Arguments.Count - 1]);
            _builder.Append(" IN(");

            Expression exp = m.Object != null ? m.Object : m.Arguments[0];
            if (exp.NodeType == ExpressionType.Constant)
            {
                _visitor.Visit(exp);
            }
            else if (exp.NodeType == ExpressionType.MemberAccess)
            {
                _visitor.Visit(exp.Evaluate());
            }
            else if (exp.NodeType == ExpressionType.NewArrayInit)
            {
                // => new[] { 1, 2, 3 }.Contains(a.DemoId)
                var expressions = (exp as NewArrayExpression).Expressions;
                for (int i = 0; i < expressions.Count; i++)
                {
                    _visitor.Visit(expressions[i]);
                    if (i < expressions.Count - 1) _builder.Append(",");
                    else if (i == expressions.Count - 1) _visitedMark.ClearImmediately = true;
                }
            }
            else if (exp.NodeType == ExpressionType.ListInit)
            {
                // => new List<int> { 1, 2, 3 }.Contains(a.DemoId)
                var initializers = (exp as ListInitExpression).Initializers;
                for (int i = 0; i < initializers.Count; i++)
                {
                    foreach (var args in initializers[i].Arguments) _visitor.Visit(args);

                    if (i < initializers.Count - 1) _builder.Append(",");
                    else if (i == initializers.Count - 1) _visitedMark.ClearImmediately = true;
                }
            }
            else if (exp.NodeType == ExpressionType.New)
            {
                // => new List<int>(_demoIdList).Contains(a.DemoId)
                var arguments = (exp as NewExpression).Arguments;
                for (int i = 0; i < arguments.Count; i++)
                {
                    if (i == arguments.Count - 1) _visitedMark.ClearImmediately = true;
                    _visitor.Visit(arguments[i]);
                }
            }

            _visitedMark.ClearImmediately = true;
            _builder.Append(")");

            return m;
        }

        /// <summary>
        /// 访问 IDbQueryable.Contains 方法
        /// </summary>
        protected virtual Expression VisitQueryableContains(MethodCallExpression m)
        {
            IDbQueryable query = m.Arguments[0].Evaluate().Value as IDbQueryable;

            var cmd = query.Resolve(_builder.Indent + 1, false, _builder.Token != null ? new ResolveToken
            {
                Parameters = _builder.Token.Parameters,
                TableAliasName = "s",
                IsDebug = _builder.Token.IsDebug
            } : null);
            _builder.Append("EXISTS(");
            _builder.Append(cmd.CommandText);

            if (((MappingCommand)cmd).WhereFragment.Length > 0)
                _builder.Append(" AND ");
            else
                _builder.Append("WHERE ");

            var column = ((MappingCommand)cmd).Columns.First();
            _builder.AppendMember(column.TableAlias, column.Name);

            _builder.Append(" = ");
            _visitor.Visit(m.Arguments[1]);
            _builder.Append(")");

            return m;
        }

        #endregion
    }
}
