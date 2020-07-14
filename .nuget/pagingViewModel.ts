
// export default class
// export let layui: LayUIStatic = window['layui'];
// layui
// 切不可随便升级 layui 的版本，table 组件的源码有改动过

import $ = require("jquery");
import ko = require("knockout");
import kom = require("knockout.mapping");
import utils = require('lib/utils');
import Delegate = require('lib/delegate');

/**
 * 提供分页展示的视图模型
 */
class PagingViewModel {

    private _defaultOptions: any;
    private _slimEvent = false;
    private _bindNodes: any = {};
    private _$element: JQuery;
    private _tabler: Boolean = false;
    private _controller: string = '';
    private _resetCondition: any = {};
    private _btnAdvFilters: Array<any> = [];

    /**
     * layui.table 对象，注意它是一个延迟对象。
     * 必须在调用 render() 方法之后它的引用才可能不为空
     */
    public table: any;

    /**
     * 设置选项
     */
    public options: any;

    /**
     * 查询条件
     */
    public loadCondition: any;

    /**
     * 加载数据地址
     */
    public url: string;

    /**
     * 单元格格式化对象
     */
    public formatters: any;

    /**
     * 单元格中的按钮点击事件对象
     */
    public formatterEvents: any;

    /**
     * id 字段名称，主要用于批量勾选记录时获取记录ID
     */
    public idField: string;

    /**
     * 删除数据发生异常回调
     */
    public onDeleteError: Function;

    /**
     * 删除数据成功回调
     */
    public onDeleteOk: IDataHandler;

    /**
     * 加载数据发生异常回调
     */
    public onLoaded: Delegate;

    /**
     * 分页视图构造函数
     * @param tableid {string} 数据表格id
     * @param options {any} 配置选项
     * @param condition {any} 过滤条件
     */
    constructor(tableid: string, options?: any, condition?: any) {
        var self = this;

        self._$element = tableid ? $(document.getElementById(tableid)) : undefined;
        self._bindNodes = [];
        self.formatters = {};
        self.formatterEvents = {};

        if (!options) options = {};
        if (!condition) condition = {};
        if (typeof options === 'string') options = { url: options };

        //  api 路径 => api/controller  
        var path = options.url ? options.url : window.location.pathname;
        var regex = /(api)?\/?([a-zA-z]{1,})/ig;// /(api)?\/?(?<controller>[a-zA-z]{1,})/ig;
        var matches = regex.exec(path);
        self._controller = matches[2];//matches['groups']['controller'];// FireFox、ie 不支持正则命名分组
        self.idField = options.idField ? options.idField : (self._controller + 'Id');
        self.url = options.url ? options.url : ('api/' + self._controller);

        // laytable 配置选项
        // pageSize=All 查询所有，但会显示分页信息
        var all = options.pageSize === 'All';
        options.elem = '#' + tableid;
        if (options.pageSize === 'All') options.pageSize = 1024;
        if (options.pageSize) options.limit = options.pageSize;
        options.page = !all && options.pageSize && options.pageSize === 1024 ? false : true;
        options.limit = options.pageSize;

        this._setDefaultOptions();
        this.options = $.extend({}, this._defaultOptions, options);
        this.onLoaded = new Delegate();
        this.onLoaded.subscribe(this._onLoadedDefault);

        // laytable 的单元格内置格式化器
        this._setFormatters();

        // 界面查询条件
        this._resetCondition = condition;
        this.loadCondition = $.extend(true, {}, condition);
        utils.observable(this.loadCondition);
    }

