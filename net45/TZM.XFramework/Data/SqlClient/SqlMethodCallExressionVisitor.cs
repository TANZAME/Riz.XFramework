
using System;
using System.Linq;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace TZM.XFramework.Data.SqlClient
{
    /// <summary>
    /// <see cref="SqlMethodCallExressionVisitor"/> 表达式访问器
    /// </summary>
    public class SqlMethodCallExressionVisitor : MethodCallExressionVisitor
    {
        /// <summary>
        /// 实例化 <see cref="SqlMethodCallExressionVisitor"/> 类的新实例
        /// </summary>
        public SqlMethodCallExressionVisitor(IDbQueryProvider provider, ExpressionVisitorBase visitor)
            : base(provider, visitor)
        {
        }
    }
}
