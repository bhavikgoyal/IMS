using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS
{
   public static class LoaderManager
    {
        private static readonly object _sync = new object();
        private static LoaderControl _loader;
        private static Window _loaderOwner;

        /// <summary>
        /// Universal loader method – call from anywhere in the app.
        /// Shows loader only if the operation takes longer than 300 ms.
       
            public static async Task RunAsync(Func<Task> work)
            {
                if (work == null) throw new ArgumentNullException(nameof(work));

                var owner = Application.Current?.MainWindow
                            ?? throw new InvalidOperationException("MainWindow not found.");

                // ----- SHOW LOADER IMMEDIATELY -----
                await owner.Dispatcher.InvokeAsync(() =>
                {
                    lock (_sync)
                    {
                        if (_loader == null)
                        {
                            _loader = new LoaderControl(owner)
                            {
                                WindowStartupLocation = WindowStartupLocation.CenterOwner
                            };
                            owner.IsEnabled = false;
                            _loader.Show();
                        }
                    }
                });

                try
                {
                    await work();   // actual work
                }
                finally
                {
                    // hide loader
                    await owner.Dispatcher.InvokeAsync(() =>
                    {
                        lock (_sync)
                        {
                            try { _loader?.Close(); }
                            catch { }
                            finally
                            {
                                _loader = null;
                                owner.IsEnabled = true;
                            }
                        }
                    });
                }
            }

        }
}