    /**
     * 渲染界面。如果 rootNode 为空，则仅渲染表格，不绑定界面对象
     * @param rootNode {any} ko绑定的DOM对象id
     * @param handler {any} 绑定完成后的回调
     */
    public render(rootNode?: any, handler?: any) {
        var self = this;
        // 渲染表格
        if (!self._tabler) self._render();

        // 绑定编辑界面
        if (rootNode) {
            if (utils.isDOM(rootNode)) {
                if (!self._bindNodes[rootNode.id]) {
                    ko.applyBindings(self, rootNode);
                    self._bindNodes[rootNode.id] = true;
                }
            } else {
                var nodes = [];
                if (typeof rootNode === 'string') nodes = [rootNode];
                else if ($.isArray(rootNode)) nodes = rootNode;

                for (var index = 0; index < nodes.length; index++) {
                    if (!self._bindNodes[nodes[index]]) {
                        ko.applyBindings(self, document.getElementById(nodes[index]));
                        self._bindNodes[nodes[index]] = true;
                    }
                }
            }

            if (handler && $.isFunction(handler)) handler.apply(self);
        }
    }

    /**
     * 加载数据
     */
    public loadData() {
        var self = this;
        utils.observable(self.loadCondition);

        if (!self._$element) {
            // 无表格，直接用原生 ajax 请求数据
            var url = self.url;
            var condition = kom.toJS(self.loadCondition || {});
            if (self.options.parseFilter) condition = self.options.parseFilter.call(self, condition);

            // 组织提交方式
            var method = self.options.method || 'GET';
            var contentType = undefined;
            if (method.toUpperCase() === 'POST') {
                contentType = 'application/json; charset=utf-8';
                condition = JSON.stringify(condition);
            }

            utils.ajax(url, {
                type: method,
                contentType: contentType,
                data: condition,
                cache: false,
                success: function (data) {
                    if (data.status !== undefined && data.status == 0) {
                        utils.popError(data.message);
                    } else {
                        self.onLoaded.invoke(self, data)
                    }
                }
            });
        } else {
            // lay table 组件重新加载数据

            // 过滤条件
            var condition = kom.toJS(self.loadCondition || {});
            // 过滤条件+排序字段
            condition.sorts = $.map(self.options.initSort || [], elem => {
                return {
                    fieldName: elem.field,
                    order: elem.type
                };
            });
            // 再一次自定义过滤条件
            if (self.options.parseFilter)
                condition = self.options.parseFilter.call(self, condition);

            // 更新 table 组件配置
            self.options.where = condition;
            self.table.config.where = condition;
            self.table.inst.config.where = condition;

            // 加载数据
            self.table.inst.pullData(self.table.inst.page);
        }
    }

    /**
     * 删除数据项
     * @param row {any} 即将删除的行数据
     */
    public deleteItem(row): JQueryPromise<any> {
        var self = this;
        var array_id = [];
        if (row) {
            array_id.push(row[self.idField]);
        } else {
            var checkStatus = layui.table.checkStatus(self._$element[0].id);
            array_id = $.map(checkStatus.data, x => x[self.idField]);
        }

        if (!array_id.length) {
            return utils.popError(rsc.Sys_Text_CheckDelete, 3000);
        }

        var self = this;
        var msg = rsc.Sys_Text_ConfirmDelete;
        return utils.confirm(msg).then(x => {
            utils.showLoading().then(x => {
                var url = self.url;
                utils.ajax(url, {
                    //type: "DELETE",
                    //data: { "": array_id },//
                    //beforeSend: function () {

                    //},

                    type: 'POST',
                    headers: { 'TZ-HTTP-METHOD': 'DELETE' },
                    data: JSON.stringify(array_id),
                    contentType: 'application/json; charset=utf-8',
                    success: function (data) {
                        if (data.status !== undefined && data.status == 0) {
                            self.onDeleteError ? self.onDeleteError(data.message) : utils.popError(data.message);
                        } else {
                            self.loadData();
                            utils.popOk(rsc.Sys_Text_DeleteOk);
                            if (self.onDeleteOk) self.onDeleteOk(data);
                        }
                    },
                    error: function (jqXHR: JQueryXHR) {
                        if (self.onDeleteError) self.onDeleteError(jqXHR);
                    },
                    complete: function () {
                        utils.hideLoading();
                    }
                });
            });
        });
    }

