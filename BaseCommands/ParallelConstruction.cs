// CREATE 2 PARALLEL LINES FROM THE SELECTED LINE FEATURE AT SPECIFIED DISTANCES

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ElphoGenericTools.Helpers;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Editor;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

namespace ElphoGenericTools
{
    /// <summary>
    /// Summary description for dockable window toggle command
    /// </summary>
    [Guid("53b8e26d-08a3-4609-b8d2-0580db8b893e")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("Elpho.ParallelConstruction")]
    public sealed class ElphoParallelConstructionCommand : BaseCommand
    {
        private IApplication _mApplication;
        public static IHookHelper MHookHelper;
        private List<String> _editableLayers;
        private List<String> _editableAliasLayers;
        public IEditor MEditor;
        private IEditSketch _mEditSketch;
        private IEditLayers _mEditLayer;
        private IEditEvents_Event _mEngineEditEvents;


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

        public ElphoParallelConstructionCommand()
        {
            MHookHelper = new HookHelperClass();
            base.m_category = "Elpho Tools";
            base.m_caption = "Create Parallel lines";
            base.m_message = "Create Parallel lines";
            base.m_toolTip = "Create Parallel lines";
            base.m_name = "ElphoGenericMenu";

            try
            {
                string bitmapResourceName = GetType().Name + ".png";
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
            List<IFeature> iflist = GisHelper.GetSelectedFeatures(esriGeometryType.esriGeometryPolyline, map);

            float val = (float)0.0;
            using (var form = new FrmLength())
            {
                form.StartPosition = FormStartPosition.CenterScreen;
                var result = form.ShowDialog();
                if (result == DialogResult.OK)
                {
                    val = Convert.ToSingle(form.ReturnValue);
                }
            }

            foreach (var ft in iflist)
            {
                //Interface to do a "copy parallel"
                IConstructCurve3 constructA = new PolylineClass();
                IConstructCurve3 constructB = new PolylineClass();

                //Rounded, Mitered, etc
                object offset = esriConstructOffsetEnum.esriConstructOffsetSimple;

                IPolyline source = ft.Shape as IPolyline;

                //Method call (0.001 or -0.001 determines left/right)
                constructA.ConstructOffset(source, val, ref offset);
                constructB.ConstructOffset(source, -val, ref offset);

                var editLayers = MEditor as IEditLayers;
                var layer = editLayers.CurrentLayer;
                if (layer == null)
                {
                    MessageBox.Show("You are not Currently in Editing mode");
                    return;
                }

                var pFeatureClass = layer.FeatureClass;
                //Storing output shape
                IFeature newFeatureA = pFeatureClass.CreateFeature();
                newFeatureA.Shape = (IGeometry)constructA;
                newFeatureA.Store();

                IFeature newFeatureB = pFeatureClass.CreateFeature();
                newFeatureB.Shape = (IGeometry)constructB;
                newFeatureB.Store();
            }
            IActiveView activeView = map as IActiveView;
            activeView.Refresh();
        }

        #endregion

    }
}
