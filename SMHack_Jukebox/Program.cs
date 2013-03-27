using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using System.Collections;
using System.Xml;
using System.IO;

namespace SMHack_Jukebox
{

    public class Playlist
    {
        public bool SongEnded = true;

        private System.Windows.Forms.Timer CheckSong;
        private System.ComponentModel.IContainer play_components;

        ArrayList SongsInPlaylist = new ArrayList();
        private int Index = 0;
        public AxWMPLib.AxWindowsMediaPlayer MediaPlayer;

        public Playlist(string[] Songs, AxWMPLib.AxWindowsMediaPlayer Player)
        {
            AddSongs(Songs);

            MediaPlayer = Player;

            this.play_components = new System.ComponentModel.Container();
            this.CheckSong = new System.Windows.Forms.Timer(this.play_components);
            this.CheckSong.Tick += new System.EventHandler(this.CheckSong_Tick);

            MediaPlayer.PlayStateChange += new AxWMPLib._WMPOCXEvents_PlayStateChangeEventHandler(MediaPlayer_PlayStateChange);

            Play();
        }
        public void TheFix_Playlist(AxWMPLib.AxWindowsMediaPlayer Player)
        {
            MediaPlayer = Player;
            Index = 0;

            this.play_components = new System.ComponentModel.Container();
            this.CheckSong = new System.Windows.Forms.Timer(this.play_components);
            this.CheckSong.Tick += new System.EventHandler(this.CheckSong_Tick);

            MediaPlayer.PlayStateChange += new AxWMPLib._WMPOCXEvents_PlayStateChangeEventHandler(MediaPlayer_PlayStateChange);

            Play();
        }
        public void AddSongs(string[] Songs)
        {
            for (int i = 0; i < Songs.Length; i++)
            {
                AddSong(Songs[i]);
            }
        }
        public void AddSong(string Song)
        {
            SongsInPlaylist.Add(Song);
        }
        public void DeleteSong(string Song)
        {
            if (Song == SongsInPlaylist[Index].ToString())
            {
                MediaPlayer.Ctlcontrols.stop();
                Index--;
            }
            SongsInPlaylist.Remove(Song);
            MediaPlayer.Ctlcontrols.play();
        }
        public void DeletePlaylist()
        {
            MediaPlayer.Ctlcontrols.stop();
            SongsInPlaylist.Clear();
            Index = 0;
        }
        public int Volume
        {
            set { MediaPlayer.settings.volume = value; }
            get { return MediaPlayer.settings.volume; }
        }

        public void Play()
        {
            if (SongsInPlaylist[Index] != null)
            {
                MediaPlayer.URL = SongsInPlaylist[Index].ToString();
            }
        }
        public void Play(int Slot)
        {
            if (SongsInPlaylist[Slot - 1] != null)
                MediaPlayer.URL = SongsInPlaylist[Slot - 1].ToString();
        }
        public void Play(string name)
        {
            int slot = SongsInPlaylist.BinarySearch(name, null);
            if (slot >= 0 && slot < SongsInPlaylist.Count)
                MediaPlayer.URL = SongsInPlaylist[slot].ToString();
        }

        public void Pause()
        {
            MediaPlayer.Ctlcontrols.pause();
        }
        public void Stop()
        {
            MediaPlayer.Ctlcontrols.stop();
        }
        public void NextSong()
        {
            if (Index != SongsInPlaylist.Count - 1)
            {
                Index++;
                MediaPlayer.Ctlcontrols.stop();
                MediaPlayer.URL = SongsInPlaylist[Index].ToString();
                MediaPlayer.Ctlcontrols.play();
            }
            else
            {
                Index = 0;
                MediaPlayer.Ctlcontrols.stop();
                MediaPlayer.URL = SongsInPlaylist[0].ToString();
                MediaPlayer.Ctlcontrols.play();
            }
        }
        public  void PrevSong()
        {
            if (Index != 0)
            {
                Index--;
                MediaPlayer.Ctlcontrols.stop();
                MediaPlayer.URL = SongsInPlaylist[Index].ToString();
                MediaPlayer.Ctlcontrols.play();
            }
            else
            {
                Index = SongsInPlaylist.Count - 1;
                MediaPlayer.Ctlcontrols.stop();
                MediaPlayer.URL = SongsInPlaylist[Index].ToString();
                MediaPlayer.Ctlcontrols.play();
            }
        }
        private void CheckSong_Tick(object sender, System.EventArgs e)
        {
            if (SongEnded)
            {
                NextSong();
                SongEnded = false;
                CheckSong.Stop();
            }
        }

