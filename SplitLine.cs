//BRINGS THE SNAP ENVIROMENT INTO A BASETOOL AND SPLITS A LINE FEATURE AT THE SELECTED SNAP POINT

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.ArcMapUI;
using System.Windows.Forms;
using ElphoGenericTools;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Editor;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ElphoGenericTools
{
    /// <summary>
    /// Summary description for GetXY.
    /// </summary>
    [Guid("f8b061ab-8b46-405d-a3fc-f2b773da57b1")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("Elpho.SplitLine")]
    public sealed class ElphoSplitLineCommand : BaseTool
    {
        private ISnappingEnvironment m_SnappingEnvironment;
        private IPoint m_CurrentMouseCoords;
        private ISnappingFeedback m_SnappingFeedback;
        public IEditor MEditor;
        public static IHookHelper MHookHelper;

        #region COM Registration Function(s)
        [ComRegisterFunction()]
        [ComVisible(false)]
        static void RegisterFunction(Type registerType)
        {
            // Required for ArcGIS Component Category Registrar support
            ArcGISCategoryRegistration(registerType);

            //
            // TODO: Add any COM registration code here
            //
        }

        [ComUnregisterFunction()]
        [ComVisible(false)]
        static void UnregisterFunction(Type registerType)
        {
            // Required for ArcGIS Component Category Registrar support
            ArcGISCategoryUnregistration(registerType);

            //
            // TODO: Add any COM unregistration code here
            //
        }

        #region ArcGIS Component Category Registrar generated code
        /// <summary>
        /// Required method for ArcGIS Component Category registration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryRegistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            MxCommands.Register(regKey);

        }
        /// <summary>
        /// Required method for ArcGIS Component Category unregistration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryUnregistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            MxCommands.Unregister(regKey);

        }

        #endregion
        #endregion

        private IApplication m_application;
        public ElphoSplitLineCommand()
        {
            MHookHelper = new HookHelperClass();

            base.m_category = "Elpho Tools"; //localizable text
            base.m_caption = "Split PolyLine Feature"; //localizable text
            base.m_message = "Split PolyLine Feature"; //localizable text
            base.m_toolTip = "Split PolyLine Feature"; //localizable text
            base.m_name = "ElphoGenericMenu"; //unique id, non-localizable (e.g. "MyCategory_ArcMapTool")

            try
            {
                string bitmapResourceName = GetType().Name + ".bmp";
                base.m_bitmap = new Bitmap(GetType(), bitmapResourceName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message, "Invalid Bitmap");
            }
        }

        #region Overriden Class Methods

        /// <summary>
        /// Occurs when this tool is created
        /// </summary>
        /// <param name="hook">Instance of the application</param>
        public override void OnCreate(object hook)
        {
            if (hook != null)
                m_application = hook as IApplication;

            MHookHelper.Hook = hook;
            UID editorUid = new UID();
            editorUid.Value = "esriEditor.Editor";

            MEditor = m_application.FindExtensionByCLSID(editorUid) as IEditor;
        }

        /// <summary>
        /// Occurs when this tool is clicked
        /// </summary>
        public override void OnClick()
        {
            m_SnappingEnvironment = GetSnappingEnvironment();
        }

        public override void OnMouseDown(int Button, int Shift, int X, int Y)
        {

            bool hasCutPolygonsA = false;
            bool hasCutPolygonsB = false;

            var editLayers = MEditor as IEditLayers;
            var layer = editLayers.CurrentLayer;
            if (layer != null)
            {
                var pFeatureClass = layer.FeatureClass;

                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = m_CurrentMouseCoords;
                spatialFilter.GeometryField = pFeatureClass.ShapeFieldName;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                List<IFeature> spatialfeatures = new List<IFeature>();
                // Execute the query and iterate through the cursor's results.
                using (ComReleaser comReleaser = new ComReleaser())
                {
                    IFeatureCursor highwayCursor = pFeatureClass.Search(spatialFilter, false);
                    comReleaser.ManageLifetime(highwayCursor);
                    IFeature highwayFeature = null;
                    while ((highwayFeature = highwayCursor.NextFeature()) != null)
                    {
                        spatialfeatures.Add(highwayFeature);
                    }
                }

                if (spatialfeatures.Count == 1)
                {
                    IFeatureEdit featureEditA = spatialfeatures[0] as IFeatureEdit;
                    ISet newFeaturesSetA = featureEditA.Split(m_CurrentMouseCoords);

                    if (newFeaturesSetA != null)
                    {
                        newFeaturesSetA.Reset();
                        hasCutPolygonsA = true;
                    }

                    if (hasCutPolygonsA)
                    {
                        var map = MHookHelper.FocusMap;

                        map.ClearSelection();

                        IActiveView activeView = map as IActiveView;
                        activeView.Refresh();
                    }
                }
            }
            else
            {
                MessageBox.Show("You Are not in Editing Mode");
            }
        }


        public override void OnMouseMove(int Button, int Shift, int X, int Y)
        {
            ESRI.ArcGIS.Display.IScreenDisplay screenDisplay = (m_application.Document as IMxDocument).ActiveView.ScreenDisplay;

            m_CurrentMouseCoords = QueryMapPoint(screenDisplay, X, Y);

            ISnappingResult snapResult = m_SnappingEnvironment.PointSnapper.Snap(m_CurrentMouseCoords);

            if (m_SnappingFeedback == null)
            {
                m_SnappingFeedback = new SnappingFeedbackClass();
                m_SnappingFeedback.Initialize(ArcMap.Application, m_SnappingEnvironment, true);
            }
            m_SnappingFeedback.Update(snapResult, 0);

            if (snapResult != null)
                m_CurrentMouseCoords = snapResult.Location;
        }

        public override void OnMouseUp(int Button, int Shift, int X, int Y)
        {
            // TODO: Add GetXY.OnMouseUp implementation
        }

        private IPoint QueryMapPoint(IScreenDisplay m_display, int X, int Y)
        {
            IDisplayTransformation dispTransformation = m_display.DisplayTransformation;

            IPoint point = dispTransformation.ToMapPoint(X, Y);

            return point;
        }

        private ISnappingEnvironment GetSnappingEnvironment()
        {
            if (m_SnappingEnvironment == null)
            {
                IExtensionManager extensionManager = ArcMap.Application as IExtensionManager;

                if (extensionManager != null)
                {
                    UID guid = new UIDClass();
                    guid.Value = "{E07B4C52-C894-4558-B8D4-D4050018D1DA}";

                    IExtension extension = extensionManager.FindExtension(guid);
                    m_SnappingEnvironment = extension as ISnappingEnvironment;
                }
            }
            return m_SnappingEnvironment;
        }
        #endregion
    }
}