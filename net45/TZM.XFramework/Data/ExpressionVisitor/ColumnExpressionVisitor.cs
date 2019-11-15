using System;
using System.Data;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace TZM.XFramework.Data
{
    /// <summary>
    /// 选择列表达式解析器
    /// </summary>
    public class ColumnExpressionVisitor : ExpressionVisitorBase
    {
        private static IDictionary<DbExpressionType, string> _statisMethods = null;
        private IDbQueryProvider _provider = null;
        private TableAliasCache _aliases = null;
        private IDbQueryableInfo_Select _qQuery = null;
        private DbExpression _groupBy = null;
        private List<DbExpression> _include = null;

        private ColumnCollection _pickColumns = null;
        private NavigationCollection _navigations = null;
        private List<string> _navChainHopper = null;
        private int _startLength = 0;
        private string _pickColumnText = null;

        /// <summary>
        /// 选中字段，Column 对应实体的原始属性
        /// </summary>
        public ColumnCollection PickColumns
        {
            get { return _pickColumns; }
        }

        /// <summary>
        /// 选中字段的文本，给 Contains 表达式用
        /// </summary>
        public string PickColumnText
        {
            get
            {
                if (_pickColumnText == null)
                {
                    int count = _builder.Length - _startLength;
                    if (count > 0)
                    {
                        char[] chars = new char[count];
                        _builder.CopyTo(_startLength, chars, 0, count);
                        _pickColumnText = new String(chars);
                    }
                }

                return _pickColumnText;
            }
        }

        /// <summary>
        /// 导航属性描述信息
        /// <para>
        /// 从 <see cref="IDataReader"/> 到实体的映射需要使用这些信息来给导航属性赋值
        /// </para>
        /// </summary>
        public NavigationCollection Navigations
        {
            get { return _navigations; }
        }

        static ColumnExpressionVisitor()
        {
            _statisMethods = new Dictionary<DbExpressionType, string>
            {
                { DbExpressionType.Count,"COUNT" },
                { DbExpressionType.Max,"MAX" },
                { DbExpressionType.Min,"MIN" },
                { DbExpressionType.Average,"AVG" },
                { DbExpressionType.Sum,"SUM" }
            };
        }

        /// <summary>
        /// 初始化 <see cref="ColumnExpressionVisitor"/> 类的新实例
        /// </summary>
        public ColumnExpressionVisitor(IDbQueryProvider provider, TableAliasCache aliases, IDbQueryableInfo_Select qQuery)
            : base(provider, aliases, qQuery.SelectExpression.Expressions != null ? qQuery.SelectExpression.Expressions[0] : null)
        {
            _provider = provider;
            _aliases = aliases;
            _qQuery = qQuery;
            _groupBy = qQuery.GroupByExpression;
            _include = qQuery.Includes;

            if (_pickColumns == null) _pickColumns = new ColumnCollection();
            _navigations = new NavigationCollection();
            _navChainHopper = new List<string>(10);
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        public override void Write(ISqlBuilder builder)
        {
            if (base.Expression != null)
            {
                base._builder = builder;
                _builder.AppendNewLine();
                _startLength = _builder.Length;
                if (base._methodVisitor == null)
                    base._methodVisitor = _provider.CreateMethodVisitor(this);

                // SELECT 表达式解析
                if (base.Expression.NodeType != ExpressionType.Constant) base.Write(builder);
                else
                {
                    // if have no select syntax
                    Type type = (base.Expression as ConstantExpression).Value as Type;
                    this.VisitAllMember(type, "t0");
                }
                // Include 表达式解析<导航属性>
                this.VisitInclude();
                // 去掉空白字符
                _builder.TrimEnd(' ', ',');
            }
        }

        // p=>p p=>p.t p=>p.Id
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            LambdaExpression lambda = node as LambdaExpression;
            if (lambda.Body.NodeType == ExpressionType.Parameter)
            {
                // 例： a=> a
                Type type = lambda.Body.Type;
                string alias = _aliases.GetTableAlias(lambda);
                this.VisitAllMember(type, alias);
                return node;
            }
            else if (lambda.Body.CanEvaluate())
            {
                // 例：a=>1
                base.Visit(lambda.Body.Evaluate());
                // 选择字段
                string newName = _pickColumns.Add(Constant.CONSTANTNAME);
                // 添加字段别名
                _builder.AppendAs(newName);
                return node;
            }
            else if (lambda.Body.NodeType == ExpressionType.MemberAccess)
            {
                // 例： t=> t.a
                // => SELECT a.ClientId
                Type type = lambda.Body.Type;
                if (!TypeUtils.IsPrimitiveType(type)) return this.VisitAllMember(type, _aliases.GetTableAlias(lambda.Body), node);
                else
                {
                    var newNode = this.VisitWithoutRemark(x => base.VisitLambda(node));
                    string newName = _pickColumns.Add((lambda.Body as MemberExpression).Member.Name);
                    return newNode;
                }
            }
            else
            {
                var newNode = base.VisitLambda(node);
                if (_pickColumns.Count == 0)
                {
                    // 选择字段
                    string newName = _pickColumns.Add(Constant.CONSTANTNAME);
                    // 添加字段别名
                    _builder.AppendAs(newName);
                }
                return newNode;
            }
        }

        // => new App() {Id = p.Id}}
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            // 如果有一对多的导航属性会产生嵌套的SQL，这时需要强制主表选择的列里面必须包含导航外键
            // TODO #对 Bindings 进行排序，保证导航属性的赋值一定要最后面#
            // 未实现，在书写表达式时人工保证 ##

            if (node.NewExpression != null) this.VisitNewImpl(node.NewExpression);
            if (_navChainHopper.Count == 0) _navChainHopper.Add(node.Type.Name);

            for (int i = 0; i < node.Bindings.Count; i++)
            {
                MemberAssignment binding = node.Bindings[i] as MemberAssignment;
                if (binding == null) throw new XFrameworkException("Only 'MemberAssignment' binding supported.");

                Type propertyType = (node.Bindings[i].Member as System.Reflection.PropertyInfo).PropertyType;
                bool isNavigation = !TypeUtils.IsPrimitiveType(propertyType);

                #region 一般属性

                // 非导航属性
                if (!isNavigation)
                {
                    if (binding.Expression.CanEvaluate())
                        _builder.Append(binding.Expression.Evaluate().Value, binding.Member, node.Type);
                    else
                        this.VisitWithoutRemark(x => this.VisitMemberBinding(binding));

                    // 选择字段
                    this.AddPickColumn(binding.Member.Name);
                }

                #endregion 一般属性

                #region 导航属性

                else
                {
                    // 非显式指定的导航属性需要有 ForeignKeyAttribute
                    if (binding.Expression.NodeType == ExpressionType.MemberAccess && binding.Expression.Acceptable())
                    {
                        var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(binding.Member.DeclaringType);
                        var attribute = typeRuntime.GetInvokerAttribute<ForeignKeyAttribute>(binding.Member.Name);
                        if (attribute == null) throw new XFrameworkException("Complex property {{{0}}} must mark 'ForeignKeyAttribute' ", binding.Member.Name);
                    }

                    // 生成导航属性描述集合，以类名.属性名做为键值
                    int n = _navChainHopper.Count;
                    string keyName = _navChainHopper.Count > 0 ? _navChainHopper[_navChainHopper.Count - 1] : string.Empty;
                    keyName = !string.IsNullOrEmpty(keyName) ? keyName + "." + binding.Member.Name : binding.Member.Name;
                    Navigation nav = new Navigation(keyName, binding.Member);
                    if (!_navigations.ContainsKey(keyName))
                    {
                        // fix issue# spliton 列占一个位
                        nav.Start = _pickColumns.Count;
                        nav.FieldCount = GetFieldCount(binding.Expression) + (binding.Expression.NodeType == ExpressionType.MemberAccess && binding.Expression.Acceptable() ? 1 : 0);
                        _navigations.Add(keyName, nav);
                        _navChainHopper.Add(keyName);
                    }

                    // 1.不显式指定导航属性，例：a.Client.ClientList
                    // 2.表达式里显式指定导航属性，例：b
                    if (binding.Expression.NodeType == ExpressionType.MemberAccess) this.VisitNavigation(binding.Expression as MemberExpression, binding.Expression.Acceptable());
                    else if (binding.Expression.NodeType == ExpressionType.New) this.VisitNewImpl(binding.Expression as NewExpression);
                    else if (binding.Expression.NodeType == ExpressionType.MemberInit) this.VisitMemberInit(binding.Expression as MemberInitExpression);

                    // 恢复访问链
                    // 在访问导航属性时可能是 Client.CloudServer，这时要恢复为 Client，以保证能访问 Client 的下一个导航属性
                    if (_navChainHopper.Count != n) _navChainHopper.RemoveAt(_navChainHopper.Count - 1);
                }

                #endregion 导航属性
            }

            return node;
        }

        // => Client = a.Client.CloudServer
        private Expression VisitNavigation(MemberExpression node, bool visitNavigation, Expression pickExpression = null)
        {
            string alias = string.Empty;
            Type type = node.Type;

            if (node.Acceptable())
            {
                // 例： Client = a.Client.CloudServer
                // fix issue# Join 表达式显式指定导航属性时时，alias 为空
                // fix issue# 多个导航属性时 AppendNullColumn 只解析当前表达式的
                int index = 0;
                int num = this.NavMembers != null ? this.NavMembers.Count : 0;
                alias = this.VisitNavMember(node);

                if (num != this.NavMembers.Count)
                {
                    foreach (var kvp in NavMembers)
                    {
                        index += 1;
                        if (index < NavMembers.Count && index > num) alias = _aliases.GetNavigationTableAlias(kvp.Key);
                        else
                        {
                            alias = _aliases.GetNavigationTableAlias(kvp.Key);
                            type = kvp.Value.Type;
                        }
                    }
                }
                else
                {
                }
            }
            else
            {
                // 例： Client = b
                alias = _aliases.GetTableAlias(node);
                type = node.Type;
            }


            if (type.IsGenericType) type = type.GetGenericArguments()[0];
            if (pickExpression == null) this.VisitAllMember(type, alias);
            else
            {
                // Include 中选中的字段
                Expression expr = pickExpression;
                if (expr.NodeType == ExpressionType.Lambda) expr = (pickExpression as LambdaExpression).Body;
                if (expr.NodeType == ExpressionType.New)
                {
                    var newExpression = expr as NewExpression;
                    for (int i = 0; i < newExpression.Arguments.Count; i++)
                    {
                        var memberExpression = newExpression.Arguments[i] as MemberExpression;
                        if (memberExpression == null) throw new XFrameworkException("MemberExpression required at the {0} arguments.", i);

                        _builder.AppendMember(alias, memberExpression.Member.Name);
                        this.AddPickColumn(newExpression.Members != null ? newExpression.Members[i].Name : memberExpression.Member.Name);
                    }
                }
                else if (expr.NodeType == ExpressionType.MemberInit)
                {
                    var initExpression = expr as MemberInitExpression;
                    for (int i = 0; i < initExpression.Bindings.Count; i++)
                    {
                        var binding = initExpression.Bindings[i] as MemberAssignment;
                        if (binding == null) throw new XFrameworkException("Only 'MemberAssignment' binding supported.");

                        var memberExpression = binding.Expression as MemberExpression;
                        if (memberExpression == null) throw new XFrameworkException("MemberExpression required at the {0} arguments.", i);

                        _builder.AppendMember(alias, memberExpression.Member.Name);
                        this.AddPickColumn(binding.Member.Name);
                    }
                }
                else
                {
                    throw new NotSupportedException(string.Format("Include method not supporte {0}", expr.NodeType));
                }
            }

            if (visitNavigation) AddSplitOnColumn(node.Member, alias);

            return node;
        }

        // => new  {Id = p.Id}
        protected override Expression VisitNew(NewExpression node)
        {
            // TODO 未支持匿名类的导航属性
            // MemberInit的New
            // 匿名类的New
            if (node == null) return node;
            if (node.Arguments == null || node.Arguments.Count == 0)
                throw new XFrameworkException("'NewExpression' do not have any argument.");

            this.VisitNewImpl(node);

            return node;
        }

        // 遍历New表达式的参数集
        private Expression VisitNewImpl(NewExpression node)
        {
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                Expression expression = node.Arguments[i];
                Type type = expression.Type;
                int visitedQty = _visitedMark.Count;

                if (expression.NodeType == ExpressionType.Parameter)
                {
                    //例： new Client(a)
                    string alias = _aliases.GetTableAlias(expression);
                    this.VisitAllMember(type, alias);
                }
                else if (expression.CanEvaluate())
                {
                    //例： DateTime.Now
                    _builder.Append(expression.Evaluate().Value, node.Members[i], node.Type);
                    this.AddPickColumn(node.Members != null ? node.Members[i].Name : (expression as MemberExpression).Member.Name);
                }
                else if (expression.NodeType == ExpressionType.MemberAccess || expression.NodeType == ExpressionType.Call)
                {
                    bool isNavigation = !type.IsEnum && !TypeUtils.IsPrimitiveType(type);
                    if (isNavigation) this.VisitNavigation(expression as MemberExpression, false);
                    else
                    {
                        // new Client(a.ClientId)
                        this.Visit(expression);
                        this.AddPickColumn(node.Members != null ? node.Members[i].Name : (expression as MemberExpression).Member.Name);
                    }
                }
                else
                {
                    base.Visit(expression);
                    this.AddPickColumn(node.Members != null ? node.Members[i].Name : (expression as MemberExpression).Member.Name);
                }

                // 删除本次访问的成员痕迹
                if (_visitedMark.Count != visitedQty) _visitedMark.Remove(_visitedMark.Count - visitedQty);
            }

            return node;
        }

        // g.Key.CompanyName & g.Max(a)
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node == null) return node;
            else if (_groupBy == null || !node.IsGrouping()) return base.VisitMember(node);
            else
            {
                // Group By 解析  CompanyName = g.Key.Name
                LambdaExpression keySelector = _groupBy.Expressions[0] as LambdaExpression;
                Expression exp = null;
                Expression body = keySelector.Body;

                if (body.NodeType == ExpressionType.MemberAccess)
                {
                    // group xx by a.CompanyName
                    exp = body;

                    //
                    //
                    //
                    //
                }
                else if (body.NodeType == ExpressionType.New)
                {
                    // group xx by new { Name = a.CompanyName  }

                    string memberName = node.Member.Name;
                    NewExpression newExp = body as NewExpression;
                    int index = newExp.Members.IndexOf(x => x.Name == memberName);
                    exp = newExp.Arguments[index];
                }

                return this.Visit(exp);
            }
        }

        // 选择所有的字段
        private Expression VisitAllMember(Type type, string alias, Expression node = null)
        {
            if (_groupBy != null && node != null && node.IsGrouping())
            {
                // select g.Key
                LambdaExpression keySelector = _groupBy.Expressions[0] as LambdaExpression;
                return this.Visit(keySelector.Body);
            }
            else
            {
                TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(type);
                MemberInvokerCollection invokers = typeRuntime.Invokers;

                foreach (var invoker in invokers)
                {
                    if (invoker != null && invoker.Column != null && invoker.Column.NoMapped) continue;
                    if (invoker != null && invoker.ForeignKey != null) continue; // 不加载导航属性
                    if (invoker.Member.MemberType == System.Reflection.MemberTypes.Method) continue;

                    _builder.AppendMember(alias, invoker.Member.Name);

                    // 选择字段
                    string newName = _pickColumns.Add(invoker.Member.Name);
                    _builder.AppendAs(newName);
                    _builder.Append(",");
                    _builder.AppendNewLine();
                }
            }

            return node;
        }

        // g.Max(a=>a.Level)
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (_groupBy != null && node.IsGrouping())
            {
                DbExpressionType dbExpressionType = DbExpressionType.None;
                Enum.TryParse(node.Method.Name, out dbExpressionType);
                Expression exp = dbExpressionType == DbExpressionType.Count
                    ? Expression.Constant(1)
                    : (node.Arguments.Count == 1 ? null : node.Arguments[1]);
                if (exp.NodeType == ExpressionType.Lambda) exp = (exp as LambdaExpression).Body;

                // 如果是 a=> a 这种表达式，那么一定会指定 elementSelector
                if (exp.NodeType == ExpressionType.Parameter) exp = _groupBy.Expressions[1];

                _builder.Append(_statisMethods[dbExpressionType]);
                _builder.Append("(");
                this.Visit(exp);
                _builder.Append(")");

                return node;
            }

            return base.VisitMethodCall(node);
        }

        // 遍历 Include 包含的导航属性
        private void VisitInclude()
        {
            if (_include == null || _include.Count == 0) return;

            foreach (var dbExpression in _include)
            {
                Expression navExpression = dbExpression.Expressions[0];
                Expression pickExpression = dbExpression.Expressions.Length > 1 ? dbExpression.Expressions[1] : null;
                if (navExpression == null) continue;

                if (navExpression.NodeType == ExpressionType.Lambda) navExpression = (navExpression as LambdaExpression).Body;
                MemberExpression memberExpression = navExpression as MemberExpression;
                if (memberExpression == null) throw new XFrameworkException("Include expression body must be 'MemberExpression'.");

                // 例：Include(a => a.Client.AccountList[0].Client)
                // 解析导航属性链
                List<Expression> chain = new List<Expression>();
                while (memberExpression != null)
                {
                    // a.Client 要求 <Client> 必须标明 ForeignKeyAttribute
                    TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(memberExpression.Expression.Type);
                    ForeignKeyAttribute attribute = typeRuntime.GetInvokerAttribute<ForeignKeyAttribute>(memberExpression.Member.Name);
                    if (attribute == null) throw new XFrameworkException("Include member {{{0}}} must mark 'ForeignKeyAttribute'.", memberExpression);

                    MemberExpression m = null;
                    chain.Add(memberExpression);
                    if (memberExpression.Expression.NodeType == ExpressionType.MemberAccess) m = (MemberExpression)memberExpression.Expression;
                    else if (memberExpression.Expression.NodeType == ExpressionType.Call) m = (memberExpression.Expression as MethodCallExpression).Object as MemberExpression;

                    //var m = memberExpression.Expression as MemberExpression;
                    if (m == null) chain.Add(memberExpression.Expression);
                    memberExpression = m;
                }

                // 生成导航属性描述信息
                string keyName = string.Empty;
                for (int i = chain.Count - 1; i >= 0; i--)
                {
                    Expression expression = chain[i];
                    memberExpression = expression as MemberExpression;
                    if (memberExpression == null) continue;

                    keyName = memberExpression.GetKeyWidthoutAnonymous(true);
                    if (!_navigations.ContainsKey(keyName))
                    {
                        // fix issue# SplitOn 列占一个位
                        var nav = new Navigation(keyName, memberExpression.Member);
                        nav.Start = i == 0 ? _pickColumns.Count : -1;
                        nav.FieldCount = i == 0 ? (GetFieldCount(pickExpression == null ? navExpression : pickExpression) + 1) : -1;
                        _navigations.Add(keyName, nav);
                    }
                }

                this.VisitNavigation(memberExpression, true, pickExpression);
            }
        }

        // 添加额外列，用来判断整个（左）连接记录是否为空
        private void AddSplitOnColumn(System.Reflection.MemberInfo member, string alias)
        {
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(member.DeclaringType);
            var fkAttribute = typeRuntime.GetInvokerAttribute<ForeignKeyAttribute>(member.Name);
            string keyName = fkAttribute.OuterKeys[0];

            _builder.Append("CASE WHEN ");
            _builder.AppendMember(alias, keyName);
            _builder.Append(" IS NULL THEN NULL ELSE ");
            _builder.AppendMember(alias, keyName);
            _builder.Append(" END");

            // 选择字段
            string newName = _pickColumns.Add(Constant.NAVIGATIONSPLITONNAME);
            //_builder.Append(caseWhen);
            _builder.AppendAs(newName);
            _builder.Append(',');
            _builder.AppendNewLine();
        }

        // 缓存选中字段
        private void AddPickColumn(string memberName)
        {
            string newName = _pickColumns.Add(memberName);
            _builder.AppendAs(newName);
            _builder.Append(',');
            _builder.AppendNewLine();
        }

        // 计算数据库字段数量
        private static int GetFieldCount(Expression node)
        {
            int num = 0;
            if (node.NodeType == ExpressionType.Lambda) node = (node as LambdaExpression).Body;

            switch (node.NodeType)
            {
                case ExpressionType.MemberInit:
                    var initExpression = node as MemberInitExpression;
                    foreach (var exp in initExpression.NewExpression.Arguments)
                    {
                        if (TypeUtils.IsPrimitiveType(exp.Type))
                            num += 1;
                        else
                            num += _countComplex(exp);
                    }
                    foreach (MemberAssignment member in initExpression.Bindings)
                    {
                        num += _countPrimitive(((System.Reflection.PropertyInfo)member.Member).PropertyType);
                    }

                    break;

                case ExpressionType.MemberAccess:
                    var memberExpression = node as MemberExpression;
                    num += _countComplex(memberExpression);

                    break;

                case ExpressionType.New:
                    var newExpression = node as NewExpression;
                    //foreach (var exp in newExpression.Arguments) num += _countComplex(exp);
                    if (newExpression.Members != null)
                    {
                        foreach (var member in newExpression.Members)
                            num += _countPrimitive(((System.Reflection.PropertyInfo)member).PropertyType);
                    }

                    break;
            }

            return num;
        }


        // 基元类型计数器
        private static Func<Type, int> _countPrimitive = type => TypeUtils.IsPrimitiveType(type) ? 1 : 0;
        // 复合类型计数器
        private static Func<Expression, int> _countComplex = exp =>
              exp.NodeType == ExpressionType.MemberAccess && TypeUtils.IsPrimitiveType(exp.Type) ? 1 : TypeRuntimeInfoCache.GetRuntimeInfo(exp.Type.IsGenericType ? exp.Type.GetGenericArguments()[0] : exp.Type).DataFieldCount;
    }
}