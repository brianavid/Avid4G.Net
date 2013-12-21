using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Avid.Spotify
{
    /// <summary>
    /// Form to enter Spotify credentials - user name and password
    /// </summary>
    public partial class GetSpotifyCredentials : Form
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        public GetSpotifyCredentials(
            string user,
            string pass)
        {
            InitializeComponent();

            SpotifyUser = spotifyUser.Text = user;
            SpotifyPass = spotifyPass.Text = pass;
        }

        /// <summary>
        /// The data entered by the user
        /// </summary>
        public string SpotifyUser { get; private set; }
        public string SpotifyPass { get; private set; }

        /// <summary>
        /// OK
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void okButton_Click(object sender, EventArgs e)
        {
            SpotifyUser = spotifyUser.Text ;
            SpotifyPass = spotifyPass.Text;
            this.DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Cancel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
