
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ICS.XFramework.Data
{
    public class Nominator : ExpressionVisitor
    {
        private readonly Func<Expression, bool> m_fnCanBeEvaluated;
        private HashSet<Expression> m_candidates;
        private bool m_cannotBeEvaluated;

        public Nominator()
          : this(new Func<Expression, bool>(Nominator.CanBeEvaluatedLocally))
        {
        }

        public Nominator(Func<Expression, bool> fnCanBeEvaluated)
        {
            this.m_fnCanBeEvaluated = fnCanBeEvaluated ?? new Func<Expression, bool>(Nominator.CanBeEvaluatedLocally);
        }

        private static bool CanBeEvaluatedLocally(Expression exp)
        {
            return exp.NodeType != ExpressionType.Parameter && exp.NodeType != ExpressionType.MemberInit && exp.NodeType != ExpressionType.New;
        }

        public HashSet<Expression> Nominate(Expression expression)
        {
            this.m_candidates = new HashSet<Expression>();
            this.Visit(expression);
            return this.m_candidates;
        }

        public override Expression Visit(Expression expression)
        {
            if (expression != null)
            {
                bool cannotBeEvaluated = this.m_cannotBeEvaluated;
                this.m_cannotBeEvaluated = false;
                base.Visit(expression);

                if (!this.m_cannotBeEvaluated)
                {
                    if (this.m_fnCanBeEvaluated(expression))
                    {
                        if (expression.NodeType != ExpressionType.Constant)
                            this.m_candidates.Add(expression);
                    }
                    else
                    {
                        this.m_cannotBeEvaluated = true;
                    }
                }

                this.m_cannotBeEvaluated = this.m_cannotBeEvaluated | cannotBeEvaluated;
            }
            return expression;
        }
    }


}