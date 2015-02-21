using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Avid.Spotify
{
    
     public class CustomApplicationContext : ApplicationContext
     {
         private static readonly string IconFileName = "Avid4Green64.ico";
        private static readonly string DefaultTooltip = "Avid Spotify player";

        /// <summary>
		/// This class should be created and passed into Application.Run( ... )
		/// </summary>
		public CustomApplicationContext() 
		{
			InitializeContext();
		}

        # region generic code framework

        private System.ComponentModel.IContainer components;	// a list of components to dispose when the context is disposed
        private NotifyIcon notifyIcon;				            // the icon that sits in the system tray

        private void InitializeContext()
        {
            components = new System.ComponentModel.Container();
            notifyIcon = new NotifyIcon(components)
                             {
                                 ContextMenuStrip = new ContextMenuStrip(),
                                 Icon = new Icon(IconFileName),
                                 Text = DefaultTooltip,
                                 Visible = true
                             };

            MenuItem exitContextMenuItem = new System.Windows.Forms.MenuItem();
            exitContextMenuItem.Index = 1;
            exitContextMenuItem.Text = "&Exit";
            exitContextMenuItem.Click += new System.EventHandler(this.exitItem_Click);

            MenuItem loginContextMenuItem = new System.Windows.Forms.MenuItem();
            loginContextMenuItem.Index = 2;
            loginContextMenuItem.Text = "&Login";
            loginContextMenuItem.Click += new System.EventHandler(this.loginItem_Click);

            ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();
            contextMenu.MenuItems.AddRange(new MenuItem[] { exitContextMenuItem, loginContextMenuItem });

            notifyIcon.ContextMenu = contextMenu;

        }

        /// <summary>
		/// When the application context is disposed, dispose things like the notify icon.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if( disposing && components != null) { components.Dispose(); }
		}

        /// <summary>
        /// When the exit menu item is clicked, make a call to terminate the ApplicationContext.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitItem_Click(object sender, EventArgs e)
        {
            ExitThread();
        }

        /// <summary>
        /// When the exit menu item is clicked, make a call to terminate the ApplicationContext.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loginItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Exit Spotify Player and require login next time?", "Spotify Player", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey("Avid");
                key.DeleteValue("SpotifyUser");
                key.DeleteValue("SpotifyPass");
                key.DeleteValue("SpotifyToken");

                ExitThread();
            }
        }

        /// <summary>
        /// If we are presently showing a form, clean it up.
        /// </summary>
        protected override void ExitThreadCore()
        {
            notifyIcon.Visible = false; // should remove lingering tray icon
            base.ExitThreadCore();
        }

        # endregion generic code framework

    }

}
