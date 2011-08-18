using System;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

using MonoDevelop;
using MonoDevelop.Monitoring;
using MonoDevelop.CrashReporting;

namespace MacCrashLogger
{
	public partial class AppDelegate : NSApplicationDelegate
	{
		ICrashMonitor Monitor {
			get; set;
		}
		
		bool ProcessingCrashLog {
			get; set;
		}
		
		CrashReporter Reporter {
			get; set;
		}
		
		bool ShouldExit {
			get; set;
		}
		
		public AppDelegate ()
		{
			Reporter = new CrashReporter (OptionsParser.LogPath);
			
			Monitor = CrashMonitor.Create (OptionsParser.Pid);
			Monitor.ApplicationExited += HandleMonitorApplicationExited;
			Monitor.CrashDetected += HandleMonitorCrashDetected;
		}
		
		public override void FinishedLaunching (NSObject notification)
		{
			Monitor.Start ();
		}
		
		void HandleMonitorCrashDetected (object sender, CrashEventArgs e)
		{
			NSApplication.SharedApplication.InvokeOnMainThread (() => {
				try {
					ProcessingCrashLog = true;
					using (var alert = new NSAlert ()) {
						alert.AlertStyle = NSAlertStyle.Critical;
						alert.MessageText= "MonoDevelop crash detected";
						alert.InformativeText = "An unexpected error has caused MonoDevelop to crash. " +
							"Details of the issue will be sent to Xamarin so it can be diagnosed" +
							" and resolved.";
						
						NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
						var result = alert.RunModal ();
						if (result == 0) {
							Reporter.UploadOrCache (e.CrashLogPath);
						} else {
							Console.WriteLine ("NOT TODAY");
						}
					}
					
					if (ShouldExit)
						NSApplication.SharedApplication.Terminate (null);
				} finally {
					ProcessingCrashLog = false;
				}
			});
		}

		void HandleMonitorApplicationExited (object sender, EventArgs e)
		{
			NSApplication.SharedApplication.InvokeOnMainThread (() => {
				ShouldExit = true;
				if (!ProcessingCrashLog) {
					NSApplication.SharedApplication.Terminate (null);
				}
			});
		}
	}
}

