using System;

namespace Enyim.Caching
{
	internal class NLogWrapper : Enyim.Caching.ILog
	{
		private NLog.Logger log;

		public NLogWrapper(NLog.Logger log)
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
			get { return this.log.IsDebugEnabled; }
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
			this.log.DebugException((message ?? String.Empty).ToString(), exception);
		}

		void ILog.DebugFormat(string format, object arg0)
		{
			this.log.Debug(format, arg0);
		}

		void ILog.DebugFormat(string format, object arg0, object arg1)
		{
			this.log.Debug(format, arg0, arg1);
		}

		void ILog.DebugFormat(string format, object arg0, object arg1, object arg2)
		{
			this.log.Debug(format, arg0, arg1, arg2);
		}

		void ILog.DebugFormat(string format, params object[] args)
		{
			this.log.Debug(format, args);
		}

		void ILog.DebugFormat(IFormatProvider provider, string format, params object[] args)
		{
			this.log.Debug(provider, format, args);
		}

		void ILog.Info(object message)
		{
			this.log.Info(message);
		}

		void ILog.Info(object message, Exception exception)
		{
			this.log.InfoException((message ?? String.Empty).ToString(), exception);
		}

		void ILog.InfoFormat(string format, object arg0)
		{
			this.log.Info(format, arg0);
		}

		void ILog.InfoFormat(string format, object arg0, object arg1)
		{
			this.log.Info(format, arg0, arg1);
		}

		void ILog.InfoFormat(string format, object arg0, object arg1, object arg2)
		{
			this.log.Info(format, arg0, arg1, arg2);
		}

		void ILog.InfoFormat(string format, params object[] args)
		{
			this.log.Info(format, args);
		}

		void ILog.InfoFormat(IFormatProvider provider, string format, params object[] args)
		{
			this.log.Info(provider, format, args);
		}

		void ILog.Warn(object message)
		{
			this.log.Warn(message);
		}

		void ILog.Warn(object message, Exception exception)
		{
			this.log.WarnException((message ?? String.Empty).ToString(), exception);
		}

		void ILog.WarnFormat(string format, object arg0)
		{
			this.log.Warn(format, arg0);
		}

		void ILog.WarnFormat(string format, object arg0, object arg1)
		{
			this.log.Warn(format, arg0, arg1);
		}

		void ILog.WarnFormat(string format, object arg0, object arg1, object arg2)
		{
			this.log.Warn(format, arg0, arg1, arg2);
		}

		void ILog.WarnFormat(string format, params object[] args)
		{
			this.log.Warn(format, args);
		}

		void ILog.WarnFormat(IFormatProvider provider, string format, params object[] args)
		{
			this.log.Warn(provider, format, args);
		}

		void ILog.Error(object message)
		{
			this.log.Error(message);
		}

		void ILog.Error(object message, Exception exception)
		{
			this.log.ErrorException((message ?? String.Empty).ToString(), exception);
		}

		void ILog.ErrorFormat(string format, object arg0)
		{
			this.log.Error(format, arg0);
		}

		void ILog.ErrorFormat(string format, object arg0, object arg1)
		{
			this.log.Error(format, arg0, arg1);
		}

		void ILog.ErrorFormat(string format, object arg0, object arg1, object arg2)
		{
			this.log.Error(format, arg0, arg1, arg2);
		}

		void ILog.ErrorFormat(string format, params object[] args)
		{
			this.log.Error(format, args);
		}

		void ILog.ErrorFormat(IFormatProvider provider, string format, params object[] args)
		{
			this.log.Error(provider, format, args);
		}

		void ILog.Fatal(object message)
		{
			this.log.Fatal(message);
		}

		void ILog.Fatal(object message, Exception exception)
		{
			this.log.FatalException((message ?? String.Empty).ToString(), exception);
		}

		void ILog.FatalFormat(string format, object arg0)
		{
			this.log.Fatal(format, arg0);
		}

		void ILog.FatalFormat(string format, object arg0, object arg1)
		{
			this.log.Fatal(format, arg0, arg1);
		}

		void ILog.FatalFormat(string format, object arg0, object arg1, object arg2)
		{
			this.log.Fatal(format, arg0, arg1, arg2);
		}

		void ILog.FatalFormat(string format, params object[] args)
		{
			this.log.Fatal(format, args);
		}

		void ILog.FatalFormat(IFormatProvider provider, string format, params object[] args)
		{
			this.log.Fatal(provider, format, args);
		}

		#endregion
	}
}
