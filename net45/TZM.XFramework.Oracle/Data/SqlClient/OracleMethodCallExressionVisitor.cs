
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
        private ExpressionVisitorBase _visitor = null;

        #region 构造函数

        /// <summary>
        /// 实例化 <see cref="OracleMethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        public OracleMethodCallExressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
            : base(provider, visitor)
        {
            _visitor = visitor;
            _builder = visitor.SqlBuilder;

            // 替换掉解析器
            base.Container.Replace(typeof(string), (provider2, visitor2) => new OracleStringExpressionVisitor(provider2, visitor2));
            base.Container.Replace(typeof(SqlMethod), (provider2, visitor2) => new OracleSqlMethodExpressionVisitor(provider2, visitor2));
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 访问表示 null 合并运算的节点 a ?? b
        /// </summary>
        public override Expression VisitCoalesce(BinaryExpression b)
        {
            // 例： a.Name ?? "TAN" => ISNULL(a.Name,'TAN')
            Expression left = b.Left.NodeType == ExpressionType.Constant ? b.Right : b.Left;
            Expression right = b.Left.NodeType == ExpressionType.Constant ? b.Left : b.Right;

            _builder.Append("NVL(");
            _visitor.Visit(left);
            _builder.Append(',');
            _visitor.Visit(right);
            _builder.Append(')');


            return b;
        }

        /// <summary>
        /// 访问一元运算符
        /// </summary>
        /// <param name="node">一元运算符表达式</param>
        /// <returns></returns>
        public override Expression VisitUnary(UnaryExpression node)
        {
            // ORACLE的AVG函数，DataReader.GetValue()会抛异常
            if (node.NodeType == ExpressionType.Convert)
            {
                string name = "";
                if (node.Type == typeof(float)) name = "BINARY_FLOAT";
                else if (node.Type == typeof(double)) name = "BINARY_DOUBLE";
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

        #endregion
    }
}
