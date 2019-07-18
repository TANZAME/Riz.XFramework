
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
    public class NpgMethodCallExressionVisitor : MethodCallExressionVisitor
    {
        private ISqlBuilder _builder = null;
        private ExpressionVisitorBase _visitor = null;

        #region 构造函数

        /// <summary>
        /// 实例化 <see cref="NpgMethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        public NpgMethodCallExressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
            : base(provider, visitor)
        {
            _visitor = visitor;
            _builder = visitor.SqlBuilder;

            // 替换掉解析器
            base.Container.Replace(typeof(string), (provider2, visitor2) => new NpgStringExpressionVisitor(provider2, visitor2));
            base.Container.Replace(typeof(SqlMethod), (provider2, visitor2) => new NpgSqlMethodExpressionVisitor(provider2, visitor2));
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

        #endregion
    }
}
