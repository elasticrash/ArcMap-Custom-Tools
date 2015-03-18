//SELECT THE POLYGON THAT OVERLAPS ANOTHER POLYGON AND AND TRIM IT AS TO NOT OVERLAP EVERY OTHER POLYGON IN THE CURRENT LAYER

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ElphoGenericTools.Helpers;
using ESRI.ArcGIS.ADF;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Editor;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ElphoGenericTools
{
    /// <summary>
    /// Summary description for dockable window toggle command
    /// </summary>
    [Guid("07dff35d-2e18-44a9-b8e3-bdfb00e35b1a")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("Elpho.NotOverLappingPolygon")]
    public sealed class ElphoNotOverLappingPolygonCommand : BaseCommand
    {
        private IApplication _mApplication;
        public static IHookHelper MHookHelper;
        private List<String> _editableLayers;
        private List<String> _editableAliasLayers;
        public IEditor MEditor;
        private IEditSketch _mEditSketch;
        private IEditLayers _mEditLayer;
        private IEditEvents_Event _mEditEvents;


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
            GxCommands.Register(regKey);
            SxCommands.Register(regKey);
            GMxCommands.Register(regKey);

        }
        /// <summary>
        /// Required method for ArcGIS Component Category unregistration -
        /// Do not modify the contents of this method with the code editor.
        /// </summary>
        private static void ArcGISCategoryUnregistration(Type registerType)
        {
            string regKey = string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", registerType.GUID);
            MxCommands.Unregister(regKey);
            GxCommands.Unregister(regKey);
            SxCommands.Unregister(regKey);
            GMxCommands.Unregister(regKey);
        }

        #endregion
        #endregion

        public ElphoNotOverLappingPolygonCommand()
        {
            MHookHelper = new HookHelperClass();
            base.m_category = "Elpho Tools";
            base.m_caption = "Modify Polygon So as Not to overlap other features";
            base.m_message = "Modify Polygon So as Not to overlap other features";
            base.m_toolTip = "Modify Polygon So as Not to overlap other features";
            base.m_name = "ElphoGenericMenu";

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
        /// Occurs when this command is created
        /// </summary>
        /// <param name="hook">Instance of the application</param>
        public override void OnCreate(object hook)
        {
            if (hook != null)
                _mApplication = hook as IApplication;

            MHookHelper.Hook = hook;
            MHookHelper.Hook = hook;
            UID editorUid = new UID();
            editorUid.Value = "esriEditor.Editor";

            MEditor = _mApplication.FindExtensionByCLSID(editorUid) as IEditor;
        }

        /// <summary>
        /// Toggle visibility of dockable window and show the visible state by its checked property
        /// </summary>
        public override void OnClick()
        {
            var map = MHookHelper.FocusMap;
            IGeometry returnGeometry = null;
            IGeometry returnGeometryB = null;

            List<IFeature> iflist = GisHelper.GetSelectedFeatures(esriGeometryType.esriGeometryPolygon, map);

            if (iflist != null && iflist.Count == 1)
            {
                var editLayers = MEditor as IEditLayers;
                var layer = editLayers.CurrentLayer;
                if (layer == null)
                {
                    MessageBox.Show("Δεν είσαστε σε editing mode");
                    return;
                }

                var pFeatureClass = layer.FeatureClass;

                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = iflist[0].Shape;
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
                        if (highwayFeature.OID != iflist[0].OID)
                        {
                            spatialfeatures.Add(highwayFeature);
                        }
                    }
                }

                ITopologicalOperator mPolygon = spatialfeatures[0].Shape as ITopologicalOperator;

                if (spatialfeatures.Count > 1)
                {
                    for (int index = 0; index < spatialfeatures.Count - 1; index++)
                    {
                        if (index != 0)
                        {
                            mPolygon = returnGeometry as ITopologicalOperator;
                        }
                        returnGeometry = mPolygon.Union(spatialfeatures[index + 1].Shape);
                    }
                }
                else
                {
                    returnGeometry = spatialfeatures[0].Shape;
                }

                ITopologicalOperator dmPolygon = iflist[0].Shape as ITopologicalOperator;
                returnGeometryB = dmPolygon.Difference(returnGeometry);

                MEditor.StartOperation();
                iflist[0].Shape = returnGeometryB;
                iflist[0].Store();
                MEditor.StopOperation("Trim Polygon");

                IActiveView activeView = map as IActiveView;
                activeView.Refresh();
            }
        }

        #endregion

    }
}