        public void MediaPlayer_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            switch (MediaPlayer.playState)
            {
                case WMPLib.WMPPlayState.wmppsMediaEnded:
                    SongEnded = true;
                    CheckSong.Start();
                    break;
                default:
                    break;
            }
        }
    }

    static class Program
    {
        public static string DatabaseSettings(string database, string setting)
        {
            string result = "0";
            XmlTextReader textReader = new XmlTextReader(".\\dbsettings.xml");
           
            while (textReader.Read())
            {
                if (textReader.NodeType == XmlNodeType.Element)
                {
                    if (textReader.LocalName.Equals(database) && textReader.NodeType != XmlNodeType.EndElement)
                    {
                        while (textReader.Read())
                        {
                            if (textReader.LocalName.Equals(setting) && textReader.NodeType != XmlNodeType.EndElement)
                            {
                                textReader.Read();
                                result = textReader.Value;
                                break;                                
                            }
                        }
                    }
                }
            }

            textReader.Close();

            return result;
        }
        public static string MySqlEscape(string usString)
        {
            if (usString == null)
            {
                return null;
            }
            // it escapes \r, \n, \x00, \x1a, baskslash, single quotes, and double quotes
            return Regex.Replace(usString, @"[\r\n\x00\x1a\\'""]", @"\$0");
        }

        private static ArrayList getSignedin()
        {
            ArrayList loggedingenres = new ArrayList();
            bool HasRows;

            string MembersConString = "SERVER=" + DatabaseSettings("MembersDatabase", "server") +
            ";DATABASE=" + DatabaseSettings("MembersDatabase", "database") +
            ";UID=" + DatabaseSettings("MembersDatabase", "username") +
            ";PASSWORD=" + DatabaseSettings("MembersDatabase", "password") + ";";

            MySqlConnection connection = new MySqlConnection(MembersConString);

            connection.Open();
            MySqlCommand command = connection.CreateCommand();
            MySqlDataReader Reader;

            command.CommandText = "SELECT memberid FROM signedin";
            Reader = command.ExecuteReader();
            HasRows = Reader.HasRows;

            if (HasRows)
            {
                bool jukeHasRows;

                string JukeboxConString = "SERVER=" + DatabaseSettings("JukeboxDatabase", "server") +
                ";DATABASE=" + DatabaseSettings("JukeboxDatabase", "database") +
                ";UID=" + DatabaseSettings("JukeboxDatabase", "username") +
                ";PASSWORD=" + DatabaseSettings("JukeboxDatabase", "password") + ";";

                MySqlConnection jukeconnection = new MySqlConnection(JukeboxConString);
                MySqlConnection jukeconnection2 = new MySqlConnection(JukeboxConString);

                jukeconnection.Open();
                jukeconnection2.Open();
                MySqlCommand jukecommand = jukeconnection.CreateCommand();
                MySqlCommand jukecommand2 = jukeconnection2.CreateCommand();
                MySqlDataReader jukeReader;
                MySqlDataReader jukeReader2;

                while (Reader.Read())
                {
                    jukecommand.CommandText = "SELECT genreid FROM userTogenres WHERE userID ='" + Reader["memberid"] + "'";
                    jukeReader = jukecommand.ExecuteReader();
                    jukeHasRows = jukeReader.HasRows;
                    if (jukeHasRows)
                    {
                        while (jukeReader.Read())
                        {
                            jukecommand2.CommandText = "SELECT genre FROM genres WHERE uid ='" + jukeReader["genreid"] + "'";
                            jukeReader2 = jukecommand2.ExecuteReader();
                            while (jukeReader2.Read())
                            {
                                loggedingenres.Add((string)jukeReader2["genre"]);
                            }
                            jukeReader2.Close();
                        }
                        jukeconnection2.Close();

                    }

                    jukeReader.Close();
                    jukeconnection.Close();
                }
            }
            else
            {
                string JukeboxConString = "SERVER=" + DatabaseSettings("JukeboxDatabase", "server") +
                ";DATABASE=" + DatabaseSettings("JukeboxDatabase", "database") +
                ";UID=" + DatabaseSettings("JukeboxDatabase", "username") +
                ";PASSWORD=" + DatabaseSettings("JukeboxDatabase", "password") + ";";

                MySqlConnection jukeconnection2 = new MySqlConnection(JukeboxConString);
                jukeconnection2.Open();
                MySqlCommand jukecommand2 = jukeconnection2.CreateCommand();
                MySqlDataReader jukeReader2;
                jukecommand2.CommandText = "SELECT genre FROM genres";
                jukeReader2 = jukecommand2.ExecuteReader();
                while (jukeReader2.Read())
                {
                    loggedingenres.Add((string)jukeReader2["genre"]);
                }
                jukeReader2.Close();
                jukeconnection2.Close();
                //MessageBox.Show("Please sign into the space!");
            }
            Reader.Close();
            connection.Close();
            return loggedingenres;
        }

        public static ArrayList getMP3s()
        {
            bool HasRows;
            ArrayList mp3s = new ArrayList();

            string JukeboxConString = "SERVER=" + DatabaseSettings("JukeboxDatabase", "server") +
                ";DATABASE=" + DatabaseSettings("JukeboxDatabase", "database") +
                ";UID=" + DatabaseSettings("JukeboxDatabase", "username") +
                ";PASSWORD=" + DatabaseSettings("JukeboxDatabase", "password") + ";";
            MySqlConnection connection = new MySqlConnection(JukeboxConString);

            connection.Open();
            MySqlCommand command = connection.CreateCommand();
            MySqlDataReader Reader;

            ArrayList currentGenres = getSignedin();
            foreach (string genre in currentGenres)
            {
                command.CommandText = "SELECT name, location FROM files WHERE genres LIKE '%" + MySqlEscape(genre.Trim()) + "%'";
                Reader = command.ExecuteReader();
                HasRows = Reader.HasRows;
                if (HasRows)
                {
                    while (Reader.Read())
                    {
                        string mp3file = Reader["location"] + "\\" + Reader["name"];
                        mp3s.Add(mp3file);
                    }
                }
                Reader.Close();
            }
            connection.Close();

            ArrayList randommp3s = new ArrayList();
            Random r = new Random();
            int randomIndex = 0;

            while (mp3s.Count > 0)
            {
                randomIndex = r.Next(0, mp3s.Count);
                randommp3s.Add(mp3s[randomIndex]);
                mp3s.RemoveAt(randomIndex);

            }

            return randommp3s;
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
