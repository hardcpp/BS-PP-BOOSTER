using System.Collections;
using System.Collections.Generic;
using SongCore;
using TMPro;
using UnityEngine;
using BS_Utils.Utilities;
using IPA.Utilities;

namespace BS_PP_BOOSTER
{
    /// <summary>
    /// Plugin unity controller
    /// </summary>
    public class BS_PP_BOOSTERController : MonoBehaviour
    {
        private static readonly Vector3 Position        = new Vector3(0, 0.05f, 1.5f);
        private static readonly Vector3 Rotation        = new Vector3(90, 0, 0);
        private static readonly Vector3 Scale           = new Vector3(0.01f, 0.01f, 0.01f);
        private static readonly Vector2 CanvasSize      = new Vector2(400, 50);
        private static readonly Vector2 HeaderPosition  = new Vector2(10, 15);
        private static readonly float   HeaderFontSize  = 15f;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This instance
        /// </summary>
        public static BS_PP_BOOSTERController Instance { get; private set; }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Have a playlist refresh queued
        /// </summary>
        public bool QueueRefresh = false;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Canvas instance
        /// </summary>
        private Canvas m_Canvas;
        /// <summary>
        /// Text instance
        /// </summary>
        private TMP_Text m_Text;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake()
        {
            if (Instance != null)
            {
                Logger.log?.Warn($"Instance of {this.GetType().Name} already exists, destroying.");
                DestroyImmediate(this);
                return;
            }

            Instance = this;

            var l_RectTransform = null as RectTransform;

            gameObject.transform.position = Position;
            gameObject.transform.eulerAngles = Rotation;
            gameObject.transform.localScale = Scale;

            m_Canvas                    = gameObject.AddComponent<Canvas>();
            m_Canvas.renderMode         = RenderMode.WorldSpace;
            l_RectTransform             = m_Canvas.transform as RectTransform;
            l_RectTransform.sizeDelta   = CanvasSize;

            gameObject.AddComponent<HMUI.CurvedCanvasSettings>().SetRadius(0);

            m_Text = CreateText(m_Canvas.transform as RectTransform, "", HeaderPosition, CanvasSize);
            l_RectTransform = m_Text.transform as RectTransform;
            l_RectTransform.anchoredPosition    = Vector2.zero;
            m_Text.text                         = "";
            m_Text.fontSize                     = HeaderFontSize;
            m_Text.alignment                    = TextAlignmentOptions.Center;

            ///var l_Background            = new GameObject("Background").AddComponent<Image>();
            ///l_RectTransform             = l_Background.transform as RectTransform;
            ///l_RectTransform.SetParent(m_Canvas.transform, false);
            ///l_RectTransform.sizeDelta   = CanvasSize;
            ///l_Background.color          = new Color(0, 1, 0, 0.5f);

            SetVisible(false);

            /// Don't destroy this object on scene changes
            DontDestroyOnLoad(this);
        }
        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        private void OnDestroy()
        {
            Logger.log?.Debug($"{name}: OnDestroy()");
            Instance = null;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Static updated
        /// </summary>
        private void Update()
        {
            if (QueueRefresh)
            {
                Helper.RefreshPlaylist();
                QueueRefresh = false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set visible
        /// </summary>
        /// <param name="p_Visible">New state</param>
        public void SetVisible(bool p_Visible)
        {
            m_Canvas.enabled = p_Visible;
        }
        /// <summary>
        /// Set text
        /// </summary>
        /// <param name="p_Text">New text</param>
        public void SetText(string p_Text)
        {
            m_Text.text = p_Text;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create a text mesh pro instance
        /// </summary>
        /// <param name="p_Parent">Transform parent instance</param>
        /// <param name="p_Text">Text value</param>
        /// <param name="p_AnchoredPosition">Anchored position</param>
        /// <param name="p_SizeDelta">Size</param>
        /// <returns>Instance</returns>
        private TextMeshProUGUI CreateText(RectTransform p_Parent, string p_Text, Vector2 p_AnchoredPosition, Vector2 p_SizeDelta)
        {
            GameObject l_GameObject = new GameObject("CustomUIText");
            l_GameObject.SetActive(false);

            TextMeshProUGUI l_Text = l_GameObject.AddComponent<HMUI.CurvedTextMeshPro>();
            l_Text.rectTransform.SetParent(p_Parent, false);
            l_Text.font                             = Resources.Load<TMP_FontAsset>("Teko-Medium SDF No Glow");
            l_Text.text                             = p_Text;
            l_Text.fontSize                         = 4;
            l_Text.color                            = Color.white;
            l_Text.rectTransform.anchorMin          = new Vector2(0.5f, 0.5f);
            l_Text.rectTransform.anchorMax          = new Vector2(0.5f, 0.5f);
            l_Text.rectTransform.sizeDelta          = p_SizeDelta;
            l_Text.rectTransform.anchoredPosition   = p_AnchoredPosition;

            l_GameObject.SetActive(true);
            return l_Text;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Start refreshing songs
        /// </summary>
        public void RefreshSongs()
        {
            StartCoroutine(RefreshSongsCoroutine());
        }
        /// <summary>
        /// Reload song coroutine
        /// </summary>
        /// <returns></returns>
        internal IEnumerator RefreshSongsCoroutine()
        {
            SetVisible(true);
            SetText("Reloading songs");

            if (!Loader.AreSongsLoading)
                Loader.Instance.RefreshSongs();

            yield return new WaitUntil(() => Loader.AreSongsLoaded == true);

            var l_Type = typeof(PlaylistLoaderLite.LoadPlaylistScript);
            try
            {
                l_Type.InvokeMethod("load", new object[] { });
            }
            catch (System.Exception)
            {
                l_Type.InvokeMethod("Load", new object[] { });
            }

            SetText("Songs reloaded !");
        }
    }
}