    /**
     * 搜索框的按键事件
     * @param model {PagingViewModel} 分页视图对象
     * @param e {Event} 事件对象
     */
    public onKeydown(model, e): Boolean {
        if (e.keyCode === 13) {
            var $input = $(e.target);
            $input
                .closest('.input-group')
                .find('.fa-search')
                .parent()
                .focus()
                .trigger('click');

            $input.focus();
            return true;
        }
        return true;
    }

    /**
     * 下载文件
     */
    public download(mode, parseFilter?: Function, ...rests: any[]) {

        var self = this;
        mode = mode || 'download';
        // 过滤条件
        var condition = kom.toJS(self.loadCondition || {});
        // 过滤条件+排序字段
        condition.sorts = $.map(self.options.initSort || [], elem => {
            return {
                fieldName: elem.field,
                order: elem.type
            };
        });
        // 再一次自定义过滤条件
        if (self.options.parseFilter)
            condition = self.options.parseFilter.call(self, condition);

        // 组织提交方式
        var method = self.options.method || 'GET';
        var contentType = undefined;
        if (method.toUpperCase() === 'POST') {
            contentType = 'application/json; charset=utf-8';
            condition = JSON.stringify(condition);
        }

        var args = encodeURIComponent($.param(condition));
        utils.ajax('api/' + self._controller + '?mode=' + mode, {
            async: false,
            type: method,
            contentType: contentType,
            data: condition,
            timeout: 5 * 60 * 1000, // 5分钟超时
            success: data => {
                if (data.status !== undefined && data.status == 0) {
                    // 下载失败
                    utils.popError(data.message);

                } else {
                    // 下载成功
                    if (data.IsBackground == 0) {
                        // 直接下载
                        var href = serverVariables.webSiteUrl + 'Home/Download/' + data.DownloadTaskId;
                        var $lnk = $('<a><span>&nbsp;</span></a>').attr('href', href).css('display', 'none').appendTo($('#header-toolbar'));
                        $lnk.children('span').trigger('click');
                        $lnk.remove();

                    } else {
                        // 异步下载
                        var href = serverVariables.webSiteUrl + 'DownloadTask'
                        var $lnk = $('<a><span>&nbsp;</span></a>').attr('href', href).attr('target', '_blank').css('display', 'none').appendTo($('#header-toolbar'));
                        $lnk.children('span').trigger('click');
                        $lnk.remove();
                    }
                }
            }
        });
    }

    /**
     * 配置高级搜索
     */
    public configAdvFilter(selector?: string, content?: string): void {
        var self = this;
        // bootstrap 弹出框
        var $element = selector ? $(selector) : $('.header-filter .btn-adv-filter');
        $element.popover2({
            animation: true,
            html: true,
            trigger: 'click',
            placement: 'bottom',
            container: '.list-page-header',
            content: m => content ? $(content) : $('.header-filter-template>*'),
            title: m => $('<span style="font-size:10px;"><i class="fa fa-filter"></i>&nbsp;' + rsc.Sys_Text_AdvanceSearch + '</span>')
        });

        self._btnAdvFilters.push($element);

        // 配置自动隐藏
        $(document.body).on('click', e => {
            if (e.target) {
                var pannel2 = $(e.target).closest('.popover');
                if (pannel2.length == 0 && !$(e.target).hasClass('btn-adv-filter') && !$(e.target).closest('.btn-adv-filter').hasClass('btn-adv-filter')) {
                    //var $element = $('.header-filter .btn-adv-filter');
                    var popover = $element.data('bs.popover2');
                    if (popover) popover.hide();
                }
            }
        });

        // 配置回车
        var pannel = content ? $(content) : $('.header-filter-template>*');
        $('.col-sm > input[type="text"]', pannel).on('keydown', e => {
            if (e.keyCode === 13) {
                //$element
                //    .closest('.input-group')
                //    .find('.fa-search')
                //    .parent()
                //    .focus()
                //    .trigger('click');

                //$input.focus();
                $(e.target)
                    .closest('.popover')
                    .find('button:eq(0)')
                    .focus();
                $(e.target).focus();
                self.loadData();

                return true;
            }
            return true;
        });
    }

