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
    public partial class GetSpotifyCredentials : Form
    {
        public GetSpotifyCredentials(
            string user,
            string pass)
        {
            InitializeComponent();

            SpotifyUser = spotifyUser.Text = user;
            SpotifyPass = spotifyPass.Text = pass;
        }

        public string SpotifyUser { get; private set; }
        public string SpotifyPass { get; private set; }

        private void okButton_Click(object sender, EventArgs e)
        {
            SpotifyUser = spotifyUser.Text ;
            SpotifyPass = spotifyPass.Text;
            this.DialogResult = DialogResult.OK;
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
