using System;
using System.Linq;
using System.Data;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Riz.XFramework.Data
{
    /// <summary>
    /// 选择列表达式解析器
    /// </summary>
    public class ColumnExpressionVisitor : LinqExpressionVisitor
    {
        private DbQuerySelectTree _tree = null;
        private DbExpression _groupBy = null;
        private List<DbExpression> _include = null;
        private TableAliasResolver _aliasResolver = null;
        private static IDictionary<DbExpressionType, string> _aggregateMethods = null;

        private ColumnDescriptorCollection _pickColumns = null;
        private NavDescriptorCollection _navDescriptors = null;
        private List<string> _navDescriptorKeys = null;
        private int _startLength = 0;
        private string _pickColumnText = null;

        /// <summary>
        /// 选中字段，Column 对应实体的原始属性
        /// </summary>
        public ColumnDescriptorCollection PickColumns
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
        /// 选中的导航属性描述信息
        /// <para>
        /// 从 <see cref="IDataReader"/> 到实体的映射需要使用这些信息来给导航属性赋值
        /// </para>
        /// </summary>
        /// <remarks>只有选择语义才需要字段-实体映射描述。所以 Navigations</remarks>
        public NavDescriptorCollection PickNavDescriptors
        {
            get { return _navDescriptors; }
        }

        static ColumnExpressionVisitor()
        {
            _aggregateMethods = new Dictionary<DbExpressionType, string>
            {
                { DbExpressionType.Count, "COUNT" },
                { DbExpressionType.Max, "MAX" },
                { DbExpressionType.Min, "MIN" },
                { DbExpressionType.Average, "AVG" },
                { DbExpressionType.Sum, "SUM" }
            };
        }

        /// <summary>
        /// 初始化 <see cref="ColumnExpressionVisitor"/> 类的新实例
        /// </summary>
        /// <param name="aliasResolver">表别名解析器</param>
        /// <param name="tree">查询语义</param>
        public ColumnExpressionVisitor(TableAliasResolver aliasResolver, DbQuerySelectTree tree)
            : base(aliasResolver, tree.Select.Expressions != null ? tree.Select.Expressions[0] : null)
        {
            _tree = tree;
            _groupBy = tree.GroupBy;
            _include = tree.Includes;
            _aliasResolver = aliasResolver;

            if (_pickColumns == null) _pickColumns = new ColumnDescriptorCollection();
            _navDescriptors = new NavDescriptorCollection();
            _navDescriptorKeys = new List<string>(10);
        }

        /// <summary>
        /// 将表达式所表示的SQL片断写入SQL构造器
        /// </summary>
        /// <param name="builder">SQL 语句生成器</param>
        public override void Write(ISqlBuilder builder)
        {
            this.Initialize(builder);
            if (base.Expression != null)
            {
                _builder.AppendNewLine();
                _startLength = _builder.Length;

                // SELECT 表达式解析
                if (base.Expression.NodeType != ExpressionType.Constant) base.Write(builder);
                else
                {
                    // 选择所有字段
                    Type type = (base.Expression as ConstantExpression).Value as Type;
                    this.VisitAllMember(type, "t0", base.Expression);
                }

                // Include 表达式解析<导航属性>
                this.VisitInclude();
                // 最后去掉空白字符
                _builder.TrimEnd(' ', ',');
            }
        }

        /// <summary>
        /// 访问 Lambda 表达式，如 p=>p p=>p.t p=>p.Id
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="node">Lambda 表达式</param>
        /// <returns></returns>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            LambdaExpression lambda = node as LambdaExpression;
            if (lambda.Body.NodeType == ExpressionType.Parameter)
            {
                // 例： a=> a
                Type type = lambda.Body.Type;
                string alias = _aliasResolver.GetTableAlias(lambda);
                this.VisitAllMember(type, alias);
                return node;
            }
            else if (lambda.Body.CanEvaluate())
            {
                // 例：a=>1
                base.Visit(lambda.Body.Evaluate());
                // 选择字段
                string newName = _pickColumns.Add(AppConst.CONSTANT_COLUMN_NAME);
                // 添加字段别名
                _builder.AppendAs(newName);
                return node;
            }
            else if (lambda.Body.NodeType == ExpressionType.MemberAccess)
            {
                // 例： t=> t.a
                // => SELECT a.ClientId
                Type type = lambda.Body.Type;
                if (!TypeUtils.IsPrimitiveType(type)) return this.VisitAllMember(type, _aliasResolver.GetTableAlias(lambda.Body), node);
                else
                {
                    var newNode = this.VisitWithoutRemark(_ => base.VisitLambda(node));

                    var memberExpression = lambda.Body as MemberExpression;
                    string memberName = memberExpression.Member.Name;

                    var srcDbExpressionType = _builder.TranslateContext.DbExpressionType;
                    var srcIsOutQUery = _builder.TranslateContext.IsOutQuery;

                    if (!(srcDbExpressionType != null && srcDbExpressionType == DbExpressionType.Select && srcIsOutQUery != null && srcIsOutQUery.Value))
                    {
                        // 非外层字段，SELECT xx As name 的 name 使用数据库原生字段
                        memberName = TypeUtils.GetFieldName(memberExpression.Member, memberExpression.Expression.Type);
                    }

                    string newName = _pickColumns.Add(memberName);
                    return newNode;
                }
            }
            else
            {
                // 例：a => a.DemoCode，选择字段仅选一个基元类型字段。
                // 结果可能返回一个比如字符串类型列表
                var newNode = base.VisitLambda(node);
                if (_pickColumns.Count == 0)
                {
                    // 选择字段
                    string newName = _pickColumns.Add(AppConst.CONSTANT_COLUMN_NAME);
                    // 添加字段别名
                    _builder.AppendAs(newName);
                }
                return newNode;
            }
        }

        /// <summary>
        /// 访问成员初始化表达式，如 => new App { Name = "Name" }
        /// </summary>
        /// <param name="node">要访问的成员初始化表达式</param>
        /// <returns></returns>
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (_navDescriptorKeys.Count == 0)
                _navDescriptorKeys.Add(node.Type.Name);

            // New 表达式
            if (node.NewExpression != null)
                this.VisitNewImpl(node.NewExpression, false);
            // 赋值表达式
            var newBindings = node.Bindings.OrderBy(a => TypeUtils.IsPrimitive(a.Member) ? 0 : 1);
            foreach (MemberAssignment m in newBindings)
            {
                Type newType = node.Type;
                this.VisitWithoutRemark(_ => this.VisitMemberAssignmentImpl(newType, m));
            }

            return node;
        }

        // => Client = a.Client.CloudServer
        private Expression VisitNavigation(MemberExpression node, bool visitNavigation, Expression pickExpression = null, Expression predicate = null)
        {
            string alias = string.Empty;
            Type type = node.Type;

            if (node.Visitable())
            {
                // 例： Client = a.Client.CloudServer
                // Fix issue# Join 表达式显式指定导航属性时时，alias 为空
                // Fix issue# 多个导航属性时 AppendNullColumn 只解析当前表达式的
                int index = 0;
                int num = this.NavMembers != null ? this.NavMembers.Count : 0;
                alias = this.VisitNavMember(node);

                if (num != this.NavMembers.Count)
                {
                    foreach (var nav in this.NavMembers)
                    {
                        index += 1;
                        if (index < this.NavMembers.Count && index > num) alias = _aliasResolver.GetNavTableAlias(nav.Key);
                        else
                        {
                            alias = _aliasResolver.GetNavTableAlias(nav.Key);
                            type = nav.Expression.Type;
                            if (index == this.NavMembers.Count) nav.Predicate = predicate;
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
                alias = _aliasResolver.GetTableAlias(node);
                type = node.Type;
            }


            if (type.IsGenericType) type = type.GetGenericArguments()[0];
            if (pickExpression == null) this.VisitAllMember(type, alias);
            else
            {
                // Include 中选中的字段
                Expression expression = pickExpression;
                if (expression.NodeType == ExpressionType.Lambda) expression = (pickExpression as LambdaExpression).Body;
                if (expression.NodeType == ExpressionType.New)
                {
                    var newExpression = expression as NewExpression;
                    for (int i = 0; i < newExpression.Arguments.Count; i++)
                    {
                        base.Visit(newExpression.Arguments[i]);
                        this.AddPickColumn(newExpression.Members[i], newExpression.Type);
                    }
                }
                else if (expression.NodeType == ExpressionType.MemberInit)
                {
                    var initExpression = expression as MemberInitExpression;
                    for (int i = 0; i < initExpression.Bindings.Count; i++)
                    {
                        var binding = initExpression.Bindings[i] as MemberAssignment;
                        base.Visit(binding.Expression);
                        this.AddPickColumn(binding.Member, initExpression.Type);
                    }
                }
                else
                {
                    throw new NotSupportedException(string.Format("Include method not support {0}", expression.NodeType));
                }
            }

            if (visitNavigation)
                AddSplitOnColumn(node, alias);

            return node;
        }

        // => Name = "Name" 
        private void VisitMemberAssignmentImpl(Type newType, MemberAssignment m)
        {
            // 先添加当前字段的访问痕迹标记
            _visitedStack.Add(m.Member, newType);

            if (TypeUtils.IsPrimitive(m.Member))
            {
                this.VisitMemberBinding(m);
                // 选择字段
                this.AddPickColumn(m.Member, newType);
            }
            else
            {
                // 非显式指定的导航属性需要有 ForeignKeyAttribute
                if (m.Expression.NodeType == ExpressionType.MemberAccess && m.Expression.Visitable())
                {
                    var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(newType);
                    var attribute = typeRuntime.GetForeignKeyAttribute(m.Member.Name);
                    // 不能当做导航属性的复杂字段
                    if (attribute == null) return;
                }

                // 生成导航属性描述集合，以类名.属性名做为键值
                int n = _navDescriptorKeys.Count;
                string keyId = _navDescriptorKeys.Count > 0 ? _navDescriptorKeys[_navDescriptorKeys.Count - 1] : string.Empty;
                keyId = !string.IsNullOrEmpty(keyId) ? keyId + "." + m.Member.Name : m.Member.Name;
                var nav = new NavDescriptor(keyId, m.Member);
                if (!_navDescriptors.Contains(keyId))
                {
                    // Fix issue# spliton 列占一个位
                    nav.StartIndex = _pickColumns.Count;
                    nav.FieldCount = AggregateField(m.Expression) + (m.Expression.NodeType == ExpressionType.MemberAccess && m.Expression.Visitable() ? 1 : 0);
                    _navDescriptors.Add(nav);
                    _navDescriptorKeys.Add(keyId);
                }

                // 1.不显式指定导航属性，例：a.Client.ClientList
                // 2.表达式里显式指定导航属性，例：b
                if (m.Expression.NodeType == ExpressionType.MemberAccess) this.VisitNavigation(m.Expression as MemberExpression, m.Expression.Visitable());
                else if (m.Expression.NodeType == ExpressionType.New) this.VisitNewImpl(m.Expression as NewExpression, false);
                else if (m.Expression.NodeType == ExpressionType.MemberInit) this.VisitMemberInit(m.Expression as MemberInitExpression);

                // 恢复访问链
                // 在访问导航属性时可能是 Client.CloudServer，这时要恢复为 Client，以保证能访问 Client 的下一个导航属性
                if (_navDescriptorKeys.Count != n) _navDescriptorKeys.RemoveAt(_navDescriptorKeys.Count - 1);
            }
        }

        /// <summary>
        /// 访问构造函数表达式，如 => new { Name = "Name" }
        /// </summary>
        /// <param name="node">构造函数调用的表达式</param>
        /// <returns></returns>
        protected override Expression VisitNew(NewExpression node)
        {
            return this.VisitNewImpl(node, true);
        }

        // 遍历New表达式的参数集
        private Expression VisitNewImpl(NewExpression node, bool checkArguments)
        {
            // TODO 未支持匿名类的导航属性、MemberInit的New、匿名类的New
            if (node == null) return node;
            if (checkArguments && (node.Arguments == null || node.Arguments.Count == 0))
                throw new XFrameworkException("NewExpression at least one parameter is required");

            for (int i = 0; i < node.Arguments.Count; i++)
            {
                Type newType = node.Type;
                Expression argument = node.Arguments[i];
                MemberInfo member = node.Members != null ? node.Members[i] : null;
                if (member == null)
                {
                    var memberExpression = argument as MemberExpression;
                    if (memberExpression != null) member = memberExpression.Member;
                }
                this.VisitWithoutRemark(_ => this.VisitNewArgumentImpl(newType, member, argument));
            }

            return node;
        }

        // 访问 New 表达式中的参数
        private Expression VisitNewArgumentImpl(Type newType, MemberInfo member, Expression argument)
        {
            // 先添加当前字段的访问痕迹标记
            if (member != null) _visitedStack.Add(member, newType);

            if (argument.NodeType == ExpressionType.Parameter)
            {
                //例： new Client(a)
                string alias = _aliasResolver.GetTableAlias(argument);
                this.VisitAllMember(argument.Type, alias);
            }
            else if (argument.CanEvaluate())
            {
                //例： DateTime.Now
                _builder.Append(argument.Evaluate().Value, _visitedStack.Current);
                this.AddPickColumn(member, newType);
                //this.AddPickColumn(member.Name);
            }
            else if (argument.NodeType == ExpressionType.MemberAccess || argument.NodeType == ExpressionType.Call)
            {
                bool isNavigation = !argument.Type.IsEnum && !TypeUtils.IsPrimitiveType(argument.Type);
                if (isNavigation) this.VisitNavigation(argument as MemberExpression, false);
                else
                {
                    // new Client(a.ClientId)
                    this.Visit(argument);
                    this.AddPickColumn(member, newType);
                    //this.AddPickColumn(member.Name);
                }
            }
            else
            {
                if (member == null) throw new XFrameworkException("{0} is not support for NewExpression's arguments.");
                base.Visit(argument);
                this.AddPickColumn(member, newType);
                //this.AddPickColumn(member.Name);
            }

            return argument;
        }

        /// <summary>
        /// 访问字段或者属性表达式，如 g.Key.CompanyName、g.Max(a)
        /// </summary>
        /// <param name="node">字段或者成员表达式</param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            // 例： g.Max(b=>b.xxxx)
            bool byElementSelector = false;
            if (_groupBy != null && _groupBy.Expressions.Length == 2)
            {
                //group new { ClientId2 = a.RizClientId, b.RizAccountId } by new { ClientId = a.RizClientId, b.RizAccountId } into g
                //select new
                //{
                //    Max = g.Max(b => b.RizAccountId)
                //};

                var exp = _groupBy.Expressions[1];
                if (exp.NodeType == ExpressionType.Lambda) exp = (exp as LambdaExpression).Body;
                byElementSelector = node.Expression.Type == exp.Type && TypeUtils.IsAnonymousType(exp.Type);
            }

            if (node == null) return node;
            else if (_groupBy == null || !node.IsGrouping())
            {
                if (!byElementSelector) return base.VisitMember(node);
                else
                {
                    LambdaExpression elmentSelector = _groupBy.Expressions[1] as LambdaExpression;
                    NewExpression newExpression = elmentSelector.Body as NewExpression;
                    int index = newExpression.Members.IndexOf(x => x.Name == node.Member.Name);
                    var exp = newExpression.Arguments[index];
                    return this.Visit(exp);
                }
            }
            else
            {
                string memberName = node.Member.Name;

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
                    NewExpression newExpression = body as NewExpression;
                    int index = newExpression.Members.IndexOf(x => x.Name == memberName);
                    exp = newExpression.Arguments[index];
                }
                else if (body.NodeType == ExpressionType.MemberInit)
                {
                    // group xx by new App { Name = a.CompanyName  }
                    MemberInitExpression initExpression = body as MemberInitExpression;
                    int index = initExpression.Bindings.IndexOf(x => x.Member.Name == memberName);
                    exp = ((MemberAssignment)initExpression.Bindings[index]).Expression;
                }

                return this.Visit(exp);
            }
        }

        /// <summary>
        /// 选择所有的字段
        /// </summary>
        /// <param name="type">实体类型</param>
        /// <param name="alias">表别名</param>
        /// <param name="node">节点</param>
        /// <returns></returns>
        protected virtual Expression VisitAllMember(Type type, string alias, Expression node = null)
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
                foreach (var item in typeRuntime.Members)
                {
                    var m = item as FieldAccessorBase;
                    if (m == null || !m.IsDbField) continue;

                    _builder.AppendMember(alias, m.Member, type);
                    this.AddPickColumn(m.Member, type);
                }
            }

            return node;
        }

        /// <summary>
        /// 访问方法表达式，如 g.Max(a=>a.Level)
        /// </summary>
        /// <param name="node">方法表达式</param>
        /// <returns></returns>
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
                if (exp.NodeType == ExpressionType.Lambda) exp = (exp as LambdaExpression).Body;

                _builder.Append(_aggregateMethods[dbExpressionType]);
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
                Expression predicateExpression = dbExpression.Expressions.Length > 2 ? dbExpression.Expressions[2] : null;
                if (navExpression == null) continue;

                if (navExpression.NodeType == ExpressionType.Lambda) navExpression = (navExpression as LambdaExpression).Body;
                var memberExpression = navExpression as MemberExpression;
                if (memberExpression == null) throw new XFrameworkException("Include expression body must be 'MemberExpression'.");

                // 例：Include(a => a.Client.AccountList[0].Client)
                // 解析导航属性链
                List<Expression> chain = new List<Expression>();
                while (memberExpression != null)
                {
                    // a.Client 要求 <Client> 必须标明 ForeignKeyAttribute
                    var typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(memberExpression.Expression.Type);
                    var attribute = typeRuntime.GetForeignKeyAttribute(memberExpression.Member.Name);
                    if (attribute == null)
                        throw new XFrameworkException("ForeignKeyAttribute not found for include member {0}.{1}", typeRuntime.Type.Name, memberExpression.Member.Name);

                    MemberExpression m = null;
                    chain.Add(memberExpression);
                    if (memberExpression.Expression.NodeType == ExpressionType.MemberAccess) m = (MemberExpression)memberExpression.Expression;
                    else if (memberExpression.Expression.NodeType == ExpressionType.Call) m = (memberExpression.Expression as MethodCallExpression).Object as MemberExpression;

                    //var m = memberExpression.Expression as MemberExpression;
                    if (m == null) chain.Add(memberExpression.Expression);
                    memberExpression = m;
                }

                // 生成导航属性描述信息
                string keyId = string.Empty;
                for (int index = chain.Count - 1; index >= 0; index--)
                {
                    Expression expression = chain[index];
                    memberExpression = expression as MemberExpression;
                    if (memberExpression == null) continue;

                    keyId = memberExpression.GetKeyWidthoutAnonymous(true);
                    if (!_navDescriptors.Contains(keyId))
                    {
                        // Fix issue# SplitOn 列占一个位
                        var nav = new NavDescriptor(keyId, memberExpression.Member);
                        nav.StartIndex = index == 0 ? _pickColumns.Count : -1;
                        nav.FieldCount = index == 0 ? (AggregateField(pickExpression == null ? navExpression : pickExpression) + 1) : -1;
                        _navDescriptors.Add(nav);
                    }
                }

                this.VisitNavigation(memberExpression, true, pickExpression, predicateExpression);
            }
        }

        // 添加额外列，用来判断整个（左）连接记录是否为空
        private void AddSplitOnColumn(MemberExpression m, string alias)
        {
            TypeRuntimeInfo typeRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(m.Expression.Type);
            var attribute = typeRuntime.GetForeignKeyAttribute(m.Member.Name);
            string keyName = attribute.OuterKeys.FirstOrDefault(a => !a.StartsWith(AppConst.CONSTANT_FOREIGNKEY, StringComparison.Ordinal));

            _builder.Append("CASE WHEN ");
            _builder.AppendMember(alias, keyName);
            _builder.Append(" IS NULL THEN NULL ELSE ");
            _builder.AppendMember(alias, keyName);
            _builder.Append(" END");

            // 选择字段
            string newName = _pickColumns.Add(AppConst.NAVIGATION_SPLITON_NAME);
            _builder.AppendAs(newName);
            _builder.Append(',');
            _builder.AppendNewLine();
        }

        // 记录选中字段
        private void AddPickColumn(MemberInfo m, Type reflectedType)
        {
            string memberName = m.Name;
            var srcDbExpressionType = _builder.TranslateContext.DbExpressionType;
            var srcIsOutQuery = _builder.TranslateContext.IsOutQuery;

            if (!(srcDbExpressionType == DbExpressionType.Select && srcIsOutQuery == true))
            {
                // 非外层字段，SELECT xx As name 的 name 使用数据库原生字段
                memberName = TypeUtils.GetFieldName(m, reflectedType);
            }
            // 选择字段
            string newName = _pickColumns.Add(memberName);
            _builder.AppendAs(newName);
            _builder.Append(",");
            _builder.AppendNewLine();
        }

        // 累加表达式中数据库字段的数量
        private static int AggregateField(Expression node)
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
                    foreach (MemberAssignment b in initExpression.Bindings)
                    {
                        num += _countPrimitive(b.Member);
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
                            num += _countPrimitive(member);
                    }

                    break;
            }

            return num;
        }

        // 基元类型计数器
        private static Func<MemberInfo, int> _countPrimitive = member => TypeUtils.IsPrimitive(member) ? 1 : 0;
        // 复合类型计数器
        private static Func<Expression, int> _countComplex = exp =>
              exp.NodeType == ExpressionType.MemberAccess && TypeUtils.IsPrimitiveType(exp.Type)
                ? 1
                : TypeRuntimeInfoCache.GetRuntimeInfo(exp.Type.IsGenericType ? exp.Type.GetGenericArguments()[0] : exp.Type).Members.Count(a => a is FieldAccessorBase && ((FieldAccessorBase)a).IsDbField);
    }
}