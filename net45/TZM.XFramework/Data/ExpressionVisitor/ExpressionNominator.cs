
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TZM.XFramework.Data
{
    public class ExpressionNominator : ExpressionVisitor
    {
        private readonly Func<Expression, bool> _canBeEvaluated;
        private HashSet<Expression> _candidates;
        private bool _cannotBeEvaluated;

        public ExpressionNominator()
          : this(new Func<Expression, bool>(ExpressionNominator.CanBeEvaluatedLocally))
        {
        }

        public ExpressionNominator(Func<Expression, bool> fnCanBeEvaluated)
        {
            this._canBeEvaluated = fnCanBeEvaluated ?? new Func<Expression, bool>(ExpressionNominator.CanBeEvaluatedLocally);
        }

        private static bool CanBeEvaluatedLocally(Expression exp)
        {
            return exp.NodeType != ExpressionType.Parameter && exp.NodeType != ExpressionType.MemberInit && exp.NodeType != ExpressionType.New;
        }

        public HashSet<Expression> Nominate(Expression expression)
        {
            this._candidates = new HashSet<Expression>();
            this.Visit(expression);
            return this._candidates;
        }

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
    }


}