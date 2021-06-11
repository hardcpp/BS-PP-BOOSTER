using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using BS_Utils.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BS_PP_BOOSTER.Views
{
    /// <summary>
    /// Main view UI controller
    /// </summary>
    internal class MainView : BSMLResourceViewController
    {
        /// <summary>
        /// BSML file name
        /// </summary>
        public override string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        [UIValue("enabled")]
        private bool m_Enabled = true;
        [UIValue("req-accu")]
        private float m_RequiredAccuracy = 75;
        [UIValue("req-diff")]
        private float m_RequiredDifficulty = 5;
        [UIValue("playlist-size")]
        private int m_PlaylistSize = 120;
        [UIValue("type-options")]
        private List<object> m_TypeOptions;
        [UIValue("type-choice")]
        private string m_TypeChoice;
        [UIValue("accuracy-options")]
        private List<object> m_AccuracyOptions;
        [UIValue("accuracy-choice")]
        private string m_AccuracyChoice;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        [UIValue("reset-button-text")]
        public string ResetButtonText = "Set acc & diff from profile avg";

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        [UIValue("enabled-hint")]
        public string EnabledHint = "Enable or disable the information text shown below when you select a song";
        [UIValue("playlist-type-hint")]
        public string PlaylistTypeHint = "Playlist types detailed on the left panel";
        [UIValue("accuracy-type-hint")]
        public string AccuracyTypeHint = "Max : Use target accuracy\nEstimate : Adapt target accuracy according to song difficulty";

        [UIValue("target-accuracy-hint")]
        public string TargetAccuracyHint = "Songs you scored above this accuracy will be ignored";
        [UIValue("difficulty-hint")]
        public string DifficultyHint = "Songs above this star rating will be ignored";
        [UIValue("playlist-size-hint")]
        public string PlaylistSizeHint = "Maximum playlist size (This will also limit download count)";

        [UIValue("generate-button-hint")]
        public string GenerateButtonHint = "Will generate a playlist, it will also download missing songs";
        [UIValue("rescan-button-hint")]
        public string RescanButtonHint = "This will force a profile re-scan on the sever (This is automatically done every hour)";
        [UIValue("reset-button-hint")]
        public string ResetButtonHint = "This will set for you the target accuracy & difficulty\naccording to your score saber profile average";

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// List of songs to download
        /// </summary>
        private List<string> m_SongsToDownload = new List<string>();
        /// <summary>
        /// Song download index
        /// </summary>
        private int m_SongDownloadIndex = 0;
        /// <summary>
        /// Current download meta data
        /// </summary>
        private JObject m_CurrentDownloadMeta = null;
        /// <summary>
        /// Song downloader
        /// </summary>
        private WebClient m_SongDownloader = null;
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private Http m_HttpClient = null;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        [UIObject("enable-slider")]
        private GameObject m_EnableSlider;
        [UIObject("playlist-type-slider")]
        private GameObject m_PlaylistTypeSlider;
        [UIObject("accuracy-type-slider")]
        private GameObject m_AccuracyTypeSlider;
        [UIObject("accuracy-slider")]
        private GameObject m_AccuracySlider;
        [UIObject("difficulty-slider")]
        private GameObject m_DifficultySlider;
        [UIObject("playlist-size-slider")]
        private GameObject m_PlaylistSizeSlider;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor
        /// </summary>
        public MainView()
        {
            didActivate += (p_FirstActivation, _, __) =>
            {
                if (p_FirstActivation)
                {
                    PrepareSlider(m_EnableSlider);
                    PrepareSlider(m_PlaylistTypeSlider);
                    PrepareSlider(m_AccuracyTypeSlider);

                    PrepareSlider(m_AccuracySlider, true);
                    PrepareSlider(m_DifficultySlider, true);
                    PrepareSlider(m_PlaylistSizeSlider, true);
                }
            };

            m_TypeOptions           = Helper.PlaylistTypes;
            m_TypeChoice            = Helper.DefaultPlaylistType;

            m_AccuracyOptions       = Helper.AccuracyTypes;
            m_AccuracyChoice        = Helper.DefaultAccuracyType;

            var l_Config    = Plugin.instance.PluginConfig;
            var l_Section   = "Config_" + Helper.GetProfileID();

            if (l_Config.HasKey(l_Section, "Enabled"))
                m_Enabled = l_Config.GetBool(l_Section, "Enabled");
            if (l_Config.HasKey(l_Section, "PlaylistType"))
                m_TypeChoice = Helper.PlaylistIDToPlaylistType(l_Config.GetInt(l_Section, "PlaylistType"));
            if (l_Config.HasKey(l_Section, "AccuracyType"))
                m_AccuracyChoice = Helper.AccuracyIDToAccuracyType(l_Config.GetInt(l_Section, "AccuracyType"));
            if (l_Config.HasKey(l_Section, "RequiredAccuracy"))
                m_RequiredAccuracy = ((float)l_Config.GetInt(l_Section, "RequiredAccuracy")) / 100;
            if (l_Config.HasKey(l_Section, "RequiredDifficulty"))
                m_RequiredDifficulty = ((float)l_Config.GetInt(l_Section, "RequiredDifficulty")) / 100;
            if (l_Config.HasKey(l_Section, "PlaylistSize"))
                m_PlaylistSize = l_Config.GetInt(l_Section, "PlaylistSize");

            m_HttpClient = new Http(new HttpOptions()
            {
                ApplicationName = "bs_pp_booster",
                Version = new System.Version(1, 1, 0),
                HandleRateLimits = true
            });
        }
        /// <summary>
        /// Save settings
        /// </summary>
        public void SaveSettings()
        {
            var l_Section = "Config_" + Helper.GetProfileID();

            Plugin.instance.PluginConfig.SetBool(l_Section, "Enabled",              m_Enabled);
            Plugin.instance.PluginConfig.SetInt(l_Section,  "PlaylistType",         Helper.PlaylistTypeToPlaylistID(m_TypeChoice));
            Plugin.instance.PluginConfig.SetInt(l_Section,  "AccuracyType",         Helper.AccuracyTypeToAccuracyID(m_AccuracyChoice));
            Plugin.instance.PluginConfig.SetInt(l_Section,  "RequiredAccuracy",     (int)(m_RequiredAccuracy * 100));
            Plugin.instance.PluginConfig.SetInt(l_Section,  "RequiredDifficulty",   (int)(m_RequiredDifficulty * 100));
            Plugin.instance.PluginConfig.SetInt(l_Section,  "PlaylistSize",         m_PlaylistSize);
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Prepare slider widget
        /// </summary>
        /// <param name="p_Object">Slider instance</param>
        /// <param name="p_AddControls">Should add left & right controls</param>
        void PrepareSlider(GameObject p_Object, bool p_AddControls = false)
        {
            GameObject.Destroy(p_Object.GetComponentsInChildren<TextMeshProUGUI>().ElementAt(0).transform.gameObject);

            RectTransform l_RectTransform = p_Object.transform.GetChild(1) as RectTransform;
            l_RectTransform.anchorMin = new Vector2(0f, 0f);
            l_RectTransform.anchorMax = new Vector2(1f, 1f);
            l_RectTransform.sizeDelta = new Vector2(1, 1);

            p_Object.GetComponent<LayoutElement>().preferredWidth = -1f;

            if (p_AddControls)
            {
                l_RectTransform = p_Object.transform.Find("BSMLSlider") as RectTransform;
                l_RectTransform.anchorMin = new Vector2(0.65f, -0.05f);
                l_RectTransform.anchorMax = new Vector2(0.93f, 1.05f);

                FormattedFloatListSettingsValueController l_BaseSettings = MonoBehaviour.Instantiate(Resources.FindObjectsOfTypeAll<FormattedFloatListSettingsValueController>().First(x => (x.name == "VRRenderingScale")), p_Object.transform, false);
                var l_DecButton = l_BaseSettings.transform.GetChild(1).GetComponentsInChildren<Button>().First();
                var l_IncButton = l_BaseSettings.transform.GetChild(1).GetComponentsInChildren<Button>().Last();

                l_DecButton.transform.SetParent(p_Object.transform, false);
                l_IncButton.transform.SetParent(p_Object.transform, false);

                l_DecButton.transform.SetAsFirstSibling();
                l_IncButton.transform.SetAsFirstSibling();

                foreach (Transform l_Child in l_BaseSettings.transform)
                    GameObject.Destroy(l_Child.gameObject);

                GameObject.Destroy(l_BaseSettings);

                SliderSetting l_SliderSetting = p_Object.GetComponent<SliderSetting>();

                l_SliderSetting.slider.valueDidChangeEvent += (_, p_Value) =>
                {
                    l_DecButton.interactable = p_Value > l_SliderSetting.slider.minValue;
                    l_IncButton.interactable = p_Value < l_SliderSetting.slider.maxValue;
                };

                l_DecButton.interactable = l_SliderSetting.slider.value > l_SliderSetting.slider.minValue;
                l_IncButton.interactable = l_SliderSetting.slider.value < l_SliderSetting.slider.maxValue;

                l_DecButton.onClick.RemoveAllListeners();
                l_DecButton.onClick.AddListener(() =>
                {
                    l_SliderSetting.slider.value -= l_SliderSetting.increments;
                    l_SliderSetting.InvokeMethod("OnChange", new object[] { l_SliderSetting.slider, l_SliderSetting.slider.value });
                });
                l_IncButton.onClick.RemoveAllListeners();
                l_IncButton.onClick.AddListener(() =>
                {
                    l_SliderSetting.slider.value += l_SliderSetting.increments;
                    l_SliderSetting.InvokeMethod("OnChange", new object[] { l_SliderSetting.slider, l_SliderSetting.slider.value });
                });
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Download play list
        /// </summary>
        [UIAction("click-btn-generate")]
        public void Generate()
        {
            BS_PP_BOOSTERController.Instance.SetVisible(true);
            BS_PP_BOOSTERController.Instance.SetText("Fetching playlist...");

            m_SongDownloader = new WebClient();
            m_SongDownloader.Proxy                    = null;   ///< Avoid searching for a proxy on the network for 10sec
            m_SongDownloader.DownloadStringCompleted += FetchPlaylistDone;

            m_SongDownloader.DownloadStringAsync(new System.Uri(Helper.GetAjaxProfilePlaylistURL(m_TypeChoice, m_AccuracyChoice, m_RequiredAccuracy, m_RequiredDifficulty)));
        }
        /// <summary>
        /// Rescan profile
        /// </summary>
        [UIAction("click-btn-rescan")]
        public void RescanProfile()
        {
            BS_PP_BOOSTERController.Instance.SetVisible(true);
            BS_PP_BOOSTERController.Instance.SetText("Scanning profile...");

            m_SongDownloader = new WebClient();
            m_SongDownloader.Proxy                    = null;   ///< Avoid searching for a proxy on the network for 10sec
            m_SongDownloader.DownloadStringCompleted += (s, p_Event) =>
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

                var  l_JSON  = JObject.Parse(p_Event.Result);
                bool l_Error = (bool)l_JSON["error"];

                if (l_Error)
                    BS_PP_BOOSTERController.Instance.SetText("Scanning profile failed");
                else
                    BS_PP_BOOSTERController.Instance.SetText("Profile updated");
            };

            m_SongDownloader.DownloadStringAsync(new System.Uri(Helper.GetAjaxProfileURL()));
        }
        /// <summary>
        /// Rescan profile
        /// </summary>
        [UIAction("click-btn-reset")]
        public void Reset()
        {
            BS_PP_BOOSTERController.Instance.SetVisible(true);
            BS_PP_BOOSTERController.Instance.SetText("Scanning profile...");

            m_SongDownloader = new WebClient();
            m_SongDownloader.Proxy = null;   ///< Avoid searching for a proxy on the network for 10sec
            m_SongDownloader.DownloadStringCompleted += (s, p_Event) =>
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
                        var l_Section = "Config_" + Helper.GetProfileID();

                        m_RequiredAccuracy   = (float)l_JSON["AvgAcc"];
                        m_RequiredDifficulty = (float)l_JSON["MaxDiff"];

                        var l_Sliders = GetComponentsInChildren<BeatSaberMarkupLanguage.Components.Settings.SliderSetting>();
                        foreach (var l_Slider in l_Sliders)
                            l_Slider.ReceiveValue();
                    }
                    catch (System.Exception)
                    {

                    }
                }
            };

            m_SongDownloader.DownloadStringAsync(new System.Uri(Helper.GetAjaxProfileURL()));
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When the playlist is fetched, start downloading songs
        /// </summary>
        /// <param name="p_Sender">Sender</param>
        /// <param name="p_Event">Event</param>
        private void FetchPlaylistDone(object p_Sender, DownloadStringCompletedEventArgs p_Event)
        {
            if (p_Event.Cancelled)
            {
                BS_PP_BOOSTERController.Instance.SetText("Fetching playlist failed");
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

            var l_IPlaylist = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.DefaultHandler.Deserialize(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(p_Event.Result)));
            l_IPlaylist.Filename = Helper.GetPlaylistFileName();

            m_SongsToDownload.Clear();

            ///var l_JSON = JObject.Parse(p_Event.Result);
            ///if (l_JSON["songs"] != null)
            ///{
            ///    JArray l_JSONSongs = (JArray)l_JSON["songs"];
            ///    for (int l_SongIt = 0; l_SongIt < l_JSONSongs.Count && l_SongIt < m_PlaylistSize; l_SongIt++)
            ///    {
            ///        string l_Hash = (string)l_JSONSongs[l_SongIt]["hash"];
            ///        var l_Song = SongCore.Loader.CustomLevels.Values.FirstOrDefault(y => string.Equals(y.levelID.Split('_')[2], l_Hash, StringComparison.OrdinalIgnoreCase));
            ///
            ///        if (l_Song == null && !m_SongsToDownload.Contains(l_Hash))
            ///            m_SongsToDownload.Add(l_Hash);
            ///    }
            ///
            ///    while (l_JSONSongs.Count > m_PlaylistSize)
            ///        l_JSONSongs.RemoveAt(l_JSONSongs.Count - 1);
            ///}


            int l_AddedSongs = 0;
            if (l_IPlaylist is BeatSaberPlaylistsLib.Legacy.LegacyPlaylist l_LegacyPlaylist)
            {
                foreach (var l_CurrentMap in l_LegacyPlaylist)
                {
                    if (l_CurrentMap.Hash != null)
                    {
                        var l_Song = SongCore.Loader.CustomLevels.Values.FirstOrDefault(y => string.Equals(y.levelID.Split('_')[2], l_CurrentMap.Hash, StringComparison.OrdinalIgnoreCase));

                        if (l_Song == null && !m_SongsToDownload.Contains(l_CurrentMap.Hash))
                            m_SongsToDownload.Add(l_CurrentMap.Hash);

                        l_AddedSongs++;
                    }

                    if (l_AddedSongs >= m_PlaylistSize)
                        break;
                }
            }
            else if (l_IPlaylist is BeatSaberPlaylistsLib.Blist.BlistPlaylist l_BlistPlaylist)
            {
                foreach (var l_CurrentMap in l_BlistPlaylist)
                {
                    if (l_CurrentMap.Hash != null)
                    {
                        var l_Song = SongCore.Loader.CustomLevels.Values.FirstOrDefault(y => string.Equals(y.levelID.Split('_')[2], l_CurrentMap.Hash, StringComparison.OrdinalIgnoreCase));

                        if (l_Song == null && !m_SongsToDownload.Contains(l_CurrentMap.Hash))
                            m_SongsToDownload.Add(l_CurrentMap.Hash);

                        l_AddedSongs++;
                    }

                    if (l_AddedSongs >= m_PlaylistSize)
                        break;
                }
            }

            BS_PP_BOOSTERController.Instance.SetText("Writing playlist...");
            BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.StorePlaylist(l_IPlaylist);
            BS_PP_BOOSTERController.Instance.SetText("Playlist OK");

            if (m_SongsToDownload.Count != 0)
            {
                m_SongDownloadIndex = 0;
                QuerySongMeta();
            }
            else
                Helper.RefreshPlaylist();
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Query song meta informations
        /// </summary>
        private void QuerySongMeta()
        {
            string l_URL = "http://beatsaver.com/api/maps/by-hash/" + m_SongsToDownload[m_SongDownloadIndex].ToLower();
            Logger.log.Debug("Requesting " + l_URL);

            try
            {
                m_HttpClient.GetAsync(l_URL, CancellationToken.None, null).ContinueWith((x) => {

                    try
                    {
                        SongDownloaded_Meta(x.Result);
                    }
                    catch (System.Exception e)
                    {
                        Logger.log.Error("QuerySongMeta_Lambda");
                        Logger.log.Error(e.ToString());

                        m_SongDownloadIndex++;

                        if (m_SongDownloadIndex < m_SongsToDownload.Count)
                            QuerySongMeta();
                        else
                            DownloadComplete();
                    }

                });

                BS_PP_BOOSTERController.Instance.SetText("Downloading song " + (m_SongDownloadIndex + 1) + "/" + m_SongsToDownload.Count);
                BS_PP_BOOSTERController.Instance.SetTextPercentage(0);
            }
            catch (System.Exception e)
            {
                Logger.log.Error("QuerySongMeta");
                Logger.log.Error(e.ToString());
            }
        }
        /// <summary>
        /// On song meta received
        /// </summary>
        /// <param name="p_Sender">Sender</param>
        /// <param name="p_Event">Event</param>
        private void SongDownloaded_Meta(HttpResponse p_Response)
        {
            try
            {
                if (p_Response == null || !p_Response.IsSuccessStatusCode || p_Response.StatusCode != HttpStatusCode.OK)
                {
                    Logger.log.Error("Meta failed : ");
                    Logger.log.Error(p_Response.ReasonPhrase);
                    m_SongDownloadIndex++;

                    if (m_SongDownloadIndex < m_SongsToDownload.Count)
                        QuerySongMeta();
                    else
                        DownloadComplete();

                    return;
                }

                m_CurrentDownloadMeta = JObject.Parse(p_Response.String());

                string[] l_PathArray = new string[6]
                {
                    (string)m_CurrentDownloadMeta["key"],
                    " (",
                    null,
                    null,
                    null,
                    null
                 };

                l_PathArray[2] = (string)m_CurrentDownloadMeta["metadata"]["songName"];
                l_PathArray[3] = " - ";
                l_PathArray[4] = (string)m_CurrentDownloadMeta["metadata"]["levelAuthorName"];
                l_PathArray[5] = ")";

                string l_BasePath = string.Concat(l_PathArray);
                l_BasePath = string.Join("", l_BasePath.Split(((IEnumerable<char>)System.IO.Path.GetInvalidFileNameChars()).Concat<char>((IEnumerable<char>)System.IO.Path.GetInvalidPathChars()).ToArray<char>()));
                string l_ExtractPath = System.IO.Directory.GetCurrentDirectory() + "\\Beat Saber_Data\\CustomLevels\\" + l_BasePath;

                var l_ZIPFileName = System.IO.Directory.GetCurrentDirectory() + "\\Beat Saber_Data\\CustomLevels\\" + Helper.MakeValidFileName((string)m_CurrentDownloadMeta["key"] + ".zip");

                if (System.IO.File.Exists(l_ZIPFileName))
                    System.IO.File.Delete(l_ZIPFileName);

                string l_DownloadURL = "http://beatsaver.com" + (string)m_CurrentDownloadMeta["directDownload"];

                /// Support direct URL
                if (((string)m_CurrentDownloadMeta["directDownload"]).ToLower().StartsWith("http"))
                    l_DownloadURL = (string)m_CurrentDownloadMeta["directDownload"];

                //Logger.log.Debug("Downlaoding " + l_DownloadURL);

                var l_Query = m_HttpClient.GetAsync(l_DownloadURL, CancellationToken.None, new ProgressReporter());
                l_Query.GetAwaiter().OnCompleted(() =>
                {
                    try
                    {
                        if (l_Query.Result.StatusCode == HttpStatusCode.OK)
                        {
                            System.IO.File.WriteAllBytes(l_ZIPFileName, l_Query.Result.Bytes());
                            Plugin.instance.ExtractQueue.Enqueue((l_ZIPFileName, l_ExtractPath));
                        }
                        else
                            Logger.log.Error("HTTP Result " + l_Query.Result.StatusCode);

                        m_SongDownloadIndex++;

                        if (m_SongDownloadIndex < m_SongsToDownload.Count)
                            QuerySongMeta();
                        else
                            DownloadComplete();
                    }
                    catch (System.Exception e)
                    {
                        Logger.log.Error("DownloadSong");
                        Logger.log.Error(e.ToString());
                    }

                });
            }
            catch(System.Exception e)
            {
                Logger.log.Error("SongDownloaded_Meta");
                Logger.log.Error(e.ToString());
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When the download is complete
        /// </summary>
        private void DownloadComplete()
        {
            Plugin.instance.ExtractQueue.Enqueue(("REFRESH", ""));
            BS_PP_BOOSTERController.Instance.SetText("Download complete");
        }
    }

    class ProgressReporter : IProgress<double>
    {
        public void Report(double value)
        {
            BS_PP_BOOSTERController.Instance.SetTextPercentage(value);
        }
    }
}
