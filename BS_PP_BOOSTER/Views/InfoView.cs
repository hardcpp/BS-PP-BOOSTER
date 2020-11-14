using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using System.Diagnostics;

namespace BS_PP_BOOSTER.Views
{
    /// <summary>
    /// Info view UI controller
    /// </summary>
    internal class InfoView : BSMLResourceViewController
    {
        /// <summary>
        /// BSML file name
        /// </summary>
        public override string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        [UIValue("Line1")]
        private string m_Line1 = "<u><b>Playlist types :</b></u>";
        [UIValue("Line2")]
        private string m_Line2 = "- <color=#008eff><b>Grind</b></color> Consider all ranked difficulties</i>";
        [UIValue("Line3")]
        private string m_Line3 = "- <color=#008eff><b>Improve</b></color> Consider only played ranked difficulties";
        [UIValue("Line4")]
        private string m_Line4 = "- <color=#008eff><b>Discover</b></color> Consider only unplayed ranked difficulties";
        [UIValue("Line5")]
        private string m_Line5 = " ";

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Go to discord
        /// </summary>
        [UIAction("click-btn-discord")]
        private void OnTournamentDiscordPressed()
        {
            Process.Start("https://discord.gg/K4X94Ea");
        }
    }
}
