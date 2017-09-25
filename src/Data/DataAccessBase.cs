﻿/*
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2017 Zongsoft Corporation <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.CoreLibrary.
 *
 * Zongsoft.CoreLibrary is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * Zongsoft.CoreLibrary is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * Lesser General Public License for more details.
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with Zongsoft.CoreLibrary; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示数据访问的抽象基类。
	/// </summary>
	public abstract class DataAccessBase : IDataAccess
	{
		#region 事件定义
		public event EventHandler<DataCountedEventArgs> Counted;
		public event EventHandler<DataCountingEventArgs> Counting;
		public event EventHandler<DataExecutedEventArgs> Executed;
		public event EventHandler<DataExecutingEventArgs> Executing;
		public event EventHandler<DataExistedEventArgs> Existed;
		public event EventHandler<DataExistingEventArgs> Existing;
		public event EventHandler<DataIncrementedEventArgs> Incremented;
		public event EventHandler<DataIncrementingEventArgs> Incrementing;
		public event EventHandler<DataDeletedEventArgs> Deleted;
		public event EventHandler<DataDeletingEventArgs> Deleting;
		public event EventHandler<DataInsertedEventArgs> Inserted;
		public event EventHandler<DataInsertingEventArgs> Inserting;
		public event EventHandler<DataUpdatedEventArgs> Updated;
		public event EventHandler<DataUpdatingEventArgs> Updating;
		public event EventHandler<DataSelectedEventArgs> Selected;
		public event EventHandler<DataSelectingEventArgs> Selecting;
		#endregion

		#region 成员字段
		private IDataAccessNaming _naming;
		private ICollection<IDataAccessFilter> _filters;
		#endregion

		#region 构造函数
		protected DataAccessBase()
		{
			_naming = new DataAccessNaming();
			_filters = new List<IDataAccessFilter>();
		}

		protected DataAccessBase(IDataAccessNaming naming)
		{
			if(naming == null)
				throw new ArgumentNullException(nameof(naming));

			_naming = naming;
			_filters = new List<IDataAccessFilter>();
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取数据访问名称映射器。
		/// </summary>
		public IDataAccessNaming Naming
		{
			get
			{
				return _naming;
			}
		}

		/// <summary>
		/// 获取数据访问过滤器集合。
		/// </summary>
		public ICollection<IDataAccessFilter> Filters
		{
			get
			{
				return _filters;
			}
		}
		#endregion

		#region 获取主键
		public string[] GetKey<T>()
		{
			return this.GetKey(this.GetName<T>());
		}

		public abstract string[] GetKey(string name);
		#endregion

		#region 执行方法
		public IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, Func<DataExecutionContext, bool> executing = null, Action<DataExecutionContext> executed = null)
		{
			IDictionary<string, object> outParameters;
			return this.Execute<T>(name, inParameters, out outParameters, executing, executed);
		}

		public IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, Func<DataExecutionContext, bool> executing = null, Action<DataExecutionContext> executed = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateExecutionContext(name, false, typeof(T), inParameters);

			//处理数据访问操作前的回调
			if(executing != null && executing(context))
			{
				//设置默认的返回参数值
				outParameters = context.OutParameters;

				//返回委托回调的结果
				return context.Result as IEnumerable<T>;
			}

			//激发“Executing”事件，如果被中断则返回
			if(this.OnExecuting(context))
			{
				//设置默认的返回参数值
				outParameters = context.OutParameters;

				//返回事件执行后的结果
				return context.Result as IEnumerable<T>;
			}

			//调用数据访问过滤器前事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltering(ctx));

			//执行数据操作方法
			this.OnExecute<T>(context);

			//调用数据访问过滤器后事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltered(ctx));

			//激发“Executed”事件
			this.OnExecuted(context);

			//处理数据访问操作后的回调
			if(executed != null)
				executed(context);

			//再次更新返回参数值
			outParameters = context.OutParameters;

			//返回最终的结果
			return context.Result as IEnumerable<T>;
		}

		public object ExecuteScalar(string name, IDictionary<string, object> inParameters, Func<DataExecutionContext, bool> executing = null, Action<DataExecutionContext> executed = null)
		{
			IDictionary<string, object> outParameters;
			return this.ExecuteScalar(name, inParameters, out outParameters, executing, executed);
		}

		public object ExecuteScalar(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, Func<DataExecutionContext, bool> executing = null, Action<DataExecutionContext> executed = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateExecutionContext(name, true, typeof(object), inParameters);

			//处理数据访问操作前的回调
			if(executing != null && executing(context))
			{
				//设置默认的返回参数值
				outParameters = context.OutParameters;

				//返回委托回调的结果
				return context.Result;
			}

			//激发“Executing”事件，如果被中断则返回
			if(this.OnExecuting(context))
			{
				//设置默认的返回参数值
				outParameters = context.OutParameters;

				//返回事件执行后的结果
				return context.Result;
			}

			//调用数据访问过滤器前事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltering(ctx));

			//执行数据操作方法
			this.OnExecuteScalar(context);

			//调用数据访问过滤器后事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltered(ctx));

			//激发“Executed”事件
			this.OnExecuted(context);

			//处理数据访问操作后的回调
			if(executed != null)
				executed(context);

			//再次更新返回参数值
			outParameters = context.OutParameters;

			//返回最终的结果
			return context.Result;
		}

		protected abstract void OnExecute<T>(DataExecutionContext context);
		protected abstract void OnExecuteScalar(DataExecutionContext context);
		#endregion

		#region 存在方法
		public bool Exists<T>(ICondition condition, Func<DataExistenceContext, bool> existing = null, Action<DataExistenceContext> existed = null)
		{
			return this.Exists(this.GetName<T>(), condition, existing, existed);
		}

		public bool Exists(string name, ICondition condition, Func<DataExistenceContext, bool> existing = null, Action<DataExistenceContext> existed = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateExistenceContext(name, condition);

			//处理数据访问操作前的回调
			if(existing != null && existing(context))
				return context.Result;

			//激发“Existing”事件，如果被中断则返回
			if(this.OnExisting(context))
				return context.Result;

			//调用数据访问过滤器前事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltering(ctx));

			//执行存在操作方法
			this.OnExists(context);

			//调用数据访问过滤器后事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltered(ctx));

			//激发“Existed”事件
			this.OnExisted(context);

			//处理数据访问操作后的回调
			if(existed != null)
				existed(context);

			//返回最终的结果
			return context.Result;
		}

		protected abstract void OnExists(DataExistenceContext context);
		#endregion

		#region 计数方法
		public int Count<T>(ICondition condition, string includes = null, Func<DataCountContext, bool> counting = null, Action<DataCountContext> counted = null)
		{
			return this.Count(this.GetName<T>(), condition, includes, counting, counted);
		}

		public int Count(string name, ICondition condition, string includes = null, Func<DataCountContext, bool> counting = null, Action<DataCountContext> counted = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateCountContext(name, condition, includes);

			//处理数据访问操作前的回调
			if(counting != null && counting(context))
				return context.Result;

			//激发“Counting”事件，如果被中断则返回
			if(this.OnCounting(context))
				return context.Result;

			//调用数据访问过滤器前事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltering(ctx));

			//执行计数操作方法
			this.OnCount(context);

			//调用数据访问过滤器后事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltered(ctx));

			//激发“Counted”事件
			this.OnCounted(context);

			//处理数据访问操作后的回调
			if(counted != null)
				counted(context);

			//返回最终的结果
			return context.Result;
		}

		protected abstract void OnCount(DataCountContext context);
		#endregion

		#region 递增方法
		public long Increment<T>(string member, ICondition condition, int interval = 1, Func<DataIncrementContext, bool> incrementing = null, Action<DataIncrementContext> incremented = null)
		{
			return this.Increment(this.GetName<T>(), member, condition, interval, incrementing, incremented);
		}

		public long Increment(string name, string member, ICondition condition, int interval = 1, Func<DataIncrementContext, bool> incrementing = null, Action<DataIncrementContext> incremented = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(string.IsNullOrEmpty(member))
				throw new ArgumentNullException(nameof(member));

			//创建数据访问上下文对象
			var context = this.CreateIncrementContext(name, member, condition, interval);

			//处理数据访问操作前的回调
			if(incrementing != null && incrementing(context))
				return context.Result;

			//激发“Incrementing”事件
			if(this.OnIncrementing(context))
				return context.Result;

			//调用数据访问过滤器前事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltering(ctx));

			//执行递增操作方法
			this.OnIncrement(context);

			//调用数据访问过滤器后事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltered(ctx));

			//激发“Incremented”事件
			this.OnIncremented(context);

			//处理数据访问操作后的回调
			if(incremented != null)
				incremented(context);

			//返回最终的结果
			return context.Result;
		}

		public long Decrement<T>(string member, ICondition condition, int interval = 1, Func<DataIncrementContext, bool> decrementing = null, Action<DataIncrementContext> decremented = null)
		{
			return this.Increment(this.GetName<T>(), member, condition, -interval, decrementing, decremented);
		}

		public long Decrement(string name, string member, ICondition condition, int interval = 1, Func<DataIncrementContext, bool> decrementing = null, Action<DataIncrementContext> decremented = null)
		{
			return this.Increment(name, member, condition, -interval, decrementing, decremented);
		}

		protected abstract void OnIncrement(DataIncrementContext context);
		#endregion

		#region 删除方法
		public int Delete<T>(ICondition condition, params string[] cascades)
		{
			return this.Delete(this.GetName<T>(), condition, cascades, null, null);
		}

		public int Delete<T>(ICondition condition, string[] cascades, Func<DataDeletionContext, bool> deleting, Action<DataDeletionContext> deleted)
		{
			return this.Delete(this.GetName<T>(), condition, cascades, deleting, deleted);
		}

		public int Delete(string name, ICondition condition, params string[] cascades)
		{
			return this.Delete(name, condition, cascades, null, null);
		}

		public int Delete(string name, ICondition condition, string[] cascades, Func<DataDeletionContext, bool> deleting, Action<DataDeletionContext> deleted)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(cascades != null && cascades.Length == 1)
				cascades = cascades[0].Split(',', ';');

			//创建数据访问上下文对象
			var context = this.CreateDeletionContext(name, condition, cascades);

			//处理数据访问操作前的回调
			if(deleting != null && deleting(context))
				return context.Count;

			//激发“Deleting”事件，如果被中断则返回
			if(this.OnDeleting(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltering(ctx));

			//执行数据删除操作
			this.OnDelete(context);

			//调用数据访问过滤器后事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltered(ctx));

			//激发“Deleted”事件
			this.OnDeleted(context);

			//处理数据访问操作后的回调
			if(deleted != null)
				deleted(context);

			//返回最终结果
			return context.Count;
		}

		protected abstract void OnDelete(DataDeletionContext context);
		#endregion

		#region 插入方法
		public int Insert<T>(T data, string scope = null, Func<DataInsertionContext, bool> inserting = null, Action<DataInsertionContext> inserted = null)
		{
			if(data == null)
				return 0;

			return this.Insert(this.GetName(data.GetType()), data, scope, inserting, inserted);
		}

		public int Insert(string name, object data, string scope = null, Func<DataInsertionContext, bool> inserting = null, Action<DataInsertionContext> inserted = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(data == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateInsertionContext(name, false, data, scope);

			//处理数据访问操作前的回调
			if(inserting != null && inserting(context))
				return context.Count;

			//激发“Inserting”事件，如果被中断则返回
			if(this.OnInserting(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltering(ctx));

			//执行数据插入操作
			this.OnInsert(context);

			//调用数据访问过滤器后事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltered(ctx));

			//激发“Inserted”事件
			this.OnInserted(context);

			//处理数据访问操作后的回调
			if(inserted != null)
				inserted(context);

			//返回最终的结果
			return context.Count;
		}

		public int InsertMany<T>(IEnumerable<T> items, string scope = null, Func<DataInsertionContext, bool> inserting = null, Action<DataInsertionContext> inserted = null)
		{
			return this.InsertMany(this.GetName<T>(), items, scope, inserting, inserted);
		}

		public int InsertMany(string name, IEnumerable items, string scope = null, Func<DataInsertionContext, bool> inserting = null, Action<DataInsertionContext> inserted = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(items == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateInsertionContext(name, true, items, scope);

			//处理数据访问操作前的回调
			if(inserting != null && inserting(context))
				return context.Count;

			//激发“Inserting”事件，如果被中断则返回
			if(this.OnInserting(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltering(ctx));

			//执行数据插入操作
			this.OnInsert(context);

			//调用数据访问过滤器后事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltered(ctx));

			//激发“Inserted”事件
			this.OnInserted(context);

			//处理数据访问操作后的回调
			if(inserted != null)
				inserted(context);

			//返回最终的结果
			return context.Count;
		}

		protected abstract void OnInsert(DataInsertionContext context);
		#endregion

		#region 更新方法
		public int Update<T>(T data, ICondition condition = null, string scope = null, Func<DataUpdationContext, bool> updating = null, Action<DataUpdationContext> updated = null)
		{
			if(data == null)
				return 0;

			return this.Update(this.GetName(data.GetType()), data, condition, scope, updating, updated);
		}

		public int Update<T>(T data, string scope, ICondition condition = null, Func<DataUpdationContext, bool> updating = null, Action<DataUpdationContext> updated = null)
		{
			if(data == null)
				return 0;

			return this.Update(this.GetName(data.GetType()), data, condition, scope, updating, updated);
		}

		/// <summary>
		/// 根据指定的条件将指定的实体更新到数据源。
		/// </summary>
		/// <param name="name">指定的实体映射名。</param>
		/// <param name="data">要更新的实体对象。</param>
		/// <param name="condition">要更新的条件子句，如果为空(null)则根据实体的主键进行更新。</param>
		/// <param name="scope">指定的要更新的和排除更新的属性名列表，如果指定的是多个属性则属性名之间使用逗号(,)分隔；要排除的属性以减号(-)打头，星号(*)表示所有属性，感叹号(!)表示排除所有属性；如果未指定该参数则默认只会更新所有单值属性而不会更新导航属性。</param>
		/// <returns>返回受影响的记录行数，执行成功返回大于零的整数，失败则返回负数。</returns>
		public int Update(string name, object data, ICondition condition = null, string scope = null, Func<DataUpdationContext, bool> updating = null, Action<DataUpdationContext> updated = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(data == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateUpdationContext(name, false, data, condition, scope);

			//处理数据访问操作前的回调
			if(updating != null && updating(context))
				return context.Count;

			//激发“Updating”事件，如果被中断则返回
			if(this.OnUpdating(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltering(ctx));

			//执行数据更新操作
			this.OnUpdate(context);

			//调用数据访问过滤器后事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltered(ctx));

			//激发“Updated”事件
			this.OnUpdated(context);

			//处理数据访问操作后的回调
			if(updated != null)
				updated(context);

			//返回最终的结果
			return context.Count;
		}

		public int Update(string name, object data, string scope, ICondition condition = null, Func<DataUpdationContext, bool> updating = null, Action<DataUpdationContext> updated = null)
		{
			return this.Update(name, data, condition, scope, updating, updated);
		}

		public int UpdateMany<T>(IEnumerable<T> items, ICondition condition = null, string scope = null, Func<DataUpdationContext, bool> updating = null, Action<DataUpdationContext> updated = null)
		{
			return this.UpdateMany(this.GetName<T>(), items, condition, scope, updating, updated);
		}

		public int UpdateMany<T>(IEnumerable<T> items, string scope, ICondition condition = null, Func<DataUpdationContext, bool> updating = null, Action<DataUpdationContext> updated = null)
		{
			return this.UpdateMany(this.GetName<T>(), items, condition, scope, updating, updated);
		}

		/// <summary>
		/// 根据指定的条件将指定的实体集更新到数据源。
		/// </summary>
		/// <param name="name">指定的实体映射名。</param>
		/// <param name="items">要更新的数据集。</param>
		/// <param name="condition">要更新的条件子句，如果为空(null)则根据实体的主键进行更新。</param>
		/// <param name="scope">指定的要更新的和排除更新的属性名列表，如果指定的是多个属性则属性名之间使用逗号(,)分隔；要排除的属性以减号(-)打头，星号(*)表示所有属性，感叹号(!)表示排除所有属性；如果未指定该参数则默认只会更新所有单值属性而不会更新导航属性。</param>
		/// <returns>返回受影响的记录行数，执行成功返回大于零的整数，失败则返回负数。</returns>
		public int UpdateMany(string name, IEnumerable items, ICondition condition = null, string scope = null, Func<DataUpdationContext, bool> updating = null, Action<DataUpdationContext> updated = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(items == null)
				return 0;

			//创建数据访问上下文对象
			var context = this.CreateUpdationContext(name, true, items, condition, scope);

			//处理数据访问操作前的回调
			if(updating != null && updating(context))
				return context.Count;

			//激发“Updating”事件，如果被中断则返回
			if(this.OnUpdating(context))
				return context.Count;

			//调用数据访问过滤器前事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltering(ctx));

			//执行数据更新操作
			this.OnUpdate(context);

			//调用数据访问过滤器后事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltered(ctx));

			//激发“Updated”事件
			this.OnUpdated(context);

			//处理数据访问操作后的回调
			if(updated != null)
				updated(context);

			//返回最终的结果
			return context.Count;
		}

		public int UpdateMany(string name, IEnumerable items, string scope, ICondition condition = null, Func<DataUpdationContext, bool> updating = null, Action<DataUpdationContext> updated = null)
		{
			return this.UpdateMany(name, items, condition, scope, updating, updated);
		}

		protected abstract void OnUpdate(DataUpdationContext context);
		#endregion

		#region 查询方法
		public IEnumerable<T> Select<T>(ICondition condition = null, params Sorting[] sortings)
		{
			return this.Select<T>(this.GetName<T>(), condition, null, string.Empty, null, sortings);
		}

		public IEnumerable<T> Select<T>(ICondition condition, string scope, params Sorting[] sortings)
		{
			return this.Select<T>(this.GetName<T>(), condition, null, scope, null, sortings);
		}

		public IEnumerable<T> Select<T>(ICondition condition, string scope, Paging paging, params Sorting[] sortings)
		{
			return this.Select<T>(this.GetName<T>(), condition, null, scope, paging, sortings);
		}

		public IEnumerable<T> Select<T>(ICondition condition, Paging paging, params Sorting[] sortings)
		{
			return this.Select<T>(this.GetName<T>(), condition, null, string.Empty, paging, sortings);
		}

		public IEnumerable<T> Select<T>(ICondition condition, Paging paging, string scope, params Sorting[] sortings)
		{
			return this.Select<T>(this.GetName<T>(), condition, null, scope, paging, sortings);
		}

		public IEnumerable<T> Select<T>(ICondition condition, Grouping grouping, params Sorting[] sortings)
		{
			return this.Select<T>(this.GetName<T>(), condition, grouping, string.Empty, null, sortings);
		}

		public IEnumerable<T> Select<T>(ICondition condition, Grouping grouping, Paging paging, params Sorting[] sortings)
		{
			return this.Select<T>(this.GetName<T>(), condition, grouping, string.Empty, paging, sortings);
		}

		public IEnumerable<T> Select<T>(ICondition condition, Grouping grouping, Paging paging, string scope, params Sorting[] sortings)
		{
			return this.Select<T>(this.GetName<T>(), condition, grouping, scope, paging, sortings);
		}

		public IEnumerable<T> Select<T>(ICondition condition, Grouping grouping, string scope, params Sorting[] sortings)
		{
			return this.Select<T>(this.GetName<T>(), condition, grouping, scope, null, sortings);
		}

		public IEnumerable<T> Select<T>(ICondition condition, Grouping grouping, string scope, Paging paging, params Sorting[] sortings)
		{
			return this.Select<T>(this.GetName<T>(), condition, grouping, scope, paging, sortings);
		}

		public IEnumerable<T> Select<T>(ICondition condition, Grouping grouping, string scope, Paging paging, Sorting[] sortings, Func<DataSelectionContext, bool> selecting, Action<DataSelectionContext> selected)
		{
			return this.Select<T>(this.GetName<T>(), condition, grouping, scope, paging, sortings, selecting, selected);
		}

		public IEnumerable<T> Select<T>(string name, ICondition condition = null, params Sorting[] sortings)
		{
			return this.Select<T>(name, condition, null, string.Empty, null, sortings);
		}

		public IEnumerable<T> Select<T>(string name, ICondition condition, string scope, params Sorting[] sortings)
		{
			return this.Select<T>(name, condition, null, scope, null, sortings);
		}

		public IEnumerable<T> Select<T>(string name, ICondition condition, string scope, Paging paging, params Sorting[] sortings)
		{
			return this.Select<T>(name, condition, null, scope, paging, sortings);
		}

		public IEnumerable<T> Select<T>(string name, ICondition condition, Paging paging, params Sorting[] sortings)
		{
			return this.Select<T>(name, condition, null, string.Empty, paging, sortings);
		}

		public IEnumerable<T> Select<T>(string name, ICondition condition, Paging paging, string scope, params Sorting[] sortings)
		{
			return this.Select<T>(name, condition, null, scope, paging, sortings);
		}

		public IEnumerable<T> Select<T>(string name, ICondition condition, Grouping grouping, params Sorting[] sortings)
		{
			return this.Select<T>(name, condition, grouping, string.Empty, null, sortings);
		}

		public IEnumerable<T> Select<T>(string name, ICondition condition, Grouping grouping, Paging paging, params Sorting[] sortings)
		{
			return this.Select<T>(name, condition, grouping, string.Empty, paging, sortings);
		}

		public IEnumerable<T> Select<T>(string name, ICondition condition, Grouping grouping, Paging paging, string scope, params Sorting[] sortings)
		{
			return this.Select<T>(name, condition, grouping, scope, paging, sortings);
		}

		public IEnumerable<T> Select<T>(string name, ICondition condition, Grouping grouping, string scope, params Sorting[] sortings)
		{
			return this.Select<T>(name, condition, grouping, scope, null, sortings);
		}

		public IEnumerable<T> Select<T>(string name, ICondition condition, Grouping grouping, string scope, Paging paging, params Sorting[] sortings)
		{
			return this.Select<T>(name, condition, grouping, scope, paging, sortings, null, null);
		}

		public IEnumerable<T> Select<T>(string name, ICondition condition, Grouping grouping, string scope, Paging paging, Sorting[] sortings, Func<DataSelectionContext, bool> selecting, Action<DataSelectionContext> selected)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			//创建数据访问上下文对象
			var context = this.CreateSelectionContext(name, typeof(T), condition, grouping, scope, paging, sortings);

			//处理数据访问操作前的回调
			if(selecting != null && selecting(context))
				return context.Result as IEnumerable<T>;

			//激发“Selecting”事件，如果被中断则返回
			if(this.OnSelecting(context))
				return context.Result as IEnumerable<T>;

			//调用数据访问过滤器前事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltering(ctx));

			//执行数据查询操作
			this.OnSelect<T>(context);

			//调用数据访问过滤器后事件
			this.InvokeFilters(context, (filter, ctx) => filter.OnFiltered(ctx));

			//激发“Selected”事件
			this.OnSelected(context);

			//处理数据访问操作后的回调
			if(selected != null)
				selected(context);

			//返回最终的结果
			return context.Result as IEnumerable<T>;
		}

		protected abstract void OnSelect<T>(DataSelectionContext context);
		#endregion

		#region 虚拟方法
		protected virtual string GetName(Type type)
		{
			var name = _naming.Get(type);

			if(string.IsNullOrEmpty(name))
				throw new InvalidOperationException($"Missing data access name mapping of the '{type.FullName}' type.");

			return name;
		}

		protected virtual DataCountContext CreateCountContext(string name, ICondition condition, string includes)
		{
			return new DataCountContext(this, name, condition, includes);
		}

		protected virtual DataExecutionContext CreateExecutionContext(string name, bool isScalar, Type resultType, IDictionary<string, object> inParameters)
		{
			return new DataExecutionContext(this, name, isScalar, resultType, inParameters, null);
		}

		protected virtual DataExistenceContext CreateExistenceContext(string name, ICondition condition)
		{
			return new DataExistenceContext(this, name, condition);
		}

		protected virtual DataIncrementContext CreateIncrementContext(string name, string member, ICondition condition, int interval)
		{
			return new DataIncrementContext(this, name, member, condition, interval);
		}

		protected virtual DataDeletionContext CreateDeletionContext(string name, ICondition condition, string[] cascades)
		{
			return new DataDeletionContext(this, name, condition, cascades);
		}

		protected virtual DataInsertionContext CreateInsertionContext(string name, bool isMultiple, object data, string scope)
		{
			if(isMultiple)
				data = GetDataDictionaries(data);
			else
				data = GetDataDictionary(data);

			return new DataInsertionContext(this, name, isMultiple, data, scope);
		}

		protected virtual DataUpdationContext CreateUpdationContext(string name, bool isMultiple, object data, ICondition condition, string scope)
		{
			if(isMultiple)
				data = GetDataDictionaries(data);
			else
				data = GetDataDictionary(data);

			return new DataUpdationContext(this, name, isMultiple, data, condition, scope);
		}

		protected virtual DataUpsertionContext CreateUpsertionContext(string name, bool isMultiple, object data, ICondition condition, string scope)
		{
			if(isMultiple)
				data = GetDataDictionaries(data);
			else
				data = GetDataDictionary(data);

			return new DataUpsertionContext(this, name, isMultiple, data, condition, scope);
		}

		protected virtual DataSelectionContext CreateSelectionContext(string name, Type entityType, ICondition condition, Grouping grouping, string scope, Paging paging, Sorting[] sortings)
		{
			return new DataSelectionContext(this, name, entityType, condition, grouping, scope, paging, sortings);
		}
		#endregion

		#region 激发事件
		protected virtual void OnCounted(DataCountContext context)
		{
			var e = this.Counted;

			if(e != null)
				e(this, new DataCountedEventArgs(context));
		}

		protected virtual bool OnCounting(DataCountContext context)
		{
			var e = this.Counting;

			if(e == null)
				return false;

			var args = new DataCountingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnExecuted(DataExecutionContext context)
		{
			var e = this.Executed;

			if(e != null)
				e(this, new DataExecutedEventArgs(context));
		}

		protected virtual bool OnExecuting(DataExecutionContext context)
		{
			var e = this.Executing;

			if(e == null)
				return false;

			var args = new DataExecutingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnExisted(DataExistenceContext context)
		{
			var e = this.Existed;

			if(e != null)
				e(this, new DataExistedEventArgs(context));
		}

		protected virtual bool OnExisting(DataExistenceContext context)
		{
			var e = this.Existing;

			if(e == null)
				return false;

			var args = new DataExistingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnIncremented(DataIncrementContext context)
		{
			var e = this.Incremented;

			if(e != null)
				e(this, new DataIncrementedEventArgs(context));
		}

		protected virtual bool OnIncrementing(DataIncrementContext context)
		{
			var e = this.Incrementing;

			if(e == null)
				return false;

			var args = new DataIncrementingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnDeleted(DataDeletionContext context)
		{
			var e = this.Deleted;

			if(e != null)
				e(this, new DataDeletedEventArgs(context));
		}

		protected virtual bool OnDeleting(DataDeletionContext context)
		{
			var e = this.Deleting;

			if(e == null)
				return false;

			var args = new DataDeletingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnInserted(DataInsertionContext context)
		{
			var e = this.Inserted;

			if(e != null)
				e(this, new DataInsertedEventArgs(context));
		}

		protected virtual bool OnInserting(DataInsertionContext context)
		{
			var e = this.Inserting;

			if(e == null)
				return false;

			var args = new DataInsertingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnUpdated(DataUpdationContext context)
		{
			var e = this.Updated;

			if(e != null)
				e(this, new DataUpdatedEventArgs(context));
		}

		protected virtual bool OnUpdating(DataUpdationContext context)
		{
			var e = this.Updating;

			if(e == null)
				return false;

			var args = new DataUpdatingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnSelected(DataSelectionContext context)
		{
			var e = this.Selected;

			if(e != null)
				e(this, new DataSelectedEventArgs(context));
		}

		protected virtual bool OnSelecting(DataSelectionContext context)
		{
			var e = this.Selecting;

			if(e == null)
				return false;

			var args = new DataSelectingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}
		#endregion

		#region 私有方法
		private string GetName<T>()
		{
			return this.GetName(typeof(T));
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
		private void InvokeFilters(DataAccessContextBase context, Action<IDataAccessFilter, DataAccessContextBase> invoke)
		{
			foreach(var filter in _filters)
			{
				if(filter == null)
					continue;

				var predication = filter as Zongsoft.Services.IPredication;

				if(predication == null || predication.Predicate(context))
				{
					invoke(filter, context);
				}
			}
		}

		private static DataDictionary GetDataDictionary(object data)
		{
			if(data == null)
				throw new ArgumentNullException("data");

			return (data as DataDictionary) ?? new DataDictionary(data);
		}

		private static IEnumerable<DataDictionary> GetDataDictionaries(object data)
		{
			if(data == null)
				throw new ArgumentNullException("data");

			var items = data as IEnumerable;

			if(items == null)
				yield return (data as DataDictionary) ?? new DataDictionary(data);
			else
			{
				foreach(var item in items)
				{
					if(item != null)
						yield return (item as DataDictionary) ?? new DataDictionary(item);
				}
			}
		}
		#endregion
	}
}
