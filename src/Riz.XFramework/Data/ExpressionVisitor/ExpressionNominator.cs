using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 试算给定的表达式
    /// </summary>
    public class ExpressionNominator : ExpressionVisitor
    {
        private readonly Func<Expression, bool> _canBeEvaluated;
        private HashSet<Expression> _candidates;
        private bool _cannotBeEvaluated;

        /// <summary>
        /// 初始化 <see cref="ExpressionNominator"/> 类的新实例
        /// </summary>
        public ExpressionNominator()
          : this(new Func<Expression, bool>(ExpressionNominator.CanBeEvaluatedLocally))
        {
        }

        /// <summary>
        /// 初始化 <see cref="ExpressionNominator"/> 类的新实例
        /// </summary>
        /// <param name="fnCanBeEvaluated">表示表达式是否能被提前计算的委托</param>
        public ExpressionNominator(Func<Expression, bool> fnCanBeEvaluated)
        {
            this._canBeEvaluated = fnCanBeEvaluated ?? new Func<Expression, bool>(ExpressionNominator.CanBeEvaluatedLocally);
        }

        /// <summary>
        /// 试算表达式
        /// </summary>
        /// <param name="expression">要访问的表达式</param>
        /// <returns></returns>
        public HashSet<Expression> Nominate(Expression expression)
        {
            this._candidates = new HashSet<Expression>();
            this.Visit(expression);
            return this._candidates;
        }

        /// <summary>
        /// 将表达式调度到此类中更专用的访问方法之一
        /// </summary>
        /// <param name="expression">要访问的表达式</param>
        /// <returns></returns>
        public override Expression Visit(Expression expression)
        {
            if (expression != null)
            {
                bool cannotBeEvaluated = this._cannotBeEvaluated;
                this._cannotBeEvaluated = false;
                base.Visit(expression);

                if (!this._cannotBeEvaluated)
                {
                    if (this._canBeEvaluated(expression))
                    {
                        if (expression.NodeType != ExpressionType.Constant)
                            this._candidates.Add(expression);
                    }
                    else
                    {
                        this._cannotBeEvaluated = true;
                    }
                }

                this._cannotBeEvaluated = this._cannotBeEvaluated | cannotBeEvaluated;
            }
            return expression;
        }

        private static bool CanBeEvaluatedLocally(Expression exp)
        {
            return exp.NodeType != ExpressionType.Parameter && exp.NodeType != ExpressionType.MemberInit && exp.NodeType != ExpressionType.New;
        }
    }
}