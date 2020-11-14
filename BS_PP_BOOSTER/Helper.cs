using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BS_PP_BOOSTER
{
    /// <summary>
    /// Helper class
    /// </summary>
    class Helper
    {
        /// <summary>
        /// All playlist types
        /// </summary>
        internal static List<object> PlaylistTypes = new object[] { "Grind", "Improve", "Discover" }.ToList();
        /// <summary>
        /// Default playlist type
        /// </summary>
        internal static string DefaultPlaylistType = "Grind";
        /// <summary>
        /// All accuracy types
        /// </summary>
        internal static List<object> AccuracyTypes = new object[] { "Max", "Estimate" }.ToList();
        /// <summary>
        /// Default accuracy type
        /// </summary>
        internal static string DefaultAccuracyType = "Estimate";

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Get playlist ID from type
        /// </summary>
        /// <param name="p_Type">Type of the playlist</param>
        /// <returns></returns>
        internal static int PlaylistAndAccuracyTypeToPlaylistID(string p_Type, string p_Accuracy)
        {
            if (p_Type == "Grind")
                return p_Accuracy == "Max" ? 0 : 1;
            if (p_Type == "Improve")
                return p_Accuracy == "Max" ? 2 : 3;
            if (p_Type == "Discover")
                return p_Accuracy == "Max" ? 4 : 5;

            return 1;
        }
        internal static int PlaylistTypeToPlaylistID(string p_Type)
        {
            for (int l_I = 0; l_I < PlaylistTypes.Count; ++l_I)
            {
                if ((string)PlaylistTypes[l_I] == p_Type)
                    return l_I;
            }

            return 1;
        }
        /// <summary>
        /// Get playlist type from ID
        /// </summary>
        /// <param name="p_ID">ID of the playlist</param>
        /// <returns></returns>
        internal static string PlaylistIDToPlaylistType(int p_ID)
        {
            if (p_ID < PlaylistTypes.Count)
                return (string)PlaylistTypes[p_ID];

            return (string)PlaylistTypes[0];
        }
        /// <summary>
        /// Get accuracy type from ID
        /// </summary>
        /// <param name="p_ID">ID of the accuracy</param>
        /// <returns></returns>
        internal static string AccuracyIDToAccuracyType(int p_ID)
        {
            if (p_ID < AccuracyTypes.Count)
                return (string)AccuracyTypes[p_ID];

            return (string)AccuracyTypes[0];
        }
        internal static int AccuracyTypeToAccuracyID(string p_Type)
        {
            for (int l_I = 0; l_I < AccuracyTypes.Count; ++l_I)
            {
                if ((string)AccuracyTypes[l_I] == p_Type)
                    return l_I;
            }

            return 1;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Refresh playlist and songs
        /// </summary>
        internal static void RefreshPlaylist()
        {
            ///BS_PP_BOOSTERController.Instance.RefreshSongs();
            var l_Instance  = Resources.FindObjectsOfTypeAll<PlaylistLoaderLite.UI.PluginUI>().FirstOrDefault();
            var l_Type      = typeof(PlaylistLoaderLite.UI.PluginUI);
            var l_Method    = l_Type.GetMethod("RefreshButtonPressed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            l_Method.Invoke(l_Instance, null);
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        internal static string m_UserID = null;
        /// <summary>
        /// Get profile ID
        /// </summary>
        /// <returns></returns>
        internal static string GetProfileID()
        {
            if (m_UserID != null)
                return m_UserID;

            try
            {
                var l_PlatformLeaderboardsModels = Resources.FindObjectsOfTypeAll<PlatformLeaderboardsModel>();
                var l_FieldAccessor = typeof(PlatformLeaderboardsModel).GetField("_platformUserModel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                foreach (var l_Current in l_PlatformLeaderboardsModels)
                {
                    var l_PlatformUserModel = l_FieldAccessor.GetValue(l_Current) as IPlatformUserModel;

                    if (l_PlatformUserModel == null)
                        continue;

                    var l_Task = l_PlatformUserModel.GetUserInfo();
                    l_Task.Wait();

                    var l_PlayerID = l_Task.Result.platformUserId;
                    if (!string.IsNullOrEmpty(l_PlayerID))
                    {
                        m_UserID = l_PlayerID;
                        return l_PlayerID;
                    }
                }

                return GetUserInfo.GetUserID().ToString();
            }
            catch (System.Exception ex)
            {
                Logger.log.Critical(ex);
                return Plugin.instance.PluginConfig.GetString("General", "FallbackProfileID", "", true);
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Get playlist path for current profile ID
        /// </summary>
        /// <returns></returns>
        internal static string GetPlaylistFilePath()
        {
            return System.IO.Directory.GetCurrentDirectory() + "\\Playlists\\bsppbooster_" + GetProfileID() + ".json";
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Get profile refresh URL
        /// </summary>
        /// <returns></returns>
        internal static string GetAjaxProfileURL()
        {
            string l_PofileID = GetProfileID();
            //return $"http://bsppbooster/?/Home/AjaxProfile/ProfileID-{l_PofileID}";
            return $"https://bs-pp-booster.abachelet.fr/?/Home/AjaxProfile/ProfileID-{l_PofileID}";
        }
        /// <summary>
        /// Get playlist download URL
        /// </summary>
        /// <param name="p_PlaylistType">Type of the playlist</param>
        /// <param name="p_AccuracyType">Type of the accuracy</param>
        /// <param name="p_Accuracy">Desired accuracy</param>
        /// <param name="p_Difficulty">Desired difficulty</param>
        /// <returns></returns>
        internal static string GetAjaxProfilePlaylistURL(string p_PlaylistType, string p_AccuracyType, float p_Accuracy, float p_Difficulty)
        {
            string l_PofileID   = GetProfileID();
            string l_Type       = Helper.PlaylistAndAccuracyTypeToPlaylistID(p_PlaylistType, p_AccuracyType).ToString();
            string l_Acc        = System.Math.Round(p_Accuracy, 2).ToString().Replace(",", ".");
            string l_Dif        = System.Math.Round(p_Difficulty, 2).ToString().Replace(",", ".");

            //return $"http://bsppbooster/?/Home/AjaxProfilePlaylist/Download-1/ProfileID-{l_PofileID}/Type-{l_Type}/Acc-{l_Acc}/Dif-{l_Dif}";
            return $"https://bs-pp-booster.abachelet.fr/?/Home/AjaxProfilePlaylist/Download-1/ProfileID-{l_PofileID}/Type-{l_Type}/Acc-{l_Acc}/Dif-{l_Dif}";
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sanitize file name
        /// </summary>
        /// <param name="p_FileName">Filename to sanatize</param>
        /// <returns>Sanatized path</returns>
        internal static string MakeValidFileName(string p_FileName)
        {
            string l_InvalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string l_InvalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", l_InvalidChars);

            return System.Text.RegularExpressions.Regex.Replace(p_FileName, l_InvalidRegStr, "_").Trim();
        }
        /// <summary>
        /// Is a string valid JSon input
        /// </summary>
        /// <param name="p_Input">Input string</param>
        /// <returns></returns>
        internal static bool IsValidJson(string p_Input)
        {
            p_Input = p_Input.Trim();
            if ((p_Input.StartsWith("{") && p_Input.EndsWith("}")) ||
                (p_Input.StartsWith("[") && p_Input.EndsWith("]")))
            {
                try
                {
                    var l_Object = JToken.Parse(p_Input);
                    return true;
                }
                catch (JsonReaderException)
                {
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return false;
        }
    }
}
