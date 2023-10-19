using System;

namespace Dresser.Logic {
	internal static class PluginLog {
		public static void Fatal(string messageTemplate, params object[] values) => PluginServices.PluginLog.Fatal(messageTemplate, values);
		public static void Fatal(Exception? exception, string messageTemplate, params object[] values)=> PluginServices.PluginLog.Fatal(exception, messageTemplate, values);
		public static void Error(string messageTemplate, params object[] values) => PluginServices.PluginLog.Error(messageTemplate, values);
		public static void Error(Exception? exception, string messageTemplate, params object[] values) => PluginServices.PluginLog.Error(exception, messageTemplate, values);
		public static void Warning(string messageTemplate, params object[] values) => PluginServices.PluginLog.Warning(messageTemplate, values);
		public static void Warning(Exception? exception, string messageTemplate, params object[] values) => PluginServices.PluginLog.Warning(exception, messageTemplate, values);
		public static void Information(string messageTemplate, params object[] values) => PluginServices.PluginLog.Information(messageTemplate, values);
		public static void Information(Exception? exception, string messageTemplate, params object[] values) => PluginServices.PluginLog.Information(exception, messageTemplate, values);
		public static void Info(string messageTemplate, params object[] values) => PluginServices.PluginLog.Info(messageTemplate, values);
		public static void Info(Exception? exception, string messageTemplate, params object[] values) => PluginServices.PluginLog.Info(exception, messageTemplate, values);
		public static void Debug(string messageTemplate, params object[] values) => PluginServices.PluginLog.Debug(messageTemplate, values);
		public static void Debug(Exception? exception, string messageTemplate, params object[] values) => PluginServices.PluginLog.Debug(exception, messageTemplate, values);
		public static void Verbose(string messageTemplate, params object[] values) => PluginServices.PluginLog.Verbose(messageTemplate, values);
		public static void Verbose(Exception? exception, string messageTemplate, params object[] values) => PluginServices.PluginLog.Verbose(exception, messageTemplate, values);
	}
}
