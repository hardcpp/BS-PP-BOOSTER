using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using Ionic.Zip;
using IPA;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;

namespace BS_PP_BOOSTER
{
    /// <summary>
    /// Main plugin class
    /// </summary>
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        /// <summary>
        /// Plugin instance
        /// </summary>
        internal static Plugin instance { get; private set; }
        /// <summary>
        /// Plugin name
        /// </summary>
        internal static string Name => "BS_PP_BOOSTER";

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Plugin config instance
        /// </summary>
        internal BS_Utils.Utilities.Config PluginConfig = null;

        internal ConcurrentQueue<(string, string)> ExtractQueue = new ConcurrentQueue<(string, string)>();

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// UI Flow coordinator
        /// </summary>
        private Views.ViewFlowCoordinator m_UIFlowCoordinator;
        /// <summary>
        /// Controller
        /// </summary>
        private BS_PP_BOOSTERController m_Controller = null;
        /// <summary>
        /// Web client
        /// </summary>
        private WebClient m_WebClient = null;

        private System.Threading.Thread m_ExtractThread;
        private bool m_ExtractRun = true;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Called when the plugin is first loaded by IPA (either when the game starts or when the plugin is enabled if it starts disabled).
        /// </summary>
        [Init]
        public void Init(IPALogger logger)
        {
            instance = this;
            Logger.log = logger;
            Logger.log.Debug("Logger initialized.");

            PluginConfig = new BS_Utils.Utilities.Config("BS_PP_BOOSTER");
            PluginConfig.GetString("General", "FallbackProfileID", "", true);
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        MenuButton m_MenuButton = new MenuButton("BS PP BOOSTER", "Placebo !", OnModButtonPressed, true);

        [OnEnable]
        public void OnEnable()
        {
            Logger.log.Debug("OnEnable.");

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            MenuButtons.instance.RegisterButton(m_MenuButton);

            /// Start extraction thread
            m_ExtractThread = new System.Threading.Thread(ExtractThread);
            m_ExtractThread.Start();
        }
        [OnDisable]
        public void OnDisable()
        {
            Logger.log.Debug("OnDisable.");

            if (m_UIFlowCoordinator != null)
            {
                GameObject.Destroy(m_UIFlowCoordinator.gameObject);
                m_UIFlowCoordinator = null;
            }

            MenuButtons.instance.UnregisterButton(m_MenuButton);
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;

            /// Stop extracting thread
            m_ExtractRun = false;
            m_ExtractThread.Join();

            /// Destroy controller
            if (m_Controller != null)
            {
                GameObject.Destroy(m_Controller);
                m_Controller = null;
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Song extraction thread
        /// </summary>
        private void ExtractThread()
        {
            while (m_ExtractRun)
            {
                if (ExtractQueue.TryDequeue(out var l_Current))
                {
                    if (l_Current.Item1 == "REFRESH")
                        instance.m_Controller.QueueRefresh = true;
                    else
                    {
                        try
                        {
                            string l_ZIPFileName        = l_Current.Item1;
                            string l_ExtractDirectory   = l_Current.Item2;

                            if (System.IO.Directory.Exists(l_ExtractDirectory))
                                System.IO.Directory.Delete(l_ExtractDirectory, true);

                            System.IO.Directory.CreateDirectory(l_ExtractDirectory);

                            using (var l_Zip = ZipFile.Read(l_ZIPFileName))
                            {
                                foreach (ZipEntry l_FileEntry in l_Zip)
                                    l_FileEntry.Extract(l_ExtractDirectory, ExtractExistingFileAction.OverwriteSilently);

                                l_Zip.Dispose();
                            }

                            System.IO.File.Delete(l_ZIPFileName);
                        }
                        catch (System.Exception p_Exception)
                        {
                            Logger.log.Error("Extract error : ");
                            Logger.log.Error(p_Exception);
                        }
                    }
                }

                System.Threading.Thread.Sleep(10);
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When the scene is changed
        /// </summary>
        /// <param name="p_OldScene">Old scene instance</param>
        /// <param name="p_NewScene">New scene instance</param>
        private void OnActiveSceneChanged(Scene p_OldScene, Scene p_NewScene)
        {
            Logger.log.Error("Scene changed " + p_NewScene.name);

            if (p_NewScene.name == "MenuViewControllers")
            {
                GetUserInfo.TriggerReady();
                GetUserInfo.UpdateUserInfo();

                var l_LevelCollectionViewController = Resources.FindObjectsOfTypeAll<LevelCollectionViewController>().FirstOrDefault();

                if (l_LevelCollectionViewController)
                {
                    l_LevelCollectionViewController.didSelectLevelEvent -= OnMapSelected;
                    l_LevelCollectionViewController.didSelectLevelEvent += OnMapSelected;
                }

                try
                {
                    if (m_Controller == null)
                        m_Controller = new GameObject("BS_PP_BOOSTERController").AddComponent<BS_PP_BOOSTERController>();

                    m_Controller.SetVisible(true);

                    CheckForInitialSetup();
                }
                catch (System.Exception)
                {

                }
            }
            else
            {
                if (m_Controller != null)
                    m_Controller.SetVisible(false);
            }
        }
        /// <summary>
        /// When a map is selected
        /// </summary>
        /// <param name="p_Controller">Controller instance</param>
        /// <param name="p_BeatMap">BeatMap instance</param>
        private void OnMapSelected(LevelCollectionViewController p_Controller, IPreviewBeatmapLevel p_BeatMap)
        {
            var l_ConfigSection = "Config_" + Helper.GetProfileID();
            if (!PluginConfig.HasKey(l_ConfigSection, "Enabled") || PluginConfig.GetBool(l_ConfigSection, "Enabled") == false)
            {
                m_Controller.SetText("");
                return;
            }

            /// Filter only custom levels
            if (!p_BeatMap.levelID.StartsWith("custom_level_"))
            {
                m_Controller.SetText("");
                return;
            }

            string l_BeatMapHash = p_BeatMap.levelID.Substring("custom_level_".Length);

            var l_BSPPBoosterData = GetSongInformationFromPlaylist(l_BeatMapHash);
            if (l_BSPPBoosterData == null)
            {
                m_Controller.SetText("");
                return;
            }

            string l_ExceptedDifficulty = l_BSPPBoosterData["bsppb_difficulty"];
            string l_ExceptedRanking    = l_BSPPBoosterData["bsppb_rank"];
            string l_ExceptedPPRaw      = l_BSPPBoosterData["bsppb_ppraw"] + "pp";
            string l_ExceptedPP         = l_BSPPBoosterData["bsppb_ppwei"] + "pp";
            string l_MinTargetAccuracy  = l_BSPPBoosterData["bsppb_targetacc"];
            string l_ProfileName        = l_BSPPBoosterData["bsppb_profile"];
            string l_CurPP              = l_BSPPBoosterData["bsppb_curweipp"];
            string l_MaxPP              = l_BSPPBoosterData["bsppb_maxweipp"];
            string l_Rank               = l_BSPPBoosterData["bsppb_currank"];

            string l_NewMessage = "";

            l_NewMessage += $"<b>Ranked</b> <color=\"red\">{l_ProfileName}</color> rank <color=\"red\">{l_Rank}</color> Current <color=\"red\">{l_CurPP}pp</color> Playlist target <color=\"red\">{l_MaxPP}pp</color> \n";
            l_NewMessage += $"<b>Difficulty :</b> <color=\"red\">{l_ExceptedDifficulty}</color> <b>My leaderboard rank :</b> #<color=\"red\">{l_ExceptedRanking}</color>\n";
            l_NewMessage += $"<b>Target PP :</b> {l_ExceptedPPRaw} raw (<color=\"red\">{l_ExceptedPP} weighted</color>) <b>Min target accuracy :</b> <color=\"red\">{l_MinTargetAccuracy}%</color>";

            m_Controller.SetText(l_NewMessage);
        }
        /// <summary>
        /// When the mod button is pressed
        /// </summary>
        private static void OnModButtonPressed()
        {
            if (instance.m_UIFlowCoordinator == null)
                instance.m_UIFlowCoordinator = BeatSaberMarkupLanguage.BeatSaberUI.CreateFlowCoordinator<Views.ViewFlowCoordinator>();

            BeatSaberUI.MainFlowCoordinator.PresentFlowCoordinator(instance.m_UIFlowCoordinator);
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Get BS PP Booster information from the play list
        /// </summary>
        /// <param name="p_Hash">Song hash</param>
        /// <returns>Data or null</returns>
        private Dictionary<string, string> GetSongInformationFromPlaylist(string p_Hash)
        {
            Dictionary<string, string> l_Result = new Dictionary<string, string>();

            string l_PlaylistPath = Helper.GetPlaylistFilePath();

            if (!System.IO.File.Exists(l_PlaylistPath))
            {
                Logger.log.Error("File not found " + l_PlaylistPath);
                return null;
            }

            var l_JSON = JObject.Parse(System.IO.File.ReadAllText(l_PlaylistPath));
            if (l_JSON["songs"] != null)
            {
                JArray l_JSONSongs = (JArray)l_JSON["songs"];
                for (int l_SongIt = 0; l_SongIt < l_JSONSongs.Count; l_SongIt++)
                {
                    if (p_Hash.ToLower() != ((string)l_JSONSongs[l_SongIt]["hash"]).ToLower())
                        continue;

                    l_Result.Add("bsppb_difficulty",(string)l_JSONSongs[l_SongIt]["bsppb_difficulty"]);
                    l_Result.Add("bsppb_rank",      (string)l_JSONSongs[l_SongIt]["bsppb_rank"]);
                    l_Result.Add("bsppb_ppraw",     (string)l_JSONSongs[l_SongIt]["bsppb_ppraw"]);
                    l_Result.Add("bsppb_ppwei",     (string)l_JSONSongs[l_SongIt]["bsppb_ppwei"]);
                    l_Result.Add("bsppb_score",     (string)l_JSONSongs[l_SongIt]["bsppb_score"]);
                    l_Result.Add("bsppb_scoremax",  (string)l_JSONSongs[l_SongIt]["bsppb_scoremax"]);
                    l_Result.Add("bsppb_targetacc", (string)l_JSONSongs[l_SongIt]["bsppb_targetacc"]);

                    if (l_JSON.ContainsKey("bsppb_profile"))  l_Result.Add("bsppb_profile",  (string)l_JSON["bsppb_profile"]);
                    if (l_JSON.ContainsKey("bsppb_currank"))  l_Result.Add("bsppb_currank",  (string)l_JSON["bsppb_currank"]);
                    if (l_JSON.ContainsKey("bsppb_curweipp")) l_Result.Add("bsppb_curweipp", (string)l_JSON["bsppb_curweipp"]);
                    if (l_JSON.ContainsKey("bsppb_maxweipp")) l_Result.Add("bsppb_maxweipp", (string)l_JSON["bsppb_maxweipp"]);

                    break;
                }
            }

            return l_Result.Count == 0 ? null : l_Result;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Check for initial setup
        /// </summary>
        private void CheckForInitialSetup()
        {
            /// Get initial settings if first time running
            var l_ConfigSection = "Config_" + Helper.GetProfileID();

            if (!PluginConfig.HasKey(l_ConfigSection, "Enabled"))
                Plugin.instance.PluginConfig.SetBool(l_ConfigSection, "Enabled", true);

            if (!PluginConfig.HasKey(l_ConfigSection, "RequiredAccuracy"))
            {
                BS_PP_BOOSTERController.Instance.SetVisible(true);
                BS_PP_BOOSTERController.Instance.SetText("Scanning profile...");

                m_WebClient = new WebClient();
                m_WebClient.Proxy = null;   ///< Avoid searching for a proxy on the network for 10sec
                m_WebClient.DownloadStringCompleted += (s, p_Event) =>
                {
                    if (p_Event.Cancelled)
                    {
                        BS_PP_BOOSTERController.Instance.SetText("Scanning profile failed");
                        return;
                    }

                    if (p_Event.Error != null)
                    {
                        BS_PP_BOOSTERController.Instance.SetText("Network error !");
                        return;
                    }

                    if (!Helper.IsValidJson(p_Event.Result))
                    {
                        BS_PP_BOOSTERController.Instance.SetText("Invalid server response !");
                        return;
                    }

                    var l_JSON = JObject.Parse(p_Event.Result);
                    bool l_Error = (bool)l_JSON["error"];

                    if (l_Error)
                        BS_PP_BOOSTERController.Instance.SetText("Scanning profile failed");
                    else
                    {
                        BS_PP_BOOSTERController.Instance.SetText("Profile updated");

                        try
                        {
                            float l_Acc = (float)l_JSON["AvgAcc"];
                            float l_Dif = (float)l_JSON["MaxDiff"];

                            Plugin.instance.PluginConfig.SetInt(l_ConfigSection, "RequiredAccuracy", (int)(l_Acc * 100));
                            Plugin.instance.PluginConfig.SetInt(l_ConfigSection, "RequiredDifficulty", (int)(l_Dif * 100));
                        }
                        catch (System.Exception)
                        {

                        }
                    }

                    m_WebClient = null;
                };

                m_WebClient.DownloadStringAsync(new System.Uri(Helper.GetAjaxProfileURL()));
            }
        }
    }
}
