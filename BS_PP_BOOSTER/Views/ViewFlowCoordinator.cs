using System;
using System.Linq;
using IPA.Utilities;
using HMUI;
using BeatSaberMarkupLanguage;
using BS_Utils.Utilities;
using UnityEngine;

namespace BS_PP_BOOSTER.Views
{
    /// <summary>
    /// UI flow coordinator
    /// </summary>
    class ViewFlowCoordinator : FlowCoordinator
    {
        /// <summary>
        /// Info view
        /// </summary>
        private InfoView m_InfoView = null;
        /// <summary>
        /// Main view instance
        /// </summary>
        private MainView m_MainView = null;
        /// <summary>
        /// Preview view instance
        /// </summary>
        private PreviewView m_PreviewView = null;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When the Unity GameObject and all children are ready
        /// </summary>
        public void Awake()
        {
            if (m_InfoView == null)
                m_InfoView = BeatSaberUI.CreateViewController<InfoView>();
            if (m_MainView == null)
                m_MainView = BeatSaberUI.CreateViewController<MainView>();
            if (m_PreviewView == null)
                m_PreviewView = BeatSaberUI.CreateViewController<PreviewView>();
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// On activation
        /// </summary>
        /// <param name="p_FirstActivation">Is the first activation ?</param>
        /// <param name="p_ActivationType">Activation type</param>
        protected override void DidActivate(bool p_FirstActivation, bool p_AddedToHierarchy, bool screenSystemEnabling)
        {
            try
            {
                if (p_FirstActivation)
                {
                    SetTitle("BS PP BOOSTER");
                    showBackButton = true;
                }

                if (p_AddedToHierarchy)
                {
                    ProvideInitialViewControllers(m_MainView, m_InfoView, m_PreviewView);
                }
            }
            catch (System.Exception ex)
            {
                Logger.log.Critical(ex);
                throw ex;
            }
        }
        /// <summary>
        /// When the back button is pressed
        /// </summary>
        /// <param name="p_TopViewController">Controller instance</param>
        protected override void BackButtonWasPressed(ViewController p_TopViewController)
        {
            base.BackButtonWasPressed(p_TopViewController);

            if (m_PreviewView != null) m_MainView.SaveSettings();
            if (m_PreviewView != null) m_PreviewView.SaveSettings();

            BeatSaberUI.MainFlowCoordinator.DismissFlowCoordinator(this, null);
        }
    }
}