    /**
     * 隐藏高级搜索
     * @param action {string} 按钮动作
     * @param model {PagingViewModel} 分页视图对象
     * @param e {Event} 事件对象
     */
    public hideAdvFilter(action, model: PagingViewModel, e) {
        var self = this;
        if (action === 'load') model.loadData();

        for (var index = 0; index < self._btnAdvFilters.length; index++) {
            var $element = self._btnAdvFilters[index];
            var popover = $element.data('bs.popover2');
            if (popover) popover.hide();
        }
    }

    /**
     * 重置高级搜索条件
     * @param model {PagingViewModel} 分页视图对象
     * @param e {Event} 事件对象
     */
    public resetAdvFilter(model: PagingViewModel, e) {
        var self = this;
        self._reset(self.loadCondition, self._resetCondition);
        // 下面的写法会报堆栈溢出异常，why?
        // kom.fromJS(self._resetCondition, {}, self.loadCondition);

        // 还原下拉框显示状态
        $(e.target)
            .closest('.list-page-header')
            .find('.selectpicker2')
            .selectpicker('refresh');
        // 重新加载数据
        self.loadData();

        // 关闭下拉框
        for (var index = 0; index < self._btnAdvFilters.length; index++) {
            var $element = self._btnAdvFilters[index];
            var popover = $element.data('bs.popover2');
            if (popover) popover.hide();
        }
    }

    /**
     * 初始化分页查询
     * @param tableid {string} 数据表格id
     * @param options {any} 配置选项
     * @param condition {any} 过滤条件
     */
    static init(tableid: string, options?: any, condition?: any) {
        var model = new this(tableid, options, condition);
        return model;
    }

    /**
     * 计算页面除列表外其它容器已占用调试
     */
    public static _getHeight() {
        // 计算页面除列表外的其它高度
        var $content = $('#content');
        var height: any = {};
        var padding: any = {};
        var margin: any = {};

        // header 容器的高度（含补白、边距）
        height.header = $('#header').outerHeight(true);
        // content-header 容器的高度（含补白、边距）
        height.content_header = $('>.content-header', $content).outerHeight(true);
        // list-page-header 容器的高度（不含补白、边距）
        height.list_page_header = $('.list-page-header', $content).height() || 0;
        // header 容器的阴影高度
        height.shadow = 0;
        // content 容器的补白
        padding.content = {
            top: parseFloat($content.css('padding-top')) || 0,
            bottom: parseFloat($content.css('padding-bottom')) || 0
        }
        // content-body 容器的补白
        padding.content_body = {
            top: parseFloat($('>.content-body', $content).css('padding-top')) || 0,
            bottom: parseFloat($('>.content-body', $content).css('padding-bottom')) || 0
        }
        // list-page 容器的补白
        padding.list_page = {
            top: parseFloat($('.list-page', $content).css('padding-top')) || 0,
            bottom: parseFloat($('.list-page', $content).css('padding-bottom')) || 0
        };
        // list-page-header 容器的补白
        padding.list_page_header = {
            top: parseFloat($('.list-page-header', $content).css('padding-top')) || 0,
            bottom: parseFloat($('.list-page-header', $content).css('padding-bottom')) || 0
        };
        // list-page-header 容器的边距
        margin.list_page_header = {
            top: parseFloat($('.list-page-header', $content).css('margin-top')) || 0,
            bottom: parseFloat($('.list-page-header', $content).css('margin-bottom')) || 0
        };

        var sum = height.header + height.content_header + height.list_page_header + height.shadow +
            padding.content.top + padding.content.bottom +
            padding.content_body.top + padding.content_body.bottom +
            padding.list_page.top + padding.list_page.bottom +
            padding.list_page_header.top + padding.list_page_header.bottom +
            margin.list_page_header.top + margin.list_page_header.bottom;
        if (/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)) {
            // 手持设备，好像总高度少了4个像素。。。
            sum += 5;
            // 手持设备，再加上左边的树高度
            sum += $('#zTree-info', $content).height() || 0;

        } else {
            // 如果不是手持设备，则底部需要留白
            sum += (parseFloat($('>.content-header', $content).css('margin-bottom')) || 0);
        }

