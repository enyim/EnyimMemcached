using System;

namespace Enyim.Caching
{
	internal class Log4NetWrapper : ILog
	{
		private log4net.ILog log;

		public Log4NetWrapper(log4net.ILog log)
		{
			this.log = log;
		}

		#region [ ILog                         ]

		bool ILog.IsDebugEnabled
		{
			get { return this.log.IsDebugEnabled; }
		}

		bool ILog.IsInfoEnabled
		{
			get { return this.log.IsInfoEnabled; }
		}

		bool ILog.IsWarnEnabled
		{
			get { return this.log.IsWarnEnabled; }
		}

		bool ILog.IsErrorEnabled
		{
			get { return this.log.IsErrorEnabled; }
		}

		bool ILog.IsFatalEnabled
		{
			get { return this.log.IsFatalEnabled; }
		}

		void ILog.Debug(object message)
		{
			this.log.Debug(message);
		}

		void ILog.Debug(object message, Exception exception)
		{
			this.log.Debug(message, exception);
		}

		void ILog.DebugFormat(string format, object arg0)
		{
			this.log.DebugFormat(format, arg0);
		}

		void ILog.DebugFormat(string format, object arg0, object arg1)
		{
			this.log.DebugFormat(format, arg0, arg1);
		}

		void ILog.DebugFormat(string format, object arg0, object arg1, object arg2)
		{
			this.log.DebugFormat(format, arg0, arg1, arg2);
		}

		void ILog.DebugFormat(string format, params object[] args)
		{
			this.log.DebugFormat(format, args);
		}

		void ILog.DebugFormat(IFormatProvider provider, string format, params object[] args)
		{
			this.log.DebugFormat(provider, format, args);
		}

		void ILog.Info(object message)
		{
			this.log.Info(message);
		}

		void ILog.Info(object message, Exception exception)
		{
			this.log.Info(message, exception);
		}

		void ILog.InfoFormat(string format, object arg0)
		{
			this.log.InfoFormat(format, arg0);
		}

		void ILog.InfoFormat(string format, object arg0, object arg1)
		{
			this.log.InfoFormat(format, arg0, arg1);
		}

		void ILog.InfoFormat(string format, object arg0, object arg1, object arg2)
		{
			this.log.InfoFormat(format, arg0, arg1, arg2);
		}

		void ILog.InfoFormat(string format, params object[] args)
		{
			this.log.InfoFormat(format, args);
		}

		void ILog.InfoFormat(IFormatProvider provider, string format, params object[] args)
		{
			this.log.InfoFormat(provider, format, args);
		}

		void ILog.Warn(object message)
		{
			this.log.Warn(message);
		}

		void ILog.Warn(object message, Exception exception)
		{
			this.log.Warn(message, exception);
		}

		void ILog.WarnFormat(string format, object arg0)
		{
			this.log.WarnFormat(format, arg0);
		}

		void ILog.WarnFormat(string format, object arg0, object arg1)
		{
			this.log.WarnFormat(format, arg0, arg1);
		}

		void ILog.WarnFormat(string format, object arg0, object arg1, object arg2)
		{
			this.log.WarnFormat(format, arg0, arg1, arg2);
		}

		void ILog.WarnFormat(string format, params object[] args)
		{
			this.log.WarnFormat(format, args);
		}

		void ILog.WarnFormat(IFormatProvider provider, string format, params object[] args)
		{
			this.log.WarnFormat(provider, format, args);
		}

		void ILog.Error(object message)
		{
			this.log.Error(message);
		}

		void ILog.Error(object message, Exception exception)
		{
			this.log.Error(message, exception);
		}

		void ILog.ErrorFormat(string format, object arg0)
		{
			this.log.ErrorFormat(format, arg0);
		}

		void ILog.ErrorFormat(string format, object arg0, object arg1)
		{
			this.log.ErrorFormat(format, arg0, arg1);
		}

		void ILog.ErrorFormat(string format, object arg0, object arg1, object arg2)
		{
			this.log.ErrorFormat(format, arg0, arg1, arg2);
		}

		void ILog.ErrorFormat(string format, params object[] args)
		{
			this.log.ErrorFormat(format, args);
		}

		void ILog.ErrorFormat(IFormatProvider provider, string format, params object[] args)
		{
			this.log.ErrorFormat(provider, format, args);
		}

		void ILog.Fatal(object message)
		{
			this.log.Fatal(message);
		}

		void ILog.Fatal(object message, Exception exception)
		{
			this.log.Fatal(message, exception);
		}

		void ILog.FatalFormat(string format, object arg0)
		{
			this.log.FatalFormat(format, arg0);
		}

		void ILog.FatalFormat(string format, object arg0, object arg1)
		{
			this.log.FatalFormat(format, arg0, arg1);
		}

		void ILog.FatalFormat(string format, object arg0, object arg1, object arg2)
		{
			this.log.FatalFormat(format, arg0, arg1, arg2);
		}

		void ILog.FatalFormat(string format, params object[] args)
		{
			this.log.FatalFormat(format, args);
		}

		void ILog.FatalFormat(IFormatProvider provider, string format, params object[] args)
		{
			this.log.FatalFormat(provider, format, args);
		}

		#endregion
	}
}

#region [ License information          ]
/* ************************************************************
 * 
 *    Copyright (c) 2010 Attila Kiskó, enyim.com
 *    
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *    
 *        http://www.apache.org/licenses/LICENSE-2.0
 *    
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *    
 * ************************************************************/
#endregion
