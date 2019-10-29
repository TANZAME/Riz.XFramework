
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
    public abstract class MethodCallExressionVisitor
    {
        private ISqlBuilder _builder = null;
        private IDbQueryProvider _provider = null;
        private ExpressionVisitorBase _visitor = null;
        private MemberVisitedMark _visitedMark = null;
        private static TypeRuntimeInfo _typeRuntime = null;
        private static HashSet<string> _removeVisitedMethods = null;

        #region 构造函数

        static MethodCallExressionVisitor()
        {
            // 自身构成布尔表达式的，发生类型改变的，一律不需要记录访问成员痕迹，否则会产生 DbType 不一致的问题
            _removeVisitedMethods = new HashSet<string>
            {
                "ToString",
                "Contains",
                "StartsWith",
                "EndsWith",
                "IsNullOrEmpty",
                "IndexOf"
            };
        }

        /// <summary>
        /// 实例化 <see cref="MethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        public MethodCallExressionVisitor(IDbQueryProvider provider,ExpressionVisitorBase visitor)
        {
            _provider = provider;
            _visitor = visitor;
            _builder = visitor.SqlBuilder;
            _visitedMark = _visitor.VisitedMark;
        }

        #endregion

        #region 入口方法

        /// <summary>
        ///  将调度到此类中更专用的访问方法之一的表达式。
        /// </summary>
        /// <param name="node">方法节点</param>
        /// <param name="router">方法路由</param>
        /// <returns></returns>
        public Expression Visit(Expression node,MethodCall router)
        {
            int visitedQty = _visitedMark.Count;
            Expression newNode = null;

            if (router == MethodCall.Coalesce)
                newNode = this.VisitCoalesce((BinaryExpression)node);
            else if (router == MethodCall.EqualNull)
                newNode = this.VisitEqualNull((BinaryExpression)node);
            else if (router == MethodCall.MethodCall)
                newNode = this.VisitMethodCall((MethodCallExpression)node);
            else if (router == MethodCall.BinaryCall)
                newNode = this.VisitMethodCall((BinaryExpression)node);
            else if (router == MethodCall.MemberMember)
                newNode = this.VisitMemberMember((MemberExpression)node);
            else if (router == MethodCall.Unary)
                newNode = this.VisitUnary((UnaryExpression)node);

            // 自身已构成布尔表达式的则需要删除它本身所产生的访问链
            if (_visitedMark.Count != visitedQty)
            {
                bool b = router != MethodCall.Unary;
                if (b && router == MethodCall.MethodCall)
                {
                    var m = (MethodCallExpression)node;
                    b = _removeVisitedMethods.Contains(m.Method.Name);
                }

                if (b) _visitedMark.Remove(_visitedMark.Count - visitedQty);
            }
            return newNode;
        }

        /// <summary>
        /// 访问表示 null 判断运算的节点 a.Name == null
        /// </summary>
        protected virtual Expression VisitEqualNull(BinaryExpression b)
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
        protected virtual Expression VisitCoalesce(BinaryExpression b)
        {
            // 例： a.Name ?? "TAN" => ISNULL(a.Name,'TAN')
            Expression left = b.Left.NodeType == ExpressionType.Constant ? b.Right : b.Left;
            Expression right = b.Left.NodeType == ExpressionType.Constant ? b.Left : b.Right;

            _builder.Append("ISNULL(");
            _visitor.Visit(left);
            _builder.Append(",");
            _visitor.Visit(right);
            _builder.Append(')');

            return b;
        }

        /// <summary>
        /// 访问表示方法调用的节点
        /// </summary>
        /// <param name="node">方法调用节点</param>
        /// <returns></returns>
        protected Expression VisitMethodCall(MethodCallExpression node)
        {
            if (_typeRuntime == null)
                _typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(this.GetType(),true);
            MemberInvokerBase invoker = null;
            if (node.Method.Name == "Concat") invoker = _typeRuntime.GetMethod("Visit" + node.Method.Name,new[] { typeof(MethodCallExpression) });
            else invoker = _typeRuntime.GetInvoker("Visit" + node.Method.Name);

            if (invoker == null) throw new XFrameworkException("{0}.{1} is not supported.",node.Method.DeclaringType,node.Method.Name);
            else
            {
                object exp = invoker.Invoke(this,new object[] { node });
                return exp as Expression;
            }
        }

        /// <summary>
        /// 访问表示方法调用的节点
        /// </summary>
        /// <param name="node">方法调用节点</param>
        /// <returns></returns>
        protected Expression VisitMethodCall(BinaryExpression node)
        {
            if (_typeRuntime == null)
                _typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(this.GetType(),true);
            string methodName = string.Empty;
            if (node.NodeType == ExpressionType.Modulo) methodName = "Modulo";
            else methodName = node.Method.Name;

            MemberInvokerBase invoker = _typeRuntime.GetInvoker("Visit" + methodName);
            if (invoker == null) throw new XFrameworkException("{0}.{1} is not supported.",node.Method.DeclaringType,node.Method.Name);
            else
            {
                object exp = invoker.Invoke(this,new object[] { node });
                return exp as Expression;
            }
        }

        /// <summary>
        /// 访问表示字段或者属性的属性的节点 a.Name.Length
        /// </summary>
        protected Expression VisitMemberMember(MemberExpression node)
        {
            if (_typeRuntime == null) _typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(this.GetType(),true);
            MemberInvokerBase invoker = _typeRuntime.GetInvoker("Visit" + node.Member.Name);
            if (invoker == null) throw new XFrameworkException("{0}.{1} is not supported.",node.Member.DeclaringType,node.Member.Name);
            else
            {
                object exp = invoker.Invoke(this,new object[] { node });
                return exp as Expression;
            }
        }

        /// <summary>
        /// 访问一元运算符
        /// </summary>
        /// <param name="node">一元运算符表达式</param>
        /// <returns></returns>
        protected virtual Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not) _builder.Append("NOT ");
            _visitor.Visit(node.Operand);
            return node;
        }

        #endregion

        #region 解析方法

        /// <summary>
        /// 访问 ToString 方法
        /// </summary>
        protected virtual Expression VisitCast(MethodCallExpression m)
        {
            // => a.ID.ToString()
            _builder.Append("CAST(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(" AS ");
            _builder.Append(m.Arguments[1].Evaluate().Value);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 ToString 方法
        /// </summary>
        protected virtual Expression VisitToString(MethodCallExpression m)
        {
            // => a.ID.ToString()
            Expression node = null;
            if (m.Object != null) node = m.Object;
            else if (m.Arguments != null && m.Arguments.Count > 0) node = m.Arguments[0];

            this.VisitToStringImpl(node);
            return m;
        }

        /// <summary>
        /// 访问 ToString 方法
        /// </summary>
        protected virtual Expression VisitToStringImpl(Expression node)
        {
            // => a.ID.ToString()
            // 字符串不进行转换
            if (node == null || node.Type == typeof(string)) return _visitor.Visit(node);

            string name = "NVARCHAR";
            ColumnAttribute column = null;
            bool isUnicode = _provider.DbValue.IsUnicode(_visitedMark.Current,out column);
            name = isUnicode ? "NVARCHAR" : "VARCHAR";

            if (node != null && node.Type == typeof(DateTime))
            {
                _builder.Append("CONVERT(");
                _builder.Append(name);
                _builder.Append(",");
                _visitor.Visit(node);
                _builder.Append(",121)");
            }
            else
            {
                // 特殊处理guid
                if (node.Type == typeof(Guid)) name = string.Format("{0}(64)",name);
                else
                {
                    if (column != null && column.Size > 0) name = string.Format("{0}({1})",name,column.Size);
                    else if (column != null && column.Size == -1) name = string.Format("{0}(max)",name);
                    else name = string.Format("{0}(max)",name);
                }

                _builder.Append("CAST(");
                _visitor.Visit(node);
                _builder.Append(" AS ");
                _builder.Append(name);
                _builder.Append(')');
            }

            return node;
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
            else throw new XFrameworkException("{0}.{1} is not supported.",node.Method.DeclaringType,node.Method.Name);
        }

        /// <summary>
        /// 访问 StartWidth 方法
        /// </summary>
        protected virtual Expression VisitStartsWith(MethodCallExpression m)
        {
            _visitor.Visit(m.Object);
            _builder.Append(" LIKE ");
            if (m.Arguments[0].CanEvaluate())
            {
                bool unicode = true;
                string value = this.GetSqlValue(m.Arguments[0].Evaluate(),ref unicode);

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

            return m;
        }

        /// <summary>
        /// 访问 EndWidth 方法
        /// </summary>
        protected virtual Expression VisitEndsWith(MethodCallExpression m)
        {
            _visitor.Visit(m.Object);
            _builder.Append(" LIKE ");
            if (m.Arguments[0].CanEvaluate())
            {
                bool unicode = true;
                string value = this.GetSqlValue(m.Arguments[0].Evaluate(),ref unicode);

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

            return m;
        }

        /// <summary>
        /// 访问 TrimStart 方法
        /// </summary>
        protected virtual Expression VisitTrimStart(MethodCallExpression m)
        {
            _builder.Append("LTRIM(");
            _visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
            _builder.Append(")");
            return m;
        }

        /// <summary>
        /// 访问 TrimEnd 方法
        /// </summary>
        protected virtual Expression VisitTrimEnd(MethodCallExpression m)
        {
            _builder.Append("RTRIM(");
            _visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
            _builder.Append(")");
            return m;
        }

        /// <summary>
        /// 访问 Trim 方法
        /// </summary>
        protected virtual Expression VisitTrim(MethodCallExpression m)
        {
            _builder.Append("RTRIM(LTRIM(");
            _visitor.Visit(m.Object != null ? m.Object : m.Arguments[0]);
            _builder.Append("))");
            return m;
        }

        /// <summary>
        /// 访问 SubString 方法
        /// </summary>
        protected virtual Expression VisitSubstring(MethodCallExpression m)
        {
            var expressions = new List<Expression>(m.Arguments);
            if (m.Object != null) expressions.Insert(0,m.Object);

            _builder.Append("SUBSTRING(");
            _visitor.Visit(expressions[0]);
            _builder.Append(",");

            if (expressions[1].CanEvaluate())
            {
                var c = expressions[1].Evaluate();
                int index = Convert.ToInt32(c.Value);
                index += 1;
                _builder.Append(index,null);
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
                    _builder.Append(expressions[2].Evaluate().Value,null);
                else
                    _visitor.Visit(expressions[2]);
            }
            else
            {
                // 带1个参数，Substring(n)
                _builder.Append("LEN(");
                _visitor.Visit(expressions[0]);
                _builder.Append(")");
            }

            _builder.Append(")");
            return m;
        }

        /// <summary>
        /// 访问 Concat 方法
        /// </summary>
        protected virtual Expression VisitConcat(BinaryExpression m)
        {
            _builder.Append("(");
            _visitor.Visit(m.Left);
            _builder.Append(" + ");
            _visitor.Visit(m.Right);
            _builder.Append(")");
            return m;
        }

        /// <summary>
        /// 访问 Concat 方法
        /// </summary>
        protected virtual Expression VisitConcat(MethodCallExpression m)
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
                    _visitor.VisitWithoutRemark(x => this.VisitToStringImpl(expressions[i]));
                    if (i < expressions.Count - 1) _builder.Append(" + ");
                }
                _builder.Append(")");
            }
            return m;
        }

        /// <summary>
        /// 访问 IsNullOrEmpty 方法
        /// </summary>
        protected virtual Expression VisitIsNullOrEmpty(MethodCallExpression m)
        {
            _builder.Append("ISNULL(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");
            bool isUnicode = _provider.DbValue.IsUnicode(_visitedMark.Current);
            string empty = isUnicode ? "N''" : "''";
            _builder.Append(empty);
            _builder.Append(") = ");
            _builder.Append(empty);
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
            return m;
        }

        /// <summary>
        /// 访问 ToUpper 方法
        /// </summary>
        protected virtual Expression VisitToUpper(MethodCallExpression m)
        {
            _builder.Append("UPPER(");
            _visitor.Visit(m.Object);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 ToLower 方法
        /// </summary>
        protected virtual Expression VisitToLower(MethodCallExpression m)
        {
            _builder.Append("LOWER(");
            _visitor.Visit(m.Object);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Replace 方法
        /// </summary>
        protected virtual Expression VisitReplace(MethodCallExpression m)
        {
            _builder.Append("REPLACE(");
            _visitor.Visit(m.Object);
            _builder.Append(",");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");
            _visitor.Visit(m.Arguments[1]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 PadLeft 方法
        /// </summary>
        protected virtual Expression VisitPadLeft(MethodCallExpression m)
        {
            _builder.Append("LPAD(");
            _visitor.Visit(m.Object);
            _builder.Append(",");

            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value,null);
            else
                _visitor.Visit(m.Arguments[0]);

            _builder.Append(",");

            if (m.Arguments.Count == 1)
                _builder.Append("' '");
            else
                _visitor.Visit(m.Arguments[1]);

            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 PadRight 方法
        /// </summary>
        protected virtual Expression VisitPadRight(MethodCallExpression m)
        {
            _builder.Append("RPAD(");
            _visitor.Visit(m.Object);
            _builder.Append(",");

            if (m.Arguments[0].CanEvaluate())
                _builder.Append(m.Arguments[0].Evaluate().Value,null);
            else
                _visitor.Visit(m.Arguments[0]);

            _builder.Append(",");

            if (m.Arguments.Count == 1)
                _builder.Append("' '");
            else
                _visitor.Visit(m.Arguments[1]);

            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 IndexOf 方法
        /// </summary>
        protected virtual Expression VisitIndexOf(MethodCallExpression m)
        {
            _builder.Append("(CHARINDEX(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");
            _visitor.Visit(m.Object);

            if (m.Arguments.Count > 1 && m.Arguments[1].Type != typeof(StringComparison))
            {
                _builder.Append(",");
                if (m.Arguments[1].CanEvaluate()) _builder.Append(Convert.ToInt32(m.Arguments[1].Evaluate().Value) + 1,null);
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
        /// 访问 % 方法
        /// </summary>
        protected virtual Expression VisitModulo(BinaryExpression m)
        {
            _visitor.Visit(m.Left);
            _builder.Append(" % ");
            _visitor.Visit(m.Right);
            return m;
        }

        /// <summary>
        /// 访问 Math.Abs 方法
        /// </summary>
        protected virtual Expression VisitAbs(MethodCallExpression m)
        {
            _builder.Append("ABS(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Acos 方法， 仅介于 -1.00 到 1.00 之间的值有效
        /// </summary>
        protected virtual Expression VisitAcos(MethodCallExpression m)
        {
            _builder.Append("ACOS(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Asin 方法， 仅介于 -1.00 到 1.00 之间的值有效
        /// </summary>
        protected virtual Expression VisitAsin(MethodCallExpression m)
        {
            _builder.Append("ASIN(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Atan 方法
        /// </summary>
        protected virtual Expression VisitAtan(MethodCallExpression m)
        {
            _builder.Append("ATAN(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Atan2 方法
        /// </summary>
        protected virtual Expression VisitAtan2(MethodCallExpression m)
        {
            _builder.Append("ATN2(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(',');
            _visitor.Visit(m.Arguments[1]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Ceiling 方法
        /// </summary>
        protected virtual Expression VisitCeiling(MethodCallExpression m)
        {
            _builder.Append("CEILING(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Cos 方法
        /// </summary>
        protected virtual Expression VisitCos(MethodCallExpression m)
        {
            _builder.Append("COS(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Exp 方法
        /// </summary>
        protected virtual Expression VisitExp(MethodCallExpression m)
        {
            _builder.Append("EXP(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Floor 方法
        /// </summary>
        protected virtual Expression VisitFloor(MethodCallExpression m)
        {
            _builder.Append("FLOOR(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Log 方法
        /// </summary>
        protected virtual Expression VisitLog(MethodCallExpression m)
        {
            //--Syntax for SQL Server
            //--LOG(float_expression[,base])

            _builder.Append("LOG(");
            _visitor.Visit(m.Arguments[0]);
            if (m.Arguments.Count > 1)
            {
                // 指定基数
                _builder.Append(",");
                _visitor.Visit(m.Arguments[1]);
            }
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Log10 方法
        /// </summary>
        protected virtual Expression VisitLog10(MethodCallExpression m)
        {
            _builder.Append("LOG10(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Pow 方法
        /// </summary>
        protected virtual Expression VisitPow(MethodCallExpression m)
        {
            _builder.Append("POWER(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");
            _visitor.Visit(m.Arguments[1]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Round 方法
        /// </summary>
        protected virtual Expression VisitRound(MethodCallExpression m)
        {
            _builder.Append("ROUND(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");
            if (m.Arguments.Count == 1)
                _builder.Append(0,null);
            else
                _visitor.Visit(m.Arguments[1]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Sign 方法
        /// </summary>
        protected virtual Expression VisitSign(MethodCallExpression m)
        {
            _builder.Append("SIGN(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Sin 方法
        /// </summary>
        protected virtual Expression VisitSin(MethodCallExpression m)
        {
            _builder.Append("SIN(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Sqrt 方法
        /// </summary>
        protected virtual Expression VisitSqrt(MethodCallExpression m)
        {
            _builder.Append("SQRT(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Tan 方法
        /// </summary>
        protected virtual Expression VisitTan(MethodCallExpression m)
        {
            _builder.Append("TAN(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 Math.Truncate 方法
        /// </summary>
        protected virtual Expression VisitTruncate(MethodCallExpression m)
        {
            _builder.Append("FLOOR(");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Now 属性
        /// </summary>
        protected virtual Expression VisitNow(MemberExpression m)
        {
            _builder.Append("GETDATE()");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Now 属性
        /// </summary>
        protected virtual Expression VisitUtcNow(MemberExpression m)
        {
            _builder.Append("GETUTCDATE()");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Now 属性
        /// </summary>
        protected virtual Expression VisitToday(MemberExpression m)
        {
            _builder.Append("CONVERT(DATE,CONVERT(CHAR(10),GETDATE(),120))");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Date 属性
        /// </summary>
        protected virtual Expression VisitDate(MemberExpression m)
        {
            _builder.Append("CONVERT(CHAR(10),");
            _visitor.Visit(m.Expression);
            _builder.Append(",120)");
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
            return m;
        }

        /// <summary>
        /// 访问 DateTime.Ticks 属性
        /// </summary>
        protected virtual Expression VisitTicks(MemberExpression m)
        {
            // tick = microsecond * 10 1microsecond = 1000nanosecond
            _builder.Append("(DATEDIFF_BIG (NANOSECOND,'1970-1-1',");
            _visitor.Visit(m.Expression);
            _builder.Append(") / 100 + 621355968000000000)");
            return m;
        }

        /// <summary>
        /// 访问 DateTime.TimeOfDay 属性
        /// </summary>
        protected virtual Expression VisitTimeOfDay(MemberExpression m)
        {
            _builder.Append("CONVERT(TIME,CONVERT(VARCHAR,");
            _visitor.Visit(m.Expression);
            _builder.Append(",14))");
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
            return m;
        }

        /// <summary>
        /// 访问 DateTime.DaysInMonth 方法
        /// </summary>
        protected virtual Expression VisitDaysInMonth(MethodCallExpression m)
        {
            if (m != null)
            {
                // 下个月一号减去一天就是上个月最后一天
                // DATEPART(DAY,DATEADD(DAY,-1,DATEADD(MONTH,1,CAST(2019 AS char(4)) + '-' + CAST(10 AS char(2)) + '-1')))
                _builder.Append("DATEPART(DAY,DATEADD(DAY,-1,DATEADD(MONTH,1,CAST(");
                _visitor.Visit(m.Arguments[0]);
                _builder.Append(" AS CHAR(4)) + '-' + CAST(");
                _visitor.Visit(m.Arguments[1]);
                _builder.Append(" AS char(2)) + '-1')))");
            }

            return m;
        }

        /// <summary>
        /// 访问 DateTime.IsLeapYear 方法
        /// </summary>
        protected virtual Expression VisitIsLeapYear(MethodCallExpression m)
        {
            bool isWhere = _visitor.GetType() == typeof(WhereExpressionVisitor);
            if (!isWhere) _builder.Append("CASE WHEN ");

            _builder.Append('(');
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(" % 4 = 0 AND ");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(" % 100 <> 0 OR ");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(" % 400 = 0)");

            if (!isWhere) _builder.Append(" THEN 1 ELSE 0 END");

            return m;
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
        //        _builder.Append("DATEADD(MILLISECOND,");
        //        _builder.Append(_provider.DbValue.GetSqlValue(((TimeSpan)c.Value).TotalMilliseconds,_builder.Token));
        //        _builder.Append(",");
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
        //        _builder.Append("DATEADD(MILLISECOND,DATEDIFF(MILLISECOND,");
        //        _visitor.Visit(b.Arguments[0]);
        //        _builder.Append(",");
        //        _visitor.Visit(b.Object);
        //        _builder.Append("),'1970-01-01')");
        //    }

        //    _visitedMark.Clear();
        //    return b;
        //}

        /// <summary>
        /// 访问 DateTime.AddDays 方法
        /// </summary>
        protected virtual Expression VisitAddDays(MethodCallExpression m)
        {
            _builder.Append("DATEADD(DAY,");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");
            _visitor.Visit(m.Object);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddDays 方法
        /// </summary>
        protected virtual Expression VisitAddHours(MethodCallExpression m)
        {
            _builder.Append("DATEADD(HOUR,");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");
            _visitor.Visit(m.Object);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddMilliseconds 方法
        /// </summary>
        protected virtual Expression VisitAddMilliseconds(MethodCallExpression m)
        {
            _builder.Append("DATEADD(MILLISECOND,");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");
            _visitor.Visit(m.Object);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddMinutes 方法
        /// </summary>
        protected virtual Expression VisitAddMinutes(MethodCallExpression m)
        {
            _builder.Append("DATEADD(MINUTE,");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");
            _visitor.Visit(m.Object);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddMonths 方法
        /// </summary>
        protected virtual Expression VisitAddMonths(MethodCallExpression m)
        {
            _builder.Append("DATEADD(MONTH,");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");
            _visitor.Visit(m.Object);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddSeconds 方法
        /// </summary>
        protected virtual Expression VisitAddSeconds(MethodCallExpression m)
        {
            _builder.Append("DATEADD(SECOND,");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");
            _visitor.Visit(m.Object);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddTicks 方法
        /// </summary>
        protected virtual Expression VisitAddTicks(MethodCallExpression m)
        {
            _builder.Append("DATEADD(MILLISECOND,");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(" / 10000,");
            _visitor.Visit(m.Object);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 DateTime.AddYears 方法
        /// </summary>
        protected virtual Expression VisitAddYears(MethodCallExpression m)
        {
            _builder.Append("DATEADD(YEAR,");
            _visitor.Visit(m.Arguments[0]);
            _builder.Append(",");
            _visitor.Visit(m.Object);
            _builder.Append(')');
            return m;
        }

        /// <summary>
        /// 访问 RowNumber 方法
        /// </summary>
        protected virtual Expression VisitRowNumber(MethodCallExpression m)
        {
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

            _visitor.Visit(m.Object);
            _builder.Append(" LIKE ");
            if (m.Arguments[0].CanEvaluate())
            {
                bool unicode = true;
                string value = this.GetSqlValue(m.Arguments[0].Evaluate(),ref unicode);

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
            return m;
        }

        /// <summary>
        /// 访问 IEnumerable.Contains 方法
        /// </summary>
        protected virtual Expression VisitEnumerableContains(MethodCallExpression m)
        {
            if (m == null) return m;

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
                // => new[] { 1,2,3 }.Contains(a.DemoId)
                var expressions = (exp as NewArrayExpression).Expressions;
                for (int i = 0; i < expressions.Count; i++)
                {
                    _visitor.Visit(expressions[i]);
                    if (i < expressions.Count - 1) _builder.Append(",");
                }
            }
            else if (exp.NodeType == ExpressionType.ListInit)
            {
                // => new List<int> { 1,2,3 }.Contains(a.DemoId)
                var initializers = (exp as ListInitExpression).Initializers;
                for (int i = 0; i < initializers.Count; i++)
                {
                    foreach (var args in initializers[i].Arguments) _visitor.Visit(args);
                    if (i < initializers.Count - 1) _builder.Append(",");
                }
            }
            else if (exp.NodeType == ExpressionType.New)
            {
                // => new List<int>(_demoIdList).Contains(a.DemoId)
                var arguments = (exp as NewExpression).Arguments;
                for (int i = 0; i < arguments.Count; i++) _visitor.Visit(arguments[i]);
            }
            _builder.Append(")");

            return m;
        }

        /// <summary>
        /// 访问 IDbQueryable.Contains 方法
        /// </summary>
        protected virtual Expression VisitQueryableContains(MethodCallExpression m)
        {
            var query = m.Arguments[0].Evaluate().Value as IDbQueryable;
            var cmd = query.Resolve(_builder.Indent + 1,false,_builder.Token != null ? new ResolveToken
            {
                Parameters = _builder.Token.Parameters,
                TableAliasName = "s",
                IsDebug = _builder.Token.IsDebug
            } : null) as MappingCommand;
            _builder.Append("EXISTS(");
            _builder.Append(cmd.CommandText);

            if (cmd.WhereFragment.Length > 0)
                _builder.Append(" AND ");
            else
                _builder.Append("WHERE ");

            _builder.Append(cmd.PickColumnText);
            _builder.Append(" = ");
            _visitor.Visit(m.Arguments[1]);
            _builder.Append(")");

            return m;
        }

        // 生成字符串片断
        protected string GetSqlValue(ConstantExpression c,ref bool unicode)
        {
            unicode = false;
            var visited = _visitedMark.Current;
            MemberInfo member = visited != null ? visited.Member : null;
            Type objType = visited != null && visited.Expression != null ? visited.Expression.Type : null;
            string value = _provider.DbValue.GetSqlValue(c.Value,_builder.Token,member,objType);
            if (!_builder.Parameterized)
            {
                unicode = _provider.DbValue.IsUnicode(visited);
                if (value != null) value = value.TrimStart('N').Trim('\'');
            }

            return value;
        }

        #endregion
    }

    /// <summary>
    /// 方法动态调用枚举
    /// </summary>
    public enum MethodCall
    {
        /// <summary>
        /// 一个表示空合并操作，如节点 (a ?? b)
        /// </summary>
        Coalesce = 1,

        /// <summary>
        /// 表示一个空判断操作，如节点(a.Name == null)
        /// </summary>
        EqualNull = 2,

        /// <summary>
        /// 表示某个方法调用
        /// </summary>
        MethodCall = 3,

        /// <summary>
        /// 表示一个二元运算操作
        /// </summary>
        BinaryCall = 4,

        /// <summary>
        /// 表示访问成员的成员，如 (a.Name.Length)
        /// </summary>
        MemberMember = 5,

        /// <summary>
        /// 表示一个一元运算操作
        /// </summary>
        Unary = 6
    }
}