        sum = parseInt(sum);
        return sum;
    }

    private _getColumns() {
        var self = this;
        var columns = [];
        var $header = this._$element.find('>thead');

        $header.find('tr').each(function () {
            var column = [];

            $(this).find('th').each(function () {

                if (typeof $(this).data('field') !== 'undefined') {
                    $(this).data('field', $(this).data('field') + '');
                }

                var c = $.extend({}, {
                    title: $(this).html(),
                    rowspan: $(this).attr('rowspan') ? +$(this).attr('rowspan') : undefined,
                    colspan: $(this).attr('colspan') ? +$(this).attr('colspan') : undefined
                }, $(this).data());

                if (c.formatter) {
                    c.templet = row => {
                        var value = row[c.field];
                        var index = row['LAY_INDEX'];
                        var formatter = self.formatters[c.formatter];
                        if (!formatter)
                            throw c.formatter;
                        else
                            return formatter(value, row, index, c.field);
                    };
                }

                // 手机版本，左边不用固定列
                if (/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)) {
                    if (c.fixed !== 'right') c.fixed = undefined;
                }

                column.push(c);
            });
            columns.push(column);
        });
        $header.remove();

        return columns;
    }

    private _setDefaultOptions() {
        var self = this;
        var height: any = PagingViewModel._getHeight();

        self._defaultOptions = {
            loading: false,
            height: 'full-' + height,
            text: {
                none: '没有找到匹配的记录'
            },
            page: false,
            limit: /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) ? 50 : 100,
            limits: [50, 100, 200],
            autoSort: false,
            method: 'GET',
            multiSort: true,
            url: serverVariables.webSiteUrl + self.url,
            headers: utils.xhrHeader,
            request: { pageName: 'pageIndex', limitName: 'pageSize' },
            response: {
                statusName: 'status',
                msgName: 'message',
                statusCode: 1
            },
            parseData: data => {
                if (data.status !== undefined && data.status === 0) {
                    var result: any = {
                        status: 0,
                        message: data.message
                    };
                    return result;
                } else if (data.status !== undefined && data.status === 401) {
                    utils.login(data.status, data.moduleName);
                    var result: any = {
                        status: 1,
                        count: 0,
                        data: []
                    };
                    return result;
                } else {
                    var result: any = {
                        status: 1,
                        count: data.RowCount,
                        data: data.Items
                    };
                    if (data.Totals) result.totalRow = data.Totals;
                    return result;
                }
            },
            parseFilter: false,
            done: (data, index, count, response) => self.onLoaded.invoke(self, data, index, count, response)
        };
    }

    private _setFormatters() {
        var self = this;
        self.formatters['checkboxFormatter'] = utils.checkboxFormatter;
        self.formatters['dateFormatter'] = utils.dateFormatter;
        self.formatters['timeFormatter'] = utils.timeFormatter;
        self.formatters['complexFormatter'] = utils.complexFormatter;
        self.formatters['moneyFormatter'] = utils.moneyFormatter;
    }

    private _render() {
        var self = this;
        if (!self._$element) {
            // 无表格，直接用原生 ajax 请求数据
            var url = self.url;
            var condition = kom.toJS(self.loadCondition || {});
            if (self.options.parseFilter)
                condition = self.options.parseFilter.call(self, condition);

            utils.ajax(url, {
                data: condition,
                cache: false,
                type: self.options.method,
                success: function (data) {
                    if (data.status !== undefined && data.status == 0) {
                        utils.popError(data.message);
                    } else {
                        self.onLoaded.invoke(self, data)
                    }
                }
            });
        } else {
            // 渲染 lay table 组件
            layui.use(['table'], component => {
                var columns = self.options.columns || self._getColumns();
                // 修复列定义
                if (self.options.parseColumns)
                    columns = self.options.parseColumns(columns);

                for (var index = 0; index < columns.length; index++) {
                    for (var i = 0; i < columns[index].length; i++) {
                        var c = columns[index][i];
                        // 手机版本，左边不用固定列
                        if (/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)) {
                            if (c.fixed !== 'right') c.fixed = undefined;
                        }
                    }
                }

                self.options.cols = columns;

                // 过滤条件
                var condition = kom.toJS(self.loadCondition || {});

                // 表格一开始的默认排序
                var inits = $.extend({}, $(self._$element).data());
                if (inits.sortName) {
                    self.options.initSort = [{
                        field: inits.sortName,
                        type: inits.sortOrder || 'asc'
                    }];
                }
                // 过滤条件+排序字段
                condition.sorts = $.map(self.options.initSort || [], elem => {
                    return {
                        fieldName: elem.field,
                        order: elem.type
                    };
                });

                // 再一次自定义过滤条件
                if (self.options.parseFilter)
                    condition = self.options.parseFilter.call(self, condition);
                self.options.where = condition;

                // 渲染表格
                self.table = layui.table.render(self.options);
                self.table.destroy = self._destroy.bind(self);

                // 监听列排序
                var layAttribute = self._$element.attr('lay-filter');
                layui.table.on('sort(' + layAttribute + ')', function (obj) {
                    // 过滤条件
                    var sortFilter = $.extend({}, kom.toJS(self.loadCondition || {}));
                    // 过滤条件+排序字段
                    sortFilter.sorts = $.map(obj, elem => {
                        return {
                            fieldName: elem.field,
                            order: elem.type
                        };
                    });

                    // 再一次自定义过滤条件
                    if (self.options.parseFilter)
                        sortFilter = self.options.parseFilter.call(self, sortFilter);

                    // 更新 table 组件配置
                    self.options.where = sortFilter;
                    self.table.config.where = sortFilter;
                    self.table.inst.config.where = sortFilter;
                    // 排序说明
                    self.options.initSort = obj;
                    self.table.config.initSort = obj;
                    self.table.inst.config.initSort = obj;

                    // 加载数据
                    self.table.inst.pullData(self.table.inst.page);
                });

                // 监听行事件
                layui.table.on('tool(' + layAttribute + ')', function (obj) {
                    self.formatterEvents[obj.event](obj.event, obj.data, obj);
                });

                // 监听选中事件
                layui.table.on('row(' + layAttribute + ')', function (obj) {
                    obj.tr
                        .addClass('layui-table-click')
                        .siblings()
                        .removeClass('layui-table-click');
                });
            });
        }

        self._tabler = true;
    }

    private _reset(obj, defaults) {
        var self = this;
        for (var i in obj) {
            if (obj.hasOwnProperty(i)) {
                if (!ko.isObservable(obj[i])) {
                    var value = obj[i];
                    var d = defaults ? defaults[i] : undefined;
                    if ($.isArray(value))
                        obj[i] = d || [];
                    else if ($.isPlainObject(value))
                        self._reset(obj[i], d);
                    else
                        obj[i] = d || '';

                } else {
                    var value = obj[i]();
                    var d = defaults ? defaults[i] : undefined;
                    if ($.isArray(value))
                        obj[i](d || []);
                    else if ($.isPlainObject(value))
                        self._reset(value, d);
                    else
                        obj[i](d || '');

                }
            }
        }
    }

    private _onLoadedDefault(data) {
        // 第一次加载后，后面所有的请求都需要显示 正在加载 的状态
        this.options.loading = true;
        if (this.table) this.table.config.loading = true;

        // 结束加载状态
        if (this.options.hidePageLoader !== false) {
            $.when($('#page-loader').removeClass('show')).done(e => $('#page-loader').addClass('d-none'));
        }
    }

    // 注销 laytable 组件
    private _destroy() {
        var self = this;
        self._tabler = false;
        if (self.table) {
            self.table.inst.elem.remove();
            // 清理缓存数据 ??
        }
    }
}

export = PagingViewModel;