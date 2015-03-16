// Copyright 2014 ESRI
// 
// All rights reserved under the copyright laws of the United States
// and applicable international laws, treaties, and conventions.
// 
// You may freely redistribute and use this sample code, with or
// without modification, provided you include the original copyright
// notice and use restrictions.
// 
// See the use restrictions at <your ArcGIS install location>/DeveloperKit10.2/userestrictions.txt.
//
//THIS IS A TOOL THAT CREATES A BUFFERS FROM A GRAPHIC LINE WITH DIFFERENT LENGTH RIGHT/LEFT AND THEN MERGES THEM INTO A SINGLE POLYGON

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
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Display;
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
    [Guid("bc5d89d7-1404-4a90-beba-e8a67a5b0ff3")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("Elpho.CreateZone")]
    public sealed class ElphoCreateZoneCommand : BaseTool
    {
        private IApplication _mApplication;
        public static IHookHelper MHookHelper;
        private List<String> _editableLayers;
        private List<String> _editableAliasLayers;
        public IEditor MEditor;
        private IEditSketch _mEditSketch;
        private IGeometry _geometry;

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

        public ElphoCreateZoneCommand()
        {
            MHookHelper = new HookHelperClass();
            base.m_category = "Elpho Tools";
            base.m_caption = "Create Zone";
            base.m_message = "Create Zone";
            base.m_toolTip = "Create Zone";
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
            UID editorUid = new UID();
            editorUid.Value = "esriEditor.Editor";

            MEditor = _mApplication.FindExtensionByCLSID(editorUid) as IEditor;
        }

        /// <summary>
        /// Toggle visibility of dockable window and show the visible state by its checked property
        /// </summary>
        public override void OnClick()
        {
        }

        private void OnSketchFinished(IGeometry element)
        {
            var map = MHookHelper.FocusMap;
            _mApplication.CurrentTool = null;
            
            var editLayers = MEditor as IEditLayers;
            var layer = editLayers.CurrentLayer;
            if (layer != null)
            {
                var pFeatureClass = layer.FeatureClass;

                IPolygon ipolLeft = new PolygonClass();
                IPolygon ipolRight = new PolygonClass();

                List<IGeometry> leftIgeo = new List<IGeometry>();
                List<IGeometry> rightIgeo = new List<IGeometry>();

                float val = (float) 0.0;
                using (var form = new FrmLength("Left Side"))
                {
                    form.StartPosition = FormStartPosition.CenterScreen;
                    var result = form.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        val = Convert.ToSingle(form.ReturnValue);
                    }
                }

                IBufferConstructionProperties bcpL = new BufferConstructionClass();
                bcpL.EndOption = esriBufferConstructionEndEnum.esriBufferFlat;
                bcpL.SideOption = esriBufferConstructionSideEnum.esriBufferLeft;
                bcpL.GenerateCurves = false;
                IBufferConstruction bcL = bcpL as BufferConstruction;

                IGeometryCollection originalGeometryBagA;
                IEnumGeometry originalGeometryEnumA;
                IGeometryCollection outBufferedGeometryColA;
                IGeometry resultGeoA = null;
                originalGeometryBagA = new GeometryBagClass();
                originalGeometryBagA.AddGeometry(element, Type.Missing, Type.Missing);

                originalGeometryEnumA = originalGeometryBagA as IEnumGeometry;
                outBufferedGeometryColA = new GeometryBagClass();
                bcL.ConstructBuffers(originalGeometryEnumA, val, outBufferedGeometryColA);
                IEnumGeometry resultingGeometriesA;

                resultingGeometriesA = outBufferedGeometryColA as IEnumGeometry;
                if (resultingGeometriesA != null)
                {
                    resultGeoA = resultingGeometriesA.Next();
                    ipolLeft = (IPolygon) resultGeoA;
                    leftIgeo.Add(ipolLeft);
                }

                using (var form = new FrmLength("Right Side"))
                {
                    form.StartPosition = FormStartPosition.CenterScreen;
                    var result = form.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        val = Convert.ToSingle(form.ReturnValue);
                    }
                }

                IBufferConstructionProperties bcpR = new BufferConstructionClass();
                bcpR.EndOption = esriBufferConstructionEndEnum.esriBufferFlat;
                bcpR.SideOption = esriBufferConstructionSideEnum.esriBufferRight;
                bcpR.GenerateCurves = false;
                IBufferConstruction bcR = bcpR as BufferConstruction;

                IGeometryCollection originalGeometryBagB;
                IEnumGeometry originalGeometryEnumB;
                IGeometryCollection outBufferedGeometryColB;
                IGeometry resultGeoB = null;
                originalGeometryBagB = new GeometryBagClass();
                originalGeometryBagB.AddGeometry(element, Type.Missing, Type.Missing);

                originalGeometryEnumB = originalGeometryBagB as IEnumGeometry;
                outBufferedGeometryColB = new GeometryBagClass();
                bcR.ConstructBuffers(originalGeometryEnumB, val, outBufferedGeometryColB);
                IEnumGeometry resultingGeometriesB;

                resultingGeometriesB = outBufferedGeometryColB as IEnumGeometry;
                if (resultingGeometriesB != null)
                {
                    resultGeoB = resultingGeometriesB.Next();
                    ipolRight = (IPolygon) resultGeoB;
                    rightIgeo.Add(ipolRight);
                }

                for (int index = 0; index < leftIgeo.Count; index++)
                {
                    var gmL = leftIgeo[index];
                    var gmR = rightIgeo[index];
                    ITopologicalOperator mPolygon = gmL as ITopologicalOperator;

                    var result = mPolygon.Union(gmR);

                    editLayers = MEditor as IEditLayers;
                    layer = editLayers.CurrentLayer;
                    if (layer == null)
                    {
                        MessageBox.Show("You are not Currently in Edit Mode");
                        return;
                    }

                    pFeatureClass = layer.FeatureClass;

                    MEditor.StartOperation();
                    var pFeature1 = pFeatureClass.CreateFeature();
                    pFeature1.Shape = (IGeometry) result;
                    pFeature1.Store();
                    MEditor.StopOperation("Create Zone");
                }

                IActiveView activeView = GetActiveViewFromArcMap(_mApplication);
                activeView.Refresh();
            }

        }

        public IActiveView GetActiveViewFromArcMap(IApplication application)
        {
            if (application == null)
            {
                return null;
            }
            IMxDocument mxDocument = application.Document as IMxDocument; // Dynamic Cast
            IActiveView activeView = mxDocument.ActiveView;

            return activeView;
        }

        public override void OnMouseDown(int Button, int Shift, int X, int Y)
        {
            //Get the active view from the application object (ie. hook)
            IActiveView activeView = GetActiveViewFromArcMap(_mApplication);

            //Get the polyline object from the users mouse clicks
            IPolyline polyline = GetPolylineFromMouseClicks(activeView);

            //Make a color to draw the polyline 
            IRgbColor rgbColor = new RgbColorClass();
            rgbColor.Red = 255;

            //Add the users drawn graphics as persistent on the map
            AddGraphicToMap(activeView.FocusMap, polyline, rgbColor, rgbColor);

            //Only redraw the portion of the active view that contains the graphics 
            activeView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
        }

        public IPolyline GetPolylineFromMouseClicks(IActiveView activeView)
        {

            IScreenDisplay screenDisplay = activeView.ScreenDisplay;

            IRubberBand rubberBand = new RubberLineClass();
            IGeometry geometry = rubberBand.TrackNew(screenDisplay, null);

            IPolyline polyline = (IPolyline)geometry;

            return polyline;

        }

        public void AddGraphicToMap(IMap map, IGeometry geometry, IRgbColor rgbColor, IRgbColor outlineRgbColor)
        {
            IGraphicsContainer graphicsContainer = (IGraphicsContainer)map; // Explicit Cast
            IElement element = null;

            if (geometry != null)
            {
                if ((geometry.GeometryType) == esriGeometryType.esriGeometryPoint)
                {
                    // Marker symbols
                    ISimpleMarkerSymbol simpleMarkerSymbol = new SimpleMarkerSymbolClass();
                    simpleMarkerSymbol.Color = rgbColor;
                    simpleMarkerSymbol.Outline = true;
                    simpleMarkerSymbol.OutlineColor = outlineRgbColor;
                    simpleMarkerSymbol.Size = 15;
                    simpleMarkerSymbol.Style = esriSimpleMarkerStyle.esriSMSCircle;

                    IMarkerElement markerElement = new MarkerElementClass();
                    markerElement.Symbol = simpleMarkerSymbol;
                    element = (IElement) markerElement; // Explicit Cast
                }
                else if ((geometry.GeometryType) == esriGeometryType.esriGeometryPolyline)
                {
                    //  Line elements
                    ISimpleLineSymbol simpleLineSymbol = new SimpleLineSymbolClass();
                    simpleLineSymbol.Color = rgbColor;
                    simpleLineSymbol.Style = esriSimpleLineStyle.esriSLSSolid;
                    simpleLineSymbol.Width = 5;

                    ILineElement lineElement = new LineElementClass();
                    lineElement.Symbol = simpleLineSymbol;
                    element = (IElement) lineElement; // Explicit Cast
                }
                else if ((geometry.GeometryType) == esriGeometryType.esriGeometryPolygon)
                {
                    // Polygon elements
                    ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbolClass();
                    simpleFillSymbol.Color = rgbColor;
                    simpleFillSymbol.Style = esriSimpleFillStyle.esriSFSForwardDiagonal;
                    IFillShapeElement fillShapeElement = new PolygonElementClass();
                    fillShapeElement.Symbol = simpleFillSymbol;
                    element = (IElement) fillShapeElement; // Explicit Cast
                }
                if (!(element == null))
                {
                    element.Geometry = geometry;
                    graphicsContainer.AddElement(element, 0);
                    OnSketchFinished(geometry);
                }
            }
        }

        #endregion

    }
}
