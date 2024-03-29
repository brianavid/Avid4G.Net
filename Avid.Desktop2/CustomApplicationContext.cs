﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace Avid.Desktop
{

    public class CustomApplicationContext : ApplicationContext
    {
        private static readonly string IconFileName = "Avid4Blue64.ico";
        private static readonly string DefaultTooltip = "Avid desktop interaction support #2";

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

            MenuItem loginContextMenuItem = new System.Windows.Forms.MenuItem();
            loginContextMenuItem.Index = 1;
            loginContextMenuItem.Text = "&Spotify Login";
            loginContextMenuItem.Click += new System.EventHandler(this.spotifyLoginItem_Click);

            MenuItem exitContextMenuItem = new System.Windows.Forms.MenuItem();
            exitContextMenuItem.Index = 2;
            exitContextMenuItem.Text = "&Exit";
            exitContextMenuItem.Click += new System.EventHandler(this.exitItem_Click);

            ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();
            contextMenu.MenuItems.AddRange(new MenuItem[] { loginContextMenuItem, exitContextMenuItem });

            notifyIcon.ContextMenu = contextMenu;

        }

        /// <summary>
        /// When the application context is disposed, dispose things like the notify icon.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) { components.Dispose(); }
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
        /// When the Spotify Login menu item is clicked, make a call to request a Spotify login and the credentials stored in the registry.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void spotifyLoginItem_Click(object sender, EventArgs e)
        {
            SpotifyAuth.Auth();
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
