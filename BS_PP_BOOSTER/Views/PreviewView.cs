using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using BS_Utils.Utilities;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BS_PP_BOOSTER.Views
{
    internal class PreviewView : BSMLResourceViewController
    {
        /// <summary>
        /// BSML file name
        /// </summary>
        public override string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        [UIValue("type-options")]
        private List<object> m_TypeOptions = null;
        [UIValue("type-choice")]
        private string m_TypeChoice;
        [UIValue("accuracy-options")]
        private List<object> m_AccuracyOptions;
        [UIValue("accuracy-choice")]
        private string m_AccuracyChoice;
        [UIValue("req-accu")]
        private float m_RequiredAccuracy = 75;
        [UIValue("req-diff")]
        private float m_RequiredDifficulty = 5;
        [UIComponent("preview-text1")]
        private TextMeshProUGUI m_PreviewText1 = null;
        [UIComponent("preview-text2")]
        private TextMeshProUGUI m_PreviewText2 = null;
        [UIComponent("preview-text3")]
        private TextMeshProUGUI m_PreviewText3 = null;
        [UIComponent("preview-text4")]
        private TextMeshProUGUI m_PreviewText4 = null;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        [UIObject("playlist-type-slider")]
        private GameObject m_PlaylistTypeSlider;
        [UIObject("accuracy-type-slider")]
        private GameObject m_AccuracyTypeSlider;
        [UIObject("accuracy-slider")]
        private GameObject m_AccuracySlider;
        [UIObject("difficulty-slider")]
        private GameObject m_DifficultySlider;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        [UIValue("playlist-type-hint")]
        public string PlaylistTypeHint = "Playlist types detailed on the left panel";
        [UIValue("accuracy-type-hint")]
        public string AccuracyTypeHint = "Max : Use target accuracy\nEstimate : Adapt target accuracy according to song difficulty";
        [UIValue("target-accuracy-hint")]
        public string TargetAccuracyHint = "Songs you scored above this accuracy will be ignored";
        [UIValue("difficulty-hint")]
        public string DifficultyHint = "Songs above this star rating will be ignored";

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Song downloader
        /// </summary>
        private WebClient m_SongDownloader = null;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor
        /// </summary>
        public PreviewView()
        {
            didActivate += (p_FirstActivation, _, __) =>
            {
                if (p_FirstActivation)
                {
                    PrepareSlider(m_PlaylistTypeSlider);
                    PrepareSlider(m_AccuracyTypeSlider);
                    PrepareSlider(m_AccuracySlider, true);
                    PrepareSlider(m_DifficultySlider, true);
                }
            };

            m_TypeOptions = Helper.PlaylistTypes;
            m_TypeChoice  = Helper.DefaultPlaylistType;

            m_AccuracyOptions = Helper.AccuracyTypes;
            m_AccuracyChoice  = Helper.DefaultAccuracyType;

            var l_Config    = Plugin.instance.PluginConfig;
            var l_Section   = "Config_" + Helper.GetProfileID();

            if (l_Config.HasKey(l_Section, "PreviewPlaylistType"))
                m_TypeChoice = Helper.PlaylistIDToPlaylistType(l_Config.GetInt(l_Section, "PreviewPlaylistType"));
            if (l_Config.HasKey(l_Section, "PreviewAccuracyType"))
                m_AccuracyChoice = Helper.AccuracyIDToAccuracyType(l_Config.GetInt(l_Section, "PreviewAccuracyType"));
            if (l_Config.HasKey(l_Section, "PreviewRequiredAccuracy"))
                m_RequiredAccuracy = ((float)l_Config.GetInt(l_Section, "PreviewRequiredAccuracy")) / 100;
            if (l_Config.HasKey(l_Section, "PreviewRequiredDifficulty"))
                m_RequiredDifficulty = ((float)l_Config.GetInt(l_Section, "PreviewRequiredDifficulty")) / 100;
        }
        /// <summary>
        /// Save settings
        /// </summary>
        public void SaveSettings()
        {
            var l_Section = "Config_" + Helper.GetProfileID();

            Plugin.instance.PluginConfig.SetInt(l_Section, "PreviewPlaylistType",       Helper.PlaylistTypeToPlaylistID(m_TypeChoice));
            Plugin.instance.PluginConfig.SetInt(l_Section, "PreviewAccuracyType",       Helper.AccuracyTypeToAccuracyID(m_AccuracyChoice));
            Plugin.instance.PluginConfig.SetInt(l_Section, "PreviewRequiredAccuracy",   (int)(m_RequiredAccuracy   * 100));
            Plugin.instance.PluginConfig.SetInt(l_Section, "PreviewRequiredDifficulty", (int)(m_RequiredDifficulty * 100));
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
                l_RectTransform.anchorMin = new Vector2(1.00f, -0.05f);
                l_RectTransform.anchorMax = new Vector2(0.90f, 1.05f);

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
        /// Preview
        /// </summary>
        [UIAction("click-btn-preview")]
        public void Preview()
        {
            BS_PP_BOOSTERController.Instance.SetVisible(true);
            BS_PP_BOOSTERController.Instance.SetText("Updating preview...");

            m_SongDownloader = new WebClient();
            m_SongDownloader.Proxy = null;   ///< Avoid searching for a proxy on the network for 10sec
            m_SongDownloader.DownloadStringCompleted += FetchPlaylistDone;

            m_SongDownloader.DownloadStringAsync(new System.Uri(Helper.GetAjaxProfilePlaylistURL(m_TypeChoice, m_AccuracyChoice, m_RequiredAccuracy, m_RequiredDifficulty)));
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When the playlist is fetched, update preview text
        /// </summary>
        /// <param name="p_Sender">Sender</param>
        /// <param name="p_Event">Event</param>
        private void FetchPlaylistDone(object p_Sender, DownloadStringCompletedEventArgs p_Event)
        {
            if (p_Event.Cancelled)
            {
                BS_PP_BOOSTERController.Instance.SetText("Updating preview failed");
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

            try
            {
                var l_JSON = JObject.Parse(p_Event.Result);
                string l_ProfileName = (string)l_JSON["bsppb_profile"];
                string l_CurPP       = (string)l_JSON["bsppb_curweipp"];
                string l_MaxPP       = (string)l_JSON["bsppb_maxweipp"];
                string l_Rank        = (string)l_JSON["bsppb_currank"];

                m_PreviewText1.text = $"<b>Ranked</b> <color=\"red\">{l_ProfileName}</color>";
                m_PreviewText2.text = $"Rank <color=\"red\">{l_Rank}</color>";
                m_PreviewText3.text = $"Current <color=\"red\">{l_CurPP}pp</color>";
                m_PreviewText4.text = $"Playlist target <color=\"red\">{l_MaxPP}pp</color>";
            }
            catch(System.Exception)
            {
                m_PreviewText1.text = "";
                m_PreviewText2.text = "";
                m_PreviewText3.text = "";
                m_PreviewText4.text = "";
            }
            __ResetViewController();
            BS_PP_BOOSTERController.Instance.SetText("Preview updated");
        }
    }
}
